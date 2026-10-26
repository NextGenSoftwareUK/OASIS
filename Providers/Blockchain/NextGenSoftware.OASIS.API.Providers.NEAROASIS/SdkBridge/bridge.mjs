import { Account, JsonRpcProvider, KeyPair, actions, teraToGas } from "near-api-js";
import nearSeedPhrase from "near-seed-phrase";
import bip39 from "bip39-light";
import { collectEntries } from "./entries-page.mjs";
import { submitCheckedRecord } from "./checked-write.mjs";
import { decodeCallArguments } from "./call-arguments.mjs";
import { submitNativeAction } from "./native-mutation.mjs";
import {pathToFileURL} from 'node:url';
import {prepareNearAction,verifyNearPreparation,recoverNearPreparation,exactNearSubmission,submitNearPreparation} from './native-preparation.mjs';

async function readInput() {
  const chunks = [];
  for await (const chunk of process.stdin) chunks.push(chunk);
  return JSON.parse(Buffer.concat(chunks).toString("utf8"));
}

function providerFor(input) {
  if (!input.rpcEndpoint) throw new Error("rpcEndpoint is required");
  return new JsonRpcProvider({ url: input.rpcEndpoint });
}

function accountFor(input, provider) {
  if (!input.accountId) throw new Error("accountId is required for writes");
  if (!input.privateKey) throw new Error("privateKey is required for writes");
  return new Account(input.accountId, provider, input.privateKey);
}

