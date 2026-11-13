'use strict';
const fs = require('node:fs/promises');
const path = require('node:path');
const crypto = require('node:crypto');
const { contract, credential } = require('./configuration.cjs');

async function durableExclusive(file, value) {
  const handle = await fs.open(file, 'wx', 0o600);
  try { await handle.writeFile(JSON.stringify(value)); await handle.sync(); }
  finally { await handle.close(); }
  // Linux hosting also requires the directory entry durable before mutation.
  // Windows qualification can sync the file but cannot open directories this way.
  if (process.platform === 'linux') {
    const directory = await fs.open(path.dirname(file), 'r');
    try { await directory.sync(); } finally { await directory.close(); }
  }
}

// Only explicit provisioning calls this operation. An intent, including one with
// an unknown outcome, permanently prevents a second request for this source.
async function seedOnce(directory, prefix, records, token, signal) {
  credential(token);
  if (!/^[a-f0-9]{8}-(?:[a-f0-9]{4}-){3}[a-f0-9]{11}$/.test(prefix) ||
      !Array.isArray(records) || records.length !== 4) throw Error('Invalid Dapr source creation plan.');
  for (const [index, suffix] of ['e', '1', '2', '3'].entries()) {
    const row = records[index]; const id = prefix + suffix;
    if (row?.key !== 'oasis/holon/' + id.replaceAll('-', '') || typeof row.value !== 'string' ||
        Buffer.byteLength(row.value) > 4096) throw Error('Invalid Dapr source record.');
    const value = JSON.parse(row.value);
    if (value.id !== id || value.createdByAvatarId !== prefix + '0' || value.isPublic !== true ||
        (suffix === '3' ? !Number.isFinite(Date.parse(value.deletedDate)) || value.deletedDate === '0001-01-01T00:00:00'
          : value.deletedDate !== '0001-01-01T00:00:00')) throw Error('Invalid Dapr source authority.');
  }
  signal.throwIfAborted();
  if (!path.isAbsolute(directory) || await fs.realpath(directory) !== directory) throw Error('Invalid Dapr source directory.');
  const body = JSON.stringify(records);
  await durableExclusive(path.join(directory, 'seed.intent.json'), {
    operation: crypto.randomUUID(), prefix, keys: records.map(row => row.key),
    payloadSha256: crypto.createHash('sha256').update(body).digest('hex'), replayAllowed: false
  });
  signal.throwIfAborted();
  const response = await fetch(`http://127.0.0.1:${contract.ports.http}/v1.0/state/${contract.store}`, {
    method: 'POST', headers: { 'content-type': 'application/json', 'dapr-api-token': token }, body,
    redirect: 'error', signal: AbortSignal.any([signal, AbortSignal.timeout(3000)])
  });
  if (response.status !== 204) throw Error('Dapr creation not acknowledged; reconcile original intent, never resend.');
  await durableExclusive(path.join(directory, 'seed.acknowledged.json'), { prefix, acknowledged: true, count: records.length });
}
module.exports = { durableExclusive, seedOnce };
