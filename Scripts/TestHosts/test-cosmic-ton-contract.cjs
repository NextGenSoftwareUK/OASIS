const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const {spawnSync} = require('node:child_process');
const {test} = require('node:test');
const root = path.resolve(__dirname, '../..');
const output = path.join(root, 'TestResults/TonCqlCurrent');
const {Blockchain, snapshotToSerializable, snapshotFromSerializable} = require('@ton/sandbox');
const {Address, beginCell, toNano} = require('@ton/core');
const {TonHolonStore} = require(path.join(output, 'compiled/TonHolonStore_TonHolonStore.cjs'));

async function readSnapshot() {
    const chain = await Blockchain.create();
    chain.verbosity = {blockchainLogs:false, vmLogs:'none', debugLogs:false};
    await chain.loadFrom(snapshotFromSerializable(JSON.parse(fs.readFileSync(process.argv[3], 'utf8'))));
    const contract = chain.openContract(TonHolonStore.fromAddress(Address.parse(process.argv[4])));
    const values = [];
    for (const key of [1n,2n,3n,99n]) {
        const cell = await contract.getRecord(key);
        values.push(cell === null ? null : cell.beginParse().loadStringTail());
    }
    console.log(JSON.stringify(values));
}

if (process.argv[2] === '--read-snapshot') {
    readSnapshot().catch(error => {console.error(error);process.exitCode=1;});
} else {
    test('Native TON TVM storage and process-reload invariants', async t => {
        const chain = await Blockchain.create();
        chain.verbosity = {blockchainLogs:false, vmLogs:'none', debugLogs:false};
        const owner = await chain.treasury('cosmic-ton-contract-owner');
        const outsider = await chain.treasury('cosmic-ton-contract-outsider');
        const contract = chain.openContract(await TonHolonStore.fromInit(owner.address));
        const values = ['Espresso ☕', 'Latte 9007199254740993', 'Tea ' + 'x'.repeat(4096)];
        const body = text => beginCell().storeStringTail(text).endCell();
        const targetTransaction = result => {
            const tx = result.transactions.find(tx => tx.inMessage?.info.type === 'internal'
                && tx.inMessage.info.dest.equals(contract.address));
            assert(tx, 'Expected a transaction at the actual TVM contract');
            assert.equal(tx.description.type,'generic');
            assert.equal(tx.description.computePhase.type,'vm');
            return tx.description;
        };
        const succeeded = result => {
            const tx = targetTransaction(result);
            assert.equal(tx.aborted,false);
            assert.equal(tx.computePhase.exitCode,0);
        };
        const stateHash = async () => {
            const account = await chain.getContract(contract.address);
            assert.equal(account.accountState.type,'active');
            return account.accountState.state.data.hash().toString('hex');
        };

        await t.test('Deployment binds the immutable storage owner', async () => {
            succeeded(await contract.send(owner.getSender(), {value:toNano('0.1')}, null));
            assert((await contract.getStorageOwner()).equals(owner.address));
            assert.equal(await contract.getRecord(1n),null);
        });

        await t.test('Three records preserve Unicode, precise strings and chained cells', async () => {
            for (let index=0;index<values.length;index++)
                succeeded(await contract.send(owner.getSender(), {value:toNano('0.1')},
                    {$$type:'PutRecord', key:BigInt(index+1), value:body(values[index])}));
            const before = await stateHash();
            for (let index=0;index<values.length;index++)
                assert.equal((await contract.getRecord(BigInt(index+1))).beginParse().loadStringTail(), values[index]);
            assert.equal(await stateHash(),before,'Getters must leave contract storage unchanged');
        });

        await t.test('Owner updates and deletes the selected key only', async () => {
            succeeded(await contract.send(owner.getSender(), {value:toNano('0.1')},
                {$$type:'PutRecord',key:1n,value:body('Updated Espresso')}));
            assert.equal((await contract.getRecord(1n)).beginParse().loadStringTail(),'Updated Espresso');
            assert.equal((await contract.getRecord(3n)).beginParse().loadStringTail(),values[2]);
            succeeded(await contract.send(owner.getSender(), {value:toNano('0.1')}, {$$type:'DeleteRecord',key:2n}));
            assert.equal(await contract.getRecord(2n),null);
            succeeded(await contract.send(owner.getSender(), {value:toNano('0.1')},
                {$$type:'PutRecord',key:2n,value:body(values[1])}));
            values[0]='Updated Espresso';
        });

        await t.test('Foreign writes and deletes fail without changing stored records', async () => {
            const abi=JSON.parse(fs.readFileSync(path.join(output,'compiled/TonHolonStore_TonHolonStore.abi'),'utf8'));
            const denied=Number(Object.keys(abi.errors).find(code => abi.errors[code].message==='OWNER_ONLY'));
            assert(Number.isInteger(denied));
            const before=await stateHash();
            for (const message of [{$$type:'PutRecord',key:1n,value:body('forged')},{$$type:'DeleteRecord',key:3n}]) {
                const tx=targetTransaction(await contract.send(outsider.getSender(),{value:toNano('0.1')},message));
                assert.equal(tx.aborted,true);
                assert.equal(tx.computePhase.exitCode,denied);
            }
            assert.equal(await stateHash(),before);
        });

        await t.test('A separate process reloads the serialized TVM state without reseeding', async () => {
            const snapshot=path.join(output,'contract-test-snapshot.json');
            fs.writeFileSync(snapshot,JSON.stringify(snapshotToSerializable(chain.snapshot())));
            const child=spawnSync(process.execPath,[__filename,'--read-snapshot',snapshot,contract.address.toString()],
                {encoding:'utf8',timeout:30000,maxBuffer:1024*1024});
            assert.equal(child.status,0,child.stderr);
            assert.deepEqual(JSON.parse(child.stdout.trim()),[...values,null]);
        });
    });
}
