const fs = require("node:fs/promises");
const path = require("node:path");

const audioUrl = "https://bgmer.net/wp-content/uploads/2023/06/373_BPM125.mp3";
const outputPath = path.resolve(
  __dirname,
  "..",
  "Assets",
  "Project",
  "BGM",
  "373_BPM125.mp3",
);
const temporaryPath = `${outputPath}.download`;

async function fetchBgm() {
  try {
    await fs.access(outputPath);
    throw new Error(
      `${path.relative(process.cwd(), outputPath)} already exists. Remove it before downloading again.`,
    );
  } catch (error) {
    if (error.code !== "ENOENT") {
      throw error;
    }
  }

  const response = await fetch(audioUrl);
  if (!response.ok) {
    throw new Error(`Failed to fetch BGM: ${response.status} ${response.statusText}`);
  }

  const audio = Buffer.from(await response.arrayBuffer());
  if (audio.length === 0) {
    throw new Error(`The response from ${audioUrl} is empty.`);
  }

  const hasId3Tag = audio.subarray(0, 3).toString("ascii") === "ID3";
  const hasMpegFrameSync = audio[0] === 0xff && (audio[1] & 0xe0) === 0xe0;
  if (!hasId3Tag && !hasMpegFrameSync) {
    const contentType = response.headers.get("content-type") ?? "unknown content type";
    throw new Error(
      `The response from ${audioUrl} does not look like an MP3 file (${contentType}).`,
    );
  }

  await fs.mkdir(path.dirname(outputPath), { recursive: true });
  try {
    await fs.writeFile(temporaryPath, audio, { flag: "wx" });
    await fs.rename(temporaryPath, outputPath);
  } catch (error) {
    await fs.rm(temporaryPath, { force: true });
    throw error;
  }

  console.log(`Fetched BGM to ${path.relative(process.cwd(), outputPath)}`);
}

fetchBgm().catch((error) => {
  console.error("Failed to fetch BGM:", error);
  process.exitCode = 1;
});
