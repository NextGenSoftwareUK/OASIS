// Narrow source correction for pinned CosmJS 0.39.0; fail if upstream changes.
import { createRequire } from 'node:module';
import { readFileSync, writeFileSync } from 'node:fs';
import { dirname, resolve } from 'node:path';
const require = createRequire(import.meta.url);
const build = dirname(require.resolve('@cosmjs/stargate'));
const version = JSON.parse(readFileSync(resolve(build, '../package.json'), 'utf8')).version;
if (version !== '0.39.0') throw new Error('Revalidate the CosmJS timer correction before changing its pinned version.');
const path = resolve(build, 'stargateclient.js');
const original = `        const transactionId = await this.broadcastTxSync(tx);
        return pollForTx(transactionId).finally(() => {
            clearTimeout(txPollTimeout);
        });`;
const corrected = `        try {
            const transactionId = await this.broadcastTxSync(tx);
            return await pollForTx(transactionId);
        } finally {
            clearTimeout(txPollTimeout);
        }`;
const source = readFileSync(path, 'utf8');
if (source.includes(corrected) && !source.includes(original)) {
    // An already-corrected reusable installation requires no rewrite.
} else {
    if (source.split(original).length !== 2 || source.includes(corrected))
        throw new Error('Pinned CosmJS broadcast source does not match the reviewed timer correction.');
    writeFileSync(path, source.replace(original, corrected));
}
