import { spawn } from "node:child_process";
import { constants as fsConstants, existsSync } from "node:fs";
import { access, copyFile, mkdir, mkdtemp, readFile, rm, writeFile } from "node:fs/promises";
import os from "node:os";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { StdioServerTransport } from "@modelcontextprotocol/sdk/server/stdio.js";
import { z } from "zod";

const projectRoot = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "..");
const outputDirectory = path.resolve(
  projectRoot,
  process.env.BLENDER_MCP_OUTPUT_DIR ?? path.join("Assets", "Generated", "Blender"),
);
const maxObjects = 200;
const maxLogLength = 12_000;
const blenderScript = `
import bpy
import json
import math
import sys

def main():
    args = sys.argv[sys.argv.index("--") + 1:]
    spec_path, blend_path, fbx_path, result_path = args
    with open(spec_path, "r", encoding="utf-8") as spec_file:
        spec = json.load(spec_file)

    bpy.ops.wm.read_factory_settings(use_empty=True)
    created_objects = []
    for item in spec["objects"]:
        shape = item["shape"]
        location = item["location"]
        if shape == "cube":
            bpy.ops.mesh.primitive_cube_add(size=1, location=location)
        elif shape == "sphere":
            bpy.ops.mesh.primitive_uv_sphere_add(segments=32, ring_count=16, radius=0.5, location=location)
        elif shape == "ico_sphere":
            bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=2, radius=0.5, location=location)
        elif shape == "cylinder":
            bpy.ops.mesh.primitive_cylinder_add(vertices=32, radius=0.5, depth=1, location=location)
        elif shape == "cone":
            bpy.ops.mesh.primitive_cone_add(vertices=32, radius1=0.5, radius2=0, depth=1, location=location)
        elif shape == "torus":
            bpy.ops.mesh.primitive_torus_add(major_segments=48, minor_segments=12, major_radius=0.5, minor_radius=0.125, location=location)
        elif shape == "plane":
            bpy.ops.mesh.primitive_plane_add(size=1, location=location)
        else:
            raise ValueError("Unsupported shape: " + shape)

        obj = bpy.context.object
        obj.name = item["name"]
        obj.scale = item["scale"]
        obj.rotation_euler = tuple(math.radians(value) for value in item["rotationDegrees"])

        material_spec = item.get("material")
        if material_spec is not None:
            material = bpy.data.materials.new(name=item["name"] + "_Material")
            material.diffuse_color = (*material_spec["color"], 1.0)
            material.use_nodes = True
            shader = material.node_tree.nodes.get("Principled BSDF")
            shader.inputs["Base Color"].default_value = (*material_spec["color"], 1.0)
            shader.inputs["Metallic"].default_value = material_spec["metallic"]
            shader.inputs["Roughness"].default_value = material_spec["roughness"]
            obj.data.materials.append(material)
        created_objects.append(obj)

    bpy.ops.object.select_all(action="DESELECT")
    for obj in created_objects:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = created_objects[0]

    if "FINISHED" not in bpy.ops.wm.save_as_mainfile(filepath=blend_path):
        raise RuntimeError("Blender did not finish saving the .blend file")
    if "FINISHED" not in bpy.ops.export_scene.fbx(
        filepath=fbx_path,
        use_selection=True,
        object_types={"MESH"},
        apply_scale_options="FBX_SCALE_ALL",
        axis_forward="-Z",
        axis_up="Y",
        bake_anim=False,
        path_mode="AUTO",
    ):
        raise RuntimeError("Blender did not finish exporting the FBX file")

    with open(result_path, "w", encoding="utf-8") as result_file:
        json.dump({"blenderVersion": bpy.app.version_string}, result_file)

main()
`;

const vector = z.tuple([
  z.number().finite().min(-10000).max(10000),
  z.number().finite().min(-10000).max(10000),
  z.number().finite().min(-10000).max(10000),
]);

const objectSchema = z.object({
  name: z.string().regex(/^[A-Za-z0-9][A-Za-z0-9 _-]{0,63}$/),
  shape: z.enum(["cube", "sphere", "ico_sphere", "cylinder", "cone", "torus", "plane"]),
  location: vector.default([0, 0, 0]),
  rotationDegrees: vector.default([0, 0, 0]),
  scale: z.tuple([
    z.number().finite().gt(0).max(10000),
    z.number().finite().gt(0).max(10000),
    z.number().finite().gt(0).max(10000),
  ]).default([1, 1, 1]),
  material: z.object({
    color: z.tuple([
      z.number().finite().min(0).max(1),
      z.number().finite().min(0).max(1),
      z.number().finite().min(0).max(1),
    ]),
    metallic: z.number().finite().min(0).max(1).default(0),
    roughness: z.number().finite().min(0).max(1).default(0.5),
  }).optional(),
});

function findBlenderExecutable() {
  if (process.env.BLENDER_EXECUTABLE) {
    return process.env.BLENDER_EXECUTABLE;
  }

  if (process.platform === "win32") {
    const programFiles = process.env.ProgramFiles ?? "C:\\Program Files";
    const candidates = [
      path.join(programFiles, "Blender Foundation", "Blender 5.2", "blender.exe"),
      path.join(programFiles, "Blender Foundation", "Blender", "blender.exe"),
    ];
    const found = candidates.find((candidate) => existsSync(candidate));
    if (found) {
      return found;
    }
  }

  return "blender";
}

