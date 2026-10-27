import test from 'node:test';
import assert from 'node:assert/strict';
import {readFileSync} from 'node:fs';
import {createRequire} from 'node:module';
const require=createRequire(process.env.OASIS_NEAR_TEST_MODULE_MANIFEST??import.meta.url);
const ts=require('typescript');
const source=readFileSync(new URL('./contract/src/token-escrow.ts',import.meta.url),'utf8');
const compiled=ts.transpileModule(source,{compilerOptions:{target:ts.ScriptTarget.ES2022,module:ts.ModuleKind.ES2022}}).outputText;
const {nftEscrowPrefix,lockNftDeposit,releaseNftDeposit,readNftDeposit,tokenEscrowPrefix,lockTokenDeposit,releaseTokenDeposit,readTokenDeposit,burnSpendableToken,requireApplicationRecordKey}=await import('data:text/javascript;base64,'+Buffer.from(compiled).toString('base64'));
const owner='holder.near',symbol='OWN',id='1'.repeat(32);
function map(entries=[]){const values=new Map(entries),writes=[];return {values,writes,get:key=>values.get(key)??null,set(key,value){writes.push([key,value]);values.set(key,value);}};}
function fixture({amount='10',frozen=false}={}){
 const tokens=map([[symbol,JSON.stringify({symbol,supply:amount,issuer_id:'issuer.near',metadata_json:'{}',locked:frozen,balances:{[owner]:amount}})]]),records=map();
 return {tokens,records,token:()=>JSON.parse(tokens.get(symbol)),lock:(quantity='3',deposit=id)=>lockTokenDeposit(tokens,records,owner,symbol,deposit,quantity),release:()=>releaseTokenDeposit(tokens,records,owner,symbol,id)};
}
function unchanged(f,operation){const before=[...f.tokens.values],locks=[...f.records.values],writes=f.tokens.writes.length,records=f.records.writes.length;assert.throws(operation);assert.deepEqual([...f.tokens.values],before);assert.deepEqual([...f.records.values],locks);assert.equal(f.tokens.writes.length,writes);assert.equal(f.records.writes.length,records);}

