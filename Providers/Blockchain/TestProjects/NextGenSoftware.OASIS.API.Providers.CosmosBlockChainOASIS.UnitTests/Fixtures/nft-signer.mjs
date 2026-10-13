// Controlled transport fixture only. No CosmJS, node or ledger is used.
let input='';for await(const chunk of process.stdin)input+=chunk;const request=JSON.parse(input);
let result,error,failureEnvelope,handled=false;
if(['put','get','delete'].includes(request.operation)) {
 handled=true;const mode=request.mode ?? (request.key==='prepared-storage'?'prepared':undefined);
 if(mode?.startsWith('prepared')){
  const {pathToFileURL}=await import('node:url');
  const {preparationClient}=await import(pathToFileURL(process.env.OASIS_NATIVE_PREPARATION_MODULE).href);
  try{
   await preparationClient(process.env.OASIS_NATIVE_PREPARATION_PIPE,5000)({transactionHash:'a'.repeat(64),signedTransactionBase64:'AQID'});
   if(mode==='prepared-hold')setInterval(()=>{},1000);
   else if(mode==='prepared-foreign-error')process.stdout.write(JSON.stringify({ok:false,error:'controlled foreign receipt',nativeMutation:{transactionHash:'b'.repeat(64),executionSucceeded:true}}));
   else if(mode==='prepared-bad-utf8')process.stdout.write(Buffer.from([255]));
   else if(mode==='prepared-bad-json')process.stdout.write('invalid JSON');
   else process.stdout.write(JSON.stringify({ok:true,result:{transactionHash:(mode==='prepared-wrong'?'b':'a').repeat(64)}}));
  }catch{process.stdout.write(JSON.stringify({ok:false,error:'controlled persistence rejection'}));process.exitCode=1;}
 }
 else if(mode==='hold')setInterval(()=>{},1000);
 else if(mode==='empty'){process.stderr.write('untrusted test-only-secret');process.exitCode=1;}
 else if(mode==='bad-json'){process.stdout.write('invalid JSON');process.exitCode=1;}
 else if(mode==='bad-utf8'){process.stdout.write(Buffer.from([255]));process.exitCode=1;}
 else {
  const envelope=mode==='no-ok'?{}:mode==='bad-ok'?{ok:'true'}:mode==='no-result'?{ok:true}
   :mode==='null-result'?{ok:true,result:null}:mode==='no-hash'?{ok:true,result:{}}
   :mode==='invalid-hash'?{ok:true,result:{transactionHash:'fabricated'}}
   :mode==='preflight'?{ok:false,error:'controlled preflight error'}:{ok:true,result:{transactionHash:'a'.repeat(64)}};
  process.stdout.write(JSON.stringify(envelope));if(mode==='preflight')process.exitCode=1;
 }
}
else if(request.operation==='nativeFeeReceipt')result={transactionHash:request.transactionHash,chainId:'fixture-chain',basis:'observed-ante-fee-deduction',payer:'fixture-owner',success:false,fees:[{denom:'stake',amount:'5'}]};
else if(['upload','instantiate'].includes(request.operation)){
 result={transactionHash:'a'.repeat(64)};
 const field=request.operation==='upload'?'codeId':'contractAddress';
 const values=request.operation==='upload'?{valid:2,zero:0,negative:-1,string:'2',null:null,overflow:1e30,fraction:1.5}
  :{valid:'fixture-contract',blank:' ',number:2,null:null};
 if(Object.hasOwn(values,request.mode))result[field]=values[request.mode];
}
else if(request.operation==='restoreKey')result={publicKey:'fixture-public',address:'fixture-owner'};
else if(request.operation==='verifyNativePreparation')result={verified:request.preparation.mode==='valid',transactionHash:request.preparation.transactionHash,sender:request.preparation.sender};
else if(request.operation==='verifyNftCustodyReceipt'){
 if(request.contractAddress!=='fixture-contract'||request.tokenId!=='fixture-token'||request.custody!=='fixture-owner')
  error='Custody selectors were not forwarded';
 else if(request.transactionHash==='a'.repeat(64))result={verified:true,transactionHash:request.transactionHash};
 else if(request.transactionHash==='b'.repeat(64))result={verified:true,transactionHash:'a'.repeat(64)};
 else error='Source custody transfer not found';
}
else if(request.operation==='execute'){
 const mint=request.message?.asset?.message?.nft_mint;
 const burn=request.message?.asset?.message?.nft_burn??request.message?.asset?.message?.token_burn;
 if(burn?.token_id?.startsWith('native-evidence-')) {
  const mode=burn.token_id.slice('native-evidence-'.length);
  const nativeMutation=mode==='bad-shape' ? [] : { transactionHash:mode==='unknown' || mode==='unknown-executed' ? null : mode==='invalid-hash' ? 'fabricated' : 'a'.repeat(64),
   ...(mode==='missing-status' ? {} : {executionSucceeded:mode==='failed' ? false : mode==='executed' || mode==='unknown-executed' ? true : mode==='bad-status' ? 'false' : null}) };
  failureEnvelope={ok:false,...(mode==='missing-error' ? {} : {error:mode==='bad-error' ? [] : 'original controlled native error'}),
   ...(mode==='ordinary' ? {} : {nativeMutation})};
 }
 if(burn&&request.fromWalletAddress!=null)throw new Error('Public key must not be forwarded as a burn wallet address.');
 if(mint){const metadata=JSON.parse(mint.metadata_json);
  if(metadata.StoreNFTMetaDataOnChain===false){
   if(!metadata.JSONMetaDataURL || mint.metadata_json.includes('private-canary'))throw new Error('Off-chain data was silently published.');
  }
  else if(metadata.Price!==12.5||metadata.Discount!==2||metadata.RoyaltyPercentage!==7||metadata.SellerFeeBasisPoints!==700||metadata.MemoText!=='mint memo')
   throw new Error('Mint economic metadata was not serialized.');
  if(process.env.OASIS_NATIVE_PREPARATION_PIPE){
   const {pathToFileURL}=await import('node:url');
   const {preparationClient}=await import(pathToFileURL(process.env.OASIS_NATIVE_PREPARATION_MODULE).href);
   try{await preparationClient(process.env.OASIS_NATIVE_PREPARATION_PIPE,5000)({transactionHash:'a'.repeat(64),
    signedTransactionBase64:'AQID',signDocBase64:'AQID',sender:'fixture-owner',chainId:request.chainId,mode:'valid'});}
   catch{error='controlled preparation rejection';}
  }
 }
 if(request.privateKey!=='test-only-explicit')error='Explicit signer was not forwarded';
 else result={transactionHash:'a'.repeat(64)};
}else if(request.operation==='query'){
 const tokenId=request.message?.asset?.message?.nft?.token_id;
 if(tokenId?.startsWith('ownership-'))result={token_id:tokenId,locked:false,
  owner:tokenId==='ownership-matching'?'fixture-recipient':'fixture-owner',
  metadata_json:JSON.stringify({NFTTokenAddress:'fixture-contract#'+tokenId})};
 else error='forced separate NFT readback failure';
}
else error='Unexpected fixture operation';
if(!handled)process.stdout.write(JSON.stringify(failureEnvelope??(error?{ok:false,error}:{ok:true,result})));if(error||failureEnvelope)process.exitCode=1;
