'use strict';
const test = require('node:test');
const assert = require('node:assert');
const { EventEmitter } = require('node:events');
const { setTimeout: delay } = require('node:timers/promises');
const { superviseOnDemand } = require('../../Docker/host-sandbox-ondemand.cjs');

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
  clock = 5000; await delay(40);
  assert.strictEqual(await ensure('DaprOASIS'), 204);
  assert.deepStrictEqual(launched, ['web4', 'garnet', 'daprd', 'garnet', 'daprd']);
  signals.emit('SIGTERM');
  assert.strictEqual(await done, 0);
});
