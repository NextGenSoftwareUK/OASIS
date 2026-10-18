import { CosmWasmClient, SigningCosmWasmClient, wasmTypes } from "@cosmjs/cosmwasm";
import { DirectSecp256k1Wallet, DirectSecp256k1HdWallet, decodeTxRaw, Registry } from "@cosmjs/proto-signing";
import { verifiedBankTransfer, verifiedContractExecution, verifiedUpload, verifiedInstantiation, verifiedNftCustodyReceipt, cosmosNativeFeeReceipt, bankTransferLegs, cosmosErrorEnvelope, guardCosmosSigningClient, withCosmosNativeEvidence, indexedHistorySnapshot } from './transaction-results.mjs';
import { GasPrice, calculateFee, defaultRegistryTypes } from "@cosmjs/stargate";
import { pubkeyToAddress } from '@cosmjs/amino';
import { createHash } from 'node:crypto';
import { fromHex, toHex, fromBech32 } from "@cosmjs/encoding";
import { pathToFileURL } from 'node:url';
import { captureCosmosDirectSigning, verifyCosmosPreparedIdentity } from './transaction-results.mjs';
import { cosmosIndexedTransactionStatus } from './transaction-results.mjs';
import { snapshotCosmosFeeLimits } from './transaction-results.mjs';
import { snapshotCosmosReviewedHeight } from './transaction-results.mjs';

async function readInput() {
  const chunks = [];
  for await (const chunk of process.stdin) chunks.push(chunk);
  return JSON.parse(Buffer.concat(chunks).toString("utf8"));
}

async function walletFor(input) {
  if (!input.privateKey) throw new Error("Cosmos privateKey is required for signed operations");
  const prefix = input.addressPrefix ?? "cosmos";
  return input.privateKey.trim().includes(" ")
    ? DirectSecp256k1HdWallet.fromMnemonic(input.privateKey, { prefix })
    : DirectSecp256k1Wallet.fromKey(fromHex(input.privateKey.replace(/^0x/, "")), prefix);
}

// Diagnostic simulation only. The public-key signer cannot sign; this estimate
// neither authorizes spending nor replaces verified signed fee maxima.
async function estimateUpload(input) {
  if(input.privateKey)throw new Error('Upload estimation must not receive a private key');
  const wasm=Buffer.from(input.wasmBase64??'','base64');
  if(!wasm.length||wasm.length>1048576||wasm.toString('base64')!==input.wasmBase64
    || !/^[0-9a-f]{64}$/.test(input.expectedWasmSha256??'')
    ||createHash('sha256').update(wasm).digest('hex')!==input.expectedWasmSha256)
    throw new Error('Exact bounded canonical WASM and expected checksum required');
  const prefix=fromBech32(input.sender,90).prefix;
  if(prefix!==input.addressPrefix)throw new Error('Exact sender address prefix required');
  const gasPrice=GasPrice.fromString(input.gasPrice);
  const reader=await CosmWasmClient.connect(input.rpcEndpoint);let client;
  try{
    const chainId=await reader.getChainId();
    if(!input.chainId||chainId!==input.chainId)throw new Error('Exact simulation chain required');
    const account=await reader.getAccount(input.sender);
    if(account?.address!==input.sender||account.pubkey?.type!=='tendermint/PubKeySecp256k1'
      ||pubkeyToAddress(account.pubkey,prefix)!==input.sender)throw new Error('Registered public account key must match sender');
    const pubkey=Buffer.from(account.pubkey.value,'base64');
    if(pubkey.length!==33||pubkey.toString('base64')!==account.pubkey.value)throw new Error('Canonical compressed public key required');
    const signer={getAccounts:async()=>[{address:input.sender,algo:'secp256k1',pubkey}],
      signDirect:async()=>{throw new Error('Read-only upload simulation cannot sign');}};
    client=await SigningCosmWasmClient.connectWithSigner(input.rpcEndpoint,signer);
    const before=await reader.getBalance(input.sender,gasPrice.denom);
    const compressed=new Uint8Array(await new Response(new Blob([wasm]).stream().pipeThrough(new CompressionStream('gzip'))).arrayBuffer());
    const gasUsed=await client.simulate(input.sender,[{typeUrl:'/cosmwasm.wasm.v1.MsgStoreCode',
      value:{sender:input.sender,wasmByteCode:compressed}}],'');
    if(!Number.isSafeInteger(gasUsed)||gasUsed<=0||!Number.isSafeInteger(Math.round(gasUsed*1.1)))throw new Error('Exact SDK-safe positive gas estimate required');
    const after=await reader.getBalance(input.sender,gasPrice.denom);
    if(before.denom!==gasPrice.denom||after.denom!==before.denom||after.amount!==before.amount)
      throw new Error('Simulation wallet balance changed; do not treat it as spending proof');
    return {chainId,sender:input.sender,wasmSha256:input.expectedWasmSha256,originalSize:wasm.length,compressedSize:compressed.length,
      gasUsed,gasMultiplier:1.1,feeEstimate:calculateFee(Math.round(gasUsed*1.1),gasPrice),balanceBefore:before,balanceAfter:after,
      signed:false,broadcast:false,basis:'sdk-upload-simulation-not-a-fee-maximum'};
  }finally{client?.disconnect();reader.disconnect();}
}

