/**
 * Vercel serverless entry-point for Pan Galactic Monitor.
 * Wraps the existing Express lib — sweep runs once per cold-start,
 * then cached for CACHE_TTL_MS across warm invocations.
 */
import express from "express";
import { join, dirname } from "node:path";
import { fileURLToPath } from "node:url";
import { runSweep } from "../app/lib/sweep.mjs";

const __dirname = dirname(fileURLToPath(import.meta.url));

const app = express();

app.use((_req, res, next) => {
  res.setHeader("Access-Control-Allow-Origin", "*");
  res.setHeader("Access-Control-Allow-Methods", "GET, OPTIONS");
  res.setHeader("Access-Control-Allow-Headers", "Content-Type");
  if (_req.method === "OPTIONS") { res.sendStatus(204); return; }
  next();
});

// Serve the static frontend from app/public/
app.use(express.static(join(__dirname, "../app/public")));

// Cache the sweep result across warm Vercel invocations
let cachedPayload = null;
let cacheTime = 0;
const CACHE_TTL_MS = 60_000;

async function getPayload() {
  if (!cachedPayload || Date.now() - cacheTime > CACHE_TTL_MS) {
    cachedPayload = await runSweep();
    cacheTime = Date.now();
  }
  return cachedPayload;
}

app.get("/api/data", async (_req, res) => {
  try {
    const payload = await getPayload();
    res.json(payload);
  } catch (err) {
    console.error("[api/data]", err);
    res.status(500).json({ error: String(err.message ?? err) });
  }
});

app.get("/api/health", (_req, res) =>
  res.json({
    ok: true,
    service: "pan-galactic-monitor",
    cachedAt: cacheTime ? new Date(cacheTime).toISOString() : null,
    pins: cachedPayload?.sites?.length ?? 0,
  })
);

// SPA fallback
app.get("*", (_req, res) =>
  res.sendFile(join(__dirname, "../app/public/index.html"))
);

export default app;
