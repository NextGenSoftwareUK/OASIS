import { test } from "node:test";
import assert from "node:assert/strict";
import { spawnSync } from "node:child_process";
import { fileURLToPath } from "node:url";
import { fromBech32 } from "@cosmjs/encoding";
import { DirectSecp256k1Wallet } from "@cosmjs/proto-signing";
import { invoke as invokeTrusted } from './bridge.mjs';

test('trusted entry point validates callback and does not prepare key recovery as a submission', async () => {
  await assert.rejects(invokeTrusted({operation:'generateKey'},true),/awaited callback/);
  let prepared=0;
  const wallet=await invokeTrusted({operation:'restoreKey',privateKey:'07'.repeat(32),addressPrefix:'wasm'},async()=>{prepared++;});
  assert.equal(fromBech32(wallet.address,90).prefix,'wasm');assert.equal(prepared,0);
});

test("raw signing keys recover exact official SDK identity with the requested prefix", async () => {
  const wallet = await DirectSecp256k1Wallet.fromKey(new Uint8Array(32).fill(7), "wasm");
  const [account] = await wallet.getAccounts();
  for (const privateKey of ["07".repeat(32), "0x" + "07".repeat(32)]) {
    const restored = invoke({ operation: "restoreKey", privateKey, addressPrefix: "wasm" });
    assert.equal(restored.ok, true);
    assert.equal(restored.result.address, account.address);
    assert.equal(restored.result.publicKey, Buffer.from(account.pubkey).toString("hex"));
    assert.equal(fromBech32(restored.result.address, 90).prefix, "wasm");
  }
});

const bridge = fileURLToPath(new URL("bridge.mjs", import.meta.url));
function invoke(request) {
  const process = spawnSync(globalThis.process.execPath, [bridge], {
    input: JSON.stringify(request), encoding: "utf8", timeout: 10000,
  });
  assert.ifError(process.error);
  return { status: process.status, ...JSON.parse(process.stdout) };
}

test("official SDK creates a valid recoverable Cosmos wallet", () => {
  const generated = invoke({ operation: "generateKey", addressPrefix: "wasm" });
  assert.equal(generated.status, 0);
  assert.equal(generated.ok, true);
  assert.equal(generated.result.mnemonic.split(" ").length, 24);
  // Cosmos account addresses fit the Bech32 standard's 90-character limit.
  assert.equal(fromBech32(generated.result.address, 90).prefix, "wasm");
  assert.equal(fromBech32(generated.result.address, 90).data.length, 20);
  assert.equal(generated.result.publicKey.length, 66);
  const restored = invoke({ operation: "restoreKey", privateKey: generated.result.mnemonic, addressPrefix: "wasm" });
  assert.equal(restored.result.address, generated.result.address);
  assert.equal(restored.result.publicKey, generated.result.publicKey);
});

test("invalid signing key fails instead of inventing a wallet", () => {
  const response = invoke({ operation: "restoreKey", privateKey: "not-a-key" });
  assert.equal(response.status, 1);
  assert.equal(response.ok, false);
  assert.equal(typeof response.error, "string");
});

test("unreachable RPC propagates failure, not synthetic activation", () => {
  const response = invoke({ operation: "probe", rpcEndpoint: "http://127.0.0.1:1", chainId: "oasis-test", contractAddress: "invalid" });
  assert.equal(response.status, 1);
  assert.equal(response.ok, false);
});
test('storage activation authority is explicit and fail closed without paid mutation', async()=>{
 const {verifyStorageAuthority}=await import('./bridge.mjs');
 assert.doesNotThrow(()=>verifyStorageAuthority({owner:'wasm1owner'},'wasm1owner'));
 for(const [config,sender] of [[null,'wasm1owner'],[{},'wasm1owner'],[{owner:'other'},'wasm1owner'],[{owner:''},''],[{owner:'wasm1owner'},null]])
  assert.throws(()=>verifyStorageAuthority(config,sender),/owner does not match/);
});
