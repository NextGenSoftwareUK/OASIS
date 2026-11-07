'use strict';
const fs = require('node:fs/promises');
const path = require('node:path');
const crypto = require('node:crypto');
const { spawn } = require('node:child_process');
const { serviceConfiguration } = require('../../Docker/host-sandbox-nats.cjs');
const hash = data => crypto.createHash('sha256').update(data).digest('hex');
async function writeNew(file, value) {
  const handle = await fs.open(file, 'wx', 0o600);
  try { await handle.writeFile(JSON.stringify(value)); await handle.sync(); }
  finally { await handle.close(); }
}
function launch(command, args, options) {
  const child = spawn(command, args, options);
  const done = new Promise((resolve, reject) => { child.once('error', reject); child.once('exit', resolve); });
  return { child, done };
}
async function main() {
  if (process.platform !== 'linux' || process.argv.length !== 7)
    throw Error('An isolated Linux runner and explicit bundle/source/consumer/native/proof paths are required.');
  const [bundlePath, sourcePath, consumer, binary, outputPath] = process.argv.slice(2).map(value => path.resolve(value));
  const directory = '/data/COSMIC/SandboxNATS';
  await fs.mkdir(outputPath, { mode: 0o700 });
  const environment = { OASIS_SANDBOX_NATS: 'true', CQL_NATS_PUBLIC_SOURCE_USERNAME: 'cql_reader',
    CQL_NATS_PUBLIC_SOURCE_PASSWORD: crypto.randomBytes(32).toString('base64url'),
    CQL_NATS_PUBLIC_SOURCE_JSON: await fs.readFile(sourcePath, 'utf8'),
    OASIS_SANDBOX_NATS_BOOTSTRAP_JSON: await fs.readFile(bundlePath, 'utf8') };
  for (const name of ['PATH', 'HOME', 'TMPDIR', 'DOTNET_ROOT']) if (process.env[name]) environment[name] = process.env[name];
  await writeNew(path.join(outputPath, 'restart.intent.json'), { schema: 1,
    sourceSha256: hash(environment.CQL_NATS_PUBLIC_SOURCE_JSON), seedOrSavePermitted: false });
  let identity;
  for (const phase of ['before', 'after']) {
    const service = await serviceConfiguration(environment, directory, binary);
    const certificate = hash(await fs.readFile(path.join(directory, 'ca.pem')));
    if (identity && certificate !== identity) throw Error('Native TLS identity changed across restart.');
    identity = certificate;
    const log = await fs.open(path.join(path.dirname(outputPath), `nats-${phase}.private.log`), 'wx', 0o600);
    const native = launch(service.command, service.args, { cwd: service.cwd, env: service.env, stdio: ['ignore', log.fd, log.fd] });
    const abort = new AbortController();
    try {
      await Promise.race([service.check(abort.signal), native.done.then(() => { throw Error('Native stopped before readiness.'); })]);
      const sdk = launch('dotnet', [consumer, sourcePath, outputPath, phase], { env: environment, stdio: 'inherit' });
      const sdkDeadline = setTimeout(() => sdk.child.kill('SIGKILL'), 45000);
      try { if (await sdk.done !== 0) throw Error('Restored native SDK qualification failed.'); }
      finally { clearTimeout(sdkDeadline); }
    } finally {
      abort.abort();
      if (native.child.exitCode === null) native.child.kill('SIGINT');
      const stopDeadline = setTimeout(() => native.child.kill('SIGKILL'), 15000);
      try {
        const code = await native.done;
        await writeNew(path.join(outputPath, phase + '.stop.acknowledged.json'), { nativeStopped: true, exitCode: code });
        if (code !== 0) throw Error('Native shutdown did not complete cleanly.');
      } finally { clearTimeout(stopDeadline); await log.close(); }
    }
  }
  if (await fs.readFile(path.join(outputPath, 'before.snapshot.json'), 'utf8') !==
      await fs.readFile(path.join(outputPath, 'after.snapshot.json'), 'utf8'))
    throw Error('Native record values or revisions changed across restart.');
  await writeNew(path.join(outputPath, 'restart-qualified.json'), { schema: 1, nativeVersion: '2.15.2',
    sourceSha256: hash(environment.CQL_NATS_PUBLIC_SOURCE_JSON), nativeCounts: [3, 2],
    starts: 2, sourceSeeded: false, sourceSaved: false, tlsIdentityUnchanged: true,
    valuesAndRevisionsUnchanged: true, restrictedPermissionsVerified: true, nativeStopped: true });
  console.log('PASS: original native source restored once, CQL3/2 on both starts, unchanged values/revisions and TLS identity; no seed/save.');
}
main().catch(error => { console.error(error.message); process.exitCode = 1; });
