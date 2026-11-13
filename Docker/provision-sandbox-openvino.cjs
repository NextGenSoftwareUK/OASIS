'use strict';
const fs = require('node:fs/promises');
const path = require('node:path');
const crypto = require('node:crypto');
const { modelConfiguration, verifyModels } = require('./host-sandbox-openvino.cjs');

// Build-time only: no runtime download, replacement of existing models or retries.
async function provision(directory, pins, request = fetch) {
  const config = modelConfiguration(pins);
  if (!/^[a-f0-9]{40}$/.test(pins.sourceCommit || '') ||
      Object.keys(pins.files || {}).sort().join(',') !== 'dummy.bin,dummy.xml')
    throw Error('Exact upstream model source pins are required.');
  if (!path.isAbsolute(directory) || path.resolve(directory) !== directory ||
      await fs.realpath(path.dirname(directory)) !== path.dirname(directory))
    throw Error('Model build target must have a canonical absolute parent.');
  // mkdir without recursive/exist handling enforces one new owned build target.
  await fs.mkdir(directory);
  const files = new Map();
  for (const [name, pin] of Object.entries(pins.files)) {
    if (!Number.isInteger(pin.bytes) || pin.bytes < 1 || pin.bytes > 65536 || !/^[a-f0-9]{64}$/.test(pin.sha256 || ''))
      throw Error('Model file pin exceeds its contract.');
    const uri = `https://raw.githubusercontent.com/openvinotoolkit/model_server/${pins.sourceCommit}/src/test/dummy/1/${name}`;
    const response = await request(uri, { redirect: 'error', signal: AbortSignal.timeout(20000) });
    if (response.status !== 200) throw Error('Pinned model source did not return HTTP200.');
    const chunks = []; let bytes = 0;
    for await (const chunk of response.body) {
      bytes += chunk.byteLength;
      if (bytes > pin.bytes) throw Error('Pinned model source exceeds its byte bound.');
      chunks.push(Buffer.from(chunk));
    }
    const body = Buffer.concat(chunks);
    if (body.length !== pin.bytes || crypto.createHash('sha256').update(body).digest('hex') !== pin.sha256)
      throw Error('Pinned model source hash differs.');
    files.set(name, body);
  }
  for (const version of pins.versions) {
    const folder = path.join(directory, pins.modelName, version);
    await fs.mkdir(folder, { recursive: true });
    for (const [name, body] of files) await fs.writeFile(path.join(folder, name), body, { flag: 'wx', mode: 0o444 });
  }
  await fs.writeFile(path.join(directory, 'config.json'), JSON.stringify(config), { flag: 'wx', mode: 0o444 });
  await verifyModels(directory, config, pins);
}
module.exports = { provision };
if (require.main === module) {
  if (process.argv.length !== 4) throw Error('Explicit new model directory and pin manifest are required.');
  fs.readFile(process.argv[3], 'utf8').then(text => provision(process.argv[2], JSON.parse(text)))
    .catch(error => { console.error(error); process.exitCode = 1; });
}
