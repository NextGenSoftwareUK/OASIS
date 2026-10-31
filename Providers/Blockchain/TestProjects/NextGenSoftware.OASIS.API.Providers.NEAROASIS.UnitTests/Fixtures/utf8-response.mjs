import { readFileSync } from 'node:fs';
// Controlled process-codec fixture only: no SDK, network, credentials or writes.
const request = JSON.parse(readFileSync(0, 'utf8'));
if (request.methodName === 'malformed') {
  process.stdout.write(Buffer.concat([Buffer.from('{"ok":true,"result":{"value":"'), Buffer.from([255]), Buffer.from('"}}')]));
} else process.stdout.write(JSON.stringify({ ok: true, result: { value: request.args.value } }));
