// Controlled transport fixture only. No CosmJS, node or ledger is used.
let input='';for await(const chunk of process.stdin)input+=chunk;const request=JSON.parse(input);
let result,error,failureEnvelope;
if(request.operation==='restoreKey')result={publicKey:'fixture-public',address:'fixture-owner'};
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
  const nativeMutation=mode==='bad-shape' ? [] : { transactionHash:mode==='unknown' ? null : 'a'.repeat(64),
   ...(mode==='missing-status' ? {} : {executionSucceeded:mode==='failed' ? false : mode==='executed' ? true : mode==='bad-status' ? 'false' : null}) };
  failureEnvelope={ok:false,...(mode==='missing-error' ? {} : {error:mode==='bad-error' ? [] : 'original controlled native error'}),
   ...(mode==='ordinary' ? {} : {nativeMutation})};
 }
 if(burn&&request.fromWalletAddress!=null)throw new Error('Public key must not be forwarded as a burn wallet address.');
 if(mint){const metadata=JSON.parse(mint.metadata_json);
  if(metadata.Price!==12.5||metadata.Discount!==2||metadata.RoyaltyPercentage!==7||metadata.SellerFeeBasisPoints!==700||metadata.MemoText!=='mint memo')
   throw new Error('Mint economic metadata was not serialized.');}
 if(request.privateKey!=='test-only-explicit')error='Explicit signer was not forwarded';
 else result={transactionHash:'fixture-committed-cosmos-nft'};
}else if(request.operation==='query')error='forced separate NFT readback failure';
else error='Unexpected fixture operation';
process.stdout.write(JSON.stringify(failureEnvelope??(error?{ok:false,error}:{ok:true,result})));if(error||failureEnvelope)process.exitCode=1;
