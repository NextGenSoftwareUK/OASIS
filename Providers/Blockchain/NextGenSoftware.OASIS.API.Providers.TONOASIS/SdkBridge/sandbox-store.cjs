// Explicit local TVM source owner. This never connects to or impersonates a TON
// network. Public-network transport must implement its own signed admission.
const fs = require('node:fs');
const path = require('node:path');
const crypto = require('node:crypto');
const {Blockchain, TreasuryContract, snapshotToSerializable, snapshotFromSerializable} = require('@ton/sandbox');
const {Address, Cell, beginCell, toNano} = require('@ton/core');
const MODE = 'LocalTonTvmSandbox';
const UUID = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/;
const digest = bytes => crypto.createHash('sha256').update(bytes).digest('hex');

class TonStoreError extends Error {
    constructor(code, message) {super(message);this.code=code;}
}
function fail(code, message) {throw new TonStoreError(code,message);}
function writeNew(file, value) {
    const fd=fs.openSync(file,'wx');
    try {fs.writeFileSync(fd,JSON.stringify(value));fs.fsyncSync(fd);} finally {fs.closeSync(fd);}
}

class TonSandboxStore {
    constructor({directory, sourceId, binding, expectedCodeHash}) {
        if(!path.isAbsolute(directory) || !path.isAbsolute(binding) || !UUID.test(sourceId)
            || !/^[0-9a-f]{64}$/.test(expectedCodeHash)) fail('TON_CONFIGURATION','Explicit source, binding and code identity required.');
        if(!fs.statSync(directory).isDirectory()) fail('TON_CONFIGURATION','Owned source directory must already exist.');
        this.directory=directory;this.sourceId=sourceId;this.expectedCodeHash=expectedCodeHash;
        this.Contract=require(binding).TonHolonStore;
        this.statePath=path.join(directory,'source.json');
        this.pendingPath=path.join(directory,'pending.json');
        this.operations=path.join(directory,'operations');
    }

