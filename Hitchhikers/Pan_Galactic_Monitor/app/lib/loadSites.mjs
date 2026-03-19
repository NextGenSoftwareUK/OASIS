import { readFile } from "node:fs/promises";
import { existsSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

const __dirname = dirname(fileURLToPath(import.meta.url));
const APP_ROOT = join(__dirname, "..");
const DATA_DIR = join(APP_ROOT, "..", "data");

export async function loadSitesJson() {
  const envPath = process.env.SITES_JSON_PATH;
  const candidates = [];
  if (envPath) {
    candidates.push(envPath.startsWith("/") ? envPath : join(APP_ROOT, envPath));
    candidates.push(join(process.cwd(), envPath));
  }
  candidates.push(join(DATA_DIR, "sites.json"), join(DATA_DIR, "sites.example.json"));

  for (const p of candidates) {
    if (existsSync(p)) {
      const raw = JSON.parse(await readFile(p, "utf8"));
      const sites = Array.isArray(raw.sites) ? raw.sites : raw;
      if (!Array.isArray(sites)) throw new Error("sites.json must have { sites: [...] }");
      return { sites, sourcePath: p };
    }
  }
  throw new Error("No sites.json or sites.example.json in ../data/");
}
