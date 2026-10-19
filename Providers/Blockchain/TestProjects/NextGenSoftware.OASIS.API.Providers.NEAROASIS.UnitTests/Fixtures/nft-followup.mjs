import { readFileSync } from 'node:fs';
// Controlled IPC boundary fixture only: no SDK, signing, network or persistence.
const input=JSON.parse(readFileSync(0,'utf8'));
let result;
if(input.operation==='probe')result={};
else if(input.operation==='generateKey')result={publicKey:'ed25519:fixture-public',privateKey:'ed25519:fixture-private',implicitAccountId:'1'.repeat(64)};
else if(input.operation==='derivePublicKey')result={publicKey:'fixture-public-key',implicitAccountId:'1'.repeat(64)};
else if(input.operation==='view'&&input.methodName==='token_balance')result=process.env.OASIS_NEAR_TOKEN_TEST_ZERO==='1'?'0':'1000000000';
else if(input.operation==='call'&&input.methodName==='token_burn'){
  if(process.env.OASIS_NEAR_TOKEN_TEST_ZERO==='1')throw new Error('Insufficient balance must stop before mutation.');
  if(input.accountId!==process.env.OASIS_NEAR_TOKEN_TEST_ACCOUNT||input.privateKey!=='controlled-owner-key')throw new Error('Owner signer was not forwarded.');
  result={transactionHash:'controlled-original-token-transaction'};
}
else if(input.operation==='view'&&input.methodName==='nft_token')result=process.env.OASIS_NEAR_NFT_TEST_MODE==='missing'?null:
  {token_id:input.args.token_id,owner_id:'service.near',metadata_json:'{}',locked:false};
else if(input.operation==='call'&&['nft_transfer','nft_burn'].includes(input.methodName))result={transactionHash:'controlled-original-nft-transaction'};
else throw new Error('Unexpected fixture operation; wallet followup must not precede verified ownership.');
process.stdout.write(JSON.stringify({ok:true,result}));
