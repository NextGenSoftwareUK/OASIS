// Ceramic storage service for CeramicOASIS. A ceramic-one node only accepts signed events (DAG-JOSE, did:key) encoded
// as CAR files, so this process owns one did:key and one Ceramic model instance document and exposes it on loopback.
//   node storage-service.mjs <data directory> <port> <ceramic-one URL>   (env OASIS_CERAMIC_STORAGE_TOKEN: 32+ chars)
// Routes (same contract as the OrbitDB/Scuttlebutt services): GET /health, GET|PUT|DELETE /kv?key=K, GET /keys?prefix=P.
// The document's content is {records: {key: value}}; each put/delete is one signed data event (a JSON patch) on that
// stream, so the Ceramic stream is the record store. <data directory>/state.json holds only the DID seed, the model and
// document stream ids and the CID of the last event this service wrote; at start-up the records are loaded from the
// node's aggregated stream state, which must have reached that CID.
import { randomBytes, timingSafeEqual } from "node:crypto";
import { existsSync, mkdirSync, readFileSync, renameSync, writeFileSync } from "node:fs";
import { createServer } from "node:http";
import { join } from "node:path";
import { CeramicClient } from "@ceramic-sdk/http-client";
import { CommitID, StreamID } from "@ceramic-sdk/identifiers";
import { ModelClient } from "@ceramic-sdk/model-client";
import { ModelInstanceClient } from "@ceramic-sdk/model-instance-client";
import { getAuthenticatedDID } from "@didtools/key-did";

const [directory, portText, ceramicUrl] = process.argv.slice(2);
const port = Number(portText);
const token = process.env.OASIS_CERAMIC_STORAGE_TOKEN;
if (!directory || !Number.isInteger(port) || port < 1024 || port > 65535 || !/^https?:\/\//.test(ceramicUrl ?? "")
  || !token || token.length < 32 || /[^A-Za-z0-9_-]/.test(token))
  throw new Error("usage: OASIS_CERAMIC_STORAGE_TOKEN=<32+ chars> node storage-service.mjs <data directory> <port> <ceramic-one URL>");
const expected = Buffer.from(`Bearer ${token}`);
// ceramic-one rejects event blocks over 256000 bytes; a data event carries one key and value plus the signed envelope.
const maxRecordBytes = 240000;
// How long start-up waits for the node's stream aggregation to reach the last written event.
const stateWaitMs = 120000;

const statePath = join(directory, "state.json");
const ceramic = new CeramicClient({ url: ceramicUrl.replace(/\/+$/, "") });

const recordsModel = {
  version: "2.0",
  name: "OASISKeyValueRecords",
  description: "OASIS key-value records (encrypted values) owned by one storage service DID.",
  accountRelation: { type: "list" },
  interface: false,
  implements: [],
  schema: {
    type: "object",
    properties: { records: { type: "object", additionalProperties: { type: "string" } } },
    required: ["records"],
    additionalProperties: false,
  },
};

const saveState = state => {
  writeFileSync(statePath + ".next", JSON.stringify(state), { mode: 0o600 });
  renameSync(statePath + ".next", statePath);
};

const sleep = ms => new Promise(resolve => setTimeout(resolve, ms));

async function open() {
  mkdirSync(directory, { recursive: true });
  if (!existsSync(statePath)) {
    const seed = randomBytes(32);
    const did = await getAuthenticatedDID(seed);
    const model = await new ModelClient({ ceramic, did }).createDefinition(recordsModel);
    await ceramic.registerInterestModel(model.toString());
    const documents = new ModelInstanceClient({ ceramic, did });
    const created = await documents.createInstance({ model, content: { records: {} }, shouldIndex: true });
    const state = { seed: seed.toString("hex"), model: model.toString(), document: created.baseID.toString(), tip: created.commit.toString() };
    saveState(state);
    return { state, documents, did, current: created, records: {} };
  }
  const state = JSON.parse(readFileSync(statePath, "utf8"));
  const did = await getAuthenticatedDID(Uint8Array.from(Buffer.from(state.seed, "hex")));
  await ceramic.registerInterestModel(state.model);
  const documents = new ModelInstanceClient({ ceramic, did });
  const streamId = StreamID.fromString(state.document);
  const deadline = Date.now() + stateWaitMs;
  let lastSeen = "no state";
  while (Date.now() < deadline) {
    try {
      const document = await documents.getDocumentState(streamId);
      lastSeen = document.commitID.commit.toString();
      if (lastSeen === state.tip) {
        const records = document.content?.records;
        if (records === null || typeof records !== "object" || Object.values(records).some(value => typeof value !== "string"))
          throw new Error(`Document ${state.document} does not hold OASIS records.`);
        return { state, documents, did, current: CommitID.fromStream(streamId, document.commitID.commit), records: { ...records } };
      }
    } catch (error) {
      if (String(error?.message).includes("does not hold")) throw error;
      lastSeen = String(error?.message ?? error);
    }
    await sleep(500);
  }
  throw new Error(`Ceramic stream ${state.document} never reached the last written event ${state.tip} (node reports ${lastSeen}).`);
}

