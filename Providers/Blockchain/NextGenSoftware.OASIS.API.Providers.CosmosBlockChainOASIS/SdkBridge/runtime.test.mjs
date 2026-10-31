import { test } from "node:test";
import assert from "node:assert/strict";
import { spawnSync } from "node:child_process";
import { readFileSync, writeFileSync, existsSync } from "node:fs";
import { createHash, randomUUID } from "node:crypto";
import { fileURLToPath } from "node:url";

const bridge = fileURLToPath(new URL("bridge.mjs", import.meta.url));
const wasmPath = process.env.OASIS_COSMOS_TEST_WASM;
if (!wasmPath) throw new Error("OASIS_COSMOS_TEST_WASM must point to the compiled storage contract");
const base = {
  rpcEndpoint: "http://127.0.0.1:26657", chainId: "oasis-cosmos-local",
  addressPrefix: "wasm", gasPrice: "0.025stake",
  // Public, local-only development key. Never use for real assets.
  privateKey: `${"abandon ".repeat(23)}art`,
};
function invoke(operation, args = {}, options = {}) {
  const child = spawnSync(process.execPath, [bridge], {
    input: JSON.stringify({ ...base, operation, ...args }), encoding: "utf8", timeout: 90000,
  });
  assert.ifError(child.error);
  const envelope = JSON.parse(child.stdout);
  if (!options.expectError) assert.equal(envelope.ok, true, envelope.error);
  else {
    assert.equal(envelope.ok, false, "operation unexpectedly succeeded");
    if (options.errorPattern) assert.match(envelope.error, options.errorPattern);
  }
  return envelope.result;
}

test("real signed Cosmos contract storage, authorization and transaction readback", { timeout: 600000 }, () => {
  const wasm = readFileSync(wasmPath);
  const module = new WebAssembly.Module(wasm);
  const allowedImports = new Set(["abort", "db_read", "db_write", "db_remove", "db_scan", "db_next",
    "addr_validate", "addr_canonicalize", "addr_humanize", "secp256k1_verify", "secp256k1_recover_pubkey",
    "ed25519_verify", "ed25519_batch_verify", "debug", "query_chain"]);
  for (const entry of WebAssembly.Module.imports(module)) {
    assert.equal(entry.module, "env");
    assert.equal(allowedImports.has(entry.name), true, `unexpected import ${entry.name}`);
  }
  const digest = createHash("sha256").update(wasm).digest("hex");
  const cachePath = `${wasmPath}.deployment.json`;
  let deployment = existsSync(cachePath) ? JSON.parse(readFileSync(cachePath, "utf8")) : null;
  if (!deployment || deployment.digest !== digest) {
    const uploaded = invoke("upload", { wasmBase64: wasm.toString("base64") });
    const instantiated = invoke("instantiate", { codeId: uploaded.codeId, message: { owner: null }, label: "OASIS local storage" });
    deployment = { digest, codeId: uploaded.codeId, contractAddress: instantiated.contractAddress };
    writeFileSync(cachePath, JSON.stringify(deployment));
  }
  base.contractAddress = deployment.contractAddress;
  const probe = invoke("probe");
  assert.equal(probe.chainId, base.chainId);
  assert.equal(probe.config.owner, invoke("restoreKey").address);
  const key = `oasis/evidence/${randomUUID()}`;
  assert.equal(invoke("get", { key }), null);
  const put = invoke("put", { key, value: "committed by official CosmJS" });
  assert.ok(put.transactionHash);
  assert.equal(invoke("transactionStatus", { transactionHash: put.transactionHash }).code, 0);
  assert.equal(invoke("get", { key }), "committed by official CosmJS");
  assert.ok(invoke("list", { prefix: "oasis/evidence/" }).includes(key));
  assert.equal(invoke("list", { prefix: "oasis/evidence/", startAfter: key }).includes(key), false);
  const outsider = invoke("generateKey");
  // Fund the outsider so rejection proves contract authorization, not lack of gas.
  invoke("transfer", { toWalletAddress: outsider.address, amountUnits: "1000000", denom: "stake" });
  assert.equal(invoke("balance", { walletAddress: outsider.address, denom: "stake" }).amount, "1000000");
  invoke("put", { key, value: "unauthorized", privateKey: outsider.mnemonic },
    { expectError: true, errorPattern: /unauthorized: storage owner required/ });
  assert.equal(invoke("get", { key }), "committed by official CosmJS");
  invoke("probe", { chainId: "wrong-chain" }, { expectError: true, errorPattern: /chain mismatch/ });
  invoke("delete", { key });
  assert.equal(invoke("get", { key }), null);
  invoke("delete", { key });
});
