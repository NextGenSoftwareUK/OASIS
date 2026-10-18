// Controlled C# request forwarding only; never contacts RPC or signs.
let text='';for await(const chunk of process.stdin)text+=chunk;
const input=JSON.parse(text);
if(input.operation!=='transactions'||input.walletAddress!=='controlled-wallet'||input.fromBlock!==40||input.toBlock!==45){
  process.stdout.write(JSON.stringify({ok:false,error:'Exact history range was not forwarded'}));process.exitCode=1;
}else process.stdout.write(JSON.stringify({ok:true,result:[]}));
