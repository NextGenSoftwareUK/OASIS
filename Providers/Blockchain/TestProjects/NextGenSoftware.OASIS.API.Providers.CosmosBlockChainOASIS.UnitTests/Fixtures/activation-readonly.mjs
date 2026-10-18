// Controlled C# activation sequencing only; no ledger or SDK authority claim.
let input='';for await(const chunk of process.stdin)input+=chunk;
const request=JSON.parse(input);
if(request.operation!=='probe'){
 process.stdout.write(JSON.stringify({ok:false,error:'Activation attempted a non-probe operation'}));process.exitCode=1;
}else process.stdout.write(JSON.stringify({ok:true,result:{chainId:request.chainId,config:{owner:'controlled-owner'}}}));
