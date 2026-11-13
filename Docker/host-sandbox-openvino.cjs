'use strict';
const fs = require('node:fs/promises');
const path = require('node:path');
const crypto = require('node:crypto');
const { isDeepStrictEqual } = require('node:util');
const { setTimeout: delay } = require('node:timers/promises');
const endpoint = 'http://127.0.0.1:28000';
const root = '/opt/ovms-rootfs';
const loader = `${root}/usr/lib/x86_64-linux-gnu/ld-linux-x86-64.so.2`;

function configuration(environment, pins) {
  if (environment.OASIS_SANDBOX_OPENVINO !== 'true' ||
      !/^[a-f0-9]{8}-(?:[a-f0-9]{4}-){3}[a-f0-9]{12}$/.test(environment.CQL_OPENVINO_MODEL_QUERY_SOURCE_OWNER || '') ||
      environment.CQL_OPENVINO_MODEL_QUERY_SOURCE_OWNER === '00000000-0000-0000-0000-000000000000')
    throw Error('Explicit OpenVINO source owner is required.');
  return modelConfiguration(pins);
}
function modelConfiguration(pins) {
  if (pins?.modelName !== 'cosmic_dummy' || JSON.stringify(pins.versions) !== '["1","2","3"]' ||
      !/^openvino\/model_server@sha256:[a-f0-9]{64}$/.test(pins.image || '') || !pins.serverVersion)
    throw Error('Explicit OpenVINO source owner and pinned teaching model are required.');
  return { model_config_list: [{ config: { name: pins.modelName, base_path: '/models/' + pins.modelName,
    model_version_policy: { all: {} }, target_device: 'CPU' } }] };
}

async function verifyModels(directory, expected, pins) {
  async function read(file, maximum) {
    const stat = await fs.lstat(file);
    if (!stat.isFile() || stat.isSymbolicLink() || stat.size > maximum || await fs.realpath(file) !== file)
      throw Error('OpenVINO packaged model file is redirected or exceeds its bound.');
    return fs.readFile(file);
  }
  if (await fs.realpath(directory) !== directory) throw Error('OpenVINO model root is redirected.');
  if ((await read(path.join(directory, 'config.json'), 4096)).toString('utf8') !== JSON.stringify(expected))
    throw Error('OpenVINO packaged configuration differs.');
  for (const version of pins.versions) {
    for (const [name, pin] of Object.entries(pins.files)) {
      if (!['dummy.xml', 'dummy.bin'].includes(name) || !Number.isInteger(pin.bytes) || pin.bytes < 1 || pin.bytes > 65536)
        throw Error('OpenVINO fixture pin differs.');
      const body = await read(path.join(directory, pins.modelName, version, name), pin.bytes);
      if (body.length !== pin.bytes || crypto.createHash('sha256').update(body).digest('hex') !== pin.sha256)
        throw Error('OpenVINO packaged fixture hash differs.');
    }
  }
}

async function boundedResponse(response) {
  const chunks = []; let size = 0;
  for await (const chunk of response.body) {
    size += chunk.byteLength;
    if (size > 65536) throw Error('OpenVINO startup response exceeds its bound.');
    chunks.push(Buffer.from(chunk));
  }
  return Buffer.concat(chunks);
}

async function verifyNative(pins, signal, probe = fetch) {
  async function request(suffix) {
    signal.throwIfAborted();
    const response = await probe(endpoint + suffix, { redirect: 'error',
      signal: AbortSignal.any([signal, AbortSignal.timeout(3000)]) });
    const body = await boundedResponse(response);
    return { response, body };
  }
  const deadline = Date.now() + 120000;
  while (true) {
    let observed;
    try { observed = await request('/v2/health/ready'); }
    catch (error) {
      signal.throwIfAborted();
      // The owned runtime has demonstrated connection resets during its Python
      // startup. Only this bounded health phase may poll those transport states.
      if (!['ECONNREFUSED', 'ECONNRESET', 'UND_ERR_SOCKET'].includes(error.cause?.code || error.code)) throw error;
    }
    if (observed?.response.status === 200) break;
    if (observed && observed.response.status !== 503) throw Error('OpenVINO startup health response differs.');
    if (Date.now() >= deadline) throw Error('OpenVINO native startup deadline expired.');
    await delay(100, undefined, { signal });
  }
  async function json(suffix) {
    const { response, body } = await request(suffix);
    if (response.status !== 200 || !/^application\/json(?:;|$)/i.test(response.headers.get('content-type') || ''))
      throw Error('OpenVINO startup metadata response differs.');
    return JSON.parse(new TextDecoder('utf-8', { fatal: true }).decode(body));
  }
  const server = await json('/v2');
  if (server.name !== 'OpenVINO Model Server' || server.version !== pins.serverVersion)
    throw Error('OpenVINO native server identity differs.');
  for (const version of pins.versions) {
    const suffix = `/v2/models/${pins.modelName}/versions/${version}`;
    if ((await request(suffix + '/ready')).response.status !== 200) throw Error('OpenVINO model version is not ready.');
    const model = await json(suffix);
    if (model.name !== pins.modelName || model.platform !== 'OpenVINO' ||
        JSON.stringify(model.versions) !== JSON.stringify(pins.versions) ||
        !isDeepStrictEqual(model.inputs, [pins.input]) ||
        !isDeepStrictEqual(model.outputs, [pins.output]))
      throw Error('OpenVINO native model metadata differs.');
  }
}

async function serviceConfiguration(environment, pins, directory = '/models') {
  const expected = configuration(environment, pins);
  await verifyModels(directory, expected, pins);
  const stat = await fs.lstat(loader);
  if (!stat.isFile() || stat.isSymbolicLink() || !(stat.mode & 0o111)) throw Error('Pinned OpenVINO loader is absent.');
  return { command: loader, args: ['--library-path',
    `/ovms/lib:${root}/usr/lib/x86_64-linux-gnu:${root}/lib/x86_64-linux-gnu`, '/ovms/bin/ovms',
    '--config_path', '/models/config.json', '--rest_port', '28000', '--port', '29000', '--rest_bind_address', '127.0.0.1',
    '--grpc_bind_address', '127.0.0.1'],
  env: { PYTHONHOME: `${root}/usr`, PYTHONPATH: '/ovms/lib/python:/ovms/python_deps',
    ESPEAK_DATA_PATH: '/ovms/share/espeak-ng-data' }, check: signal => verifyNative(pins, signal) };
}
module.exports = { modelConfiguration, configuration, verifyModels, verifyNative, serviceConfiguration };
