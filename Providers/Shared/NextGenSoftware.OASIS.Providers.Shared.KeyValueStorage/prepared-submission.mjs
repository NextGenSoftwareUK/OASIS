import net from 'node:net';

// Local transport only. The parent owns the endpoint and durable persistence.
function validateTransport(endpoint,timeoutMs) {
  if (typeof endpoint !== 'string' || !(endpoint.startsWith('\\\\.\\pipe\\oasis-native-') || endpoint.startsWith('/tmp/oasis-native-') || endpoint.startsWith('/tmp/CoreFxPipe_oasis-native-')))
    throw new Error('An explicitly owned local preparation pipe is required');
  if (!Number.isSafeInteger(timeoutMs) || timeoutMs <= 0 || timeoutMs > 30000)
    throw new Error('A bounded preparation acknowledgement deadline is required');
}

function exchange(endpoint,timeoutMs,envelope,matches) {
  return new Promise((resolve,reject)=>{
    const request=JSON.stringify(envelope)+'\n';
    if (Buffer.byteLength(request)>1048576) return reject(new Error('Prepared evidence exceeds transport limit'));
    const socket=net.createConnection(endpoint);
    let response=Buffer.alloc(0), complete=false;
    const finish=error=>{
      if(complete)return;complete=true;clearTimeout(timer);socket.destroy();
      if(error)reject(error);else resolve();
    };
    const timer=setTimeout(()=>finish(new Error('Prepared persistence acknowledgement timed out')),timeoutMs);
    socket.once('connect',()=>socket.write(request));
    socket.on('error',()=>finish(new Error('Prepared persistence transport failed')));
    socket.once('close',()=>{if(!complete)finish(new Error('Prepared persistence acknowledgement was not received'));});
    socket.on('data',chunk=>{
      response=Buffer.concat([response,chunk]);
      if(response.length>4096)return finish(new Error('Prepared acknowledgement exceeds transport limit'));
      const newline=response.indexOf(10);if(newline<0)return;
      try {
        if(newline!==response.length-1)throw new Error();
        const reply=JSON.parse(new TextDecoder('utf-8',{fatal:true}).decode(response.subarray(0,newline)));
        if(reply.protocol!=='oasis.native-preparation.v1' || reply.persisted!==true || !matches(reply))
          throw new Error();
        finish();
      } catch {finish(new Error('Prepared persistence acknowledgement is invalid'));}
    });
  });
}

export function preparationClient(endpoint,timeoutMs=30000) {
  validateTransport(endpoint,timeoutMs);
  return evidence=>{
    if(!evidence||typeof evidence.transactionHash!=='string'||!evidence.transactionHash)
      return Promise.reject(new Error('Original prepared identity is required'));
    const identity=evidence.transactionHash;
    return exchange(endpoint,timeoutMs,{protocol:'oasis.native-preparation.v1',evidence},reply=>reply.transactionHash===identity);
  };
}

export function preparationSequenceClient(endpoint,maximumRequests,timeoutMs=30000) {
  const prepare=preparationClient(endpoint,timeoutMs);
  if(!Number.isInteger(maximumRequests)||maximumRequests<1||maximumRequests>512)
    throw new Error('Bounded physical preparation request count is required');
  let count=0,busy=false,closed=false,failed=false;
  return Object.freeze({
    persist:async evidence=>{
      if(busy||closed||failed||count>=maximumRequests)throw new Error('Preparation sequence is unavailable or exhausted');
      busy=true;
      try {await prepare(evidence);count++;}
      catch(error){failed=true;throw error;}
      finally{busy=false;}
    },
    complete:async()=>{
      if(busy||closed||failed)throw new Error('Preparation sequence cannot complete');
      closed=true;
      await exchange(endpoint,timeoutMs,{protocol:'oasis.native-preparation.v1',completed:true,preparedCount:count},
        reply=>reply.completed===true&&reply.preparedCount===count);
    }
  });
}
