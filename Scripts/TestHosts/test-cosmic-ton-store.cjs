const assert=require('node:assert/strict');
const fs=require('node:fs');
const path=require('node:path');
const crypto=require('node:crypto');
const {spawnSync}=require('node:child_process');
const {test}=require('node:test');
const root=path.resolve(__dirname,'../..');
const sdk=path.join(root,'Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.TONOASIS/SdkBridge');
const {TonSandboxStore,MODE}=require(path.join(sdk,'sandbox-store.cjs'));
const binding=path.join(root,'TestResults/TonCqlCurrent/compiled/TonHolonStore_TonHolonStore.cjs');
const {TonHolonStore}=require(binding);
const {Address}=require('@ton/core');
const hash=bytes=>crypto.createHash('sha256').update(bytes).digest('hex');

test('Native TVM source owns persistence and rejects uncertain or mismatched operations',async t=>{
    const directory=path.join(root,'TestResults/TonCqlCurrent/store-tests');
    assert(!fs.existsSync(directory),'Preserve an existing fixture; never reset it.');
    fs.mkdirSync(directory);
    const sourceId=crypto.randomUUID();
    const code=(await TonHolonStore.init(Address.parseRaw('0:'+'1'.repeat(64)))).code;
    const options={binding,directory,sourceId,expectedCodeHash:code.hash().toString('hex')};
    const store=new TonSandboxStore(options);
    const request=(operation,values={})=>({mode:MODE,sourceId,operation,...values});
    const state=()=>fs.readFileSync(path.join(directory,'source.json'));
    const operations=[];
    const keys=['oasis/private/v1/aaa','oasis/private/v1/bbb','oasis/private/v1/ccc'];
    const values=['transport marker ☕','9007199254740993','x'.repeat(4096)];
    await t.test('Explicit creation is durable and refuses source replacement',async()=>{
        const operationId=crypto.randomUUID();operations.push(operationId);
        const created=await store.execute(request('create',{operationId}));
        assert.equal(created.result.operationId,operationId);
        assert.equal(fs.existsSync(path.join(directory,'pending.json')),false);
        await assert.rejects(store.execute(request('create',{operationId:crypto.randomUUID()})),{code:'TON_SOURCE_EXISTS'});
        assert.equal((await store.execute(request('verify'))).result.codeHash,options.expectedCodeHash);
    });
    await t.test('Three actual TVM writes persist exact values and native prefix keys',async()=>{
        for(let index=0;index<keys.length;index++) {
            const operationId=crypto.randomUUID();operations.push(operationId);
            const result=await store.execute(request('put',{operationId,key:keys[index],value:values[index]}));
            assert.equal(result.result.operationId,operationId);
            assert.match(result.result.transactionHash,/^[0-9a-f]{64}$/);
        }
        assert.deepEqual((await store.execute(request('list',{prefix:'oasis/private/v1/'}))).result,keys);
        assert.deepEqual((await store.execute(request('list',{prefix:'absent/'}))).result,[]);
    });
    await t.test('Independent process reopens the original source without writes',async()=>{
        const before=hash(state());
        for(const key of [...keys,'missing']) {
            const child=spawnSync(process.execPath,[path.join(sdk,'native-worker.cjs'),binding,directory,sourceId,options.expectedCodeHash],
                {input:JSON.stringify(request('get',{key})),encoding:'utf8',timeout:30000,maxBuffer:1024*1024});
            assert.equal(child.status,0,child.stderr+' '+child.stdout);
            assert.equal(JSON.parse(child.stdout).result,keys.includes(key)?values[keys.indexOf(key)]:null);
        }
        assert.equal(hash(state()),before);
        assert.equal(fs.readdirSync(path.join(directory,'operations')).length,operations.length);
    });
    await t.test('Recorded operations cannot be resubmitted and wrong sources cannot read',async()=>{
        const before=hash(state());
        await assert.rejects(store.execute(request('put',{operationId:operations[1],key:keys[0],value:'replay'})),{code:'TON_OPERATION_COMMITTED'});
        const other=new TonSandboxStore({...options,sourceId:crypto.randomUUID()});
        await assert.rejects(other.execute({...request('get',{key:keys[0]}),sourceId:other.sourceId}),{code:'TON_SOURCE_MISMATCH'});
        const wrongCode=new TonSandboxStore({...options,expectedCodeHash:'0'.repeat(64)});
        await assert.rejects(wrongCode.execute(request('get',{key:keys[0]})),{code:'TON_SOURCE_MISMATCH'});
        await assert.rejects(wrongCode.execute(request('inspect',{operationId:operations[0]})),{code:'TON_SOURCE_MISMATCH'});
        await assert.rejects(store.execute(request('put',{operationId:crypto.randomUUID(),key:'\ud800',value:'invalid'})),{code:'TON_KEY'});
        assert.equal(hash(state()),before);
    });
    await t.test('Pending intent blocks reads and writes; inspection never replays it',async()=>{
        const operationId=crypto.randomUUID();
        const before=hash(state());
        const pending={mode:MODE,sourceId,operationId,requestHash:'a'.repeat(64),beforeHash:before,
            expectedCodeHash:options.expectedCodeHash,operation:'put',injectedByThisTestWithoutSubmission:true};
        const pendingPath=path.join(directory,'pending.json');
        fs.writeFileSync(pendingPath,JSON.stringify(pending),{flag:'wx'});
        const intentHash=hash(fs.readFileSync(pendingPath));
        await assert.rejects(store.execute(request('get',{key:keys[0]})),{code:'TON_OUTCOME_PENDING'});
        await assert.rejects(store.execute(request('put',{operationId:crypto.randomUUID(),key:keys[0],value:'forged'})),{code:'TON_OUTCOME_PENDING'});
        assert.equal((await store.execute(request('inspect',{operationId}))).result.status,'Pending');
        assert.equal(hash(state()),before);
        assert.equal(hash(fs.readFileSync(pendingPath)),intentHash);
        // This is an explicitly injected, never-submitted test fence. Do not
        // generalize its teardown to recovery of a real pending operation.
        assert.equal(JSON.parse(fs.readFileSync(pendingPath,'utf8')).injectedByThisTestWithoutSubmission,true);
        fs.unlinkSync(pendingPath);
    });
    await t.test('Acknowledgements retain source and operation provenance',async()=>{
        for(const operationId of operations) {
            const result=await store.execute(request('inspect',{operationId}));
            assert.equal(result.result.status,'Acknowledged');
            assert.equal(result.result.acknowledgement.sourceId,sourceId);
            assert.equal(result.result.acknowledgement.operationId,operationId);
            assert.equal(result.result.acknowledgement.codeHash,options.expectedCodeHash);
        }
    });
    // Exact synthetic fixture teardown after child exits and successful checks.
    assert.equal(JSON.parse(state()).sourceId,sourceId);
    assert.deepEqual(fs.readdirSync(directory).sort(),['operations','source.json']);
    const journals=fs.readdirSync(path.join(directory,'operations')).sort();
    assert.deepEqual(journals,operations.map(id=>id+'.json').sort());
    for(const file of journals) fs.unlinkSync(path.join(directory,'operations',file));
    fs.rmdirSync(path.join(directory,'operations'));
    fs.unlinkSync(path.join(directory,'source.json'));
    fs.rmdirSync(directory);
});
