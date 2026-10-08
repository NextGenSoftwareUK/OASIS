import { test } from "node:test";
import assert from "node:assert/strict";
import { spawnSync } from "node:child_process";
import { fileURLToPath } from "node:url";
import { fromBech32 } from "@cosmjs/encoding";

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
