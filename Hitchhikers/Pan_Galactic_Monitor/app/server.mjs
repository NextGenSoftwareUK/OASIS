import express from "express";
import { mkdir, writeFile } from "node:fs/promises";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

import { computeDelta } from "./lib/delta.mjs";
import { runSweep } from "./lib/sweep.mjs";

const __dirname = dirname(fileURLToPath(import.meta.url));
const PORT = Number(process.env.PORT || 3142);
const REFRESH_SEC = Math.max(30, Number(process.env.REFRESH_INTERVAL_SEC || 90));

let lastPayload = null;
let previousPayload = null;
let sweepRunning = false;

const sseClients = new Set();

function broadcastSse(event, data) {
  const line = `event: ${event}\ndata: ${JSON.stringify(data)}\n\n`;
  for (const write of sseClients) {
    try {
      write(line);
    } catch {
      sseClients.delete(write);
    }
  }
}

async function persistRun(payload) {
  const dir = join(__dirname, "runs");
  await mkdir(dir, { recursive: true });
  await writeFile(join(dir, "latest.json"), JSON.stringify(payload, null, 2), "utf8");
}

async function doSweep() {
  if (sweepRunning) return;
  sweepRunning = true;
  try {
    previousPayload = lastPayload;
    const raw = await runSweep();
    const delta = computeDelta(previousPayload, raw);
    lastPayload = { ...raw, delta };
    await persistRun(lastPayload);
    broadcastSse("sweep", {
      meta: lastPayload.meta,
      deltaSummary: lastPayload.delta.summary,
    });
  } catch (e) {
    console.error("[sweep]", e);
    broadcastSse("error", { message: String(e.message || e) });
  } finally {
    sweepRunning = false;
  }
}

const app = express();

// CORS — allow the Timebank (and any localhost origin) to call the API
app.use((req, res, next) => {
  const origin = req.headers.origin ?? "*";
  res.setHeader("Access-Control-Allow-Origin", origin);
  res.setHeader("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
  res.setHeader("Access-Control-Allow-Headers", "Content-Type");
  if (req.method === "OPTIONS") { res.sendStatus(204); return; }
  next();
});

app.use(express.static(join(__dirname, "public")));

app.get("/api/health", (_req, res) => {
  res.json({
    ok: true,
    service: "pan-galactic-monitor",
    port: PORT,
    refreshSec: REFRESH_SEC,
    lastSweep: lastPayload?.meta?.generatedAt ?? null,
    pins: lastPayload?.sites?.length ?? 0,
  });
});

app.get("/api/data", (_req, res) => {
  if (!lastPayload) return res.status(503).json({ error: "Sweep not ready" });
  res.json(lastPayload);
});

app.get("/api/site/:id", (req, res) => {
  if (!lastPayload?.sites) return res.status(503).json({ error: "No data" });
  const s = lastPayload.sites.find((x) => x.id === req.params.id);
  if (!s) return res.status(404).json({ error: "Site not found" });
  res.json(s);
});

app.post("/api/sweep", async (_req, res) => {
  await doSweep();
  res.json({ ok: true, meta: lastPayload?.meta });
});

app.get("/events", (req, res) => {
  res.setHeader("Content-Type", "text/event-stream");
  res.setHeader("Cache-Control", "no-cache");
  res.setHeader("Connection", "keep-alive");
  res.flushHeaders?.();
  const write = (c) => res.write(c);
  sseClients.add(write);
  write(`event: connected\ndata: ${JSON.stringify({ refreshSec: REFRESH_SEC })}\n\n`);
  req.on("close", () => sseClients.delete(write));
});

app.listen(PORT, async () => {
  console.log(`
  Pan Galactic Monitor
  http://localhost:${PORT}
  Registry: ../data/sites.json
  `);
  await doSweep();
  setInterval(doSweep, REFRESH_SEC * 1000);
});
