// One reusable, local-only chain. The runner owns stdin and requests a flush
// before shutdown; historical chain state is never reset between demo runs.
const path = require('node:path');
const fs = require('node:fs');
const { createRequire } = require('node:module');
const root = path.resolve(__dirname, '../..');
const runtime = path.join(root, 'LocalState/COSMIC/EvmRuntime');
const load = createRequire(path.join(runtime, 'package.json'));
if (load('ganache/package.json').version !== '7.9.2') throw new Error('Pinned Ganache7.9.2 required.');
const ganache = load('ganache');
const data = path.join(root, 'LocalState/COSMIC/EvmCql/chain-data');
fs.mkdirSync(data, { recursive: true });
const server = ganache.server({
    chain: { chainId: 31338, networkId: 31338, hardfork: 'shanghai' },
    database: { dbPath: data },
    wallet: { deterministic: true },
    logging: { quiet: true }
});
let closing = false;
async function close() {
    if (closing) return;
    closing = true;
    await server.close();
    process.exitCode = 0;
    process.stdin.pause();
}
process.stdin.setEncoding('utf8');
process.stdin.on('data', text => {
    if (text.trim() !== 'stop') throw new Error('Only explicit stop is accepted.');
    close().catch(error => { console.error(error); process.exitCode = 1; });
});
process.on('SIGINT', () => close().catch(error => { console.error(error); process.exitCode = 1; }));
server.listen(18547, '127.0.0.1').then(() => console.log('COSMIC local EVM ready: chain31338 port18547'))
    .catch(error => { console.error(error); process.exitCode = 1; });
