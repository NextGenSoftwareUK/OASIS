// Controlled child environment observation only: no SDK, signing, RPC or secrets echoed.
import { readFileSync } from 'node:fs';
JSON.parse(readFileSync(0, 'utf8'));
process.stdout.write(JSON.stringify({ ok: true, result: {
  storageKeyPresent: Object.hasOwn(process.env, 'OASIS_POLKADOT_STORAGE_ENCRYPTION_KEY')
} }));
