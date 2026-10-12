import test from 'node:test';
import assert from 'node:assert/strict';
import net from 'node:net';
import { preparationClient } from './prepared-submission.mjs';
const endpoint=process.platform==='win32'?'\\\\.\\pipe\\oasis-native-preparation-tests':'/tmp/oasis-native-preparation-tests';
for(const mode of ['persisted','wrong-hash','rejected','disconnect','invalid-json','deadline']) {
 test(`local prepared acknowledgement ${mode}`,async()=>{
  const sockets=new Set();
  const server=net.createServer(socket=>{
   sockets.add(socket);socket.once('close',()=>sockets.delete(socket));
   let request='';socket.on('data',data=>{
    request+=data; if(!request.endsWith('\n'))return;
    const parsed=JSON.parse(request);assert.equal(parsed.protocol,'oasis.native-preparation.v1');
    assert.equal(parsed.evidence.signedTransactionBase64,'AQID');
    if(mode==='deadline')return;
    if(mode==='disconnect'){socket.destroy();return;}
    socket.write(mode==='invalid-json'?'invalid\n':JSON.stringify({protocol:parsed.protocol,persisted:mode!=='rejected',transactionHash:mode==='wrong-hash'?'other':parsed.evidence.transactionHash})+'\n');
   });
  });
  await new Promise((resolve,reject)=>{server.once('error',reject);server.listen(endpoint,resolve);});
  try {
   const attempt=preparationClient(endpoint,mode==='deadline'?20:1000)({transactionHash:'original',signedTransactionBase64:'AQID'});
   if(mode==='persisted')await attempt;else await assert.rejects(attempt);
  } finally {
   for(const socket of sockets)socket.destroy();
   await new Promise(resolve=>server.close(resolve));
  }
 });
}
