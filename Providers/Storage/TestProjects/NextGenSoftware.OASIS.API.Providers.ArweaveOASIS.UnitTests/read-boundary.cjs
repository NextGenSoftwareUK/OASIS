// Protocol fixture only: no Arweave runtime/network or wallet is used.
let input = '';
process.stdin.on('data', data => input += data);
process.stdin.on('end', () => {
  const request = JSON.parse(input);
  if (request.id === 'failed') {
    process.stdout.write(JSON.stringify({ok:false,error:'synthetic gateway failure'}));
    process.exitCode = 1;
  } else {
    process.stdout.write(JSON.stringify({ok:true,dataBase64:request.id === 'malformed' ? '!' : 'AAH/'}));
  }
});
