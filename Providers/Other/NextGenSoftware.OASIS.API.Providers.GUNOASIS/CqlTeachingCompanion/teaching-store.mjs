import { stat } from "node:fs/promises";
import { createRequire } from "node:module";
import { join } from "node:path";

const Gun = createRequire(import.meta.url)("gun");
const marker = "oasis.cql.teaching";
const scope = "oasis-cql-public-teaching-v1";
const soul = id => `oasis.holons/${id}`;

function bounded(start) {
  return new Promise((resolve, reject) => {
    const timer = setTimeout(() => reject(new Error("GUN did not acknowledge the bounded operation.")), 7000);
    start((error, data) => {
      clearTimeout(timer);
      if (error) reject(error);
      else resolve(data);
    });
  });
}

export async function openTeachingStore(directory, seed = false) {
  const file = join(directory, "radata");
  if (!seed && !(await stat(file)).isDirectory())
    throw new Error("An existing GUN teaching directory is required.");
  const gun = Gun({ file, peers: [], radisk: true, localStorage: false,
    multicast: false, axe: false, stats: false });
  const read = key => bounded(done => gun.get(key).once((data, _, message) => {
    if (message?.err) done(new Error("GUN reported a native read error."));
    else done(null, data);
  }));
  const put = (key, fields) => bounded(done => gun.get(key).put(fields, acknowledgement =>
    acknowledgement.err ? done(new Error("GUN did not persist the graph node.")) : done()));
  if (seed) await put(marker, { scope });
  else if ((await read(marker))?.scope !== scope) throw new Error("The GUN teaching marker is missing.");
  return {
    read: id => read(soul(id)),
    seed: (id, payload, deleted) => put(soul(id), { is_deleted: String(deleted), data_json: JSON.stringify(payload) }),
  };
}
