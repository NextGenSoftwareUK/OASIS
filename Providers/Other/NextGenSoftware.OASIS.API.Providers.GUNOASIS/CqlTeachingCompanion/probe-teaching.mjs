import assert from "node:assert/strict";
import { randomUUID } from "node:crypto";
import { mkdir, readFile, writeFile } from "node:fs/promises";
import { join, resolve } from "node:path";
import { openTeachingStore } from "./teaching-store.mjs";

const [mode, path] = process.argv.slice(2);
if (!path || !["seed", "verify"].includes(mode)) throw new Error("Use seed or verify with an owned fixture directory.");
const directory = resolve(path);
const receipt = join(directory, "fixture.json");
const payload = (actor, id, privateId) => ({
  id, createdByAvatarId: actor, name: `teaching-${id}`, isPublic: id !== privateId,
  children: [{ id: privateId, isPublic: false }],
});
if (mode === "seed") {
  await mkdir(directory, { recursive: true });
  const store = await openTeachingStore(directory, true);
  const ids = Array.from({ length: 6 }, () => randomUUID());
  const actor = randomUUID();
  for (const id of ids.slice(0, 5)) await store.seed(id, payload(actor, id, ids[3]), id === ids[4]);
  await writeFile(receipt, JSON.stringify({ actor, ids }), { flag: "wx" });
} else {
  const { actor, ids } = JSON.parse(await readFile(receipt, "utf8"));
  const store = await openTeachingStore(directory);
  for (const id of ids.slice(0, 5)) {
    const node = await store.read(id);
    assert.equal(node?._?.["#"], `oasis.holons/${id}`);
    assert.equal(node.is_deleted, String(id === ids[4]));
    assert.deepEqual(JSON.parse(node.data_json), payload(actor, id, ids[3]));
  }
  assert.equal(await store.read(ids[5]), undefined);
}
