// Compile the shipping V2 contract using the retained pinned compiler only.
// This creates a regenerable artifact; it never connects to or deploys a chain.
const fs = require('node:fs');
const path = require('node:path');
const crypto = require('node:crypto');
const { createRequire } = require('node:module');
const root = path.resolve(__dirname, '../..');
const load = createRequire(path.join(root, 'LocalState/COSMIC/EvmRuntime/package.json'));
const solc = load('solc');
if (solc.version() !== '0.8.26+commit.8a97fa7a.Emscripten.clang')
    throw new Error('Retained Solidity0.8.26 compiler required; no download attempted.');

const provider = path.join(root, 'Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS');
const contracts = fs.realpathSync(path.join(provider, 'Contracts'));
const helper = path.join(provider, 'src/Web3CoreOASISBaseProviderHelper.Part1.cs');
const artifactPath = path.join(root, 'TestResults/EvmV2ConfiguredCurrent/Web3CoreOASIS.json');
const hash = value => crypto.createHash('sha256').update(value).digest('hex');
const helperHash = hash(fs.readFileSync(helper));
const canonical = value => Array.isArray(value) ? value.map(canonical) : value && typeof value === 'object'
    ? Object.fromEntries(Object.keys(value).sort().map(key => [key, canonical(value[key])])) : value;
const embedded = fs.readFileSync(helper, 'utf8').match(/public const string Abi = @"([\s\S]*?)";/);
if (!embedded) throw new Error('Shipping SDK ABI is missing.');
const shippingAbi = JSON.stringify(canonical(JSON.parse(embedded[1].replace(/""/g, '"'))));

// Reuse a matching artifact; source changes invalidate it explicitly.
if (fs.existsSync(artifactPath)) {
    const cached = JSON.parse(fs.readFileSync(artifactPath, 'utf8'));
    if (cached.compiler === solc.version() && cached.helperSha256 === helperHash
        && JSON.stringify(canonical(cached.abi)) === shippingAbi
        && Object.entries(cached.sourceHashes ?? {}).length > 0
        && Object.entries(cached.sourceHashes).every(([name, digest]) => {
            const resolved = fs.realpathSync(path.resolve(contracts, name));
            return resolved.startsWith(contracts + path.sep) && hash(fs.readFileSync(resolved)) === digest;
        }) && hash(cached.bytecode) === cached.bytecodeSha256
        && hash(cached.runtimeBytecode) === cached.runtimeBytecodeSha256) {
        console.log(JSON.stringify({ compiler: cached.compiler, reused: true, bytecodeBytes: cached.bytecode.length / 2 }));
        process.exit(0);
    }
    throw new Error('Artifact source/hash mismatch; reconcile the previous build before replacing it.');
}

const sourceHashes = {};
function source(name) {
    const resolved = fs.realpathSync(path.resolve(contracts, name));
    if (!resolved.startsWith(contracts + path.sep)) throw new Error('Contract import escaped shipping source root.');
    const text = fs.readFileSync(resolved, 'utf8');
    sourceHashes[name] = hash(Buffer.from(text));
    return text;
}
const entry = 'Web3CoreOASIS.sol';
const compiled = JSON.parse(solc.compile(JSON.stringify({ language: 'Solidity',
    sources: { [entry]: { content: source(entry) } },
    settings: { evmVersion: 'paris', optimizer: { enabled: true, runs: 200 },
        outputSelection: { '*': { '*': ['abi', 'evm.bytecode.object', 'evm.deployedBytecode.object'] } } }
}), { import: name => ({ contents: source(name) }) }));
const errors = (compiled.errors ?? []).filter(item => item.severity === 'error');
if (errors.length) throw new Error(errors.map(item => item.formattedMessage).join('\n'));
const contract = compiled.contracts[entry].Web3CoreOASIS;
if (shippingAbi !== JSON.stringify(canonical(contract.abi)))
    throw new Error('Compiled shipping ABI differs from the production SDK; no deployment artifact admitted.');
const bytecode = contract.evm.bytecode.object;
const runtimeBytecode = contract.evm.deployedBytecode.object;
if (![bytecode, runtimeBytecode].every(value => value.length > 0 && /^[0-9a-f]+$/i.test(value)))
    throw new Error('Complete linked bytecode required.');
fs.mkdirSync(path.dirname(artifactPath), { recursive: true });
fs.writeFileSync(artifactPath, JSON.stringify({ compiler: solc.version(), helperSha256: helperHash,
    sourceHashes, abi: contract.abi, bytecode, runtimeBytecode,
    bytecodeSha256: hash(bytecode), runtimeBytecodeSha256: hash(runtimeBytecode) }), { flag: 'wx' });
console.log(JSON.stringify({ compiler: solc.version(), reused: false,
    bytecodeBytes: bytecode.length / 2, runtimeBytecodeBytes: runtimeBytecode.length / 2,
    imports: Object.keys(sourceHashes).length, productionAbiMatched: true }));
