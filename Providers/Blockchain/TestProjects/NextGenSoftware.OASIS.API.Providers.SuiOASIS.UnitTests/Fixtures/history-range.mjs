// Controlled public C# history request projection; no ledger or signing.
let text='';for await(const chunk of process.stdin)text+=chunk;
const input=JSON.parse(text);
const expected={
  '1':['40','45'], '2':['0','0'], '3':['9007199254740993','18446744073709551615'], '4':[null,null],
}[input.walletAddress?.slice(-1)];
if(input.operation!=='history'||!expected||input.fromBlock!==expected[0]||input.toBlock!==expected[1]){
  process.stdout.write(JSON.stringify({ok:false,error:'Exact checkpoint range was not forwarded'}));process.exitCode=1;
}else process.stdout.write(JSON.stringify({ok:true,result:[]}));
