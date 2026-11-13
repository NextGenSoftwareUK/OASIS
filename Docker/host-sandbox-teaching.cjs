'use strict';
const crypto = require('node:crypto');
const fs = require('node:fs/promises');
const path = require('node:path');
const { setTimeout: delay } = require('node:timers/promises');

const providers = {
  OrbitDBOASIS: { flag: 'ORBITDB', directory: '/data/COSMIC/SandboxOrbitDB', runtime: '/opt/teaching/orbitdb' },
  GUNOASIS: { flag: 'GUN', directory: '/data/COSMIC/SandboxGUN', runtime: '/opt/teaching/gun' }
};
const guid = /^[a-f0-9]{8}-(?:[a-f0-9]{4}-){3}[a-f0-9]{12}$/;
const empty = '00000000-0000-0000-0000-000000000000';
const notReady = new Set(['ECONNREFUSED', 'ECONNRESET', 'UND_ERR_CONNECT_TIMEOUT', 'UND_ERR_SOCKET']);

function review(environment, provider) {
  const source = providers[provider];
  if (!source || environment['OASIS_SANDBOX_' + source.flag] !== 'true')
    throw Error('Explicit native teaching runtime flag required.');
  const prefix = 'CQL_' + source.flag + '_QUERY_';
  const address = environment[prefix + 'SIDECAR'];
  const token = environment[prefix + 'READER_TOKEN'];
  const probe = environment[prefix + 'PROBE_ID'];
  const port = /^http:\/\/127\.0\.0\.1:([1-9][0-9]{3,4})\/$/.exec(address || '');
  if (!port || port[0] !== address || +port[1] < 1024 || +port[1] > 65535 ||
      typeof token !== 'string' || token.length < 32 || token.length > 256 || /[^A-Za-z0-9_-]/.test(token) ||
      typeof probe !== 'string' || probe.length !== 36 || !guid.test(probe) || probe === empty)
    throw Error('Reviewed loopback endpoint, reader and public probe required.');
  const args = [path.posix.join(source.runtime, 'serve-teaching.mjs'), source.directory];
  if (provider === 'OrbitDBOASIS') {
    const database = environment.OASIS_SANDBOX_ORBITDB_ADDRESS;
    if (typeof database !== 'string' || /^\/orbitdb\/z[1-9A-HJ-NP-Za-km-z]{40,80}$/.exec(database)?.[0] !== database)
      throw Error('Reviewed OrbitDB manifest address required.');
    args.push(database);
  }
  args.push(port[1]);
  return { ...source, args, address, token, probe, provider };
}

async function serviceConfiguration(environment, provider) {
  const source = review(environment, provider);
  const modules = path.posix.join(source.runtime, 'node_modules');
  if (!(await fs.stat(modules)).isDirectory())
    throw Error('The image must include the pinned native teaching runtime.');
  if (await fs.realpath(source.directory) !== source.directory || !(await fs.stat(source.directory)).isDirectory())
    throw Error('An existing owned teaching directory is required.');
  const instance = crypto.randomBytes(16).toString('hex');
  return {
    command: process.execPath, args: source.args,
    env: { ['OASIS_' + source.flag + '_CQL_READER_TOKEN']: source.token, OASIS_CQL_TEACHING_INSTANCE: instance },
    check: signal => waitForNative(source, instance, signal)
  };
}

async function waitForNative(source, instance, signal, fetchRow = fetch) {
  const deadline = Date.now() + 15000;
  const url = new URL('oasis/cql/holon?id=' + source.probe, source.address);
  while (Date.now() < deadline) {
    signal.throwIfAborted();
    const timeout = AbortSignal.timeout(1000);
    let response;
    try {
      response = await fetchRow(url, { redirect: 'error', signal: AbortSignal.any([signal, timeout]),
        headers: { Authorization: 'Bearer ' + source.token } });
    } catch (error) {
      signal.throwIfAborted();
      if (!timeout.aborted && !notReady.has(error.cause?.code)) throw error;
      await delay(100, undefined, { signal });
      continue;
    }
    if (response.status !== 200 || response.headers.get('X-OASIS-Teaching-Provider') !== source.provider ||
        response.headers.get('X-OASIS-Teaching-Instance') !== instance)
      throw Error('The native teaching process differs from the reviewed source.');
    const reader = response.body.getReader();
    const chunks = [];
    let bytes = 0;
    try {
      for (;;) {
        const item = await reader.read();
        if (item.done) break;
        bytes += item.value.length;
        if (bytes > 1024 * 1024) { await reader.cancel(); throw Error('Teaching readiness response exceeds its budget.'); }
        chunks.push(item.value);
      }
    } finally { reader.releaseLock(); }
    const row = JSON.parse(new TextDecoder('utf-8', { fatal: true }).decode(Buffer.concat(chunks)));
    if (row?.id !== source.probe || row.isPublic !== true || row.isDeleted === true)
      throw Error('The native teaching probe differs from the reviewed public record.');
    return;
  }
  throw Error('Native teaching readiness deadline expired.');
}

module.exports = { review, serviceConfiguration, waitForNative };
