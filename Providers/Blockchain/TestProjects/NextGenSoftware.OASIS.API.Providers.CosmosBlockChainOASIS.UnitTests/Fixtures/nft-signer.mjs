// Controlled transport fixture only. No CosmJS, node or ledger is used.
let input='';for await(const chunk of process.stdin)input+=chunk;const request=JSON.parse(input);
let result,error;
if(request.operation==='restoreKey')result={publicKey:'fixture-public',address:'fixture-owner'};
else if(request.operation==='execute'){
 const mint=request.message?.asset?.message?.nft_mint;
 const burn=request.message?.asset?.message?.nft_burn??request.message?.asset?.message?.token_burn;
 if(burn&&request.fromWalletAddress!=null)throw new Error('Public key must not be forwarded as a burn wallet address.');
 if(mint){const metadata=JSON.parse(mint.metadata_json);
  if(metadata.Price!==12.5||metadata.Discount!==2||metadata.RoyaltyPercentage!==7||metadata.SellerFeeBasisPoints!==700||metadata.MemoText!=='mint memo')
   throw new Error('Mint economic metadata was not serialized.');}
 if(request.privateKey!=='test-only-explicit')error='Explicit signer was not forwarded';
 else result={transactionHash:'fixture-committed-cosmos-nft'};
}else if(request.operation==='query')error='forced separate NFT readback failure';
else error='Unexpected fixture operation';
process.stdout.write(JSON.stringify(error?{ok:false,error}:{ok:true,result}));if(error)process.exitCode=1;