test('deposit removes only exact spendable quantity without changing supply',()=>{
 const f=fixture();f.lock();assert.equal(f.token().balances[owner],'7');assert.equal(f.token().supply,'10');
 assert.deepEqual(readTokenDeposit(f.records,id),{protocol:1,lock_id:id,symbol,owner_id:owner,amount:'3',released:false});assert.equal(f.tokens.writes.length,1);assert.equal(f.records.writes.length,1);
});
test('owner release returns same amount and retains anti-replay tombstone',()=>{
 const f=fixture();f.lock();f.release();assert.equal(f.token().balances[owner],'10');assert.equal(f.token().supply,'10');assert.equal(readTokenDeposit(f.records,id).released,true);
 unchanged(f,()=>f.release());unchanged(f,()=>f.lock());
});
test('burn excludes custody from spendable balance but retains it in supply',()=>{
 const f=fixture();f.lock();burnSpendableToken(f.tokens,owner,symbol,'2');assert.equal(f.token().balances[owner],'5');assert.equal(f.token().supply,'8');
 f.release();assert.equal(f.token().balances[owner],'8');assert.equal(f.token().supply,'8');
});
test('custody release remains possible while issuer has frozen trading',()=>{
 const f=fixture();f.lock();const token=f.token();token.locked=true;f.tokens.set(symbol,JSON.stringify(token));f.release();assert.equal(f.token().balances[owner],'10');
 unchanged(f,()=>burnSpendableToken(f.tokens,owner,symbol,'1'));
});
test('exact token values beyond JS safe integers never become Number',()=>{
 const amount='900719925474099312345678';const quantity='900719925474099300000000';const f=fixture({amount});f.lock(quantity);
 assert.equal(f.token().balances[owner],(BigInt(amount)-BigInt(quantity)).toString());assert.equal(readTokenDeposit(f.records,id).amount,quantity);f.release();assert.equal(f.token().balances[owner],amount);
});
test('individual deposits are independent and one ID cannot select another token',()=>{
 const f=fixture();f.lock('2');f.lock('3','2'.repeat(32));assert.equal(f.token().balances[owner],'5');f.release();assert.equal(f.token().balances[owner],'7');
 assert.equal(readTokenDeposit(f.records,'2'.repeat(32)).released,false);unchanged(f,()=>releaseTokenDeposit(f.tokens,f.records,owner,'OTHER','2'.repeat(32)));
});
for(const amount of ['0','-1','01','1.0','1e2',' 1','1 ',1,null,(1n<<256n).toString()])test('invalid amount '+JSON.stringify(amount)+' refuses before either write',()=>{const f=fixture();unchanged(f,()=>f.lock(amount));});
for(const deposit of ['', '0'.repeat(32),'A'.repeat(32),'1'.repeat(31),'1'.repeat(33),'11111111-1111-1111-1111-111111111111'])test('noncanonical deposit '+deposit+' refuses before writes',()=>{const f=fixture();unchanged(f,()=>f.lock('1',deposit));});
for(const badOwner of ['system','a','.near','BAD.near','a..near','a'.repeat(65)])test('invalid owner '+badOwner+' refuses before writes',()=>{const f=fixture();unchanged(f,()=>lockTokenDeposit(f.tokens,f.records,badOwner,symbol,id,'1'));});
test('insufficient or frozen balances refuse before all mutation',()=>{const f=fixture();unchanged(f,()=>f.lock('11'));const frozen=fixture({frozen:true});unchanged(frozen,()=>frozen.lock());});
test('foreign release and duplicate lock cannot mutate valid custody',()=>{const f=fixture();f.lock();unchanged(f,()=>releaseTokenDeposit(f.tokens,f.records,'attacker.near',symbol,id));unchanged(f,()=>f.lock());});
test('unavailable deposits and tokens never become implicit new balances',()=>{const f=fixture();unchanged(f,()=>f.release());unchanged(f,()=>lockTokenDeposit(f.tokens,f.records,owner,'MISSING',id,'1'));assert.equal(readTokenDeposit(f.records,id),null);});
test('corrupt original deposit refuses before token or deposit changes',()=>{
 for(const edit of [d=>d.released=0,d=>d.amount='01',d=>d.lock_id='2'.repeat(32),d=>d.protocol=2,d=>d.extra=true,d=>d.owner_id='system']){
  const f=fixture();f.lock();const d=readTokenDeposit(f.records,id);edit(d);f.records.set(tokenEscrowPrefix+id,JSON.stringify(d));unchanged(f,()=>f.release());
 }
});
test('corrupt supply cannot manufacture funds on burn or release',()=>{
 const f=fixture();f.lock();let token=f.token();token.supply='6';f.tokens.set(symbol,JSON.stringify(token));unchanged(f,()=>f.release());unchanged(f,()=>burnSpendableToken(f.tokens,owner,symbol,'1'));
});
test('generic record put/delete cannot overwrite or remove custody entries',()=>{
 assert.throws(()=>requireApplicationRecordKey(tokenEscrowPrefix+id));for(const key of ['holon:owned','avatar:owned','wallet-transaction:owned'])assert.doesNotThrow(()=>requireApplicationRecordKey(key));
});
test('shipping entry points bind custody owner to native predecessor, not caller JSON',()=>{
 const text=readFileSync(new URL('./contract/src/contract.ts',import.meta.url),'utf8');const file=ts.createSourceFile('contract.ts',text,ts.ScriptTarget.ES2022,true,ts.ScriptKind.TS);
 const declaration=file.statements.find(s=>ts.isClassDeclaration(s)&&s.name?.text==='OasisStorage');assert.ok(declaration);
 for(const [name,helper,index] of [['token_lock','lockTokenDeposit',2],['token_release','releaseTokenDeposit',2],['token_burn','burnSpendableToken',1]]){
  const method=declaration.members.find(m=>m.name?.getText(file)===name);assert.ok(method);assert.equal(method.body.statements.length,1);const call=method.body.statements[0].expression;
  assert.equal(call.expression.getText(file),helper);assert.equal(call.arguments[index].getText(file),'near.predecessorAccountId()');
 }
 for(const [name,write] of [['put','this.records.set'],['delete_record','this.records.remove']]){
  const method=declaration.members.find(m=>m.name?.getText(file)===name);const calls=method.body.statements.map(s=>s.expression?.expression?.getText(file));assert.ok(calls.indexOf('requireApplicationRecordKey')>=0);assert.ok(calls.indexOf('requireApplicationRecordKey')<calls.indexOf(write));
 }
});