    async execute(request) {
        if(request.mode!==MODE || request.sourceId!==this.sourceId) fail('TON_SOURCE_MISMATCH','Explicit local TVM source required.');
        if(request.operation==='inspect') return this.inspect(request.operationId);
        if(fs.existsSync(this.pendingPath)) fail('TON_OUTCOME_PENDING','Preserve the original pending operation; do not replay it.');
        const mutation=['create','put','delete'].includes(request.operation);
        if(!mutation && !['verify','get','list'].includes(request.operation)) fail('TON_OPERATION','Unknown operation.');
        if(mutation && !UUID.test(request.operationId || '')) fail('TON_OPERATION','An explicit operation identity is required.');
        if(mutation && fs.existsSync(path.join(this.operations,request.operationId+'.json')))
            fail('TON_OPERATION_COMMITTED','Operation already acknowledged; inspect it instead of submitting again.');
        if(['put','delete','get'].includes(request.operation)
            && (typeof request.key!=='string' || !request.key || !request.key.isWellFormed() || Buffer.byteLength(request.key)>256))
            fail('TON_KEY','A bounded nonempty key is required.');
        if(request.operation==='put' && (typeof request.value!=='string' || !request.value.isWellFormed() || Buffer.byteLength(request.value)>256*1024))
            fail('TON_VALUE','A bounded string value is required.');
        if(request.operation==='list' && typeof request.prefix!=='string') fail('TON_KEY','An explicit prefix is required.');

        const creating=request.operation==='create';
        if(creating && fs.existsSync(this.statePath)) fail('TON_SOURCE_EXISTS','Never replace an existing native source.');
        const original=creating ? null : this.readStateBytes();
        const chain=await Blockchain.create();
        chain.verbosity={blockchainLogs:false,vmLogs:'none',debugLogs:false};
        let state, contract, owner;
        if(!creating) {
            state=JSON.parse(original.toString('utf8'));
            if(state.mode!==MODE || state.sourceId!==this.sourceId || state.codeHash!==this.expectedCodeHash)
                fail('TON_SOURCE_MISMATCH','Native source identity differs.');
            await chain.loadFrom(snapshotFromSerializable(state.snapshot));
            contract=chain.openContract(this.Contract.fromAddress(Address.parse(state.contract)));
            owner=chain.openContract(TreasuryContract.create(0,BigInt(state.ownerSubwalletId)));
            const expected=await this.Contract.fromInit(owner.address);
            const observed=await chain.getContract(contract.address);
            if(!owner.address.equals(Address.parse(state.owner)) || !expected.address.equals(contract.address)
                || expected.init.code.hash().toString('hex')!==this.expectedCodeHash
                || observed.accountState?.type!=='active'
                || observed.accountState.state.code.hash().toString('hex')!==this.expectedCodeHash
                || !(await contract.getStorageOwner()).equals(owner.address))
                fail('TON_SOURCE_MISMATCH','Native contract code, address or owner differs.');
        }

        if(!mutation) {
            let result;
            if(request.operation==='verify') result={contract:state.contract,owner:state.owner,codeHash:state.codeHash};
            if(request.operation==='get') result=await this.get(contract,request.key);
            if(request.operation==='list') {
                const records=await contract.getStoredRecords();
                if(records.size>1000) fail('TON_LIMIT','Complete native record budget exceeded.');
                result=[];
                for(const [key,cell] of records) {
                    const record=this.decode(cell,key);
                    if(record.key.startsWith(request.prefix)) result.push(record.key);
                }
                result.sort();
            }
            // Readers publish only the source bytes they actually observed.
            if(fs.existsSync(this.pendingPath) || !original.equals(this.readStateBytes()))
                fail('TON_SOURCE_CHANGED','Source changed during the read; no partial result.');
            return this.response(result);
        }

        const requestHash=digest(Buffer.from(JSON.stringify(request)));
        const beforeHash=original===null ? null : digest(original);
        fs.mkdirSync(this.operations,{recursive:true});
        // CreateNew is the cross-process write fence. An interrupted operation
        // stays pending; no later read/write silently replays or replaces it.
        writeNew(this.pendingPath,{mode:MODE,sourceId:this.sourceId,operationId:request.operationId,
            expectedCodeHash:this.expectedCodeHash,requestHash,beforeHash,operation:request.operation});
        if((creating && fs.existsSync(this.statePath)) || (!creating && !original.equals(this.readStateBytes())))
            fail('TON_SOURCE_CHANGED','Original source changed before mutation admission.');
        let trace;
        if(creating) {
            owner=await chain.treasury('cosmic-ton-'+this.sourceId);
            contract=chain.openContract(await this.Contract.fromInit(owner.address));
            if(contract.init.code.hash().toString('hex')!==this.expectedCodeHash)
                fail('TON_SOURCE_MISMATCH','Compiled contract code differs; preserve setup intent.');
            trace=await contract.send(owner.getSender(),{value:toNano('0.1')},null);
            state={mode:MODE,sourceId:this.sourceId,codeHash:this.expectedCodeHash,
                contract:contract.address.toString(),owner:owner.address.toString(),ownerSubwalletId:owner.subwalletId.toString()};
        } else {
            const key=BigInt('0x'+digest(Buffer.from(request.key,'utf8')));
            const message=request.operation==='put'
                ? {$$type:'PutRecord',key,value:beginCell().storeStringTail(JSON.stringify({key:request.key,value:request.value})).endCell()}
                : {$$type:'DeleteRecord',key};
            trace=await contract.send(owner.getSender(),{value:toNano('0.1')},message);
            const observed=await this.get(contract,request.key);
            if(observed!==(request.operation==='put' ? request.value : null))
                fail('TON_WRITE_NOT_OBSERVED','Native mutation readback differs; preserve its original intent.');
        }
        const transaction=trace.transactions.find(tx => tx.inMessage?.info.type==='internal'
            && tx.inMessage.info.dest.equals(contract.address));
        if(!transaction || transaction.description.type!=='generic' || transaction.description.aborted
            || transaction.description.computePhase.type!=='vm' || transaction.description.computePhase.exitCode!==0)
            fail('TON_EXECUTION_FAILED','Native TVM transaction did not succeed; preserve intent.');
        state.snapshot=snapshotToSerializable(chain.snapshot());
        state.lastOperation={operationId:request.operationId,requestHash,transactionHash:transaction.hash().toString('hex')};
        const temporary=path.join(this.directory,'commit-'+request.operationId+'.json');
        writeNew(temporary,state);
        if((creating && fs.existsSync(this.statePath)) || (!creating && !original.equals(this.readStateBytes())))
            fail('TON_SOURCE_CHANGED','Source changed before commit; preserve original operation.');
        fs.renameSync(temporary,this.statePath);
        const acknowledgement={mode:MODE,sourceId:this.sourceId,codeHash:this.expectedCodeHash,
            ...state.lastOperation,stateHash:digest(this.readStateBytes())};
        writeNew(path.join(this.operations,request.operationId+'.json'),acknowledgement);
        fs.unlinkSync(this.pendingPath);
        return this.response(acknowledgement);
    }

