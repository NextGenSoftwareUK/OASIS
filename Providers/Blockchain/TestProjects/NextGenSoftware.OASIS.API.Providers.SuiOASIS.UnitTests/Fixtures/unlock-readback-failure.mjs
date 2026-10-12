// Controlled process fixture only: no SDK or ledger operation occurs.
let input='';for await(const chunk of process.stdin)input+=chunk;
const request=JSON.parse(input);let result;
switch(request.operation){
 case 'verifyNativePreparation':result={verified:request.preparation.mode==='valid',transactionHash:request.preparation.transactionHash,sender:request.preparation.sender};break;
 case 'put': case 'get': {
  const mode=request.mode;
  if(mode==='prepared' || mode==='prepared-wrong-id' || mode==='prepared-hold') {
   const {pathToFileURL}=await import('node:url');
   const {preparationClient}=await import(pathToFileURL(process.env.OASIS_NATIVE_PREPARATION_MODULE).href);
   try {
    await preparationClient(process.env.OASIS_NATIVE_PREPARATION_PIPE,5000)({transactionHash:'1'.repeat(32),signedTransactionBase64:'AQID'});
    if(mode==='prepared-hold')setInterval(()=>{},1000);
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
  const title=JSON.parse(request.metadata[0]).Title;
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
 case 'nftGet':process.stdout.write(JSON.stringify({ok:false,error:'forced separate NFT readback failure'}));process.exitCode=1;break;
 default:throw new Error('Unexpected fixture operation.');
}
if(result)process.stdout.write(JSON.stringify({ok:true,result}));
