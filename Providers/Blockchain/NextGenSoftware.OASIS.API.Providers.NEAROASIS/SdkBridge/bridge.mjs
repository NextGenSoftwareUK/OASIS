import { Account, JsonRpcProvider, KeyPair, actions, teraToGas } from "near-api-js";

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
  const provider = providerFor(input);
  switch (input.operation) {
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
      return await provider.callFunction({ contractId: input.contractId, method: "entries", args: { prefix: input.prefix ?? "" } });
    case "balance": {
      const accountId = input.targetAccountId ?? input.accountId;
      if (!accountId) throw new Error("targetAccountId is required");
      const state = await provider.viewAccount({ accountId, blockQuery: { finality: "final" } });
      return { amount: state.amount.toString(), locked: state.locked.toString(), storageUsage: state.storage_usage };
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
      const outcome = await account.signAndSendTransaction({
        receiverId: input.receiverId,
        actions: [actions.transfer(BigInt(input.amountYocto))],
        waitUntil: "FINAL",
      });
      return { transactionHash: outcome.transaction_outcome.id };
    }
    case "transactionStatus": {
      if (!input.transactionHash) throw new Error("transactionHash is required");
      const outcome = await provider.viewTransactionStatus({
        txHash: input.transactionHash,
        accountId: input.signerAccountId ?? input.accountId,
        waitUntil: "FINAL",
      });
      return { status: outcome.final_execution_status };
    }
    case "view":
      return await provider.callFunction({
        contractId: input.targetContractId ?? input.contractId,
        method: input.methodName,
        args: input.args ?? {},
      });
    case "call": {
      const account = accountFor(input, provider);
      const outcome = await account.callFunctionRaw({
        contractId: input.targetContractId ?? input.contractId,
        methodName: input.methodName,
        args: input.args ?? {},
        gas: BigInt(input.gas ?? teraToGas("100")),
        deposit: BigInt(input.depositYocto ?? "0"),
        waitUntil: "FINAL",
      });
      return { transactionHash: outcome.transaction_outcome.id, outcome };
    }
    case "put": {
      const account = accountFor(input, provider);
      const outcome = await account.callFunctionRaw({
        contractId: input.contractId,
        methodName: "put",
        args: { key: input.key, value: input.value },
        gas: teraToGas("100"),
        waitUntil: "FINAL",
      });
      return { transactionHash: outcome.transaction_outcome.id };
    }
    case "delete": {
      const account = accountFor(input, provider);
      const outcome = await account.callFunctionRaw({
        contractId: input.contractId,
        methodName: "delete_record",
        args: { key: input.key },
        gas: teraToGas("100"),
        waitUntil: "FINAL",
      });
      return { transactionHash: outcome.transaction_outcome.id };
    }
    default:
      throw new Error(`Unsupported NEAR SDK operation: ${input.operation}`);
  }
}

try {
  process.stdout.write(JSON.stringify({ ok: true, result: await execute(await readInput()) }));
} catch (error) {
  process.stdout.write(JSON.stringify({ ok: false, error: error instanceof Error ? error.message : String(error) }));
  process.exitCode = 1;
}
