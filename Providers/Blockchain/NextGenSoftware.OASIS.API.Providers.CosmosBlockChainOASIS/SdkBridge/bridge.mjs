import { CosmWasmClient, SigningCosmWasmClient, wasmTypes } from "@cosmjs/cosmwasm";
import { DirectSecp256k1Wallet, DirectSecp256k1HdWallet, decodeTxRaw, Registry } from "@cosmjs/proto-signing";
import { verifiedBankTransfer, verifiedContractExecution, verifiedUpload, verifiedInstantiation, verifiedNftCustodyReceipt, cosmosNativeFeeReceipt, bankTransferLegs, cosmosErrorEnvelope, guardCosmosSigningClient, withCosmosNativeEvidence, indexedHistorySnapshot } from './transaction-results.mjs';
import { GasPrice, defaultRegistryTypes } from "@cosmjs/stargate";
import { fromHex, toHex, fromBech32 } from "@cosmjs/encoding";
import { pathToFileURL } from 'node:url';
import { captureCosmosDirectSigning, verifyCosmosPreparedIdentity } from './transaction-results.mjs';

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

async function execute(input, persistPreparedSubmission) {
  if (input.operation === 'verifyNativePreparation') return verifyCosmosPreparedIdentity(input.preparation);
  if (input.operation === "generateKey" || input.operation === "restoreKey") {
    const wallet = input.operation === "generateKey"
      ? await DirectSecp256k1HdWallet.generate(24, { prefix: input.addressPrefix ?? "cosmos" })
      : await walletFor(input);
    const [account] = await wallet.getAccounts();
    return { address: account.address, publicKey: toHex(account.pubkey), mnemonic: wallet.mnemonic ?? "" };
  }
  if (!input.rpcEndpoint) throw new Error("Cosmos rpcEndpoint is required");
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
      ? evidence => persistPreparedSubmission(Object.freeze({ ...originalSigning(evidence), chainId, sender })) : undefined);
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
      case "put":
      case "delete":
      case "execute": {
        if (input.fromWalletAddress && input.fromWalletAddress !== sender)
          throw new Error("The signing key does not own the requested Cosmos sender address");
        const message = input.operation === "put" ? { put: { key: input.key, value: input.value } }
          : input.operation === "delete" ? { delete: { key: input.key } } : input.message;
        const result = await client.execute(sender, input.targetContractAddress ?? input.contractAddress,
          message, "auto", input.memo ?? "", input.funds ?? []);
        verifiedContractExecution(result);
        return { transactionHash: result.transactionHash, height: result.height };
      }
      case "balance":
        return await client.getBalance(input.walletAddress, input.denom ?? "uatom");
      case "transactions": {
        const address = input.walletAddress;
        const indexed = await indexedHistorySnapshot(client, address);
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
        const tx = await client.getTx(input.transactionHash);
        return tx ? { code: tx.code, height: tx.height, hash: tx.hash } : null;
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
