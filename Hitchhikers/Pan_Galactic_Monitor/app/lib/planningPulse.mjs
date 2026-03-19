import { readdir, stat } from "node:fs/promises";
import { join } from "node:path";
import { existsSync } from "node:fs";

const MS_7D = 7 * 24 * 60 * 60 * 1000;

/**
 * Recent .md under Planning-Sprint (or env path). Cap scan for perf.
 */
export async function scanPlanningSprint(rootPath) {
  if (!rootPath || !existsSync(rootPath)) {
    return { files: [], rootPath: rootPath || null, scanned: false };
  }
  const out = [];
  const maxCollect = 400;

  async function walk(dir, depth) {
    if (out.length >= maxCollect || depth > 14) return;
    let entries;
    try {
      entries = await readdir(dir, { withFileTypes: true });
    } catch {
      return;
    }
    for (const e of entries) {
      if (e.name.startsWith(".") || e.name === "node_modules") continue;
      const p = join(dir, e.name);
      if (e.isDirectory()) await walk(p, depth + 1);
      else if (e.name.endsWith(".md")) {
        try {
          const st = await stat(p);
          out.push({
            path: p.slice(rootPath.length + 1).replace(/\\/g, "/"),
            mtime: st.mtimeMs,
          });
        } catch {
          /* skip */
        }
      }
    }
  }

  await walk(rootPath, 0);
  out.sort((a, b) => b.mtime - a.mtime);
  const now = Date.now();
  const count7d = out.filter((f) => now - f.mtime < MS_7D).length;
  const recent = out.slice(0, 100);

  return {
    files: out.slice(0, 60),
    count7d,
    totalTouched: recent.length,
    rootPath,
    scanned: true,
  };
}
