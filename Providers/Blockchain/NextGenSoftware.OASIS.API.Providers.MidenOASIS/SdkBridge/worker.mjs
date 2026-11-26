// Long-lived Miden SDK worker for MidenOASIS: one client (native SQLite store) for the storage account, because each
// client start re-syncs the chain.
//
// Protocol: one JSON request per stdin line; one reply per request on stdout, as a line that starts with REPLY_PREFIX
// followed by {id, ok, result | error, txId?}. Other output is SDK logging and is ignored by the host. Seeds (the
// account's secrets) arrive only on stdin.
import { readFileSync } from 'node:fs';
import { randomUUID } from 'node:crypto';
import { createInterface } from 'node:readline';
import { AccountId, AuthSecretKey, MidenClient, StorageMap, StorageSlot, Word } from '@miden-sdk/miden-sdk';

const REPLY_PREFIX = '@@OASIS-MIDEN@@';
const SLOT = 'oasis::kv';
const COMPONENT_CODE = readFileSync(new URL('./oasis_kv.masm', import.meta.url), 'utf8');

let state; // { client, component, accountId }

class SubmittedError extends Error {
  constructor(message, txId) { super(message); this.txId = txId; }
}

const seedBytes = (hex) => {
  const bytes = Buffer.from(hex.replace(/^0x/, ''), 'hex');
  if (bytes.length !== 32) throw new Error('Miden seeds must be 32 bytes.');
  return new Uint8Array(bytes);
};
// Words travel as four decimal u64 strings so no precision is lost across JSON.
const toWord = (limbs) => new Word(BigUint64Array.from(limbs.map((limb) => BigInt(limb))));
const fromWord = (word) => Array.from(word.toU64s(), (limb) => limb.toString());

async function openClient(rpcUrl) {
  const client = await MidenClient.create({ rpcUrl, storeName: `oasis-miden-${randomUUID()}`, autoSync: false });
  await client.sync();
  const component = await client.compile.component({
    code: COMPONENT_CODE,
    namespace: 'oasis::kv',
    slots: [StorageSlot.map(SLOT, new StorageMap())],
  });
  return { client, component };
}

/** Fixture only: creates the public storage account, funds it for fees and returns its id. */
async function deploy({ rpcUrl, accountSeed, keySeed, faucetUrl }) {
  if (state) throw new Error('The Miden worker is already initialised.');
  const { client, component } = await openClient(rpcUrl);
  const auth = AuthSecretKey.rpoFalconWithRNG(seedBytes(keySeed));
  const account = await client.accounts.create({ seed: seedBytes(accountSeed), auth, components: [component], storage: 'public' });
  const funded = await client.accounts.fundAccount(account, { faucetUrl });
  state = { client, component, accountId: account.id() };
  return { accountId: account.id().toString(), balance: funded.balance.toString() };
}

async function init({ rpcUrl, accountId, keySeed }) {
  if (state) throw new Error('The Miden worker is already initialised.');
  const { client, component } = await openClient(rpcUrl);
  const id = AccountId.fromHex(accountId);
  await client.accounts.import(id);
  await client.keystore.insert(id, AuthSecretKey.rpoFalconWithRNG(seedBytes(keySeed)));
  state = { client, component, accountId: id };
  return { accountId: id.toString() };
}

function requireState() {
  if (!state) throw new Error('The Miden worker is not initialised.');
  return state;
}

async function readEntry(key) {
  const { client, accountId } = requireState();
  await client.sync();
  const account = await client.accounts.get(accountId);
  if (!account) throw new Error(`The Miden storage account ${accountId.toString()} is not in the client store.`);
  const item = account.storage().getMapItem(SLOT, toWord(key));
  return item ? fromWord(item) : ['0', '0', '0', '0'];
}

async function setEntries(entries) {
  const { client, component, accountId } = requireState();
  const lines = entries.map(({ key, value }) =>
    `    push.[${value.join(',')}]\n    push.[${key.join(',')}]\n    call.kv::set_entry\n    dropw dropw dropw dropw`);
  const script = await client.compile.txScript({ code: `use oasis::kv\n\nbegin\n${lines.join('\n')}\nend\n`, libraries: [{ component }] });
  let txId;
  try {
    const submitted = await client.transactions.execute({ account: accountId, script, waitForConfirmation: true, timeout: 300_000 });
    txId = submitted?.toString?.() ?? submitted?.transactionId?.toString?.();
  } catch (error) {
    throw new SubmittedError(error?.message ?? String(error), txId);
  }
  // Acknowledge only what the committed account state shows.
  for (const { key, value } of entries) {
    const stored = await readEntry(key);
    if (stored.join(',') !== value.join(',')) throw new SubmittedError(`Miden map entry [${key}] reads [${stored}] after commit, expected [${value}].`, txId);
  }
  return { txId: txId ?? null };
}

const operations = {
  deploy,
  init,
  set: async ({ entries }) => setEntries(entries),
  get: async ({ keys }) => ({ values: await Promise.all(keys.map(readEntry)) }),
  list: async () => {
    const { client, accountId } = requireState();
    await client.sync();
    const account = await client.accounts.get(accountId);
    const entries = account.storage().getMapEntries(SLOT) ?? [];
    return { entries: entries.map((entry) => ({ key: fromWord(Word.fromHex(entry.key)), value: fromWord(Word.fromHex(entry.value)) })) };
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
      if (!operation) throw new Error(`Unknown Miden worker operation '${request.operation}'.`);
      reply({ id: request.id, ok: true, result: await operation(request) });
    } catch (error) {
      reply({ id: request?.id ?? null, ok: false, error: error?.message ?? String(error), txId: error?.txId });
    }
  });
});
lines.on('close', () => { queue.then(async () => { if (state) await state.client.terminate(); process.exit(0); }); });
