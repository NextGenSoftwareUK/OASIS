// Internal native-TVM process protocol. Host configuration is separate from
// request data; it must never be filled from a browser-supplied provider scope.
const {TonSandboxStore, TonStoreError} = require('./sandbox-store.cjs');

(async () => {
    const [binding,directory,sourceId,expectedCodeHash]=process.argv.slice(2);
    let input='';
    process.stdin.setEncoding('utf8');
    for await(const chunk of process.stdin) {
        input+=chunk;
        if(Buffer.byteLength(input)>512*1024) throw new TonStoreError('TON_LIMIT','Request budget exceeded.');
    }
    const store=new TonSandboxStore({binding,directory,sourceId,expectedCodeHash});
    const result=await store.execute(JSON.parse(input));
    const output=JSON.stringify(result);
    if(Buffer.byteLength(output)>4*1024*1024) throw new TonStoreError('TON_LIMIT','Complete response budget exceeded.');
    process.stdout.write(output);
})().catch(error => {
    process.stdout.write(JSON.stringify({ok:false,error:{
        code:error instanceof TonStoreError ? error.code : 'TON_EXECUTION_ERROR',
        message:error instanceof TonStoreError ? error.message : 'Native TVM operation failed; preserve source and journal.'
    }}));
    process.exitCode=1;
});
