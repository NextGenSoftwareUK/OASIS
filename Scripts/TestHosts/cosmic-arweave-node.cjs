const fs = require('node:fs');
const path = require('node:path');
const net = require('node:net');
const readline = require('node:readline');
const {once} = require('node:events');
const root = path.resolve(__dirname,'../..');
require('./Prepare-CosmicArweaveRuntime.cjs').prepare();
const runtime = path.join(root,'LocalState/COSMIC/ArweaveRuntime');
const ArLocal = require(path.join(runtime,'node_modules/arlocal')).default;
const state = path.join(root,'LocalState/COSMIC/ArweaveCql/data');
const port = 18549;
async function main() {
  // Binding fails on an occupied port; no unknown service is adopted or stopped.
  const probe = net.createServer();probe.listen(port,'127.0.0.1');await once(probe,'listening');
  await new Promise((resolve,reject)=>probe.close(error=>error?reject(error):resolve()));
  fs.mkdirSync(state,{recursive:true});
  const node = new ArLocal(port,false,state,true);
  let stopping;
  const stop = () => stopping ??= node.stop();
  await node.start();
  const server=node.getServer();if(!server.listening)await once(server,'listening');
  const address=server.address();if(address.address!=='127.0.0.1')throw new Error('Native listener not loopback.');
  console.log(JSON.stringify({ready:true,pid:process.pid,port,host:address.address,persistent:true}));
  const lines=readline.createInterface({input:process.stdin});
  for await(const line of lines) {
    if(line !== 'stop')throw new Error('Unknown gateway lifecycle command.');
    await stop();lines.close();console.log(JSON.stringify({stopped:true}));return;
  }
  await stop();
}
main().catch(error=>{console.error(error.message);process.exitCode=1;});
