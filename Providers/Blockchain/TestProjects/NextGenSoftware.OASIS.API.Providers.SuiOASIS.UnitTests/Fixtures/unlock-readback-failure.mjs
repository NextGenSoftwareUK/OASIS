// Controlled process fixture only: no SDK or ledger operation occurs.
let input='';for await(const chunk of process.stdin)input+=chunk;
const request=JSON.parse(input);let result;
switch(request.operation){
 case 'restoreKey':result={privateKey:request.privateKey,publicKey:'fixture-public',address:'fixture-owner'};break;
 case 'nftMint': {
  const title=JSON.parse(request.metadata[0]).Title;
  if(title.startsWith('native-evidence/')) {
   const mode=title.split('/')[1];
   const mutation=mode==='bad-shape' ? [] : {
    transactionHash:mode==='unknown' ? null : 'original-controlled-digest',
    ...(mode==='missing-status' ? {} : {executionSucceeded:mode==='failed' ? false : mode==='executed' ? true : mode==='bad-status' ? 'false' : null})
   };
   process.stdout.write(JSON.stringify({ok:false,...(mode==='missing-error' ? {} : {error:mode==='bad-error' ? [] : 'original controlled native error'}),...(mode==='ordinary' ? {} : {nativeMutation:mutation})}));
   process.exitCode=1;
   break;
  }
  result={transactionHash:'fixture-committed-mint-digest',tokenIds:['fixture-nft'],recipient:'fixture-owner'};break;
 }
 case 'nftEscrowGet':result={nft:{metadata:JSON.stringify({Id:'9daf56a1-a66d-4507-a5bc-a31de17e223a'})}};break;
 case 'nftUnlock':result={transactionHash:'fixture-committed-unlock-digest'};break;
 case 'nftSend':
  if(request.privateKey!=='test-only-explicit')throw new Error('Explicit signer was not forwarded.');
  result={transactionHash:'fixture-committed-send-digest'};break;
 case 'nftGet':process.stdout.write(JSON.stringify({ok:false,error:'forced separate NFT readback failure'}));process.exitCode=1;break;
 default:throw new Error('Unexpected fixture operation.');
}
if(result)process.stdout.write(JSON.stringify({ok:true,result}));
