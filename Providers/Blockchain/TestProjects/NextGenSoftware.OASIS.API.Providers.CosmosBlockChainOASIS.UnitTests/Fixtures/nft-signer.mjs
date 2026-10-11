// Controlled transport fixture only. No CosmJS, node or ledger is used.
let input='';for await(const chunk of process.stdin)input+=chunk;const request=JSON.parse(input);
let result,error;
if(request.operation==='restoreKey')result={publicKey:'fixture-public',address:'fixture-owner'};
else if(request.operation==='execute'){
 if(request.privateKey!=='test-only-explicit')error='Explicit signer was not forwarded';
 else result={transactionHash:'fixture-committed-cosmos-nft'};
}else if(request.operation==='query')error='forced separate NFT readback failure';
else error='Unexpected fixture operation';
process.stdout.write(JSON.stringify(error?{ok:false,error}:{ok:true,result}));if(error)process.exitCode=1;