const readBody = request => new Promise((resolve, reject) => {
  const chunks = []; let size = 0;
  request.on("data", chunk => { size += chunk.length; if (size > 1024 * 1024) { reject(new Error("body too large")); request.destroy(); } else chunks.push(chunk); });
  request.on("end", () => resolve(Buffer.concat(chunks).toString("utf8")));
  request.on("error", reject);
});

async function main() {
  const store = await open();
  // Data events chain through `prev`, so writes are serialised and the in-memory records follow the stream tip.
  let queue = Promise.resolve();
  const serial = operation => (queue = queue.then(operation, operation));
  const write = async records => {
    const next = await store.documents.postData({
      currentID: store.current, currentContent: { records: store.records }, newContent: { records }, shouldIndex: true,
    });
    saveState({ ...store.state, tip: next.commit.toString() });
    store.state.tip = next.commit.toString();
    store.current = next;
    store.records = records;
    return next.commit.toString();
  };

  const server = createServer(async (request, response) => {
    const reply = (status, body, contentType = "application/json") => {
      response.writeHead(status, { "Content-Type": contentType, "Cache-Control": "no-store" });
      response.end(body);
    };
    try {
      const header = Buffer.from(request.headers.authorization ?? "");
      if (header.length !== expected.length || !timingSafeEqual(header, expected)) return reply(401, '{"error":"unauthorized"}');
      const url = new URL(request.url, "http://127.0.0.1");
      if (url.pathname === "/health" && request.method === "GET") {
        const node = await fetch(`${ceramicUrl.replace(/\/+$/, "")}/ceramic/liveness`);
        if (!node.ok) return reply(503, JSON.stringify({ error: `ceramic-one liveness returned ${node.status}` }));
        return reply(200, JSON.stringify({ address: store.state.document, controller: store.did.id }));
      }
      if (url.pathname === "/keys" && request.method === "GET") {
        const prefix = url.searchParams.get("prefix") ?? "";
        return reply(200, JSON.stringify(Object.keys(store.records).filter(key => key.startsWith(prefix)).sort()));
      }
      if (url.pathname !== "/kv") return reply(404, '{"error":"not found"}');
      const key = url.searchParams.get("key");
      if (!key || url.searchParams.size !== 1) return reply(400, '{"error":"exactly one key is required"}');
      switch (request.method) {
        case "GET":
          return Object.hasOwn(store.records, key) ? reply(200, store.records[key], "text/plain; charset=utf-8") : reply(404, '{"error":"missing"}');
        case "PUT": {
          const value = await readBody(request);
          if (Buffer.byteLength(key) + Buffer.byteLength(value) > maxRecordBytes)
            return reply(413, JSON.stringify({ error: `record exceeds the ${maxRecordBytes}-byte Ceramic event budget` }));
          const hash = await serial(() => write({ ...store.records, [key]: value }));
          return reply(200, JSON.stringify({ hash }));
        }
        case "DELETE": {
          const hash = await serial(async () => {
            if (!Object.hasOwn(store.records, key)) return null;
            const { [key]: _removed, ...rest } = store.records;
            return write(rest);
          });
          return reply(200, JSON.stringify({ hash }));
        }
        default:
          return reply(405, '{"error":"method not allowed"}');
      }
    } catch (error) {
      return reply(500, JSON.stringify({ error: String(error?.message ?? error) }));
    }
  });
  const shutdown = () => server.close(() => process.exit(0));
  for (const signal of ["SIGINT", "SIGTERM"]) process.once(signal, shutdown);
  server.listen(port, "127.0.0.1", () => console.log(`Ceramic storage service ready on 127.0.0.1:${port} (${store.state.document}, ${store.did.id})`));
}
main().catch(error => { console.error(error); process.exit(1); });