    readStateBytes() {
        if(fs.statSync(this.statePath).size>16*1024*1024) fail('TON_LIMIT','Complete native snapshot budget exceeded.');
        return fs.readFileSync(this.statePath);
    }
    decode(cell,key) {
        const record=JSON.parse(cell.beginParse().loadStringTail());
        if(typeof record.key!=='string' || !record.key.isWellFormed() || typeof record.value!=='string' || !record.value.isWellFormed()
            || BigInt('0x'+digest(Buffer.from(record.key,'utf8')))!==key)
            fail('TON_RECORD_IDENTITY','Native key envelope does not match its dictionary identity.');
        return record;
    }
    async get(contract,key) {
        const native=BigInt('0x'+digest(Buffer.from(key,'utf8')));
        const cell=await contract.getRecord(native);
        if(cell===null) return null;
        const record=this.decode(cell,native);
        if(record.key!==key) fail('TON_RECORD_IDENTITY','Native record differs from the requested key.');
        return record.value;
    }
    response(result) {return {ok:true,mode:MODE,sourceId:this.sourceId,result};}
    inspect(operationId) {
        if(!UUID.test(operationId || '')) fail('TON_OPERATION','Exact operation identity required.');
        const ackPath=path.join(this.operations,operationId+'.json');
        if(fs.existsSync(ackPath)) {
            const acknowledgement=JSON.parse(fs.readFileSync(ackPath,'utf8'));
            if(acknowledgement.mode!==MODE || acknowledgement.sourceId!==this.sourceId || acknowledgement.operationId!==operationId
                || acknowledgement.codeHash!==this.expectedCodeHash)
                fail('TON_SOURCE_MISMATCH','Acknowledgement belongs to another source or operation.');
            return this.response({status:'Acknowledged',acknowledgement});
        }
        const pending=fs.existsSync(this.pendingPath) ? JSON.parse(fs.readFileSync(this.pendingPath,'utf8')) : null;
        if(pending && pending.operationId!==operationId) fail('TON_OUTCOME_PENDING','Another exact operation remains pending.');
        const state=fs.existsSync(this.statePath) ? JSON.parse(this.readStateBytes().toString('utf8')) : null;
        if((pending && (pending.mode!==MODE || pending.sourceId!==this.sourceId || pending.expectedCodeHash!==this.expectedCodeHash))
            || (state && (state.mode!==MODE || state.sourceId!==this.sourceId || state.codeHash!==this.expectedCodeHash)))
            fail('TON_SOURCE_MISMATCH','Inspection evidence belongs to another native source.');
        return this.response({status:state?.lastOperation?.operationId===operationId ? 'CommittedUnacknowledged'
            : pending ? 'Pending' : 'NotRecorded',pending,lastOperation:state?.lastOperation || null});
    }
}

module.exports={TonSandboxStore,TonStoreError,MODE};
