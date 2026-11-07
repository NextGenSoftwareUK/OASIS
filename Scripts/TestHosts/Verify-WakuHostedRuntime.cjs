'use strict';
const fs = require('node:fs/promises');
const path = require('node:path');
const crypto = require('node:crypto');
const { spawn } = require('node:child_process');
const { serviceConfiguration } = require('../../Docker/host-sandbox-waku.cjs');

async function startAndStop(service, logPath) {
  const log = await fs.open(logPath, 'wx', 0o600);
  const controller = new AbortController();
  const child = spawn(service.command, service.args, { cwd: service.cwd, env: service.env,
    stdio: ['ignore', log.fd, log.fd] });
  const closed = new Promise(resolve => {
    child.once('error', error => resolve({ error }));
    child.once('exit', (code, signal) => resolve({ code, signal }));
  });
  const readiness = service.check(controller.signal);
  try {
    await Promise.race([readiness, closed.then(() => { throw Error('Owned Waku exited before readiness.'); })]);
  } finally {
    controller.abort(Error('Owned Waku qualification ended.'));
    await Promise.allSettled([readiness]);
    child.kill('SIGTERM');
    let timer;
    try {
      await Promise.race([closed, new Promise((_, reject) => {
        timer = setTimeout(() => reject(Error('Owned Waku did not stop gracefully.')), 10000);
      })]);
    } catch (error) {
      child.kill('SIGKILL');
      await closed;
      throw error;
    } finally { clearTimeout(timer); await log.close(); }
  }
}

async function main() {
  const [binaryText, sourceText, workspaceText] = process.argv.slice(2);
  if (!binaryText || !sourceText || !workspaceText) throw Error('Supply the native binary, original source and owned workspace.');
  const binary = await fs.realpath(binaryText);
  const source = await fs.realpath(sourceText);
  const workspace = await fs.realpath(workspaceText);
  const original = await fs.readFile(path.join(source, 'store-qualified.sqlite3'));
  const receipt = JSON.parse(await fs.readFile(path.join(source, 'archive-qualified.json'), 'utf8'));
  const digest = crypto.createHash('sha256').update(original).digest('hex');
  const environment = {
    OASIS_SANDBOX_WAKU: 'true', PATH: process.env.PATH,
    OASIS_SANDBOX_WAKU_BOOTSTRAP_JSON: JSON.stringify({ schema: 1, archiveBase64: original.toString('base64'), receipt }),
    CQL_WAKU_STORE_QUERY_SOURCE_JSON: JSON.stringify({ sourceOwner: 'b05b1234-1000-4000-8000-000000000001',
      messageHashes: receipt.messageHashes })
  };
  const directory = path.join(workspace, 'native');
  const service = await serviceConfiguration(environment, directory, binary);
  await startAndStop(service, path.join(workspace, 'first-start.log'));
  const acknowledgement = await fs.readFile(path.join(directory, 'bootstrap.acknowledged.json'));
  const restarted = await serviceConfiguration(environment, directory, binary);
  await startAndStop(restarted, path.join(workspace, 'restart.log'));
  if (!(await fs.readFile(path.join(directory, 'bootstrap.acknowledged.json'))).equals(acknowledgement) ||
      !(await fs.readFile(path.join(source, 'store-qualified.sqlite3'))).equals(original))
    throw Error('Waku original archive or provisioning acknowledgement changed.');
  const result = { archiveSha256: digest, originalArchiveBytes: original.length, nativeRows: 3,
    nativeStarts: 2, originalSourceUnchanged: true, acknowledgementUnchanged: true };
  await fs.writeFile(path.join(workspace, 'qualification.json'), JSON.stringify(result, null, 2), { flag: 'wx' });
  console.log(JSON.stringify(result));
}

main().catch(error => { console.error(error); process.exitCode = 1; });
