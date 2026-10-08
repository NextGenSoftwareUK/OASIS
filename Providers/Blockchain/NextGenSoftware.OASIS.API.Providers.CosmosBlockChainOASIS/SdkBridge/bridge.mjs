import { CosmWasmClient, SigningCosmWasmClient } from "@cosmjs/cosmwasm";
import { DirectSecp256k1Wallet, DirectSecp256k1HdWallet, decodeTxRaw, Registry } from "@cosmjs/proto-signing";
import { GasPrice, defaultRegistryTypes } from "@cosmjs/stargate";
import { fromHex, toHex, fromBech32 } from "@cosmjs/encoding";

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

async function execute(input) {
  if (input.operation === "generateKey" || input.operation === "restoreKey") {
    const wallet = input.operation === "generateKey"
      ? await DirectSecp256k1HdWallet.generate(24, { prefix: input.addressPrefix ?? "cosmos" })
      : await walletFor(input);
    const [account] = await wallet.getAccounts();
    return { address: account.address, publicKey: toHex(account.pubkey), mnemonic: wallet.mnemonic ?? "" };
  }
  if (!input.rpcEndpoint) throw new Error("Cosmos rpcEndpoint is required");
  const isSigned = ["put", "delete", "execute", "transfer", "upload", "instantiate"].includes(input.operation);
  const wallet = isSigned ? await walletFor(input) : null;
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
    switch (input.operation) {
      case "probe": {
        if (!input.contractAddress) throw new Error("CosmWasm contractAddress is required for OASIS storage");
        const contract = await client.getContract(input.contractAddress);
        const config = await client.queryContractSmart(input.contractAddress, { config: {} });
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
        const message = input.operation === "put" ? { put: { key: input.key, value: input.value } }
          : input.operation === "delete" ? { delete: { key: input.key } } : input.message;
        const result = await client.execute(sender, input.targetContractAddress ?? input.contractAddress,
          message, "auto", input.memo ?? "", input.funds ?? []);
        return { transactionHash: result.transactionHash, height: result.height };
      }
      case "balance":
        return await client.getBalance(input.walletAddress, input.denom ?? "uatom");
      case "transactions": {
        const address = input.walletAddress;
        fromBech32(address, 90); // Validate before embedding an account into the Comet query.
        const received = await client.searchTx([{ key: "transfer.recipient", value: address }]);
        const sent = await client.searchTx([{ key: "message.sender", value: address }]);
        const indexed = new Map([...received, ...sent].map(tx => [tx.hash, tx]));
        const registry = new Registry(defaultRegistryTypes);
        const transfers = [];
        for (const tx of [...indexed.values()].sort((a, b) => a.height - b.height || a.txIndex - b.txIndex)) {
          if (tx.code !== 0) continue;
          const raw = decodeTxRaw(tx.tx);
          const block = await client.getBlock(tx.height);
          for (let messageIndex = 0; messageIndex < raw.body.messages.length; messageIndex++) {
            const encoded = raw.body.messages[messageIndex];
            if (encoded.typeUrl !== "/cosmos.bank.v1beta1.MsgSend") continue;
            const message = registry.decode(encoded);
            if (message.fromAddress !== address && message.toAddress !== address) continue;
            for (const coin of message.amount)
              if (coin.denom === input.denom)
                transfers.push({ hash: tx.hash, messageIndex, from: message.fromAddress,
                  to: message.toAddress, amountUnits: coin.amount, denom: coin.denom,
                  memo: raw.body.memo, time: block.header.time });
          }
        }
        return transfers;
      }
      case "transfer": {
        if (input.fromWalletAddress && input.fromWalletAddress !== sender)
          throw new Error("The signing key does not own the requested Cosmos sender address");
        const result = await client.sendTokens(sender, input.toWalletAddress,
          [{ denom: input.denom ?? "uatom", amount: input.amountUnits }], "auto", input.memo ?? "");
        if (result.code !== 0) throw new Error(`Cosmos transfer failed: ${result.rawLog}`);
        return { transactionHash: result.transactionHash, height: result.height, sender };
      }
      case "transactionStatus": {
        const tx = await client.getTx(input.transactionHash);
        return tx ? { code: tx.code, height: tx.height, hash: tx.hash } : null;
      }
      case "upload": {
        const result = await client.upload(sender, Buffer.from(input.wasmBase64, "base64"), "auto");
        return { codeId: result.codeId, transactionHash: result.transactionHash };
      }
      case "instantiate": {
        const result = await client.instantiate(sender, input.codeId, input.message, input.label, "auto");
        return { contractAddress: result.contractAddress, transactionHash: result.transactionHash };
      }
      default: throw new Error(`Unsupported Cosmos SDK operation: ${input.operation}`);
    }
  } finally { client.disconnect(); }
}

try {
  process.stdout.write(JSON.stringify({ ok: true, result: await execute(await readInput()) }));
} catch (error) {
  process.stdout.write(JSON.stringify({ ok: false, error: error instanceof Error ? error.message : String(error) }));
  process.exitCode = 1;
}
