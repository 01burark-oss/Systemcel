import { spawnSync } from "node:child_process";
import { fileURLToPath } from "node:url";
import path from "node:path";

const webRoot = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "..");
const playwrightCli = path.join(webRoot, "node_modules", "@playwright", "test", "cli.js");
const result = spawnSync(
  process.execPath,
  [playwrightCli, "test", "e2e/capture-all-screens.spec.ts", "--project=desktop-wide", "--reporter=line"],
  {
    cwd: webRoot,
    env: { ...process.env, SYSTEMCEL_CAPTURE: "1" },
    stdio: "inherit"
  }
);

process.exit(result.status ?? 1);
