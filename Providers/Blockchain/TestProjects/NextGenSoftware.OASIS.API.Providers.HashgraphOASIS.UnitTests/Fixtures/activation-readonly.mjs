// Controlled operation sequencing only, not live authority or free-query proof.
let input='';for await(const chunk of process.stdin)input+=chunk;
const {action}=JSON.parse(input);
if(action!=='storageProbe'){process.stderr.write(JSON.stringify({error:'unexpected activation mutation'}));process.exitCode=1;}
else process.stdout.write(JSON.stringify({owner:'controlled-owner',sender:'0.0.1001',contractId:'0.0.2001'}));
