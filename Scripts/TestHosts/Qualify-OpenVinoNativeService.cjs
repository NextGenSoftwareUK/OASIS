'use strict';
// Cloud-only real service-command qualification. This does not stand in for the API.
const { spawn } = require('node:child_process');
const { serviceConfiguration } = require('/app/host-sandbox-openvino.cjs');
const pins = require('/app/openvino-model-server-pins.json');
if (process.env.GITHUB_ACTIONS !== 'true') throw Error('Owned cloud qualification only.');

async function main() {
  const service = await serviceConfiguration({ OASIS_SANDBOX_OPENVINO: 'true',
    CQL_OPENVINO_MODEL_QUERY_SOURCE_OWNER: 'a5808c3e-7031-47e7-92de-48a659568f1b' }, pins);
  const controller = new AbortController();
  let stop;
  const stopping = new Promise(resolve => { stop = resolve; });
  const signal = () => { controller.abort(); stop(); };
  process.once('SIGTERM', signal); process.once('SIGINT', signal);
  const child = spawn(service.command, service.args, { env: service.env, stdio: 'inherit' });
  let ended = false;
  const terminal = new Promise(resolve => {
    child.once('error', error => { ended = true; resolve({ error }); });
    child.once('exit', (code, signal) => { ended = true; resolve({ code, signal }); });
  });
  try {
    const ready = service.check(controller.signal);
    try {
      await Promise.race([ready, terminal.then(result => { throw result.error || Error('Native service exited before readiness.'); }),
        stopping.then(() => { throw Error('Qualification interrupted before readiness.'); })]);
    } finally {
      if (controller.signal.aborted || ended) controller.abort();
      // Check owns no detached health work on exit or interruption.
      if (controller.signal.aborted) await Promise.allSettled([ready]);
    }
    console.log(JSON.stringify({ nativeServiceReady: true, loopbackOnly: true, apiQualified: false }));
    await Promise.race([stopping, terminal.then(result => { throw result.error || Error('Native service exited before owned stop.'); })]);
  } finally {
    controller.abort();
    process.removeListener('SIGTERM', signal); process.removeListener('SIGINT', signal);
    if (!ended) child.kill('SIGTERM');
    let timer;
    try {
      const result = await Promise.race([terminal, new Promise((_, reject) => {
        timer = setTimeout(() => { if (!ended) child.kill('SIGKILL'); reject(Error('Native service stop exceeded its bound.')); }, 10000);
      })]);
      if (result.error) throw result.error;
      console.log(JSON.stringify({ nativeServiceStopped: true, result, apiQualified: false }));
    } finally { clearTimeout(timer); }
  }
}
main().catch(error => { console.error(error); process.exitCode = 1; });
