import { timingSafeEqual } from "node:crypto";
import { createServer } from "node:http";
import { openTeachingStore } from "./teaching-store.mjs";

const [directory, portText] = process.argv.slice(2);
const port = Number(portText);
const reader = process.env.OASIS_GUN_CQL_READER_TOKEN;
const instance = process.env.OASIS_CQL_TEACHING_INSTANCE;
if (!directory || !/^\d{4,5}$/.test(portText) || port < 1024 || port > 65535
  || !reader || reader.length < 32 || reader.length > 256 || /[^A-Za-z0-9_-]/.test(reader)
  || (instance !== undefined && (instance.length !== 32 || /[^0-9a-f]/.test(instance))))
  throw new Error("A reviewed GUN source, loopback port and protected reader are required.");

const expected = Buffer.from(`Bearer ${reader}`);
const store = await openTeachingStore(directory);
const canonicalId = /^[0-9a-f]{8}-(?:[0-9a-f]{4}-){3}[0-9a-f]{12}$/;
const emptyId = "00000000-0000-0000-0000-000000000000";
const server = createServer(async (request, response) => {
  const reply = (status, body) => {
    response.writeHead(status, { "Content-Type": "application/json", "Cache-Control": "no-store",
      "X-OASIS-Teaching-Provider": "GUNOASIS",
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
    const node = await store.read(id);
    if (node === undefined) return reply(200, "null");
    if (node?._?.["#"] !== `oasis.holons/${id}` || !["true", "false"].includes(node.is_deleted))
      return reply(500);
    if (node.is_deleted === "true") return reply(200, "null");
    if (typeof node.data_json !== "string" || Buffer.byteLength(node.data_json) > 1024 * 1024) return reply(500);
    const row = JSON.parse(node.data_json);
    if (row?.id !== id || row.isDeleted === true) return reply(500);
    if (row.isPublic !== true) return reply(403);
    const projection = Object.fromEntries(Object.entries(row).filter(([field]) => field.toLowerCase() !== "children"));
    return reply(200, JSON.stringify(projection));
  } catch {
    return reply(500);
  }
});
server.on("error", () => {
  process.exitCode = 1;
  process.disconnect?.();
});
function shutdown() {
  server.close(() => process.disconnect?.());
}
for (const signal of ["SIGINT", "SIGTERM"]) {
  process.once(signal, shutdown);
}
if (process.send) process.once("message", message => { if (message === "shutdown") shutdown(); });
server.listen(port, "127.0.0.1", () => process.send?.("ready"));
