// Offline real-signature/controlled-transport checks. Never starts a chain.
const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const { deployOnce, hash, ethers } = require('./deploy-cosmic-ethereum-v2.cjs');
const parent = path.resolve(__dirname, '../../TestResults/EvmV2ConfiguredCurrent/deployment-tests');

function fixture(t, mode) {
    fs.mkdirSync(parent, { recursive: true });
    const state = fs.mkdtempSync(path.join(parent, 'case-'));
    t.after(() => {
        assert.equal(path.dirname(fs.realpathSync(state)), fs.realpathSync(parent));
        fs.rmSync(state, { recursive: true }); // Exact newly owned synthetic case only.
    });
    const artifact = { bytecode: '60006000', runtimeBytecode: '6000',
        bytecodeSha256: hash('60006000'), runtimeBytecodeSha256: hash('6000') };
    const wallet = new ethers.Wallet('0x4f3edf983ac636a65a842ce7c78d9aa706d3b113bce9c46f30d7d21715b23b1d');
    let broadcasts = 0;
    let signed;
    const provider = {
        send: async method => method === 'eth_chainId' ? (mode === 'wrong-chain' ? '0x1' : '0x7a6a') : '0x64',
        getTransactionCount: async (_, tag) => mode === 'pending' && tag === 'pending' ? 1 : 0,
        getBalance: async () => 100000000000000000000n,
        broadcastTransaction: async raw => {
            broadcasts++;
            signed = raw;
            const intent = JSON.parse(fs.readFileSync(path.join(state, 'deployment.intent.json'), 'utf8'));
            const decoded = ethers.Transaction.from(raw);
            assert.equal(intent.transactionHash, ethers.keccak256(raw));
            assert.equal(decoded.chainId, 31338n);
            assert.equal(decoded.from, wallet.address);
            assert.equal(decoded.nonce, intent.nonce);
            if (mode === 'broadcast-loss') throw new Error('controlled response loss');
            return { hash: ethers.keccak256(raw), wait: async () => {
                if (mode === 'receipt-timeout') throw new Error('controlled observation timeout');
                return { hash: ethers.keccak256(raw), status: 1,
                    contractAddress: ethers.getCreateAddress({ from: wallet.address, nonce: 0 }),
                    blockNumber: 1, blockHash: '0x' + '1'.repeat(64), gasUsed: 1000000n };
            } };
        },
        getCode: async () => mode === 'wrong-runtime' ? '0x6001' : '0x6000'
    };
    return { state, artifact, wallet, provider, broadcasts: () => broadcasts, signed: () => signed };
}

test('Signed deployment identity is durable before broadcast and exact receipt/runtime acknowledge it', async t => {
    const f = fixture(t);
    const ack = await deployOnce(f.state, f.artifact, f.provider, f.wallet);
    assert.equal(f.broadcasts(), 1);
    assert.equal(ack.runtimeCodeVerified, true);
    assert.equal(ack.transactionHash, ethers.keccak256(f.signed()));
    assert.ok(fs.existsSync(path.join(f.state, 'deployment.acknowledged.json')));
    for (const name of fs.readdirSync(f.state)) {
        const text = fs.readFileSync(path.join(f.state, name), 'utf8');
        assert.equal(text.includes(f.wallet.privateKey), false);
        assert.equal(text.includes(f.signed()), false);
    }
});

for (const mode of ['broadcast-loss', 'receipt-timeout', 'wrong-runtime']) {
    test(`${mode} retains original hash/stage and refuses another native dispatch`, async t => {
        const f = fixture(t, mode);
        await assert.rejects(deployOnce(f.state, f.artifact, f.provider, f.wallet), /preserve original identity/);
        assert.ok(fs.existsSync(path.join(f.state, 'deployment.intent.json')));
        assert.equal(fs.existsSync(path.join(f.state, 'deployment.acknowledged.json')), false);
        const failure = JSON.parse(fs.readFileSync(path.join(f.state, 'deployment.failure-observed.json'), 'utf8'));
        assert.equal(failure.requiresReconciliation, true);
        assert.equal(failure.transactionHash, ethers.keccak256(f.signed()));
        await assert.rejects(deployOnce(f.state, f.artifact, f.provider, f.wallet), /without resubmission/);
        assert.equal(f.broadcasts(), 1);
    });
}

for (const mode of ['wrong-chain', 'pending']) {
    test(`${mode} preflight rejects before signing or broadcasting`, async t => {
        const f = fixture(t, mode);
        await assert.rejects(deployOnce(f.state, f.artifact, f.provider, f.wallet));
        assert.equal(f.broadcasts(), 0);
        assert.equal(f.signed(), undefined);
        assert.deepEqual(fs.readdirSync(f.state), []);
    });
}
