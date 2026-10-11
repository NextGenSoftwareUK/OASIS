// Controlled process fixture only: no SDK or ledger operation occurs.
let input='';for await(const chunk of process.stdin)input+=chunk;
const request=JSON.parse(input);let result;
switch(request.operation){
 case 'restoreKey':result={privateKey:request.privateKey,publicKey:'fixture-public'};break;
 case 'nftEscrowGet':result={nft:{metadata:JSON.stringify({Id:'9daf56a1-a66d-4507-a5bc-a31de17e223a'})}};break;
 case 'nftUnlock':result={transactionHash:'fixture-committed-unlock-digest'};break;
 case 'nftGet':process.stdout.write(JSON.stringify({ok:false,error:'forced separate NFT readback failure'}));process.exitCode=1;break;
 default:throw new Error('Unexpected fixture operation.');
}
if(result)process.stdout.write(JSON.stringify({ok:true,result}));
