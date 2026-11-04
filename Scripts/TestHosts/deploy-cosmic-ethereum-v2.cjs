// One new local Ethereum V2 contract. No node startup, key creation or resubmission.
const fs = require('node:fs');
const path = require('node:path');
const crypto = require('node:crypto');
const { createRequire } = require('node:module');
const root = path.resolve(__dirname, '../..');
const load = createRequire(path.join(root, 'LocalState/COSMIC/EvmRuntime/package.json'));
const ethers = load('ethers');
if (ethers.version !== '6.13.1') throw new Error('Retained ethers6.13.1 required.');
const hash = value => crypto.createHash('sha256').update(value).digest('hex');

function writeNew(file, value) {
    const fd = fs.openSync(file, 'wx');
    try {
        fs.writeFileSync(fd, JSON.stringify(value, null, 2));
        fs.fsyncSync(fd);
    } finally { fs.closeSync(fd); }
}

async function deployOnce(state, artifact, provider, wallet) {
    fs.mkdirSync(state, { recursive: true });
    if (fs.readdirSync(state).length !== 0)
        throw new Error('Original deployment state exists; reconcile without resubmission.');
    if (BigInt(await provider.send('eth_chainId', [])) !== 31338n)
        throw new Error('Only the retained local chain31338 is admitted.');
    if (!/^[0-9a-f]+$/i.test(artifact.bytecode ?? '') || !artifact.bytecode.length
        || hash(artifact.bytecode) !== artifact.bytecodeSha256
        || hash(artifact.runtimeBytecode) !== artifact.runtimeBytecodeSha256)
        throw new Error('Complete verified deployment artifact required.');
    const pending = await provider.getTransactionCount(wallet.address, 'pending');
    const latest = await provider.getTransactionCount(wallet.address, 'latest');
    if (pending !== latest) throw new Error('Pending original native transaction requires reconciliation.');
    const gasPrice = BigInt(await provider.send('eth_gasPrice', []));
    const gasLimit = 8000000n;
    if (gasPrice <= 0n || await provider.getBalance(wallet.address) < gasLimit * gasPrice)
        throw new Error('Local development balance/fee preflight failed.');
    const signed = await wallet.signTransaction({ type: 0, chainId: 31338, nonce: pending,
        gasLimit, gasPrice, value: 0, data: '0x' + artifact.bytecode });
    const transactionHash = ethers.keccak256(signed);
    const expectedContract = ethers.getCreateAddress({ from: wallet.address, nonce: pending });
    const intent = { provider: 'Ethereum', chainId: 31338, nonce: pending,
        transactionHash, expectedContract, sender: wallet.address,
        bytecodeSha256: artifact.bytecodeSha256, runtimeBytecodeSha256: artifact.runtimeBytecodeSha256 };
    // Hash/address are durable before the only broadcast. Signed raw bytes and
    // the public test key never enter journals or diagnostic output.
    writeNew(path.join(state, 'deployment.intent.json'), intent);
    let stage = 'broadcast';
    try {
        const sent = await provider.broadcastTransaction(signed);
        if (sent.hash.toLowerCase() !== transactionHash.toLowerCase())
            throw new Error('Native transaction identity differs.');
        writeNew(path.join(state, 'deployment.broadcast-ack.json'), { transactionHash });
        stage = 'receipt';
        const receipt = await sent.wait(1, 60000);
        if (!receipt) throw new Error('Original deployment receipt not observed.');
        const observed = { transactionHash: receipt.hash, status: receipt.status,
            contractAddress: receipt.contractAddress, blockNumber: receipt.blockNumber,
            blockHash: receipt.blockHash, gasUsed: receipt.gasUsed.toString() };
        writeNew(path.join(state, 'deployment.receipt-observed.json'), observed);
        if (receipt.status !== 1 || receipt.hash.toLowerCase() !== transactionHash.toLowerCase()
            || receipt.contractAddress?.toLowerCase() !== expectedContract.toLowerCase())
            throw new Error('Observed deployment did not match its original intent.');
        stage = 'runtime-code';
        const code = await provider.getCode(expectedContract);
        if (!code.startsWith('0x') || hash(code.slice(2)) !== artifact.runtimeBytecodeSha256)
            throw new Error('Deployed runtime differs from the shipping artifact.');
        const acknowledged = { ...intent, ...observed, runtimeCodeVerified: true };
        writeNew(path.join(state, 'deployment.acknowledged.json'), acknowledged);
        return acknowledged;
    } catch (_) {
        writeNew(path.join(state, 'deployment.failure-observed.json'), {
            transactionHash, expectedContract, stage, requiresReconciliation: true });
        throw new Error('Deployment incomplete; preserve original identity and reconcile without resubmission.');
    }
}

async function main() {
    if (process.env.OASIS_CREATE_ETHEREUM_V2_DEMO !== '1')
        throw new Error('Explicit separate Ethereum V2 fixture invocation required.');
    const state = path.join(root, 'LocalState/COSMIC/EvmV2ConfiguredCurrent/Ethereum');
    const artifact = JSON.parse(fs.readFileSync(path.join(root, 'TestResults/EvmV2ConfiguredCurrent/Web3CoreOASIS.json'), 'utf8'));
    // Public Ganache development identity. No external RPC or funded signer input.
    const wallet = new ethers.Wallet('0x4f3edf983ac636a65a842ce7c78d9aa706d3b113bce9c46f30d7d21715b23b1d');
    const provider = new ethers.JsonRpcProvider('http://127.0.0.1:18547');
    try {
        const result = await deployOnce(state, artifact, provider, wallet);
        console.log(JSON.stringify({ contract: result.contractAddress,
            transactionHash: result.transactionHash, runtimeCodeVerified: true }));
    } finally { provider.destroy(); }
}

module.exports = { deployOnce, hash, ethers };
if (require.main === module) main().catch(() => {
    console.error('Ethereum V2 deployment did not complete; inspect original journals,no automatic retry.');
    process.exitCode = 1;
});
