import assert from "node:assert/strict";
import { randomUUID } from "node:crypto";
import { mkdir, readFile, writeFile } from "node:fs/promises";
import { join, resolve } from "node:path";
import { openTeachingStore } from "./teaching-store.mjs";

const mode = process.argv[2];
const directory = resolve(process.argv[3] ?? "");
if (!["seed", "verify"].includes(mode) || !process.argv[3])
  throw new Error("Use seed or verify with a disposable fixture directory.");
await mkdir(directory, { recursive: true });

const receiptPath = join(directory, "fixture.json");
const fixture = mode === "verify" ? JSON.parse(await readFile(receiptPath, "utf8")) : undefined;
const { db, close } = await openTeachingStore(directory, fixture?.address);
try {
  if (mode === "seed") {
    const ids = Array.from({ length: 6 }, () => randomUUID());
    const actor = randomUUID();
    for (const id of ids.slice(0, 5)) {
      await db.put(id, { id, createdByAvatarId: actor, name: `teaching-${id}`,
        isPublic: id !== ids[3], isDeleted: id === ids[4], children: [{ id: ids[3], isPublic: false }] });
    }
    await writeFile(receiptPath, JSON.stringify({ address: db.address, actor, ids }), { flag: "wx" });
    console.log("Seeded an owned indexed OrbitDB teaching database.");
  } else {
    const { actor, ids } = fixture;
    const records = [];
    for (const [position, id] of ids.slice(0, 5).entries()) {
      const row = await db.get(id);
      assert.deepEqual(row, { id, createdByAvatarId: actor, name: `teaching-${id}`, isPublic: position !== 3, isDeleted: position === 4,
        children: [{ id: ids[3], isPublic: false }] });
      if (row.isPublic && !row.isDeleted) records.push(row.id);
    }
    assert.equal(await db.get(ids[5]), undefined);
    assert.deepEqual(records, ids.slice(0, 3));
    console.log("PASS: real OrbitDB indexed reads after restart, public/private/deleted/missing.");
  }
} finally {
  await close();
}
