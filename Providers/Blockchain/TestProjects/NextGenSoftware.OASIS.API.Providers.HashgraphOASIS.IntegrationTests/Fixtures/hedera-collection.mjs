// Controlled transport only: no SDK or ledger submission.
let input='';for await(const chunk of process.stdin)input+=chunk;
const {action,request}=JSON.parse(input);
if(action!=='nftCollectionCreate'||request.network!=='testnet'||request.sender!=='0.0.1001'
 ||request.maxSupply!=='9007199254740993'||request.maxFeeTinybars!=='100000000'
 ||request.keyType!=='der'||request.privateKey!=='test-only-collection-key')
 throw new Error('Collection request projection mismatch');
if(request.name==='failure'){
 process.stderr.write(JSON.stringify({transactionId:request.transactionId,stage:'receipt',status:'INSUFFICIENT_TX_FEE'}));
 process.exitCode=1;
}else{
 const result={transactionId:request.name==='mismatch'?'0.0.1002@1700000000.000000001':request.transactionId,
  status:'SUCCESS',tokenId:request.name==='missing-token'?null:'0.0.2001'};
 process.stdout.write(JSON.stringify(result));
}