async function execute(input, persistPreparedSubmission) {
  const feeLimits=snapshotCosmosFeeLimits(input.maximumFeeUnitsByDenomination);
  snapshotCosmosReviewedHeight(input.reviewedMaximumHeight);
  if (input.operation === 'instantiate' && (!Number.isSafeInteger(input.codeId) || input.codeId <= 0))
    throw new Error('Positive SDK-safe exact Cosmos code ID required');
  if (input.operation === 'verifyNativePreparation') return verifyCosmosPreparedIdentity(input.preparation);
  if (input.operation === "generateKey" || input.operation === "restoreKey") {
    const wallet = input.operation === "generateKey"
      ? await DirectSecp256k1HdWallet.generate(24, { prefix: input.addressPrefix ?? "cosmos" })
      : await walletFor(input);
    const [account] = await wallet.getAccounts();
    return { address: account.address, publicKey: toHex(account.pubkey), mnemonic: wallet.mnemonic ?? "" };
  }
  if (!input.rpcEndpoint) throw new Error("Cosmos rpcEndpoint is required");
  if(input.operation==='estimateUpload')return estimateUpload(input);
  const isSigned = ["put", "delete", "execute", "transfer", "upload", "instantiate"].includes(input.operation);
  const wallet = isSigned || input.operation === 'probe' ? await walletFor(input) : null;
  const originalSigning = isSigned && persistPreparedSubmission ? captureCosmosDirectSigning(wallet) : null;
  const client = isSigned
    ? await SigningCosmWasmClient.connectWithSigner(input.rpcEndpoint, wallet, {
      gasPrice: GasPrice.fromString(input.gasPrice ?? "0.025uatom"),
      broadcastTimeoutMs: 60000,
      broadcastPollIntervalMs: 500,
    })
    : await CosmWasmClient.connect(input.rpcEndpoint);
  try {
    const chainId = await client.getChainId();
    if (input.chainId && input.chainId !== chainId)
      throw new Error(`Cosmos chain mismatch: expected ${input.chainId}, received ${chainId}`);
    const sender = wallet ? (await wallet.getAccounts())[0].address : null;
    if (isSigned) guardCosmosSigningClient(client, persistPreparedSubmission
      ? evidence => persistPreparedSubmission(Object.freeze({ ...originalSigning(evidence), chainId, sender })) : undefined,feeLimits,input.reviewedMaximumHeight);
    switch (input.operation) {
      case "nativeFeeReceipt": {
        const hash = input.transactionHash;
        if (!/^[0-9a-fA-F]{64}$/.test(hash ?? '')) throw new Error('Exact Cosmos transaction hash required');
        return { ...cosmosNativeFeeReceipt(await client.getTx(hash), hash), chainId };
      }
      case "verifyNftCustodyReceipt": {
        if (!/^[0-9a-fA-F]{64}$/.test(input.transactionHash ?? '')) throw new Error('Exact Cosmos source transaction hash required');
        const tx = await client.getTx(input.transactionHash);
        verifiedNftCustodyReceipt(tx, input, new Registry([...defaultRegistryTypes, ...wasmTypes]));
        return { transactionHash: tx.hash, verified: true };
      }
      case "probe": {
        if (!input.contractAddress) throw new Error("CosmWasm contractAddress is required for OASIS storage");
        const contract = await client.getContract(input.contractAddress);
        const config = await client.queryContractSmart(input.contractAddress, { config: {} });
        verifyStorageAuthority(config,sender);
        return { chainId, height: await client.getHeight(), contract, config };
      }
      case "get":
        return await client.queryContractSmart(input.contractAddress, { get: { key: input.key } });
      case "list":
        return await client.queryContractSmart(input.contractAddress, {
          list: { prefix: input.prefix ?? "", start_after: input.startAfter ?? null, limit: 100 },
        });
      case "query":
        return await client.queryContractSmart(input.targetContractAddress ?? input.contractAddress, input.message);
      case "tokenEscrowInfo": {
        const address = input.targetContractAddress;
        const contract = await client.getContract(address);
        if (contract.address !== address || !Number.isSafeInteger(contract.codeId) || contract.codeId <= 0 || contract.admin)
          throw new Error('Escrow must be the exact immutable contract with a valid code ID');
        const code = await client.getCodeDetails(contract.codeId);
        if (code.id !== contract.codeId || code.checksum !== input.codeSha256)
          throw new Error('Escrow code checksum differs from reviewed contract');
        const protocol = await client.queryContractSmart(address, { asset: { message: { token_escrow_protocol: {} } } });
        if (protocol !== 1) throw new Error('Exact token escrow protocol1 required');
        return { contractAddress: address, codeSha256: code.checksum, protocol };
      }
      case "put":
      case "delete":
      case "execute": {
        if (input.fromWalletAddress && input.fromWalletAddress !== sender)
          throw new Error("The signing key does not own the requested Cosmos sender address");
        const message = input.operation === "put" ? { put: { key: input.key, value: input.value } }
          : input.operation === "delete" ? { delete: { key: input.key } } : input.message;
        const result = await client.execute(sender, input.targetContractAddress ?? input.contractAddress,
          message, input.fee ?? "auto", input.memo ?? "", input.funds ?? []);
        verifiedContractExecution(result);
        return { transactionHash: result.transactionHash, height: result.height };
      }
      case "balance":
        return await client.getBalance(input.walletAddress, input.denom ?? "uatom");
      case "transactions": {
        const address = input.walletAddress;
        const indexed = await indexedHistorySnapshot(client, address, {fromBlock:input.fromBlock,toBlock:input.toBlock});
        const registry = new Registry(defaultRegistryTypes);
        const transfers = [];
        for (const tx of indexed) {
          if (!Number.isSafeInteger(tx.code) || tx.code < 0)
            throw new Error('Cosmos indexed transaction lacks an exact execution code');
          const raw = decodeTxRaw(tx.tx);
          const block = await client.getBlock(tx.height);
          for (let messageIndex = 0; messageIndex < raw.body.messages.length; messageIndex++) {
            const encoded = raw.body.messages[messageIndex];
            for (const leg of bankTransferLegs(encoded,registry)) {
            if (leg.from !== address && leg.to !== address) continue;
            for (const coin of leg.coins)
              if (coin.denom === input.denom)
                transfers.push({ hash: tx.hash, messageIndex, transferIndex:leg.transferIndex, operation:leg.operation, from: leg.from,
                  to: leg.to, amountUnits: coin.amount, denom: coin.denom,
                  code: tx.code, memo: raw.body.memo, time: block.header.time });
            }
          }
        }
        return transfers;
      }
      case "transfer": {
        if (input.fromWalletAddress && input.fromWalletAddress !== sender)
          throw new Error("The signing key does not own the requested Cosmos sender address");
        const result = await client.sendTokens(sender, input.toWalletAddress,
          [{ denom: input.denom ?? "uatom", amount: input.amountUnits }], "auto", input.memo ?? "");
        verifiedBankTransfer(result);
        return { transactionHash: result.transactionHash, height: result.height, sender };
      }
      case "transactionStatus": {
        if (!/^[0-9a-fA-F]{64}$/.test(input.transactionHash ?? '')) throw new Error('Exact Cosmos transaction hash required');
        const tx = await client.getTx(input.transactionHash);
        return cosmosIndexedTransactionStatus(tx, input.transactionHash);
      }
      case "upload": {
        const result = await client.upload(sender, Buffer.from(input.wasmBase64, "base64"), "auto");
        verifiedUpload(result);
        return { codeId: result.codeId, transactionHash: result.transactionHash };
      }
      case "instantiate": {
        const result = await client.instantiate(sender, input.codeId, input.message, input.label, "auto");
        verifiedInstantiation(result, input.addressPrefix ?? "cosmos");
        return { contractAddress: result.contractAddress, transactionHash: result.transactionHash };
      }
      default: throw new Error(`Unsupported Cosmos SDK operation: ${input.operation}`);
    }
  } finally { client.disconnect(); }
}

