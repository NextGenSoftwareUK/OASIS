/* Run only in the explicit network-isolated cached CSS probe container. */
'use strict';
const fs = require('node:fs/promises');
const { spawn } = require('node:child_process');
const { captureBundle, prepare, verify } = require('/owned/runtime/host-sandbox-solid.cjs');
const { Parser } = require('/community-server/node_modules/n3');

async function main() {
  const bundle = captureBundle(await fs.readFile('/owned/source-bundle.json', 'utf8'));
  const source = await prepare({ bundle, directory: '/proof/source', runtime: '/community-server' });
  const abort = new AbortController();
  const child = spawn(source.command, source.args, { env: source.env, cwd: source.cwd, stdio: 'inherit' });
  let finished = false;
  const terminal = new Promise(resolve => {
    const finish = code => { if (!finished) { finished = true; abort.abort(); resolve(code); } };
    child.once('error', () => finish(1));
    child.once('exit', code => finish(code ?? 1));
  });
  const stop = () => abort.abort();
  process.on('SIGTERM', stop);
  process.on('SIGINT', stop);
  try {
    await verify(source, abort.signal, Parser);
    console.log(JSON.stringify({ nativeCssVersion: source.version, node: process.version, records: bundle.records.length,
      fresh: source.fresh, digest: bundle.digest, nativeRdfVerified: true, readOnlyPermissionsVerified: true }));
  } finally {
    abort.abort();
    process.removeListener('SIGTERM', stop);
    process.removeListener('SIGINT', stop);
    if (!finished) child.kill('SIGTERM');
    let timer;
    await Promise.race([terminal, new Promise(resolve => { timer = setTimeout(resolve, 5000); })]);
    clearTimeout(timer);
    if (!finished) child.kill('SIGKILL');
    let finalTimer;
    try { await Promise.race([terminal, new Promise((_, reject) => {
      finalTimer = setTimeout(() => reject(Error('Native CSS process teardown unconfirmed.')), 5000);
    })]); } finally { clearTimeout(finalTimer); }
  }
}
main().catch(error => { console.error(error.message); process.exitCode = 1; });