async function execute(input) {
  if(input.operation==='submitNativePreparation'&&(!process.env.OASIS_NATIVE_PREPARATION_PIPE||!process.env.OASIS_NATIVE_PREPARATION_MODULE))
    throw new Error('Existing owned native preparation pipe/module required before submission.');
  const provider = providerFor(input);
  switch (input.operation) {
    case 'prepareNativeAction':
      if(input.request?.chainId!==input.networkId||input.request?.sender!==input.accountId)throw new Error('Native request differs from configured signer/network.');
      return await prepareNearAction(accountFor(input,provider),input.request);
    case 'verifyNativePreparation':
      return await verifyNearPreparation(provider,input.preparation,input.networkId);
    case 'recoverNativePreparation': {
      const original=await recoverNearPreparation(input.preparation,input.networkId);
      await verifyNearPreparation(provider,original,input.networkId);
      return original;
    }
    case 'preparedNativeOutcome': {
      const identity=await verifyNearPreparation(provider,input.preparation,input.networkId);
      return {outcome:await provider.viewTransactionStatus({txHash:identity.transactionHash,accountId:identity.sender,waitUntil:'NONE'})};
    }
    case 'submitNativePreparation': {
      const {preparationClient}=await import(pathToFileURL(process.env.OASIS_NATIVE_PREPARATION_MODULE).href);
      return await submitNearPreparation(provider,input.preparation,input.networkId,preparationClient(process.env.OASIS_NATIVE_PREPARATION_PIPE),
        exactNearSubmission(input.rpcEndpoint,input.preparation.signedTransactionBase64));
    }
    case "probe": {
      const status = await provider.viewNodeStatus();
      const networkId = await provider.getNetworkId();
      if (input.networkId && networkId !== input.networkId) {
        throw new Error(`NEAR network mismatch: expected ${input.networkId}, received ${networkId}`);
      }
      let contractCodeLength = 0;
      if (input.contractId) {
        const code = await provider.viewContractCode({ contractId: input.contractId });
        contractCodeLength = code?.length ?? code?.code?.length ?? code?.code_base64?.length ?? 0;
        if (contractCodeLength === 0) throw new Error(`No contract code at ${input.contractId}`);
      }
      return { networkId, latestBlockHeight: status.sync_info?.latest_block_height, contractCodeLength };
    }
    case "get":
      return await provider.callFunction({ contractId: input.contractId, method: "get", args: { key: input.key } });
    case "entries":
      return await collectEntries(provider,input.contractId,input.prefix??"");
    case "balance": {
      const accountId = input.targetAccountId ?? input.accountId;
      if (!accountId) throw new Error("targetAccountId is required");
      const state = await provider.viewAccount({ accountId, blockQuery: { finality: "final" } });
      return { amount: state.amount.toString(), locked: state.locked.toString(), storageUsage: state.storage_usage };
    }
    case "generateMnemonic":
    case "deriveMnemonic": {
      const phrase = input.operation === "generateMnemonic"
        ? nearSeedPhrase.generateSeedPhrase().seedPhrase
        : typeof input.seedPhrase === "string" ? nearSeedPhrase.normalizeSeedPhrase(input.seedPhrase) : "";
      if (!bip39.validateMnemonic(phrase)) throw new Error("A valid NEAR BIP39 mnemonic with a valid checksum is required");
      const derived = nearSeedPhrase.parseSeedPhrase(phrase);
      const publicKey = KeyPair.fromString(derived.secretKey).getPublicKey();
      if (publicKey.toString() !== derived.publicKey) throw new Error("NEAR mnemonic SDK key identity differs");
      return { seedPhrase: phrase, privateKey: derived.secretKey, publicKey: derived.publicKey,
        implicitAccountId: Buffer.from(publicKey.data).toString("hex") };
    }
    case "generateKey": {
      const keyPair = KeyPair.fromRandom("ed25519");
      const publicKey = keyPair.getPublicKey();
      return {
        privateKey: keyPair.toString(),
        publicKey: publicKey.toString(),
        implicitAccountId: Buffer.from(publicKey.data).toString("hex"),
      };
    }
    case "derivePublicKey": {
      if (!input.secretKey) throw new Error("secretKey is required");
      const publicKey = KeyPair.fromString(input.secretKey).getPublicKey();
      return { publicKey: publicKey.toString(), implicitAccountId: Buffer.from(publicKey.data).toString("hex") };
    }
    case "transfer": {
      const account = accountFor(input, provider);
      if (!input.receiverId) throw new Error("receiverId is required");
      const outcome = await submitNativeAction(account,input.receiverId,actions.transfer(BigInt(input.amountYocto)));
      return { transactionHash: outcome.transaction_outcome.id, outcome };
    }
    case "transactionStatus": {
      if (!input.transactionHash) throw new Error("transactionHash is required");
      const outcome = await provider.viewTransactionStatus({
        txHash: input.transactionHash,
        accountId: input.signerAccountId ?? input.accountId,
        // Observation must report current state, not block waiting for finality.
        // All submission paths continue to require FINAL.
        waitUntil: "NONE",
      });
      return { outcome };
    }
    case "view":
      return await provider.callFunction({
        contractId: input.targetContractId ?? input.contractId,
        method: input.methodName,
        args: input.args ?? {},
      });
    case "call": {
      const args=decodeCallArguments(input.argsBase64);
      const account = accountFor(input, provider);
      const outcome = await submitNativeAction(account,input.targetContractId ?? input.contractId,
        actions.functionCall(input.methodName,args,BigInt(input.gas ?? teraToGas("100")),BigInt(input.depositYocto ?? "0")));
      return { transactionHash: outcome.transaction_outcome.id, outcome };
    }
    case "putRecordChecked":
      return await submitCheckedRecord(provider,()=>accountFor(input,provider),input,
        args=>actions.functionCall('put_record_checked',args,teraToGas('100'),0n));
    default:
      throw new Error(`Unsupported NEAR SDK operation: ${input.operation}`);
  }
}

try {
  process.stdout.write(JSON.stringify({ ok: true, result: await execute(await readInput()) }));
} catch (error) {
  process.stdout.write(JSON.stringify({ ok: false, error: error instanceof Error ? error.message : String(error),checkedWritePhase:error?.checkedWritePhase??null }));
  process.exitCode = 1;
}
