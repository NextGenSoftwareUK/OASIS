import Arweave from "arweave";
import { queryTransactionIds } from "./query.mjs";

const chunks = [];
for await (const chunk of process.stdin) chunks.push(chunk);
const input = JSON.parse(Buffer.concat(chunks).toString("utf8"));
const endpoint = new URL(input.gateway);
const arweave = Arweave.init({
  host: endpoint.hostname,
  port: endpoint.port ? Number(endpoint.port) : endpoint.protocol === "https:" ? 443 : 80,
  protocol: endpoint.protocol.slice(0, -1),
  timeout: 60_000,
  logging: false
});

const write = value => process.stdout.write(JSON.stringify(value));

try {
  switch (input.operation) {
    case "info": {
      const info = await arweave.network.getInfo();
      write({ ok: true, network: info.network, height: info.height });
      break;
    }
    case "generateWallet": {
      const wallet = await arweave.wallets.generate();
      const address = await arweave.wallets.jwkToAddress(wallet);
      write({ ok: true, wallet, address });
      break;
    }
    case "post": {
      const wallet = input.wallet;
      const transaction = await arweave.createTransaction(
        { data: Buffer.from(input.dataBase64, "base64") },
        wallet);
      transaction.addTag("Content-Type", input.contentType);
      for (const [name, value] of Object.entries(input.tags ?? {}))
        transaction.addTag(name, value);
      await arweave.transactions.sign(transaction, wallet);
      const response = await arweave.transactions.post(transaction);
      if (response.status !== 200 && response.status !== 202)
        throw new Error(`Arweave rejected transaction ${transaction.id}: HTTP ${response.status} ${JSON.stringify(response.data)}`);
      if (input.mineAfterPost)
        await arweave.api.get("mine");
      write({ ok: true, id: transaction.id, status: response.status });
      break;
    }
    case "get": {
      const data = await arweave.transactions.getData(input.id, { decode: true, string: false });
      write({ ok: true, dataBase64: Buffer.from(data).toString("base64") });
      break;
    }
    case "queryOwned": {
      if (!input.wallet) throw new Error("Configured wallet required for owner-pinned query.");
      const owner = await arweave.wallets.jwkToAddress(input.wallet);
      const ids = await queryTransactionIds(arweave.api, input.tags ?? {}, owner);
      write({ ok: true, ids });
      break;
    }
    case "query": {
      const ids = await queryTransactionIds(arweave.api, input.tags ?? {});
      write({ ok: true, ids });
      break;
    }
    default:
      throw new Error(`Unknown Arweave SDK bridge operation '${input.operation}'.`);
  }
} catch (error) {
  write({ ok: false, error: error?.stack ?? String(error) });
  process.exitCode = 1;
}
