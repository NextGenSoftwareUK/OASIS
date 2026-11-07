'use strict';
const test = require('node:test');
const assert = require('node:assert/strict');
const path = require('node:path');
const runtime = require('../../Docker/DaprRuntime/configuration.cjs');
const root = path.resolve('LocalState/COSMIC/OwnedDaprFixture');
const acl = path.join(root, 'reader.acl');
const password = 'a'.repeat(64);

test('Linux explicitly selects the reviewed native io_uring backend without default or libaio selection', () => {
  assert.deepEqual(runtime.contract.linuxStorageArguments, ['--device-type', 'Native', '--device-io-backend', 'Uring']);
  const args = runtime.garnetArguments(root, acl, 'recover');
  if (process.platform === 'linux') {
    assert.equal(args[args.indexOf('--device-type') + 1], 'Native');
    assert.equal(args[args.indexOf('--device-io-backend') + 1], 'Uring');
    assert.equal(args.filter(value => value === '--device-io-backend').length, 1);
  } else assert.ok(!args.includes('--device-io-backend'));
});
test('AOF recovery requires synchronous durability and strict recovery without invalid tiered LogDir', () => {
  const args = runtime.garnetArguments(root, acl, 'recover');
  for (const flag of ['--aof', '--aof-commit-wait', '--recover', '--fail-on-recovery-error']) assert.ok(args.includes(flag));
  assert.ok(!args.includes('--logdir'));
  assert.equal(args[args.indexOf('--checkpointdir') + 1], path.join(root, 'store'));
});
test('only explicit provisioning can omit recovery and cannot be selected by an unknown mode', () => {
  assert.ok(!runtime.garnetArguments(root, acl, 'provision').includes('--recover'));
  assert.throws(() => runtime.garnetArguments(root, acl, 'auto'));
  assert.throws(() => runtime.garnetArguments('relative', acl, 'recover'));
});
test('shipping reader ACL has no active default writer or mutation grant', () => {
  const acl = runtime.readerAcl(password);
  assert.ok(acl.startsWith('user default off\n'));
  assert.ok(!acl.includes('+@all'));
  assert.ok(acl.includes('user reader on'));
});
test('credentials cannot inject ACL or YAML instructions', () => {
  for (const value of ['', 'a'.repeat(63), 'a'.repeat(64) + '\n', 'token >writer +@all']) {
    assert.throws(() => runtime.readerAcl(value));
    assert.throws(() => runtime.component(value, 'reader'));
  }
  assert.throws(() => runtime.component(password, 'unreviewed'));
});
test('sidecar application listeners are explicitly loopback and ports match native qualification', () => {
  const args = runtime.daprArguments(root);
  assert.equal(args[args.indexOf('--dapr-listen-addresses') + 1], '127.0.0.1');
  assert.equal(args[args.indexOf('--dapr-grpc-port') + 1], String(runtime.contract.ports.grpc));
  assert.ok(runtime.component(password, 'reader').includes(`127.0.0.1:${runtime.contract.ports.redis}`));
});

test('reader credentials cannot authenticate the separate provisioning writer', () => {
  assert.throws(() => runtime.writerAcl(password, password));
  const acl = runtime.writerAcl('b'.repeat(64), password);
  assert.ok(acl.includes('user default on >' + 'b'.repeat(64)));
  assert.ok(acl.includes('user reader on >' + password));
});
