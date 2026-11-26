// Flow SDK bridge: one JSON request on stdin, one {ok, result} or {ok:false, error} envelope on stdout.
// The signer key arrives only on stdin and is never echoed. Every write waits until its transaction is sealed and
// fails unless the seal reports success.
import { readFileSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath, pathToFileURL } from 'node:url';
import * as fcl from '@onflow/fcl';
import elliptic from 'elliptic';
import { SHA3 } from 'sha3';

const OPERATIONS = ['deploy', 'kvPut', 'kvRemove', 'kvGet', 'kvKeys'];
const CONTRACT = 'OasisKv';
const here = dirname(fileURLToPath(import.meta.url));

export function normalizeAddress(value, name = 'address') {
  const text = String(value ?? '').toLowerCase().replace(/^0x/, '');
  if (!/^[0-9a-f]{16}$/.test(text)) throw new Error(`A 16-hex-digit Flow ${name} is required.`);
  return '0x' + text;
}

export function validateEndpoint(value) {
  const url = new URL(String(value ?? ''));
  if (url.protocol !== 'http:' && url.protocol !== 'https:') throw new Error('An http(s) Flow access node REST endpoint is required.');
  return url.href.replace(/\/$/, '');
}

const curves = { ECDSA_P256: new elliptic.ec('p256'), ECDSA_secp256k1: new elliptic.ec('secp256k1') };

/** Flow signature: SHA3-256 of the message bytes, signed with the account key, encoded as r||s (32 bytes each). */
export function signMessage(privateKeyHex, messageHex, algorithm = 'ECDSA_P256') {
  const curve = curves[algorithm];
  if (!curve) throw new Error('Only ECDSA_P256 and ECDSA_secp256k1 signer keys are supported.');
  if (!/^[0-9a-fA-F]{64}$/.test(String(privateKeyHex ?? ''))) throw new Error('A 32-byte hex Flow signer key is required.');
  const hash = new SHA3(256);
  hash.update(Buffer.from(messageHex, 'hex'));
  const signature = curve.keyFromPrivate(privateKeyHex, 'hex').sign(hash.digest());
  return signature.r.toArrayLike(Buffer, 'be', 32).toString('hex') + signature.s.toArrayLike(Buffer, 'be', 32).toString('hex');
}

function authorization(signer) {
  const address = normalizeAddress(signer.address, 'signer address');
  const keyId = Number.isInteger(signer.keyId) && signer.keyId >= 0 ? signer.keyId : 0;
  return async account => ({
    ...account,
    tempId: `${address}-${keyId}`,
    addr: fcl.sansPrefix(address),
    keyId,
    signingFunction: async signable => ({
      addr: fcl.withPrefix(address),
      keyId,
      signature: signMessage(signer.privateKey, signable.message, signer.algorithm),
    }),
  });
}

async function send(cadence, args, signer) {
  const authz = authorization(signer);
  const id = await fcl.mutate({ cadence, args, proposer: authz, payer: authz, authorizations: [authz], limit: 9999 });
  let sealed;
  try { sealed = await fcl.tx(id).onceSealed(); }
  catch (error) {
    // Submitted but not observed sealed: the outcome is unknown, so the caller must reconcile by id, never replay.
    const unknown = new Error(`Flow transaction ${id} was submitted but its seal was not observed: ${error?.message ?? error}`);
    unknown.transactionId = id;
    throw unknown;
  }
  if (sealed.statusCode !== 0 || sealed.errorMessage) throw new Error(`Flow transaction ${id} failed: ${sealed.errorMessage || sealed.statusCode}`);
  return id;
}

const putTx = contract => `import OasisKv from ${contract}
transaction(key: String, value: String) {
  let admin: auth(OasisKv.Write) &OasisKv.Admin
  prepare(signer: auth(BorrowValue) &Account) {
    self.admin = signer.storage.borrow<auth(OasisKv.Write) &OasisKv.Admin>(from: OasisKv.AdminStoragePath)
      ?? panic("The signer does not hold the OasisKv admin resource.")
  }
  execute { self.admin.put(key: key, value: value) }
}`;

const removeTx = contract => `import OasisKv from ${contract}
transaction(key: String) {
  let admin: auth(OasisKv.Write) &OasisKv.Admin
  prepare(signer: auth(BorrowValue) &Account) {
    self.admin = signer.storage.borrow<auth(OasisKv.Write) &OasisKv.Admin>(from: OasisKv.AdminStoragePath)
      ?? panic("The signer does not hold the OasisKv admin resource.")
  }
  execute { self.admin.remove(key: key) }
}`;

const deployTx = `transaction(name: String, code: String) {
  prepare(signer: auth(AddContract) &Account) {
    signer.contracts.add(name: name, code: code.decodeHex())
  }
}`;

export async function run(request) {
  if (!request || !OPERATIONS.includes(request.operation)) throw new Error('Unknown Flow SDK operation.');
  fcl.config({ 'accessNode.api': validateEndpoint(request.endpoint), 'flow.network': request.network ?? 'local' });
  const contract = request.operation === 'deploy' ? null : normalizeAddress(request.contract, 'contract address');

  switch (request.operation) {
    case 'deploy': {
      const signer = request.signer;
      const address = normalizeAddress(signer?.address, 'signer address');
      const account = await fcl.account(address);
      if (account.contracts && account.contracts[CONTRACT]) return { address, deployed: false };
      const code = readFileSync(join(here, 'OasisKv.cdc'), 'utf8');
      const transactionId = await send(deployTx, (arg, t) => [arg(CONTRACT, t.String), arg(Buffer.from(code, 'utf8').toString('hex'), t.String)], signer);
      return { address, deployed: true, transactionId };
    }
    case 'kvPut': {
      if (typeof request.key !== 'string' || typeof request.value !== 'string') throw new Error('A string key and value are required.');
      return { transactionId: await send(putTx(contract), (arg, t) => [arg(request.key, t.String), arg(request.value, t.String)], request.signer) };
    }
    case 'kvRemove': {
      if (typeof request.key !== 'string') throw new Error('A string key is required.');
      return { transactionId: await send(removeTx(contract), (arg, t) => [arg(request.key, t.String)], request.signer) };
    }
    case 'kvGet': {
      if (typeof request.key !== 'string') throw new Error('A string key is required.');
      const value = await fcl.query({
        cadence: `import OasisKv from ${contract}\naccess(all) fun main(key: String): String? { return OasisKv.get(key: key) }`,
        args: (arg, t) => [arg(request.key, t.String)],
      });
      return { value: value ?? null };
    }
    case 'kvKeys': {
      const keys = await fcl.query({
        cadence: `import OasisKv from ${contract}\naccess(all) fun main(prefix: String): [String] { return OasisKv.keys(prefix: prefix) }`,
        args: (arg, t) => [arg(String(request.prefix ?? ''), t.String)],
      });
      if (!Array.isArray(keys)) throw new Error('Flow returned a non-array key listing.');
      return { keys };
    }
  }
}

if (import.meta.url === pathToFileURL(process.argv[1] ?? '').href) {
  let request;
  try { request = JSON.parse(readFileSync(0, 'utf8')); }
  catch { process.stdout.write(JSON.stringify({ ok: false, error: 'The bridge request is not valid JSON.' })); process.exit(0); }
  run(request)
    .then(result => process.stdout.write(JSON.stringify({ ok: true, result })))
    .catch(error => {
      const secret = request?.signer?.privateKey;
      let message = String(error?.message ?? error);
      if (secret) message = message.split(secret).join('[redacted]');
      process.stdout.write(JSON.stringify({ ok: false, error: message, transactionId: error?.transactionId }));
    });
}
