// OrbitDB storage service for OrbitDBOASIS. OrbitDB is a JavaScript library with no network API, so this process owns
// one Helia node and one indexed OrbitDB keyvalue database and exposes it to the .NET provider on loopback only.
//   node storage-service.mjs <data directory> <port>      (env OASIS_ORBITDB_STORAGE_TOKEN: bearer token, 32+ chars)
// Routes: GET /health, GET /kv?key=K, PUT /kv?key=K (body = value), DELETE /kv?key=K, GET /keys?prefix=P.
// A write is answered 204 only after OrbitDB has appended the entry to the oplog and updated the persistent index.
import { randomUUID, timingSafeEqual } from "node:crypto";
import { mkdir, readFile, writeFile } from "node:fs/promises";
import { createServer } from "node:http";
import { join } from "node:path";
import { createOrbitDB, isValidAddress, KeyValueIndexed } from "@orbitdb/core";
import { gossipsub } from "@libp2p/gossipsub";
import { identify } from "@libp2p/identify";
import { LevelBlockstore } from "blockstore-level";
import { createHelia } from "helia";
import { createLibp2p } from "libp2p";

const [directory, portText] = process.argv.slice(2);
const port = Number(portText);
const token = process.env.OASIS_ORBITDB_STORAGE_TOKEN;
if (!directory || !Number.isInteger(port) || port < 1024 || port > 65535 || !token || token.length < 32 || /[^A-Za-z0-9_-]/.test(token))
  throw new Error("usage: OASIS_ORBITDB_STORAGE_TOKEN=<32+ chars> node storage-service.mjs <data directory> <port>");
const expected = Buffer.from(`Bearer ${token}`);
const maxBody = 16 * 1024 * 1024;

const libp2p = await createLibp2p({ services: { pubsub: gossipsub({ allowPublishToZeroTopicPeers: true }), identify: identify() } });
const ipfs = await createHelia({ libp2p, blockstore: new LevelBlockstore(join(directory, "blocks")), routers: [] });
// The identity id and the database address must survive restarts: createOrbitDB otherwise picks a random identity, and
// the database manifest (its write access) is bound to that identity, so reopening by name would create a new, empty
// database. Both are recorded in the data directory on first start.
await mkdir(directory, { recursive: true });
const statePath = join(directory, "oasis-storage.json");
const state = await readFile(statePath, "utf8").then(JSON.parse, error => { if (error.code === "ENOENT") return null; throw error; });
if (state !== null && (typeof state.id !== "string" || !isValidAddress(state.address)))
  throw new Error(`${statePath} is not a valid OrbitDB storage state file.`);
const id = state?.id ?? randomUUID();
const orbitdb = await createOrbitDB({ ipfs, id, directory: join(directory, "orbitdb") });
const options = { Database: KeyValueIndexed(), sync: false };
const db = state === null
  ? await orbitdb.open("oasis.storage", { ...options, type: "keyvalue" })
  : await orbitdb.open(state.address, options);
if (db.type !== "keyvalue" || (state !== null && db.address.toString() !== state.address))
  throw new Error("The OrbitDB storage database differs from the recorded one.");
if (state === null)
  await writeFile(statePath, JSON.stringify({ id, address: db.address.toString() }), { flag: "wx" });

// OrbitDB keyvalue writes are applied in oplog order; serialising them keeps each acknowledgement tied to its own entry.
let queue = Promise.resolve();
const serial = operation => (queue = queue.then(operation, operation));

const readBody = request => new Promise((resolve, reject) => {
  const chunks = []; let size = 0;
  request.on("data", chunk => { size += chunk.length; if (size > maxBody) { reject(new Error("body too large")); request.destroy(); } else chunks.push(chunk); });
  request.on("end", () => resolve(Buffer.concat(chunks).toString("utf8")));
  request.on("error", reject);
});

const server = createServer(async (request, response) => {
  const reply = (status, body, type = "application/json") => {
    response.writeHead(status, { "Content-Type": type, "Cache-Control": "no-store" });
    response.end(body);
  };
  try {
    const header = Buffer.from(request.headers.authorization ?? "");
    if (header.length !== expected.length || !timingSafeEqual(header, expected)) return reply(401, '{"error":"unauthorized"}');
    const url = new URL(request.url, "http://127.0.0.1");
    if (url.pathname === "/health" && request.method === "GET")
      return reply(200, JSON.stringify({ address: db.address.toString() }));
    if (url.pathname === "/keys" && request.method === "GET") {
      const prefix = url.searchParams.get("prefix") ?? "";
      const keys = [];
      for await (const { key, value } of db.iterator()) if (key.startsWith(prefix) && typeof value === "string") keys.push(key);
      keys.sort();
      return reply(200, JSON.stringify(keys));
    }
    if (url.pathname !== "/kv") return reply(404, '{"error":"not found"}');
    const key = url.searchParams.get("key");
    if (!key || url.searchParams.size !== 1) return reply(400, '{"error":"exactly one key is required"}');
    switch (request.method) {
      case "GET": {
        const value = await db.get(key);
        if (value === undefined) return reply(404, '{"error":"missing"}');
        if (typeof value !== "string") return reply(500, '{"error":"non-string value"}');
        return reply(200, value, "text/plain; charset=utf-8");
      }
      case "PUT": {
        const value = await readBody(request);
        const hash = await serial(() => db.put(key, value));
        return reply(200, JSON.stringify({ hash }));
      }
      case "DELETE": {
        // Deleting a missing key succeeds without writing an oplog entry.
        const hash = await serial(async () => (await db.get(key)) === undefined ? null : db.del(key));
        return reply(200, JSON.stringify({ hash }));
      }
      default:
        return reply(405, '{"error":"method not allowed"}');
    }
  } catch (error) {
    return reply(500, JSON.stringify({ error: String(error?.message ?? error) }));
  }
});

const shutdown = async () => { server.close(); await orbitdb.stop(); await ipfs.stop(); process.exit(0); };
for (const signal of ["SIGINT", "SIGTERM"]) process.once(signal, shutdown);
server.listen(port, "127.0.0.1", () => console.log(`OrbitDB storage service ready on 127.0.0.1:${port} (${db.address})`));
