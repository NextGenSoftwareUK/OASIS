// Long-lived Aztec SDK worker for AztecOASIS. It holds one embedded wallet (an in-process PXE) for the storage account,
// because creating a PXE per call would dominate every storage operation.
//
// Protocol: one JSON request per stdin line; one reply per request on stdout, as a line that starts with REPLY_PREFIX
// followed by {id, ok, result | error, txHash?}. Any other stdout or stderr output (SDK logging) is ignored by the host.
// Secrets arrive only on stdin, never in arguments or the environment.
import { readFileSync } from 'node:fs';
import { createInterface } from 'node:readline';
import { loadContractArtifact } from '@aztec/aztec.js/abi';
import { AztecAddress } from '@aztec/aztec.js/addresses';
import { Contract, NO_WAIT } from '@aztec/aztec.js/contracts';
import { Fq, Fr } from '@aztec/aztec.js/fields';
import { createAztecNodeClient, waitForNode, waitForTx } from '@aztec/aztec.js/node';
import { EmbeddedWallet } from '@aztec/wallets/embedded';

const REPLY_PREFIX = '@@OASIS-AZTEC@@';
const ARTIFACT_PATH = new URL('./OasisKv.json', import.meta.url);

let state; // { node, wallet, account, artifact, contract? }

const hex = (value) => '0x' + BigInt(value).toString(16).padStart(64, '0');

class SubmittedError extends Error {
  constructor(message, txHash) { super(message); this.txHash = txHash; }
}

async function init({ nodeUrl, secret, salt, signingKey, contract }) {
  if (state) throw new Error('The Aztec worker is already initialised.');
  const node = createAztecNodeClient(nodeUrl);
  await waitForNode(node);
  const wallet = await EmbeddedWallet.create(node, { ephemeral: true });
  const account = await wallet.createSchnorrInitializerlessAccount(Fr.fromString(secret), Fr.fromString(salt), Fq.fromString(signingKey));
  const artifact = loadContractArtifact(JSON.parse(readFileSync(ARTIFACT_PATH, 'utf8')));
  const ready = { node, wallet, account, artifact };
  // Commit the worker state only once the contract is attached, so a failed init can be retried with its real error.
  if (contract) ready.contract = await attach(ready, contract);
  state = ready;
  return { address: account.address.toString() };
}

async function attach(target, address) {
  const contractAddress = AztecAddress.fromString(address);
  const instance = await target.node.getContract(contractAddress);
  if (!instance) throw new Error(`No OasisKv contract instance is published at ${address}.`);
  await target.wallet.registerContract(instance, target.artifact);
  return Contract.at(contractAddress, target.artifact, target.wallet);
}

function requireContract() {
  if (!state) throw new Error('The Aztec worker is not initialised.');
  if (!state.contract) throw new Error('No OasisKv contract is attached.');
  return state.contract;
}

async function deploy() {
  if (!state) throw new Error('The Aztec worker is not initialised.');
  const from = state.account.address;
  const { contract } = await Contract.deploy(state.wallet, state.artifact, [from]).send({ from });
  state.contract = contract;
  return { address: contract.address.toString() };
}

async function submit(interaction) {
  const { txHash } = await interaction.send({ from: state.account.address, wait: NO_WAIT });
  try {
    const receipt = await waitForTx(state.node, txHash, { timeout: 600 });
    if (!receipt.hasExecutionSucceeded()) throw new Error(`Aztec transaction ${txHash} reverted: ${receipt.error ?? 'no reason given'}`);
    return { txHash: txHash.toString(), blockNumber: Number(receipt.blockNumber) };
  } catch (error) {
    throw new SubmittedError(error?.message ?? String(error), txHash.toString());
  }
}

async function view(method, ...args) {
  const { result } = await requireContract().methods[method](...args).simulate({ from: state.account.address });
  return result;
}

const operations = {
  init,
  attach: async ({ contract }) => { if (!state) throw new Error('The Aztec worker is not initialised.'); state.contract = await attach(state, contract); return {}; },
  deploy,
  owner: async () => ({ owner: (await view('get_owner')).toString(), account: state.account.address.toString() }),
  put: async ({ id, hi, lo }) => submit(requireContract().methods.put(Fr.fromString(id), Fr.fromString(hi), Fr.fromString(lo))),
  remove: async ({ id }) => submit(requireContract().methods.remove(Fr.fromString(id))),
  get: async ({ id }) => {
    const [hi, lo] = await view('get', Fr.fromString(id));
    return { hi: hex(hi), lo: hex(lo) };
  },
  list: async () => {
    const count = Number(await view('record_count'));
    const ids = [];
    for (let position = 0; position < count; position++) ids.push(hex(await view('record_at', position)));
    return { ids };
  },
};

function reply(message) {
  process.stdout.write(REPLY_PREFIX + JSON.stringify(message) + '\n');
}

const lines = createInterface({ input: process.stdin, crlfDelay: Infinity });
let queue = Promise.resolve();
lines.on('line', (line) => {
  if (!line.trim()) return;
  queue = queue.then(async () => {
    let request;
    try {
      request = JSON.parse(line);
      const operation = operations[request.operation];
      if (!operation) throw new Error(`Unknown Aztec worker operation '${request.operation}'.`);
      reply({ id: request.id, ok: true, result: await operation(request) });
    } catch (error) {
      reply({ id: request?.id ?? null, ok: false, error: error?.message ?? String(error), txHash: error?.txHash });
    }
  });
});
lines.on('close', () => { queue.then(async () => { if (state) await state.wallet.stop(); process.exit(0); }); });
