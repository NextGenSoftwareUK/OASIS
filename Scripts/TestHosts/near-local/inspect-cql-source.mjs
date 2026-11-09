import { createRequire } from 'node:module';
import { pathToFileURL, fileURLToPath } from 'node:url';
import { readFileSync, writeFileSync } from 'node:fs';
import { createHash } from 'node:crypto';

const sdk = new URL('../../../Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.NEAROASIS/SdkBridge/package.json', import.meta.url);
const require = createRequire(sdk);
const { JsonRpcProvider } = await import(pathToFileURL(require.resolve('near-api-js')).href);
const provider = new JsonRpcProvider({ url: 'http://127.0.0.1:3030' });
const status = await provider.viewNodeStatus();
if (status.chain_id !== 'oasis-near-local') throw new Error('Retained chain identity differs.');
const block = await provider.viewBlock({ finality: 'final' });
const hash = block.header.hash;
const code = await provider.viewContractCode({ contractId: 'test.near', blockQuery: { blockId: hash } });
const expected = readFileSync('\\\\wsl.localhost\\Ubuntu\\var\\tmp\\oasis-near-evidence\\build\\oasis_storage.wasm');
const digest = value => createHash('sha256').update(value).digest('hex');
if (digest(expected) !== '86e566d1e51bdac93aa0f47a511bb581ab33f29f939cb4b6ac5a7fd0595945dd')
  throw new Error('Reviewed retained contract artifact differs; no replacement or deployment.');
if (digest(Buffer.from(code.code_base64, 'base64')) !== digest(expected)) throw new Error('Original contract code differs.');

// Explicit diagnostic inventory, not a CQL query implementation or acceptance.
// Preserve source values off the console; report only shape/count/hash summaries.
let offset = 0, total = null, holons = 0, plaintext = 0, opaque = 0;
const fingerprints = [], candidates = [], shapes = new Set();
for (let pageNumber = 0; pageNumber < 128; pageNumber++) {
  const page = await provider.callFunction({ contractId: 'test.near', method: 'entries_page',
    args: { prefix: '', offset, limit: 16 }, blockQuery: { blockId: hash } });
  if (!Number.isSafeInteger(page.total) || page.total < 0 || page.total > 2048 ||
      total !== null && total !== page.total || !Array.isArray(page.rows)) throw new Error('Invalid retained page.');
  total = page.total;
  const scanned = Math.min(16, total - offset);
  const next = offset + scanned < total ? offset + scanned : null;
  if (scanned < 0 || page.next_offset !== next || page.rows.length !== scanned) throw new Error('Invalid retained continuation.');
  for (const [key, value] of page.rows) {
    if (typeof key !== 'string' || typeof value !== 'string') throw new Error('Invalid retained row.');
    fingerprints.push([digest(key), digest(value)]);
    if (!key.startsWith('holon:')) continue;
    holons++;
    let record;
    try { record = JSON.parse(value); } catch { shapes.add('non-JSON'); opaque++; continue; }
    shapes.add(record && typeof record === 'object' ? Object.keys(record).sort().join(',') : typeof record);
    if (record && typeof record === 'object' && typeof record.Id === 'string') {
      plaintext++;
      candidates.push({ id: record.Id, actor: record.CreatedByAvatarId, deleted: record.IsDeleted === true });
    } else opaque++;
  }
  if (next === null) break;
  offset = next;
  if (pageNumber === 127) throw new Error('Diagnostic inventory bound exceeded.');
}
const report = { chain: status.chain_id, block: hash, codeSha256: digest(expected),
  total, holons, plaintextHolons: plaintext, opaqueHolons: opaque,
  inventorySha256: digest(JSON.stringify(fingerprints.sort((a,b) => a[0].localeCompare(b[0])))), candidates, shapes: [...shapes],
  caution: 'Read-only diagnostic; no decryptions, signer, submission, reset, historical reconciliation or CQL acceptance.' };
const output = fileURLToPath(new URL('../../../TestResults/NearCqlCurrent/source-inspection.json', import.meta.url));
writeFileSync(output, JSON.stringify(report, null, 2), { flag: 'wx' });
console.log(JSON.stringify({ total, holons, plaintextHolons: plaintext, opaqueHolons: opaque, output }));
