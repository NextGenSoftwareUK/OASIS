// Controlled operation sequencing only, not live authority or free-query proof.
let input='';for await(const chunk of process.stdin)input+=chunk;
const {action}=JSON.parse(input);
if(action==='storageList')process.stdout.write('[]');
else if(action==='storageProbe')process.stdout.write(JSON.stringify({owner:'controlled-owner',sender:'0.0.1001',contractId:'0.0.2001'}));
else {process.stderr.write(JSON.stringify({error:'unexpected activation mutation'}));process.exitCode=1;}
