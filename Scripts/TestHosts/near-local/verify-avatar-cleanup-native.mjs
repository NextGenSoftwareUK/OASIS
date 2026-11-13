import assert from "node:assert/strict";
import { randomUUID } from "node:crypto";
import { Worker } from "near-workspaces";

if (process.env.OASIS_NEAR_FRESH_AVATAR_CLEANUP !== "1")
  throw new Error("Enable only for the disposable local NEAR sandbox.");

const worker = await Worker.init({ network: "sandbox" });
try {
  const owner = worker.rootAccount;
  const contract = await owner.devDeploy("build/oasis_storage.wasm");
  const unrelatedCaller = await owner.createSubAccount("unrelated");
  await owner.call(contract, "init", { owner_id: owner.accountId });
  assert.deepEqual(await contract.view("entries", {}), []);

  const id = randomUUID();
  const unrelatedKey = `avatar:${randomUUID()}`;
  const ownedKeys = [
    `avatar:${id}`,
    `history:avatar:${id}:0000000001`,
    `avatar-detail:${id}`,
    `history:avatar-detail:${id}:0000000001`,
    `history:avatar:${id}:0000000002`,
  ];
  for (const key of [...ownedKeys.slice(0, 2), unrelatedKey, ...ownedKeys.slice(2)])
    await owner.call(contract, "put", { key, value: key });

  await assert.rejects(unrelatedCaller.call(contract, "delete_avatar_records", { id }),
    /Only the OASIS contract owner/);
  assert.equal((await contract.view("entries", {})).length, 6);
  await owner.call(contract, "delete_avatar_records", { id });
  assert.deepEqual(await contract.view("entries", {}), [[unrelatedKey, unrelatedKey]]);
  await assert.rejects(owner.call(contract, "delete_avatar_records", { id }), /OASIS_AVATAR_NOT_FOUND/);
  await owner.call(contract, "delete_record", { key: unrelatedKey });
  assert.deepEqual(await contract.view("entries", {}), []);
  console.log("Native NEAR verified authorized atomic avatar and history deletion, unrelated record preservation, and exact fixture cleanup.");
} finally {
  await worker.tearDown();
}
