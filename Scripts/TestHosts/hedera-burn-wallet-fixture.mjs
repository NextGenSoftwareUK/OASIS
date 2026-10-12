// Controlled transport only; never opens an SDK client or submits a ledger transaction.
let input = ''; for await (const chunk of process.stdin) input += chunk;
const { action, request } = JSON.parse(input);
if (action === 'transfer') {
  if (request.network !== 'testnet' || request.sender !== '0.0.1001' || request.recipient !== '0.0.1002'
      || request.privateKey !== 'fixture-transfer-key' || request.tinybars !== '100000000' || request.keyType !== 'der')
    throw new Error('Controlled raw transfer projection mismatch');
} else if (!['tokenBurn', 'tokenMint'].includes(action) || request.sender !== '0.0.1001' || request.recipient !== '0.0.1001'
    || request.privateKey !== 'fixture-burn-key' || request.publicKey !== 'fixture-public'
    || request.tokenId !== '0.0.2001' || request.amount !== '1' || request.network !== 'testnet')
  throw new Error('Controlled avatar burn projection mismatch');
process.stdout.write(JSON.stringify({ transactionId: request.transactionId, status: 'SUCCESS' }));
