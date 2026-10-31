// Test-only crash boundary: use the retained real SDK, then exit without returning its confirmed put reply.
import {pathToFileURL} from 'node:url';
let input='';for await(const chunk of process.stdin){input+=chunk;if(Buffer.byteLength(input)>1048576)throw new Error('Input limit');}
const request=JSON.parse(input);
const {invoke}=await import(pathToFileURL(process.env.OASIS_SUI_TEST_REAL_BRIDGE).href);
let persist;
if(process.env.OASIS_NATIVE_PREPARATION_PIPE){
 const {preparationClient}=await import(pathToFileURL(process.env.OASIS_NATIVE_PREPARATION_MODULE).href);
 persist=preparationClient(process.env.OASIS_NATIVE_PREPARATION_PIPE);
}
try{
 const result=await invoke(request,persist);
 if(request.operation==='put'&&request.key.startsWith('oasis/prepared-test/'))process.exit(17);
 process.stdout.write(JSON.stringify({ok:true,result}));
}catch{process.stdout.write(JSON.stringify({ok:false,error:'test child failed'}));process.exitCode=1;}
