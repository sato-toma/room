const fs = require("node:fs");
const path = require("node:path");

const typeDefinitionUrl = "https://docs.cluster.mu/script/index.d.ts";
const outputPath = path.resolve(
  __dirname,
  "..",
  "node_modules",
  "@types",
  "cluster-script",
  "index.d.ts",
);

async function fetchTypeDefinitions() {
  const response = await fetch(typeDefinitionUrl);
  if (!response.ok) {
    throw new Error(
      `Failed to fetch ClusterScript API types: ${response.status} ${response.statusText}`,
    );
  }

  const typeDefinitions = await response.text();
  if (
    !typeDefinitions.includes("ClusterScript") ||
    !typeDefinitions.includes("declare class Vector3")
  ) {
    throw new Error(
      `The response from ${typeDefinitionUrl} does not look like the ClusterScript API type definitions.`,
    );
  }

  fs.mkdirSync(path.dirname(outputPath), { recursive: true });
  fs.writeFileSync(outputPath, typeDefinitions, "utf8");
  console.log(`Fetched ClusterScript API types to ${path.relative(process.cwd(), outputPath)}`);
}

fetchTypeDefinitions().catch((error) => {
  console.error("Failed to fetch ClusterScript API types:", error);
  process.exitCode = 1;
});
