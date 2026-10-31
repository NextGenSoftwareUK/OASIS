// Brings every DNA copy up to the current schema (Default/OASIS_DNA.json) without changing existing values: missing sections and
// keys are inserted as text (comments survive); a null section is filled with its defaults. Secrets are never printed.
//
//   node Scripts/DnaSync/sync-dna-copies.js            dry run: what would be added to each copy
//   node Scripts/DnaSync/sync-dna-copies.js --apply    write the changes
//
// Run from the repo root, which is linked to the Railway project (railway link). Railway variables are written with
// --skip-deploys, so nothing redeploys. Dated .bak copies are kept beside the backup files. Set OASIS_DNA_BACKUP_DIR to the
// folder that holds DEV/OASIS_DNA.json and LIVE/OASIS_DNA.json (default C:/Source/BACKUP/BACKUP).
const fs = require('fs'), cp = require('child_process'), crypto = require('crypto'), j = require('./jsonc'), { merge } = require('./merge');
const BACKUP_DIR = process.env.OASIS_DNA_BACKUP_DIR || 'C:/Source/BACKUP/BACKUP';
const APPLY = process.argv.includes('--apply');
const tplText = fs.readFileSync(require('path').join(__dirname, '../../OASIS Architecture/NextGenSoftware.OASIS.API.DNA/Default/OASIS_DNA.json'), 'utf8');
const tpl = j.parse(tplText);
const flat = (o, p = '', a = {}) => { if (o && typeof o === 'object' && !Array.isArray(o)) { for (const k in o) flat(o[k], p + '/' + k, a); } else a[p] = o; return a; };
const railVars = s => JSON.parse(cp.execSync(`railway variables --service "${s}" --json`, { encoding: 'utf8', maxBuffer: 1 << 26 }));

const w4 = railVars('WEB4 OASIS API');
const devKey = j.parse(w4.OASIS_DNA_JSON).OASIS.NativeImport.ConnectionEncryptionKey;
if (!devKey || devKey.length < 32) throw new Error('dev connection key missing on WEB4');
const liveKey = crypto.randomBytes(48).toString('base64');
const GRANT_PUB = 'OASIS_OFFLINE_GRANT_SIGNING_PUBLIC_KEY';

const overridesFor = key => ({
  '/OASIS/NativeImport/ConnectionEncryptionKey': key,
  '/OASIS/OfflineSessionGrants/SigningPublicKeyEnvironmentVariable': GRANT_PUB,
});

function check(name, before, after) {
  const a = flat(j.parse(before)), b = flat(j.parse(after)), t = flat(tpl);
  const changed = Object.keys(a).filter(p => a[p] !== null && JSON.stringify(a[p]) !== JSON.stringify(b[p]));
  const nullFilled = Object.keys(a).filter(p => a[p] === null && !(p in b));
  const missing = Object.keys(t).filter(p => !(p in b));
  if (changed.length) throw new Error(`${name}: existing values changed: ${changed.slice(0, 5)}`);
  if (missing.length) throw new Error(`${name}: still missing ${missing.length}, e.g. ${missing.slice(0, 3)}`);
  const comments = s => (s.match(/^\s*\/\//gm) || []).length;
  if (comments(before) !== comments(after)) throw new Error(`${name}: comment lines changed`);
  return nullFilled.length;
}

const jobs = [];
// Files (comments kept). Dev copies take the dev key; live gets its own.
const file = (name, path, key) => jobs.push({ name, kind: 'file', path, key });
file('backup DEV', BACKUP_DIR + '/DEV/OASIS_DNA.json', devKey);
file('backup LIVE', BACKUP_DIR + '/LIVE/OASIS_DNA.json', liveKey);
file('local ONODE', 'C:/Source/OASIS/ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI/OASIS_DNA.json', devKey);
file('local STAR WebAPI', 'C:/Source/OASIS/STAR ODK/NextGenSoftware.OASIS.STAR.WebAPI/OASIS_DNA.json', devKey);
file('STAR DNA default', 'C:/Source/OASIS/STAR ODK/NextGenSoftware.OASIS.STAR.STARDNA/DNA/Default/OASIS_DNA.json', '');
// Railway dev services.
for (const s of ['WEB4 OASIS API', 'WEB5 STAR API', 'WEB6 AI API', 'WEB7 SYMBIOTIC API', 'WEB8 IGN API', 'WEB9 SINGULARITIY API', 'WEB10 THE SOURCE API'])
  jobs.push({ name: 'railway dev ' + s, kind: 'railway', service: s, key: devKey });

for (const job of jobs) {
  const before = job.kind === 'file' ? fs.readFileSync(job.path, 'utf8') : (job.service === 'WEB4 OASIS API' ? w4 : railVars(job.service)).OASIS_DNA_JSON;
  const overrides = job.key ? overridesFor(job.key) : { '/OASIS/OfflineSessionGrants/SigningPublicKeyEnvironmentVariable': GRANT_PUB };
  const { text, added } = merge(before, tpl, overrides);
  const nulls = check(job.name, before, text);
  console.log(`${job.name.padEnd(36)} +${String(added.length).padStart(3)} keys/sections (${nulls} null sections filled)${text === before ? ' (already current)' : ''}`);
  if (!APPLY || text === before) continue;
  if (job.kind === 'file') {
    const stamp = job.name.startsWith('backup') ? job.path + '.pre-' + new Date().toISOString().slice(0, 10) + '.bak' : null;
    if (stamp && !fs.existsSync(stamp)) fs.copyFileSync(job.path, stamp);
    fs.writeFileSync(job.path, text);
  } else {
    cp.execSync(`railway variable set OASIS_DNA_JSON --stdin --service "${job.service}" --skip-deploys --json`, { input: (() => { const compact = JSON.stringify(j.parse(text)); if (compact.length > 32000) throw new Error(job.name + ' is ' + compact.length + ' chars, over the Railway limit'); return compact; })(), encoding: 'utf8' });
  }
}
console.log(APPLY ? 'APPLIED' : 'dry run only (no --apply)');
