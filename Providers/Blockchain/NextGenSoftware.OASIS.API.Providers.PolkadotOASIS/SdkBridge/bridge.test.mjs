import { test } from 'node:test';
import assert from 'node:assert/strict';
import { spawnSync } from 'node:child_process';
import { fileURLToPath } from 'node:url';

const valid = { operation: 'balance', endpoint: 'http://127.0.0.1:1', genesisHash: '0x' + 'a'.repeat(64), address: 'invalid' };
test('malformed JSON never echoes signing input', () => {
  const result = spawnSync(process.execPath, [fileURLToPath(new URL('./bridge.mjs', import.meta.url))], {
    input: '{"privateKey":"owned-signing-secret",', encoding: 'utf8', timeout: 10000
  });
  assert.equal(result.status, 1);
  assert.match(JSON.parse(result.stdout).error, /valid JSON/);
  assert.doesNotMatch(result.stdout + result.stderr, /owned-signing-secret/);
});
for (const [name, override] of [
  ['unknown operation', { operation: 'send' }],
  ['embedded credentials', { endpoint: 'https://owned-secret:owned-password@example.invalid' }],
  ['missing genesis', { genesisHash: '' }],
  ['empty address', { address: '' }]
]) {
  test(`rejects ${name} before any network connection`, () => {
    const result = spawnSync(process.execPath, [fileURLToPath(new URL('./bridge.mjs', import.meta.url))], {
      input: JSON.stringify({ ...valid, ...override }), encoding: 'utf8', timeout: 10000
    });
    assert.equal(result.status, 1, result.error?.message);
    const response = JSON.parse(result.stdout);
    assert.equal(response.ok, false);
    assert.match(response.error, /request validation/);
    assert.doesNotMatch(result.stdout + result.stderr, /owned-secret|owned-password/);
  });
}