function nftFixture(){const nfts=map([['TREE',JSON.stringify({token_id:'TREE',owner_id:owner,metadata_json:'{}',locked:false})]]),records=map();return {nfts,tokens:nfts,records,nft:()=>JSON.parse(nfts.get('TREE')),lock:(deposit=id)=>lockNftDeposit(nfts,records,owner,'TREE',deposit),release:(deposit=id)=>releaseNftDeposit(nfts,records,owner,'TREE',deposit)};}
test('NFT lock binds native owner/token/current ID and release leaves tombstone',()=>{
 const f=nftFixture();f.lock();assert.equal(f.nft().locked,true);assert.equal(f.nft().lock_id,id);
 assert.deepEqual(readNftDeposit(f.records,id),{protocol:1,lock_id:id,token_id:'TREE',owner_id:owner,released:false});
 f.release();assert.equal(f.nft().locked,false);assert.equal(readNftDeposit(f.records,id).released,true);unchanged(f,()=>f.release());unchanged(f,()=>f.lock());
});
test('old NFT source cannot release a later lock cycle',()=>{
 const f=nftFixture();f.lock();f.release();const newer='2'.repeat(32);f.lock(newer);unchanged(f,()=>f.release(id));assert.equal(f.nft().lock_id,newer);f.release(newer);
});
test('NFT replay tombstone survives burn/remint of same token name',()=>{
 const f=nftFixture();f.lock();f.release();f.nfts.values.delete('TREE');f.nfts.set('TREE',JSON.stringify({token_id:'TREE',owner_id:owner,metadata_json:'{}',locked:false}));
 unchanged(f,()=>f.lock(id));f.lock('2'.repeat(32));unchanged(f,()=>f.release(id));
});
for(const mode of ['foreign-lock','foreign-release','wrong-token','wrong-id','double-lock','missing-id','nonboolean-record','wrong-current-id','extra-record'])test('NFT '+mode+' rejects before every write',()=>{
 const f=nftFixture();
 if(mode==='foreign-lock'){unchanged(f,()=>lockNftDeposit(f.nfts,f.records,'attacker.near','TREE',id));return;}
 if(mode==='missing-id'){unchanged(f,()=>f.lock(''));return;}
 f.lock();
 if(mode==='nonboolean-record'||mode==='extra-record'){const d=readNftDeposit(f.records,id);if(mode==='nonboolean-record')d.released='false';else d.extra=true;f.records.set(nftEscrowPrefix+id,JSON.stringify(d));}
 if(mode==='wrong-current-id'){const n=f.nft();n.lock_id='2'.repeat(32);f.nfts.set('TREE',JSON.stringify(n));}
 unchanged(f,()=>mode==='foreign-release'?releaseNftDeposit(f.nfts,f.records,'attacker.near','TREE',id):mode==='wrong-token'?releaseNftDeposit(f.nfts,f.records,owner,'OTHER',id):mode==='wrong-id'?f.release('2'.repeat(32)):mode==='double-lock'?f.lock('2'.repeat(32)):f.release());
});
test('NFT custody records reject generic storage overwrite/delete',()=>assert.throws(()=>requireApplicationRecordKey(nftEscrowPrefix+id)));

test('NFT shipping lock/release use native predecessor and locked burn throws before removal',()=>{
 const text=readFileSync(new URL('./contract/src/contract.ts',import.meta.url),'utf8');const file=ts.createSourceFile('contract.ts',text,ts.ScriptTarget.ES2022,true,ts.ScriptKind.TS);
 const declaration=file.statements.find(s=>ts.isClassDeclaration(s)&&s.name?.text==='OasisStorage');const method=declaration.members.find(m=>m.name?.getText(file)==='nft_set_locked');
 const calls=[];function visit(node){if(ts.isCallExpression(node)&&['lockNftDeposit','releaseNftDeposit'].includes(node.expression.getText(file)))calls.push(node);ts.forEachChild(node,visit);}visit(method);
 assert.equal(calls.length,2);for(const call of calls){assert.equal(call.arguments[2].getText(file),'near.predecessorAccountId()');assert.equal(call.arguments[4].getText(file),'lock_id');}
 const burn=declaration.members.find(m=>m.name?.getText(file)==='nft_burn');const guard=burn.body.statements.findIndex(s=>ts.isIfStatement(s)&&s.expression.getText(file)==='nft.locked'&&ts.isThrowStatement(s.thenStatement));const removal=burn.body.statements.findIndex(s=>s.expression?.expression?.getText(file)==='this.nfts.remove');assert.ok(guard>=0&&guard<removal);
});
