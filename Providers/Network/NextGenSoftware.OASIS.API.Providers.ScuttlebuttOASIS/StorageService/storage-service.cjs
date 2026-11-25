// Scuttlebutt storage service for ScuttlebuttOASIS. SSB has no network storage API: data is a signed, append-only feed
// owned by one key pair. This process owns that feed (ssb-db2 on secret-stack) and exposes it on loopback only.
//   node storage-service.cjs <data directory> <port>      (env OASIS_SSB_STORAGE_TOKEN: bearer token, 32+ chars)
// Routes (same contract as the OrbitDB service): GET /health, GET|PUT|DELETE /kv?key=K, GET /keys?prefix=P.
// Each put appends {type:"oasis-record", key, value}; a delete appends value null. The newest message for a key wins;
// the index of newest values is rebuilt from the feed at start-up, so the feed is the only durable state.
const { timingSafeEqual } = require("node:crypto");
const { createServer } = require("node:http");
const { join } = require("node:path");
const SecretStack = require("secret-stack");
const caps = require("ssb-caps");
const ssbKeys = require("ssb-keys");
const { where, and, type, author, toCallback } = require("ssb-db2/operators");

const [directory, portText] = process.argv.slice(2);
const port = Number(portText);
const token = process.env.OASIS_SSB_STORAGE_TOKEN;
if (!directory || !Number.isInteger(port) || port < 1024 || port > 65535 || !token || token.length < 32 || /[^A-Za-z0-9_-]/.test(token))
  throw new Error("usage: OASIS_SSB_STORAGE_TOKEN=<32+ chars> node storage-service.cjs <data directory> <port>");
const expected = Buffer.from(`Bearer ${token}`);
// SSB rejects messages over 8192 UTF-16 code units of JSON; keep headroom for the message envelope.
const maxValueLength = 7000;

const keys = ssbKeys.loadOrCreateSync(join(directory, "secret"));
const sbot = SecretStack({ caps }).use(require("ssb-db2")).call(null, { path: directory, keys });
const latest = new Map();

const ownRecords = () => new Promise((resolve, reject) =>
  sbot.db.query(where(and(type("oasis-record"), author(keys.id))), toCallback((error, messages) => error ? reject(error) : resolve(messages))));
const append = content => new Promise((resolve, reject) =>
  sbot.db.create({ content, keys }, (error, message) => error ? reject(error) : resolve(message)));

// Appends are serialised so the in-memory index always reflects feed order.
let queue = Promise.resolve();
const serial = operation => (queue = queue.then(operation, operation));

const readBody = request => new Promise((resolve, reject) => {
  const chunks = []; let size = 0;
  request.on("data", chunk => { size += chunk.length; if (size > 1024 * 1024) { reject(new Error("body too large")); request.destroy(); } else chunks.push(chunk); });
  request.on("end", () => resolve(Buffer.concat(chunks).toString("utf8")));
  request.on("error", reject);
});

async function main() {
  for (const message of (await ownRecords()).sort((a, b) => a.value.sequence - b.value.sequence)) {
    const { key, value } = message.value.content;
    if (typeof key !== "string" || (value !== null && typeof value !== "string")) throw new Error(`Malformed oasis-record ${message.key}`);
    if (value === null) latest.delete(key); else latest.set(key, value);
  }
  const server = createServer(async (request, response) => {
    const reply = (status, body, contentType = "application/json") => {
      response.writeHead(status, { "Content-Type": contentType, "Cache-Control": "no-store" });
      response.end(body);
    };
    try {
      const header = Buffer.from(request.headers.authorization ?? "");
      if (header.length !== expected.length || !timingSafeEqual(header, expected)) return reply(401, '{"error":"unauthorized"}');
      const url = new URL(request.url, "http://127.0.0.1");
      if (url.pathname === "/health" && request.method === "GET") return reply(200, JSON.stringify({ address: keys.id }));
      if (url.pathname === "/keys" && request.method === "GET") {
        const prefix = url.searchParams.get("prefix") ?? "";
        return reply(200, JSON.stringify([...latest.keys()].filter(key => key.startsWith(prefix)).sort()));
      }
      if (url.pathname !== "/kv") return reply(404, '{"error":"not found"}');
      const key = url.searchParams.get("key");
      if (!key || url.searchParams.size !== 1) return reply(400, '{"error":"exactly one key is required"}');
      switch (request.method) {
        case "GET":
          return latest.has(key) ? reply(200, latest.get(key), "text/plain; charset=utf-8") : reply(404, '{"error":"missing"}');
        case "PUT": {
          const value = await readBody(request);
          if (key.length + value.length > maxValueLength)
            return reply(413, JSON.stringify({ error: `record exceeds the ${maxValueLength}-character Scuttlebutt message budget` }));
          const message = await serial(async () => { const m = await append({ type: "oasis-record", key, value }); latest.set(key, value); return m; });
          return reply(200, JSON.stringify({ hash: message.key }));
        }
        case "DELETE": {
          const message = await serial(async () => {
            if (!latest.has(key)) return null;
            const m = await append({ type: "oasis-record", key, value: null }); latest.delete(key); return m;
          });
          return reply(200, JSON.stringify({ hash: message?.key ?? null }));
        }
        default:
          return reply(405, '{"error":"method not allowed"}');
      }
    } catch (error) {
      return reply(500, JSON.stringify({ error: String(error?.message ?? error) }));
    }
  });
  const shutdown = () => server.close(() => sbot.close(true, () => process.exit(0)));
  for (const signal of ["SIGINT", "SIGTERM"]) process.once(signal, shutdown);
  server.listen(port, "127.0.0.1", () => console.log(`Scuttlebutt storage service ready on 127.0.0.1:${port} (${keys.id})`));
}
main().catch(error => { console.error(error); process.exit(1); });
