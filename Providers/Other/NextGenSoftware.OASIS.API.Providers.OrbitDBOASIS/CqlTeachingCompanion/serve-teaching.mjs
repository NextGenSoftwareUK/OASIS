import { timingSafeEqual } from "node:crypto";
import { createServer } from "node:http";
import { openTeachingStore } from "./teaching-store.mjs";

const [directory, address, portText] = process.argv.slice(2);
const port = Number(portText);
const reader = process.env.OASIS_ORBITDB_CQL_READER_TOKEN;
const instance = process.env.OASIS_CQL_TEACHING_INSTANCE;
if (!directory || !address || !/^\d{4,5}$/.test(portText) || port < 1024 || port > 65535
  || !reader || reader.length < 32 || reader.length > 256 || /[^A-Za-z0-9_-]/.test(reader)
  || (instance !== undefined && (instance.length !== 32 || /[^0-9a-f]/.test(instance))))
  throw new Error("A reviewed OrbitDB source, loopback port and protected reader are required.");

const expected = Buffer.from(`Bearer ${reader}`);
const { db, close } = await openTeachingStore(directory, address);
const canonicalId = /^[0-9a-f]{8}-(?:[0-9a-f]{4}-){3}[0-9a-f]{12}$/;
const emptyId = "00000000-0000-0000-0000-000000000000";
const server = createServer(async (request, response) => {
  const reply = (status, body) => {
    response.writeHead(status, { "Content-Type": "application/json", "Cache-Control": "no-store",
      "X-OASIS-Teaching-Provider": "OrbitDBOASIS",
      ...(instance ? { "X-OASIS-Teaching-Instance": instance } : {}) });
    response.end(body);
  };
  try {
    const url = new URL(request.url, "http://127.0.0.1");
    if (request.method !== "GET" || url.pathname !== "/oasis/cql/holon") return reply(404);
    const header = Buffer.from(request.headers.authorization ?? "");
    if (header.length !== expected.length || !timingSafeEqual(header, expected)) return reply(401);
    const id = url.searchParams.get("id");
    if (!id || url.searchParams.size !== 1 || id.length !== 36 || !canonicalId.test(id) || id === emptyId)
      return reply(400);
    const row = await db.get(id);
    if (row === undefined) return reply(200, "null");
    if (row.id !== id || typeof row.isDeleted !== "boolean") return reply(500);
    if (row.isDeleted) return reply(200, "null");
    if (row.isPublic !== true) return reply(403);
    const projection = Object.fromEntries(Object.entries(row).filter(([field]) => field.toLowerCase() !== "children"));
    return reply(200, JSON.stringify(projection));
  } catch {
    return reply(500);
  }
});
server.on("error", async () => {
  process.exitCode = 1;
  await close();
  process.disconnect?.();
});
function shutdown() {
  server.close(async () => {
    await close();
    process.disconnect?.();
  });
}
for (const signal of ["SIGINT", "SIGTERM"]) {
  process.once(signal, shutdown);
}
if (process.send) process.once("message", message => { if (message === "shutdown") shutdown(); });
server.listen(port, "127.0.0.1", () => process.send?.("ready"));
