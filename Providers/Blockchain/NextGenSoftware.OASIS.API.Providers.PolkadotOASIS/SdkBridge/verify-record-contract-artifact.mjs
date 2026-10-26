import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { createHash } from 'node:crypto';
import { resolve } from 'node:path';

// Compile-format gate only; native code upload/dry-run remains mandatory.
const path = resolve(import.meta.dirname, '../../../../TestResults/PolkadotCurrent/record-contract-target/oasis-records.wasm');
const bytes = readFileSync(path);
const module = new WebAssembly.Module(bytes);
const exports = WebAssembly.Module.exports(module);
const imports = WebAssembly.Module.imports(module);
assert.deepEqual(exports.map(item => [item.name, item.kind]).sort(), [['call', 'function'], ['deploy', 'function']]);
assert.deepEqual(imports.filter(item => item.kind === 'memory'), [{ module: 'env', name: 'memory', kind: 'memory' }]);
const allowed = new Set(['seal1/get_storage', 'seal0/input', 'seal0/deposit_event', 'seal2/set_storage',
  'seal1/clear_storage', 'seal0/seal_return', 'seal0/caller', 'seal0/value_transferred', 'seal0/hash_blake2_256']);
for (const item of imports.filter(item => item.kind !== 'memory')) {
  assert.equal(item.kind, 'function');
  assert.equal(allowed.has(`${item.module}/${item.name}`), true, `Unexpected contract import ${item.module}/${item.name}`);
}
console.log(JSON.stringify({ path, bytes: bytes.length, sha256: createHash('sha256').update(bytes).digest('hex'),
  exports, imports, formatVerified: true, nativeDeploymentVerified: false }));
