/* Sidecars start when WEB4 asks for their source and stop after an idle period.
   Enabled by OASIS_SANDBOX_IDLE_MINUTES; without it the supervisor starts everything up front. */
'use strict';
const http = require('node:http');
const crypto = require('node:crypto');
const { spawn } = require('node:child_process');

async function superviseOnDemand({ groups, idleMilliseconds, launch = spawn, signals = process, stopMilliseconds = 10000,
  sweepMilliseconds = 30000, now = Date.now, api = { command: 'dotnet', args: ['NextGenSoftware.OASIS.API.ONODE.WebAPI.dll'] },
  environment = process.env }) {
  const token = crypto.randomBytes(24).toString('hex');
  const shutdown = new AbortController();
  const state = new Map(Object.entries(groups).map(([name, services]) =>
    [name, { name, services, children: [], running: false, lastUsed: 0, queue: Promise.resolve() }]));
  // One lifecycle operation per group at a time: a start never overlaps a stop.
  const serial = (group, operation) => {
    const result = group.queue.then(operation);
    group.queue = result.catch(() => {});
    return result;
  };
  function own(command, args, options) {
    const process = launch(command, args, options);
    const owned = { process, finished: false };
    owned.done = new Promise(resolve => {
      const finish = code => { if (!owned.finished) { owned.finished = true; resolve(code); } };
      process.once('error', () => finish(1));
      process.once('exit', code => finish(code ?? 1));
    });
    return owned;
  }
  async function stop(group) {
    const children = group.children.splice(0);
    group.running = false;
    // Dependents stop before what they depend on (daprd before its state store).
    for (const owned of children.reverse()) {
      if (owned.finished) continue;
      owned.process.kill('SIGTERM');
      const timer = setTimeout(() => owned.process.kill('SIGKILL'), stopMilliseconds);
      await owned.done;
      clearTimeout(timer);
    }
  }
  async function ensure(group) {
    group.lastUsed = now();
    if (group.running && group.children.every(owned => !owned.finished)) return;
    await stop(group);
    shutdown.signal.throwIfAborted();
    try {
      for (const source of group.services) {
        const owned = own(source.command, source.args, { stdio: 'inherit', env: source.env, cwd: source.cwd });
        group.children.push(owned);
        const exited = new AbortController();
        owned.done.then(() => exited.abort(Error('Sidecar exited during startup.')));
        await source.check(AbortSignal.any([shutdown.signal, exited.signal]));
        if (owned.finished) throw Error('Sidecar exited during startup.');
      }
    } catch (error) { await stop(group); throw error; }
    group.running = true;
    group.lastUsed = now();
    console.log(`Sidecar ${group.name} started on demand.`);
  }
  const server = http.createServer((request, response) => {
    const match = /^\/([a-f0-9]{48})\/ensure\/([A-Za-z0-9]+)$/.exec(request.url);
    if (request.method !== 'POST' || !match || match[1] !== token) { response.writeHead(404).end(); return; }
    const group = state.get(match[2]);
    if (!group) { response.writeHead(204).end(); return; } // This source has no sidecar.
    serial(group, () => ensure(group)).then(() => response.writeHead(204).end(), error => {
      console.error(`Sidecar ${group.name} failed to start: ${error.message}`);
      response.writeHead(503).end();
    });
  });
  await new Promise((resolve, reject) => { server.once('error', reject); server.listen(0, '127.0.0.1', resolve); });
  const sweep = setInterval(() => {
    for (const group of state.values())
      serial(group, async () => {
        if (!group.running || now() - group.lastUsed < idleMilliseconds) return;
        await stop(group);
        console.log(`Sidecar ${group.name} stopped after being idle.`);
      });
  }, sweepMilliseconds);
  let interrupt;
  const interrupted = new Promise(resolve => { interrupt = () => resolve(0); });
  signals.on('SIGTERM', interrupt); signals.on('SIGINT', interrupt);
  let web4;
  try {
    web4 = own(api.command, api.args, { stdio: 'inherit',
      env: { ...environment, OASIS_SIDECAR_CONTROL: `http://127.0.0.1:${server.address().port}/${token}/` } });
    return await Promise.race([web4.done, interrupted]);
  } finally {
    signals.removeListener('SIGTERM', interrupt); signals.removeListener('SIGINT', interrupt);
    clearInterval(sweep);
    shutdown.abort();
    server.close(); server.closeAllConnections();
    await Promise.all([...state.values()].map(group => serial(group, () => stop(group))));
    if (web4 && !web4.finished) { web4.process.kill('SIGTERM'); await web4.done; }
  }
}

module.exports = { superviseOnDemand };
