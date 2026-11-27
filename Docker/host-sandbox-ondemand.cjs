/* Sidecars start when WEB4 asks for their source and stop after an idle period.
   Enabled by OASIS.Sandbox.Sidecars.OnDemand in the DNA; otherwise the supervisor starts everything up front.
   Every lifecycle log line starts with "SIDECAR". */
'use strict';
const http = require('node:http');
const crypto = require('node:crypto');
const { spawn } = require('node:child_process');
const { readFileSync } = require('node:fs');

// Resident memory of the group's processes (Linux), for logs and the status endpoint.
function memoryMegabytes(children) {
  let kilobytes = 0;
  for (const owned of children) {
    try { kilobytes += Number(/VmRSS:\s+(\d+)/.exec(readFileSync(`/proc/${owned.process.pid}/status`, 'utf8'))?.[1] ?? 0); }
    catch { /* The process has exited, or this host has no /proc. */ }
  }
  return Math.round(kilobytes / 1024);
}

function settings(dnaText) {
  const value = JSON.parse(dnaText || '{}')?.OASIS?.Sandbox?.Sidecars ?? {};
  const minutes = value.IdleMinutes ?? 10;
  if (!Number.isInteger(minutes) || minutes < 2) throw Error('OASIS.Sandbox.Sidecars.IdleMinutes must be a whole number of at least 2.');
  return { onDemand: value.OnDemand === true, idleMilliseconds: minutes * 60000,
    alwaysOn: String(value.AlwaysOn ?? '').split(',').map(name => name.trim()).filter(Boolean) };
}

async function superviseOnDemand({ groups, idleMilliseconds, launch = spawn, signals = process, stopMilliseconds = 10000,
  sweepMilliseconds = 30000, now = Date.now, api = { command: 'dotnet', args: ['NextGenSoftware.OASIS.API.ONODE.WebAPI.dll'] },
  environment = process.env, alwaysOn = [], log = line => console.log(line) }) {
  const token = crypto.randomBytes(24).toString('hex');
  const shutdown = new AbortController();
  const state = new Map(Object.entries(groups).map(([name, services]) =>
    [name, { name, services, children: [], running: false, starting: false, active: 0, startedAt: 0, lastUsed: 0,
      queue: Promise.resolve() }]));
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
  async function stop(group, reason) {
    if (group.running && reason) log(`SIDECAR stop name=${group.name} reason=${reason} memoryFreedMb=${memoryMegabytes(group.children)}`);
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
  async function ensure(group, reason) {
    group.lastUsed = now();
    if (group.running && group.children.every(owned => !owned.finished)) return;
    if (group.running) { log(`SIDECAR crash name=${group.name}`); reason = 'crash'; }
    await stop(group);
    shutdown.signal.throwIfAborted();
    const began = Date.now();
    group.starting = true;
    log(`SIDECAR start name=${group.name} reason=${reason}`);
    try {
      for (const source of group.services) {
        const owned = own(source.command, source.args, { stdio: 'inherit', env: source.env, cwd: source.cwd });
        group.children.push(owned);
        const exited = new AbortController();
        owned.done.then(() => exited.abort(Error('Sidecar exited during startup.')));
        await source.check(AbortSignal.any([shutdown.signal, exited.signal]));
        if (owned.finished) throw Error('Sidecar exited during startup.');
      }
    } catch (error) {
      log(`SIDECAR failure name=${group.name} reason=${reason} startupMs=${Date.now() - began} error=${JSON.stringify(error.message)}`);
      await stop(group);
      throw error;
    } finally { group.starting = false; }
    group.running = true;
    group.startedAt = group.lastUsed = now();
    log(`SIDECAR ready name=${group.name} reason=${reason} startupMs=${Date.now() - began} memoryUsedMb=${memoryMegabytes(group.children)}`);
  }
  const status = () => [...state.values()].map(group => {
    const up = group.running && group.children.every(owned => !owned.finished);
    return { name: group.name, state: group.starting ? 'starting' : up ? 'running' : 'stopped', alwaysOn: alwaysOn.includes(group.name),
      activeRequests: group.active, lastUsed: group.lastUsed ? new Date(group.lastUsed).toISOString() : null,
      uptimeSeconds: up ? Math.round((now() - group.startedAt) / 1000) : 0, memoryMb: up ? memoryMegabytes(group.children) : 0 };
  });
  // ensure takes a lease that release returns; a sidecar with a lease is never stopped for being idle.
  const server = http.createServer((request, response) => {
    const match = /^\/([a-f0-9]{48})\/(?:(ensure|release)\/([A-Za-z0-9]+)|status)$/.exec(request.url);
    if (!match || match[1] !== token) { response.writeHead(404).end(); return; }
    if (!match[2]) { response.writeHead(200, { 'content-type': 'application/json' }).end(JSON.stringify(status())); return; }
    const group = state.get(match[3]);
    if (!group) { response.writeHead(204).end(); return; } // This source has no sidecar.
    if (match[2] === 'release') {
      group.active = Math.max(0, group.active - 1);
      group.lastUsed = now();
      response.writeHead(204).end();
      return;
    }
    group.active++;
    serial(group, () => ensure(group, 'first-use')).then(() => response.writeHead(204).end(), () => {
      group.active--;
      response.writeHead(503).end();
    });
  });
  await new Promise((resolve, reject) => { server.once('error', reject); server.listen(0, '127.0.0.1', resolve); });
  const sweep = setInterval(() => {
    for (const group of state.values())
      serial(group, async () => {
        if (alwaysOn.includes(group.name)) {
          await ensure(group, 'always-on').catch(() => {});
          return;
        }
        if (!group.running || group.active > 0 || now() - group.lastUsed < idleMilliseconds) return;
        await stop(group, 'idle');
      });
  }, sweepMilliseconds);
  let interrupt;
  const interrupted = new Promise(resolve => { interrupt = () => resolve(0); });
  signals.on('SIGTERM', interrupt); signals.on('SIGINT', interrupt);
  let web4;
  try {
    for (const group of state.values())
      if (alwaysOn.includes(group.name)) await serial(group, () => ensure(group, 'always-on'));
    web4 = own(api.command, api.args, { stdio: 'inherit',
      env: { ...environment, OASIS_SIDECAR_CONTROL: `http://127.0.0.1:${server.address().port}/${token}/` } });
    return await Promise.race([web4.done, interrupted]);
  } finally {
    signals.removeListener('SIGTERM', interrupt); signals.removeListener('SIGINT', interrupt);
    clearInterval(sweep);
    shutdown.abort();
    server.close(); server.closeAllConnections();
    await Promise.all([...state.values()].map(group => serial(group, () => stop(group, 'shutdown'))));
    if (web4 && !web4.finished) { web4.process.kill('SIGTERM'); await web4.done; }
  }
}

module.exports = { superviseOnDemand, settings };
