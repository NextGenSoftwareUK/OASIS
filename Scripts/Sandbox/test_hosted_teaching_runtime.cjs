'use strict';
const assert = require('node:assert/strict');
const test = require('node:test');
const { review, waitForNative } = require('../../Docker/host-sandbox-teaching.cjs');

const probe = 'aaaaaaaa-2222-4222-8222-222222222222';
const token = 'a'.repeat(32);
const instance = 'b'.repeat(32);
const orbitAddress = '/orbitdb/zdpuAoVsPQjRe7HAv3QmzGqp9FuB4uFg8kgU34HeWDd4k5P3B';
const gun = {
  OASIS_SANDBOX_GUN: 'true',
  CQL_GUN_QUERY_SIDECAR: 'http://127.0.0.1:54870/',
  CQL_GUN_QUERY_READER_TOKEN: token,
  CQL_GUN_QUERY_PROBE_ID: probe
};

function reply(fields = {}, headers = {}) {
  return Response.json({ id: probe, isPublic: true, ...fields }, { headers: {
    'X-OASIS-Teaching-Provider': 'GUNOASIS', 'X-OASIS-Teaching-Instance': instance, ...headers
  } });
}

test('review selects only the fixed native service and checks the reviewed configuration', () => {
  assert.deepEqual(review(gun, 'GUNOASIS').args,
    ['/opt/teaching/gun/serve-teaching.mjs', '/data/COSMIC/SandboxGUN', '54870']);
  const orbit = {
    OASIS_SANDBOX_ORBITDB: 'true',
    OASIS_SANDBOX_ORBITDB_ADDRESS: orbitAddress,
    CQL_ORBITDB_QUERY_SIDECAR: 'http://127.0.0.1:54869/',
    CQL_ORBITDB_QUERY_READER_TOKEN: token,
    CQL_ORBITDB_QUERY_PROBE_ID: probe
  };
  assert.deepEqual(review(orbit, 'OrbitDBOASIS').args,
    ['/opt/teaching/orbitdb/serve-teaching.mjs', '/data/COSMIC/SandboxOrbitDB', orbitAddress, '54869']);
  assert.throws(() => review({ ...orbit, OASIS_SANDBOX_ORBITDB_ADDRESS: 'unreviewed' }, 'OrbitDBOASIS'), /manifest address/);
  assert.throws(() => review({ ...gun, OASIS_SANDBOX_GUN: 'false' }, 'GUNOASIS'), /Explicit/);
  for (const address of ['http://localhost:54870/', 'http://127.1:54870/', 'http://127.0.0.1:54870/\n',
    'http://127.0.0.1:80/', 'http://127.0.0.1:54870/path']) {
    assert.throws(() => review({ ...gun, CQL_GUN_QUERY_SIDECAR: address }, 'GUNOASIS'), /Reviewed loopback/);
  }
  assert.throws(() => review({ ...gun, CQL_GUN_QUERY_READER_TOKEN: token + '\n' }, 'GUNOASIS'), /Reviewed loopback/);
});

test('readiness waits for the owned process and verifies its public record', async () => {
  let attempts = 0;
  await waitForNative(review(gun, 'GUNOASIS'), instance, new AbortController().signal, (url, options) => {
    assert.equal(url.href, gun.CQL_GUN_QUERY_SIDECAR + 'oasis/cql/holon?id=' + probe);
    assert.equal(options.redirect, 'error');
    assert.equal(options.headers.Authorization, 'Bearer ' + token);
    attempts++;
    if (attempts === 1) throw new TypeError('not ready', { cause: { code: 'ECONNREFUSED' } });
    return reply();
  });
  assert.equal(attempts, 2);
});

test('responding foreign, private, or oversized sources fail on the first response', async () => {
  for (const [response, failure] of [
    [reply({}, { 'X-OASIS-Teaching-Instance': 'foreign' }), /process differs/],
    [reply({}, { 'X-OASIS-Teaching-Provider': 'OrbitDBOASIS' }), /process differs/],
    [reply({ isPublic: false }), /public record/],
    [reply({ name: 'x'.repeat(1024 * 1024) }), /exceeds its budget/]
  ]) {
    let attempts = 0;
    await assert.rejects(waitForNative(review(gun, 'GUNOASIS'), instance, new AbortController().signal, () => {
      attempts++;
      return response;
    }), failure);
    assert.equal(attempts, 1);
  }
});
