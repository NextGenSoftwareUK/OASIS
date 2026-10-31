import test from "node:test";
import assert from "node:assert/strict";
import { KeyPair } from "near-workspaces";
import { createRetainedWorker } from "./retained-worker.mjs";

test("retained custom-network worker preserves keys without starting a sandbox",async()=>{
  const worker=await createRetainedWorker("http://127.0.0.1:1","fixture.local");
  try{
    const key=KeyPair.fromRandom("ed25519");
    await worker.manager.setKey("fixture.local",key);
    const stored=await worker.manager.getKey("fixture.local");
    assert.ok(stored);
    assert.equal(stored.getPublicKey().toString(),key.getPublicKey().toString());
    assert.equal(worker.config.network,"custom");
  }finally{await worker.tearDown();}
});
