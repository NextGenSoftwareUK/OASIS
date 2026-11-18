'use strict';
const fs = require('node:fs');
const path = require('node:path');
const browserMode = process.env.OASIS_PROFILE_BROWSER_ENABLED === 'true';
const { verify } = require(browserMode ? './profile-browser-native-http.cjs' : './profile-sdk-native-http.cjs');
const root = fs.realpathSync(process.env.OASIS_PROFILE_SDK_HANDOFF_ROOT);
const parent = fs.realpathSync(process.env.RUNNER_TEMP);
if (!root.startsWith(parent + path.sep) || !path.basename(root).startsWith('oasis-profile-sdk-'))
  throw new Error('An owned CI handoff directory is required.');
const claimed = new Set();
let completed = 0;
let failed = false;
const deadline = setTimeout(() => {
  console.error('SDK handoff timed out before all six owned cases completed.');
  process.exitCode = 1; watcher.close(); process.exit(1);
}, 240000);
async function consume(name) {
  if (!/^[0-9a-f]{32}\.request\.json$/.test(name) || claimed.has(name)) return;
  claimed.add(name);
  const stem = name.slice(0, -'.request.json'.length);
  const response = path.join(root, stem + '.response.json');
  let result;
  try {
    const input = fs.readFileSync(path.join(root, name), 'utf8');
    if (Buffer.byteLength(input) > 8192) throw new Error('Bounded SDK handoff input required.');
    result = await verify(JSON.parse(input));
    if (browserMode) console.log(JSON.stringify({
      profileBrowserVerified: result.browserVerified, openMs: result.openMs,
      saveMs: result.saveMs, profileWrites: result.profileWrites
    }));
  } catch (error) {
    failed = true; console.error(error.message);
    result = { isError: true, message: 'The actual SDK/native HTTP check failed.' };
  }
  fs.writeFileSync(response + '.tmp', JSON.stringify(result), { flag: 'wx', mode: 0o600 });
  fs.renameSync(response + '.tmp', response);
  completed++;
  if (completed === 6) {
    clearTimeout(deadline); watcher.close();
    process.exitCode = failed ? 1 : 0;
    console.log(JSON.stringify({ completed, failed }));
  }
}
const watcher = fs.watch(root, (_, filename) => {
  if (filename) consume(String(filename)).catch(error => {
    console.error(error.message); process.exitCode = 1; clearTimeout(deadline); watcher.close();
  });
});
for (const name of fs.readdirSync(root))
  consume(name).catch(error => {
    console.error(error.message); process.exitCode = 1; clearTimeout(deadline); watcher.close();
  });
