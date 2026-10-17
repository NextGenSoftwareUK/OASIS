// Controlled process fixture only: no SDK or ledger operation occurs.
let input='';for await(const chunk of process.stdin)input+=chunk;
const request=JSON.parse(input);let result;
switch(request.operation){
 case 'tokenEscrowCreate':
  if(request.privateKey!=='test-only-explicit'||request.fromWalletAddress!=='fixture-owner')throw new Error('Original registry creator changed');
  result={transactionHash:'1'.repeat(32),escrowObjectId:'0x'+'2'.repeat(64),creator:'fixture-owner',protocol:1};break;
 case 'tokenEscrowLock':
 case 'tokenEscrowRelease': {
  const releasing=request.operation==='tokenEscrowRelease';
  if(request.depositId!=='e9633e93c317a2759f2c757ef61a67e8fae6f739cb86fedc9cba54be2d8681de'
   ||(!releasing&&request.amountUnits!=='1230000000')||request.fromWalletAddress!=='fixture-owner'
   ||request.escrowObjectId!=='0x'+'2'.repeat(64))throw new Error('Original escrow intent changed');
  if(process.env.OASIS_SUI_ESCROW_FIXTURE_STATE){
   if(request.privateKey!=='test-only-explicit')throw new Error('Default encrypted wallet signer was not used');
   const fs=await import('node:fs');const path=process.env.OASIS_SUI_ESCROW_FIXTURE_STATE;
   const state=JSON.parse(fs.readFileSync(path,'utf8'));
   if(releasing&&(!state.deposit||state.deposit.released))throw new Error('Original active deposit required');
   state.submissions++;state.deposit={depositId:request.depositId,amount:'1230000000',released:releasing};
   fs.writeFileSync(path,JSON.stringify(state));
  }else if(releasing)throw new Error('Release requires controlled stored-wallet fixture state');
  result={transactionHash:'1'.repeat(32),depositId:request.depositId,amountUnits:request.privateKey==='test-only-bad-reply'?'1':request.amountUnits,
   owner:'fixture-owner',released:releasing,escrowObjectId:request.escrowObjectId};
  if(releasing)result.amountUnits='1230000000';break;
 }
 case 'history':result=[{digest:'controlled-history',amountUnits:'0',coinType:request.walletAddress==='fixture-coin'?'0x2::sui::SUI':null,
  recipients:[],sender:request.walletAddress,walletAddress:request.walletAddress,success:true,decimals:9,timestampMs:1700000000000}];break;
 case 'tokenSend':
 case 'transfer':
  if(request.fromWalletAddress?.startsWith('intent-') || request.operation==='tokenSend') {
   if(request.fromWalletAddress!=='intent-owner' || (request.recipient??request.toWalletAddress)!=='intent-recipient'
    || request.amountUnits!=='1230000000' || request.privateKey!=='test-only-explicit')throw new Error('Original token send intent was changed.');
   result={transactionHash:'1'.repeat(32)};break;
  }
  process.stdout.write(JSON.stringify({ok:false,error:'controlled original transfer outcome',nativeMutation:{transactionHash:'1'.repeat(32),executionSucceeded:true}}));process.exitCode=1;break;
 case 'nativeFeeReceipt':result={transactionHash:request.transactionHash,currency:'SUI',unit:'MIST',payer:'fixture-owner',chainIdentifier:'fixture-chain',success:false,netUnits:'5'};break;
 case 'verifyNativePreparation':result={verified:request.preparation.mode==='valid'||request.preparation.signature?.startsWith('fixture-verified'),transactionHash:request.preparation.transactionHash,sender:request.preparation.sender,gasPayer:request.preparation.signature==='fixture-verified-foreign-payer'?'foreign-payer':'fixture-owner'};break;
 case 'put': case 'get': {
  const mode=request.mode;
  if(mode==='prepared' || mode==='prepared-wrong-id' || mode==='prepared-hold' || mode==='prepared-error') {
   const {pathToFileURL}=await import('node:url');
   const {preparationClient}=await import(pathToFileURL(process.env.OASIS_NATIVE_PREPARATION_MODULE).href);
   try {
    await preparationClient(process.env.OASIS_NATIVE_PREPARATION_PIPE,5000)({transactionHash:'1'.repeat(32),signedTransactionBase64:'AQID'});
    if(mode==='prepared-hold')setInterval(()=>{},1000);
    else if(mode==='prepared-error')process.stdout.write(JSON.stringify({ok:false,error:'controlled foreign receipt',nativeMutation:{transactionHash:'D83kgAWQQbdZdCFbFQivXuRF7MB5YqiqGStZAynz3GGk',executionSucceeded:true}}));
    else process.stdout.write(JSON.stringify({ok:true,result:{transactionHash:mode==='prepared-wrong-id'?'D83kgAWQQbdZdCFbFQivXuRF7MB5YqiqGStZAynz3GGk':'1'.repeat(32)}}));
   } catch {process.stdout.write(JSON.stringify({ok:false,error:'controlled preparation rejection'}));process.exitCode=1;}
   break;
  }
  if(mode==='hold') {setInterval(()=>{},1000);break;}
  if(mode==='empty') {process.stderr.write('untrusted diagnostic test-only-secret');process.exitCode=1;break;}
  if(mode==='bad-json') {process.stdout.write('invalid JSON');process.exitCode=1;break;}
  if(mode==='bad-utf8') {process.stdout.write(Buffer.from([255]));process.exitCode=1;break;}
  const envelope=mode==='no-ok'?{}:mode==='bad-ok'?{ok:'true'}:mode==='no-result'?{ok:true}
   :mode==='null-result'?{ok:true,result:null}:mode==='no-hash'?{ok:true,result:{}}
   :mode==='preflight'?{ok:false,error:'controlled preflight error'}:{ok:true,result:{transactionHash:'1'.repeat(32)}};
  process.stdout.write(JSON.stringify(envelope));if(mode==='preflight')process.exitCode=1;
  break;
 }
 case 'restoreKey':result={privateKey:request.privateKey,publicKey:'fixture-public',address:'fixture-owner'};break;
 case 'nftMint': {
  const firstMetadata=JSON.parse(request.metadata[0]);
  const title=firstMetadata.Title??(firstMetadata.StoreNFTMetaDataOnChain===false?'identity/valid':'');
  if(title.startsWith('identity/')) {
   const mode=title.split('/')[1];
   if(process.env.OASIS_NATIVE_PREPARATION_PIPE) {
    const {pathToFileURL}=await import('node:url');
    const {preparationClient}=await import(pathToFileURL(process.env.OASIS_NATIVE_PREPARATION_MODULE).href);
    try {
     await preparationClient(process.env.OASIS_NATIVE_PREPARATION_PIPE,5000)({transactionHash:'1'.repeat(32),
      signedTransactionBase64:'AQID',signature:'controlled-fixture-signature',sender:'fixture-owner',
      chainId:mode==='foreign-preparation-chain'?'foreign-chain':request.chainId,mode:mode==='invalid-signature'?'invalid':'valid'});
    } catch {
     process.stdout.write(JSON.stringify({ok:false,error:'controlled preparation rejection'}));process.exitCode=1;break;
    }
   }
   const ids=request.metadata.map(metadata=>JSON.parse(metadata).Id);
   if(firstMetadata.StoreNFTMetaDataOnChain===false && request.metadata.some(value=>value.includes('private-canary')))
    throw new Error('Off-chain request data was silently published.');
   const tokenIds=ids.map((id,index)=>`identity:${mode}:${id}:${ids[0]}:${Buffer.from(request.metadata[index]).toString('base64url')}`);
   if(mode==='missing')tokenIds.pop();
   if(mode==='duplicate-token')tokenIds[1]=tokenIds[0];
   result={transactionHash:'1'.repeat(32),tokenIds,recipient:mode==='foreign-recipient'?'foreign-owner':mode==='short-recipient'?'0x'+'ab'.padStart(64,'0'):'fixture-owner'};
   break;
  }
  if(title.startsWith('native-evidence/')) {
   const mode=title.split('/')[1];
   const mutation=mode==='bad-shape' ? [] : {
    transactionHash:mode==='unknown' || mode==='unknown-executed' ? null : mode==='invalid-digest' ? 'fabricated' : '1'.repeat(32),
    ...(mode==='missing-status' ? {} : {executionSucceeded:mode==='failed' ? false : mode==='executed' || mode==='unknown-executed' ? true : mode==='bad-status' ? 'false' : null})
   };
   process.stdout.write(JSON.stringify({ok:false,...(mode==='missing-error' ? {} : {error:mode==='bad-error' ? [] : 'original controlled native error'}),...(mode==='ordinary' ? {} : {nativeMutation:mutation})}));
   process.exitCode=1;
   break;
  }
  result={transactionHash:'1'.repeat(32),tokenIds:['fixture-nft'],recipient:'fixture-owner'};break;
 }
 case 'nftEscrowGet':result={nft:{metadata:JSON.stringify({Id:'9daf56a1-a66d-4507-a5bc-a31de17e223a'})}};break;
 case 'nftUnlock':result={transactionHash:'1'.repeat(32)};break;
 case 'nftSend':
  if(request.privateKey!=='test-only-explicit')throw new Error('Explicit signer was not forwarded.');
  result={transactionHash:'1'.repeat(32)};break;
 case 'nftGet':
  if(request.tokenId.startsWith('identity:')) {
   const [,mode,id,firstId,original]=request.tokenId.split(':');
   const metadata=JSON.parse(Buffer.from(original,'base64url').toString());
   metadata.Id=mode==='foreign-id'?'9daf56a1-a66d-4507-a5bc-a31de17e223a':mode==='duplicate-id'?firstId:id;
   result={metadata:JSON.stringify(metadata),
    creator:mode==='foreign-creator'?'foreign-creator':'fixture-owner',owner:{AddressOwner:mode==='short-recipient'?'0x'+'ab'.padStart(64,'0'):'fixture-owner'}};
  }
  else if(request.tokenId.startsWith('ownership-'))result={metadata:'{}',creator:'fixture-owner',
   owner:{AddressOwner:'0x'+(request.tokenId==='ownership-matching'?'ab':'cd').repeat(32)}};
  else {process.stdout.write(JSON.stringify({ok:false,error:'forced separate NFT readback failure'}));process.exitCode=1;}
  break;
 default:throw new Error('Unexpected fixture operation.');
}
if(result)process.stdout.write(JSON.stringify({ok:true,result}));