function runBlender(executable, args) {
  return new Promise((resolve, reject) => {
    const child = spawn(executable, args, { windowsHide: true, stdio: ["ignore", "pipe", "pipe"] });
    let stdout = "";
    let stderr = "";
    let timedOut = false;
    const timeout = setTimeout(() => {
      timedOut = true;
      child.kill();
    }, 180_000);

    const appendLog = (current, chunk) => (current + chunk.toString()).slice(-maxLogLength);
    child.stdout.on("data", (chunk) => {
      stdout = appendLog(stdout, chunk);
    });
    child.stderr.on("data", (chunk) => {
      stderr = appendLog(stderr, chunk);
    });
    child.on("error", (error) => {
      clearTimeout(timeout);
      reject(new Error(`Unable to start Blender (${executable}): ${error.message}`));
    });
    child.on("close", (code, signal) => {
      clearTimeout(timeout);
      if (timedOut) {
        reject(new Error(`Blender exceeded the 180-second time limit. ${stderr.trim()}`));
      } else if (code !== 0) {
        reject(new Error(`Blender exited with code ${code ?? signal}. ${stderr.trim() || stdout.trim()}`));
      } else {
        resolve({ stdout, stderr });
      }
    });
  });
}

async function createFbx({ outputName, objects }) {
  const names = objects.map(({ name }) => name.toLowerCase());
  if (new Set(names).size !== names.length) {
    throw new Error("Object names must be unique (case-insensitive).");
  }

  await mkdir(outputDirectory, { recursive: true });
  const blendPath = path.join(outputDirectory, `${outputName}.blend`);
  const fbxPath = path.join(outputDirectory, `${outputName}.fbx`);
  for (const finalPath of [blendPath, fbxPath]) {
    try {
      await access(finalPath);
      throw new Error(`Output already exists; choose a different outputName: ${finalPath}`);
    } catch (error) {
      if (error.code !== "ENOENT") {
        throw error;
      }
    }
  }

  const jobDirectory = await mkdtemp(path.join(os.tmpdir(), "blender-mcp-"));
  const specPath = path.join(jobDirectory, "scene.json");
  const scriptPath = path.join(jobDirectory, "create_scene.py");
  const stagedBlendPath = path.join(jobDirectory, `${outputName}.blend`);
  const stagedFbxPath = path.join(jobDirectory, `${outputName}.fbx`);
  const resultPath = path.join(jobDirectory, "result.json");
  const blenderExecutable = findBlenderExecutable();
  const createdFinalFiles = [];

  try {
    await Promise.all([
      writeFile(specPath, JSON.stringify({ objects }), "utf8"),
      writeFile(scriptPath, blenderScript, "utf8"),
    ]);

    const { stdout, stderr } = await runBlender(blenderExecutable, [
      "--background",
      "--factory-startup",
      "--python",
      scriptPath,
      "--",
      specPath,
      stagedBlendPath,
      stagedFbxPath,
      resultPath,
    ]);

    let blenderResult;
    try {
      blenderResult = JSON.parse(await readFile(resultPath, "utf8"));
    } catch {
      throw new Error(`Blender did not report a successful export. ${stderr.trim() || stdout.trim()}`);
    }
    for (const stagedPath of [stagedBlendPath, stagedFbxPath]) {
      await access(stagedPath);
    }

    try {
      await copyFile(stagedBlendPath, blendPath, fsConstants.COPYFILE_EXCL);
      createdFinalFiles.push(blendPath);
      await copyFile(stagedFbxPath, fbxPath, fsConstants.COPYFILE_EXCL);
      createdFinalFiles.push(fbxPath);
    } catch (error) {
      await Promise.all(createdFinalFiles.map((filePath) => rm(filePath, { force: true })));
      throw error;
    }

    return {
      blendPath,
      fbxPath,
      blenderVersion: blenderResult.blenderVersion,
      objectCount: objects.length,
    };
  } finally {
    await rm(jobDirectory, { recursive: true, force: true });
  }
}

const server = new McpServer({
  name: "blender-fbx",
  version: "1.0.0",
});

server.registerTool(
  "create_fbx",
  {
    title: "Create a Blender scene and FBX",
    description:
      "Create a Blender scene from primitive meshes and export both .blend and .fbx files. Locations and scales use Blender units; rotations are degrees; material colors use RGB values from 0 to 1.",
    inputSchema: {
      outputName: z.string().regex(/^[A-Za-z0-9][A-Za-z0-9_-]{0,63}$/),
      objects: z.array(objectSchema).min(1).max(maxObjects),
    },
    annotations: {
      readOnlyHint: false,
      destructiveHint: false,
      idempotentHint: false,
      openWorldHint: false,
    },
  },
  async ({ outputName, objects }) => {
    try {
      const result = await createFbx({ outputName, objects });
      return {
        content: [{
          type: "text",
          text: [
            `Created ${result.objectCount} object(s) with Blender ${result.blenderVersion}.`,
            `FBX: ${result.fbxPath}`,
            `Blender file: ${result.blendPath}`,
          ].join("\n"),
        }],
      };
    } catch (error) {
      const message = error instanceof Error ? error.message : String(error);
      console.error(`[blender-mcp] ${message}`);
      return { isError: true, content: [{ type: "text", text: message }] };
    }
  },
);

const transport = new StdioServerTransport();
server.server.onerror = (error) => console.error("[blender-mcp] MCP error:", error);
await server.connect(transport);
