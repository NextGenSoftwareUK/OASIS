'use strict';
const test = require('node:test');
const assert = require('node:assert/strict');
const runtime = require('../../Docker/host-sandbox-openvino.cjs');
const pins = require('./openvino-model-server-pins.json');
const owner = 'a5808c3e-7031-47e7-92de-48a659568f1b';
const environment = { OASIS_SANDBOX_OPENVINO: 'true', CQL_OPENVINO_MODEL_QUERY_SOURCE_OWNER: owner };
function nativeResponse(uri) {
  if (uri.endsWith('/ready')) return new Response('', { status: 200 });
  const body = uri.endsWith('/v2') ? { name: 'OpenVINO Model Server', version: pins.serverVersion } : {
    name: pins.modelName, platform: 'OpenVINO', versions: pins.versions,
    inputs: [{ shape: pins.input.shape, datatype: pins.input.datatype, name: pins.input.name }], outputs: [pins.output] };
  return Response.json(body);
}
test('requires explicit source identity before native launch', () => {
  for (const changed of [{ OASIS_SANDBOX_OPENVINO: 'false' }, { CQL_OPENVINO_MODEL_QUERY_SOURCE_OWNER: '' },
    { CQL_OPENVINO_MODEL_QUERY_SOURCE_OWNER: '00000000-0000-0000-0000-000000000000' }])
    assert.throws(() => runtime.configuration({ ...environment, ...changed }, pins));
  assert.equal(runtime.configuration(environment, pins).model_config_list[0].config.base_path, '/models/cosmic_dummy');
});
test('checks exact server and all version identities once after health', async () => {
  const calls = [];
  await runtime.verifyNative(pins, new AbortController().signal, async uri => { calls.push(uri); return nativeResponse(uri); });
  assert.equal(calls.length, 8);
  assert.equal(new Set(calls).size, 8);
  assert(calls.every(uri => uri.startsWith('http://127.0.0.1:28000/')));
});
test('wrong native build fails without metadata retry', async () => {
  const calls = [];
  await assert.rejects(runtime.verifyNative(pins, new AbortController().signal, async uri => {
    calls.push(uri);
    return uri.endsWith('/v2') ? Response.json({ name: 'OpenVINO Model Server', version: 'wrong' }) : nativeResponse(uri);
  }), /identity differs/);
  assert.equal(calls.length, 2);
});
test('bounded startup accepts observed refusal/reset before ready, without replaying metadata', async () => {
  let health = 0; const metadata = [];
  await runtime.verifyNative(pins, new AbortController().signal, async uri => {
    if (uri.endsWith('/health/ready')) {
      health++;
      if (health <= 2) throw Object.assign(Error('owned server starting'), { cause: { code: health === 1 ? 'ECONNREFUSED' : 'ECONNRESET' } });
      if (health === 3) return new Response('', { status: 503 });
    } else metadata.push(uri);
    return nativeResponse(uri);
  });
  assert.equal(health, 4); assert.equal(metadata.length, 7); assert.equal(new Set(metadata).size, 7);
});
test('oversized response fails before accepting native identity', async () => {
  let calls = 0;
  await assert.rejects(runtime.verifyNative(pins, new AbortController().signal, async () => {
    calls++; return new Response('x'.repeat(65537));
  }), /exceeds its bound/);
  assert.equal(calls, 1);
});
test('cancelled startup never reaches transport', async () => {
  const controller = new AbortController(); controller.abort(); let calls = 0;
  await assert.rejects(runtime.verifyNative(pins, controller.signal, async () => { calls++; return nativeResponse(''); }));
  assert.equal(calls, 0);
});
test('unready version fails without metadata or startup replay', async () => {
  const calls = [];
  await assert.rejects(runtime.verifyNative(pins, new AbortController().signal, async uri => {
    calls.push(uri); return uri.endsWith('/versions/2/ready') ? new Response('', { status: 503 }) : nativeResponse(uri);
  }), /version is not ready/);
  assert.equal(calls.length, 5);
});
