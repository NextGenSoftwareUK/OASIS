// Read-only cross-platform control. No SDK/signing/key, ledger initialization or mutation.
const endpoint='http://127.0.0.1:3030';
const failures=[];let passed=0;
for(let index=0;index<16;index++){
  try{
    const response=await fetch(endpoint,{method:'POST',headers:{'content-type':'application/json'},
      body:JSON.stringify({jsonrpc:'2.0',id:`endpoint-observation-${index}`,method:'status',params:[]}),signal:AbortSignal.timeout(1500)});
    if(!response.ok)throw Object.assign(new Error('HTTP status failure'),{code:`HTTP_${response.status}`});
    const body=await response.json();
    if(body.error||body.result?.chain_id!=='oasis-near-local')throw Object.assign(new Error('Unexpected chain status'),{code:'INVALID_CHAIN_STATUS'});
    passed++;
  }catch(error){failures.push({index,code:error.cause?.code??error.code??error.name});}
}
console.log(JSON.stringify({Mode:'read-only-endpoint-control',Platform:process.platform,Node:process.version,Endpoint:endpoint,
  Requests:16,Passed:passed,Failures:failures,NoSignerOrMutation:true}));
if(failures.length)process.exitCode=1;
