import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { createHash } from 'node:crypto';
import { resolve } from 'node:path';
import { blake2AsHex } from '@polkadot/util-crypto';

// Compile-format gate only; native code upload/dry-run remains mandatory.
const kind = process.argv[2] ?? 'records';
assert.ok(['records', 'psp34', 'psp34-escrow', 'token-escrow'].includes(kind), 'Expected explicit records, psp34, psp34-escrow or token-escrow artifact.');
assert.equal(process.argv.length <= 3, true, 'Unexpected artifact-verifier arguments.');
const name = kind === 'records' ? 'oasis-records' : kind === 'token-escrow' ? 'oasis-token-escrow' : kind === 'psp34-escrow' ? 'oasis-psp34-escrow' : 'oasis-psp34';
const path = resolve(import.meta.dirname, `../../../../TestResults/PolkadotCurrent/record-contract-target/${name}.wasm`);
const bytes = readFileSync(path);
const module = new WebAssembly.Module(bytes);
const exports = WebAssembly.Module.exports(module);
const imports = WebAssembly.Module.imports(module);
assert.deepEqual(exports.map(item => [item.name, item.kind]).sort(), [['call', 'function'], ['deploy', 'function']]);
assert.deepEqual(imports.filter(item => item.kind === 'memory'), [{ module: 'env', name: 'memory', kind: 'memory' }]);
const allowed = new Set(['seal1/get_storage', 'seal0/input', 'seal0/deposit_event', 'seal2/set_storage',
  'seal1/clear_storage', 'seal0/seal_return', 'seal0/caller', 'seal0/value_transferred', 'seal0/hash_blake2_256']);
// Collection ID uses the actual account; NFT existence checks use ink5's
// contains_storage_v1 binding. Neither import widens the record contract gate.
if (kind === 'psp34' || kind === 'psp34-escrow' || kind === 'token-escrow') {
  allowed.add('seal0/address');
  allowed.add('seal1/contains_storage');
}
// Only PSP22 custody performs nested standard token calls. Do not permit that
// capability on record/NFT contracts by widening their existing import gate.
if (kind === 'token-escrow') allowed.add('seal2/call');
for (const item of imports.filter(item => item.kind !== 'memory')) {
  assert.equal(item.kind, 'function');
  assert.equal(allowed.has(`${item.module}/${item.name}`), true, `Unexpected contract import ${item.module}/${item.name}`);
}
const sourceFolder = kind === 'psp34-escrow' ? 'contracts/psp34' : kind === 'token-escrow' ? 'contracts/token-escrow' : null;
const sources = sourceFolder ? Object.fromEntries(['lib.rs', 'Cargo.toml', 'Cargo.lock'].map(file => {
  const name = `${sourceFolder}/${file}`;
  return [name, createHash('sha256').update(readFileSync(resolve(import.meta.dirname, '..', name))).digest('hex')];
})) : undefined;
console.log(JSON.stringify({ path, kind, sources, bytes: bytes.length, sha256: createHash('sha256').update(bytes).digest('hex'), codeHash: blake2AsHex(bytes, 256),
  exports, imports, formatVerified: true, nativeDeploymentVerified: false }));
