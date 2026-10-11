// Controlled transport only: no SDK or ledger submission.
let input='';for await(const chunk of process.stdin)input+=chunk;
const {action,request}=JSON.parse(input);
if(!['nftCollectionCreate','tokenCollectionDelete'].includes(action)||request.network!=='testnet'||request.sender!=='0.0.1001'
 ||(action==='nftCollectionCreate'&&request.maxSupply!=='9007199254740993')||request.maxFeeTinybars!=='100000000'
 ||request.keyType!=='der'||request.privateKey!=='test-only-collection-key')
 throw new Error('Collection request projection mismatch');
const mode=action==='tokenCollectionDelete'?({'0.0.2001':'success','0.0.2002':'failure','0.0.2003':'mismatch'})[request.tokenId]:request.name;
if(!mode)throw new Error('Unexpected collection selector');
if(action==='nftCollectionCreate'){
 const expected=mode.startsWith('royalty-')?Number(mode.slice(8)):0;
 if(request.royaltyPercentage!==expected)throw new Error('Collection royalty was not forwarded exactly');
}
if(mode==='failure'){
 process.stderr.write(JSON.stringify({transactionId:request.transactionId,stage:'receipt',status:'INSUFFICIENT_TX_FEE'}));
 process.exitCode=1;
}else{
 const result={transactionId:mode==='mismatch'?'0.0.1002@1700000000.000000001':request.transactionId,
  status:'SUCCESS',tokenId:mode==='missing-token'?null:'0.0.2001'};
 process.stdout.write(JSON.stringify(result));
}
