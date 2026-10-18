// Existing loopback chain only. Explicit fee avoids simulation-only rejection.
import { SigningCosmWasmClient } from '@cosmjs/cosmwasm';
import { DirectSecp256k1HdWallet } from '@cosmjs/proto-signing';
const endpoint = 'http://127.0.0.1:26657';
const wallet = await DirectSecp256k1HdWallet.fromMnemonic(`${'abandon '.repeat(23)}art`, { prefix: 'wasm' });
const [account] = await wallet.getAccounts();
const client = await SigningCosmWasmClient.connectWithSigner(endpoint, wallet, { broadcastTimeoutMs: 60000, broadcastPollIntervalMs: 500 });
try {
  if (await client.getChainId() !== 'oasis-cosmos-local') throw new Error('Wrong local fixture chain');
  const balance = await client.getBalance(account.address, 'stake');
  const result = await client.signAndBroadcast(account.address, [{ typeUrl: '/cosmos.bank.v1beta1.MsgSend', value: {
    fromAddress: account.address, toAddress: account.address,
    amount: [{ denom: 'stake', amount: (BigInt(balance.amount) + 1n).toString() }]
  } }], { amount: [{ denom: 'stake', amount: '5000' }], gas: '200000' }, 'local confirmed failure verification');
  if (result.code === 0 || !result.transactionHash || result.height <= 0) throw new Error('Expected committed insufficient-funds failure');
  console.log(result.transactionHash);
} finally { client.disconnect(); }
