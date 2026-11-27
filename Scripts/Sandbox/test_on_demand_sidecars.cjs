'use strict';
const test = require('node:test');
const assert = require('node:assert');
const { EventEmitter } = require('node:events');
const { setTimeout: delay } = require('node:timers/promises');
const { superviseOnDemand, settings } = require('../../Docker/host-sandbox-ondemand.cjs');

test('starts a sidecar only when asked, stops it when idle and restarts it', async () => {
  const launched = [], signals = new EventEmitter();
  let environment, clock = 0;
  const launch = (command, args, options) => {
    const child = new EventEmitter();
    child.kill = () => setImmediate(() => child.emit('exit', 0));
    launched.push(command);
    if (command === 'web4') environment = options.env;
    return child;
  };
  const source = command => ({ command, args: [], check: async () => {} });
  const done = superviseOnDemand({ groups: { DaprOASIS: [source('garnet'), source('daprd')], GUNOASIS: [source('gun')] },
    idleMilliseconds: 1000, sweepMilliseconds: 5, stopMilliseconds: 100, now: () => clock, launch, signals,
    api: { command: 'web4', args: [] }, environment: {} });
  while (!environment) await delay(5);
  assert.deepStrictEqual(launched, ['web4']);
  const ensure = async name => (await fetch(environment.OASIS_SIDECAR_CONTROL + 'ensure/' + name, { method: 'POST' })).status;
  assert.strictEqual(await ensure('MongoDBOASIS'), 204);
  assert.deepStrictEqual(await Promise.all([ensure('DaprOASIS'), ensure('DaprOASIS')]), [204, 204]);
  assert.deepStrictEqual(launched, ['web4', 'garnet', 'daprd']);
  const call = async path => fetch(environment.OASIS_SIDECAR_CONTROL + path, { method: 'POST' });
  const state = async name => (await (await call('status')).json()).find(item => item.name === name).state;
  clock = 5000; await delay(40);
  assert.strictEqual(await state('DaprOASIS'), 'running', 'a leased sidecar is never stopped');
  await call('release/DaprOASIS'); await call('release/DaprOASIS');
  clock = 10000; await delay(40);
  assert.strictEqual(await state('DaprOASIS'), 'stopped');
  assert.strictEqual(await ensure('DaprOASIS'), 204);
  assert.deepStrictEqual(launched, ['web4', 'garnet', 'daprd', 'garnet', 'daprd']);
  signals.emit('SIGTERM');
  assert.strictEqual(await done, 0);
});

test('restarts after a crash and keeps an always-on sidecar running', async () => {
  const children = [], signals = new EventEmitter(), lines = [];
  let environment, clock = 0;
  const launch = (command, args, options) => {
    const child = new EventEmitter();
    child.kill = () => setImmediate(() => child.emit('exit', 0));
    child.command = command;
    children.push(child);
    if (command === 'web4') environment = options.env;
    return child;
  };
  const done = superviseOnDemand({ groups: { GUNOASIS: [{ command: 'gun', args: [], check: async () => {} }] },
    alwaysOn: ['GUNOASIS'], idleMilliseconds: 1000, sweepMilliseconds: 5, now: () => clock, launch, signals,
    api: { command: 'web4', args: [] }, environment: {}, log: line => lines.push(line) });
  while (!environment) await delay(5);
  clock = 5000; await delay(30);
  assert.deepStrictEqual(children.map(child => child.command), ['gun', 'web4']);
  children[0].emit('exit', 1); await delay(40);
  assert.deepStrictEqual(children.map(child => child.command), ['gun', 'web4', 'gun']);
  assert.ok(lines.some(line => line.startsWith('SIDECAR crash name=GUNOASIS')));
  signals.emit('SIGTERM');
  assert.strictEqual(await done, 0);
});

test('settings default to off with a ten minute idle period and a two minute floor', () => {
  assert.deepStrictEqual(settings('{}'), { onDemand: false, idleMilliseconds: 600000, alwaysOn: [] });
  assert.throws(() => settings('{"OASIS":{"Sandbox":{"Sidecars":{"IdleMinutes":1}}}}'), /at least 2/);
});
