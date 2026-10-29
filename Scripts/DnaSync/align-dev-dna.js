// Makes every dev DNA copy equal to WEB4's (the dev source of truth), and brings the LIVE backup up to the same schema
// without overriding any value it already has: keys LIVE lacks take the dev value.
//
//   node Scripts/DnaSync/align-dev-dna.js            dry run: counts only, no values printed
//   node Scripts/DnaSync/align-dev-dna.js --apply    write
//
// Run from the repo root (linked to the Railway project). Railway writes use --skip-deploys. Dated .bak copies are kept beside
// the backup files. Production Railway is never touched. LIVE keeps OASIS.Sandbox.Enabled=false unless it already set it.
const fs = require('fs'), cp = require('child_process'), j = require('./jsonc'), { merge, replace } = require('./merge');
const BACKUP_DIR = process.env.OASIS_DNA_BACKUP_DIR || 'C:/Source/BACKUP/BACKUP';
const APPLY = process.argv.includes('--apply');
const stamp = '.pre-' + new Date().toISOString().slice(0, 10) + '.bak';
const flat = (o, p = '', a = {}) => { if (o && typeof o === 'object' && !Array.isArray(o)) { for (const k in o) flat(o[k], p + '/' + k, a); } else a[p] = o; return a; };
const railVars = s => JSON.parse(cp.execSync(`railway variables --service "${s}" --json`, { encoding: 'utf8', maxBuffer: 1 << 26 }));
const same = (a, b) => JSON.stringify(a) === JSON.stringify(b);

const source = j.parse(railVars('WEB4 OASIS API').OASIS_DNA_JSON);
const sourceFlat = flat(source);

// Sets every leaf that differs to the source value (adds missing sections first). Comments survive.
function align(text) {
  let out = merge(text, source, {}, { keepSecrets: true }).text;
  const target = flat(j.parse(out));
  let changed = 0;
  for (const p of Object.keys(sourceFlat)) {
    if (!same(target[p], sourceFlat[p])) {
      out = replace(out, p, sourceFlat[p]);
      changed++;
    }
  }
  return { text: out, changed };
}
const extra = text => Object.keys(flat(j.parse(text))).filter(p => !(p in sourceFlat)).length;

const files = [
  ['backup DEV', BACKUP_DIR + '/DEV/OASIS_DNA.json', true],
  ['local ONODE', 'C:/Source/OASIS/ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI/OASIS_DNA.json', false],
  ['local STAR WebAPI', 'C:/Source/OASIS/STAR ODK/NextGenSoftware.OASIS.STAR.WebAPI/OASIS_DNA.json', false],
];
for (const [name, path, backup] of files) {
  const before = fs.readFileSync(path, 'utf8');
  const { text, changed } = align(before);
  const after = flat(j.parse(text));
  const left = Object.keys(sourceFlat).filter(p => !same(after[p], sourceFlat[p])).length;
  if (left) throw new Error(`${name}: ${left} values still differ`);
  console.log(`${name.padEnd(24)} ${changed} values set to dev; ${extra(before)} keys only in this copy (kept)`);
  if (APPLY && text !== before) { if (backup && !fs.existsSync(path + stamp)) fs.copyFileSync(path, path + stamp); fs.writeFileSync(path, text); }
}

const compact = JSON.stringify(source);
if (compact.length > 32000) throw new Error('dev DNA is ' + compact.length + ' chars, over the Railway limit');
for (const s of ['WEB5 STAR API', 'WEB6 AI API', 'WEB7 SYMBIOTIC API', 'WEB8 IGN API', 'WEB9 SINGULARITIY API', 'WEB10 THE SOURCE API']) {
  const current = flat(j.parse(railVars(s).OASIS_DNA_JSON));
  const diff = new Set([...Object.keys(sourceFlat), ...Object.keys(current)].filter(p => !same(current[p], sourceFlat[p]))).size;
  console.log(`railway ${s.padEnd(22)} ${diff} paths differ from WEB4`);
  if (APPLY && diff) cp.execSync(`railway variable set OASIS_DNA_JSON --stdin --service "${s}" --skip-deploys --json`, { input: compact, encoding: 'utf8' });
}

// LIVE: rebuilt from its original, so keys it never had take the dev value; a value it already had is never overridden.
const livePath = BACKUP_DIR + '/LIVE/OASIS_DNA.json';
const liveOriginalPath = livePath + '.pre-2026-10-28.bak'; // LIVE as it was before any schema sync: only its own values count as existing
const liveBefore = fs.readFileSync(fs.existsSync(liveOriginalPath) ? liveOriginalPath : livePath, 'utf8');
const liveCurrent = fs.readFileSync(livePath, 'utf8');
const merged = merge(liveBefore, source, { '/OASIS/Sandbox/Enabled': false }, { keepSecrets: true });
const a = flat(j.parse(liveBefore)), b = flat(j.parse(merged.text));
const lost = Object.keys(a).filter(p => a[p] !== null && !same(a[p], b[p]));
if (lost.length) throw new Error('LIVE would lose values: ' + lost.slice(0, 5));
const stillMissing = Object.keys(sourceFlat).filter(p => !(p in b));
if (stillMissing.length) throw new Error('LIVE still missing ' + stillMissing.length);
console.log(`LIVE backup               +${merged.added.length} keys/sections from dev; existing values untouched`);
if (APPLY && merged.text !== liveCurrent) { if (!fs.existsSync(livePath + stamp)) fs.copyFileSync(livePath, livePath + stamp); fs.writeFileSync(livePath, merged.text); }
console.log(APPLY ? 'APPLIED' : 'dry run only (no --apply)');