export function verifyStorageAuthority(config,sender) {
  if(typeof sender!=='string'||!sender||typeof config?.owner!=='string'||config.owner!==sender)
    throw new Error('Cosmos storage owner does not match the configured signing account');
}

export async function invoke(input, persistPreparedSubmission) {
  if (persistPreparedSubmission !== undefined && typeof persistPreparedSubmission !== 'function')
    throw new TypeError('Prepared Cosmos persistence must be an awaited callback');
  return withCosmosNativeEvidence(() => execute(input, persistPreparedSubmission));
}

if (process.argv[1] && import.meta.url === pathToFileURL(process.argv[1]).href) {
try {
  const input = await readInput();
  let persistPreparedSubmission;
  if (process.env.OASIS_NATIVE_PREPARATION_PIPE) {
    const modulePath=process.env.OASIS_NATIVE_PREPARATION_MODULE;
    if (!modulePath) throw new Error('Parent preparation transport module is required');
    const { preparationClient }=await import(pathToFileURL(modulePath).href);
    persistPreparedSubmission=preparationClient(process.env.OASIS_NATIVE_PREPARATION_PIPE);
  }
  process.stdout.write(JSON.stringify({ ok: true, result: await invoke(input, persistPreparedSubmission) }));
} catch (error) {
  process.stdout.write(JSON.stringify(cosmosErrorEnvelope(error)));
  process.exitCode = 1;
}
}
