// Reuses the installed compiler/Hardhat. In-memory EVM only, not TVM/HTS acceptance.
const fs=require('node:fs'),path=require('node:path'),assert=require('node:assert/strict');
const {createRequire}=require('node:module');
const root=path.resolve(__dirname,'../..');
const installed=createRequire(path.join(root,'Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS/package.json'));
const solc=installed('solc'),{ethers}=installed('ethers');
process.env.HARDHAT_CONFIG=path.join(root,'Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS/hardhat.config.cjs');
process.env.HARDHAT_NETWORK='hardhat';
const hardhat=installed('hardhat');
const blockchain='Providers/Blockchain/';
const canonical=blockchain+'NextGenSoftware.OASIS.API.Providers.EthereumOASIS/Contracts/OASISTokenEscrow.sol';
const variants=[
 [canonical,'OASISTokenEscrow',false],
 [blockchain+'NextGenSoftware.OASIS.API.Providers.TRONOASIS/Contracts/OASISIdBoundTokenEscrow.sol','OASISIdBoundTokenEscrow',false],
 [blockchain+'NextGenSoftware.OASIS.API.Providers.HashgraphOASIS/Contracts/OASISHederaIdBoundTokenEscrow.sol','OASISHederaIdBoundTokenEscrow',true]
];
const fixture=`pragma solidity ^0.8.20;
interface IDeposit {function lock(bytes32,address,uint256) external;}
contract Token {
 mapping(address=>uint256) public balanceOf;
 mapping(address=>mapping(address=>uint256)) public allowance;
 mapping(address=>bool) private associated;
 uint256 public totalSupply=100;
 bool public associationRequired;uint256 public associationMode;
 bool public fail;bool public taxed;bool public supplyChange;bool public attack;
 constructor(bool hts){balanceOf[msg.sender]=100;associated[msg.sender]=true;associationRequired=hts;}
 function isAssociated() external view returns(bool){return associated[msg.sender];}
 function associate() external returns(uint256){if(associationMode==1)return 1;if(associationMode!=2)associated[msg.sender]=true;return 22;}
 function setAssociationMode(uint256 mode) external {associationMode=mode;}
 function setModes(bool f,bool t,bool s,bool a) external {fail=f;taxed=t;supplyChange=s;attack=a;}
 function approve(address spender,uint256 amount) external returns(bool){allowance[msg.sender][spender]=amount;return true;}
 function transferFrom(address from,address to,uint256 amount) external returns(bool){
  if(fail)return false;require(!associationRequired||associated[to]);
  require(balanceOf[from]>=amount&&allowance[from][msg.sender]>=amount);
  allowance[from][msg.sender]-=amount;balanceOf[from]-=amount;balanceOf[to]+=taxed?amount-1:amount;
  if(supplyChange)totalSupply++;
  if(attack)IDeposit(msg.sender).lock(bytes32(uint256(999)),address(this),1);
  return true;
 }
 function transfer(address to,uint256 amount) external returns(bool){
  if(fail)return false;require(balanceOf[msg.sender]>=amount);
  balanceOf[msg.sender]-=amount;balanceOf[to]+=taxed?amount-1:amount;if(supplyChange)totalSupply++;return true;
 }
}`;
async function main(){
 const sources=Object.fromEntries([...new Set(variants.map(v=>v[0]))].map(file=>[file,{content:fs.readFileSync(path.join(root,file),'utf8')}]));
 sources['Fixture.sol']={content:fixture};
 const output=JSON.parse(solc.compile(JSON.stringify({language:'Solidity',sources,settings:{evmVersion:'paris',outputSelection:{'*':{'*':['abi','evm.bytecode.object']}}}})));
 const errors=(output.errors||[]).filter(e=>e.severity==='error');assert.equal(errors.length,0,errors.map(e=>e.formattedMessage).join('\n'));
 const provider=new ethers.BrowserProvider(hardhat.network.provider);provider.pollingInterval=50;
 const owner=await provider.getSigner(0),outsider=await provider.getSigner(1),o=await owner.getAddress(),x=await outsider.getAddress();
 let assertions=0;
 const equal=(a,b)=>{assert.deepEqual(a,b);assertions++;};
 const reject=async action=>{await assert.rejects(action);assertions++;};
 async function deploy(file,name,args=[]){const c=output.contracts[file][name];const instance=await new ethers.ContractFactory(c.abi,c.evm.bytecode.object,owner).deploy(...args);await instance.waitForDeployment();return instance;}
 for(const [file,name,hts] of variants){
  const before=assertions,escrow=await deploy(file,name),token=await deploy('Fixture.sol','Token',[hts]);
  const e=await escrow.getAddress(),t=await token.getAddress(),id=ethers.zeroPadValue('0x01',32),second=ethers.zeroPadValue('0x02',32);
  equal(await escrow.escrowProtocolVersion(),1n);
  await(await token.approve(e,100)).wait();
  if(hts){
   await reject(async()=>{await(await escrow.lock(id,t,25,{gasLimit:500000})).wait();});
   for(const mode of [1,2]){
    await(await token.setAssociationMode(mode)).wait();
    await reject(async()=>{await(await escrow.associateToken(t,{gasLimit:500000})).wait();});
   }
   await(await token.setAssociationMode(0)).wait();await(await escrow.associateToken(t)).wait();
   await reject(()=>escrow.associateToken.staticCall(t));
   await reject(()=>escrow.associateToken.staticCall(e));
   await reject(()=>escrow.associateToken.staticCall(x));
  }
  await reject(()=>escrow.lock.staticCall(ethers.ZeroHash,t,25));
  await reject(()=>escrow.lock.staticCall(id,t,0));
  await reject(()=>escrow.lock.staticCall(id,x,25));
  await reject(()=>escrow.unlock.staticCall(id));
  for(const modes of [[true,false,false,false],[false,true,false,false],[false,false,true,false],[false,false,false,true]]){
   await(await token.setModes(...modes)).wait();
   await reject(async()=>{await(await escrow.lock(id,t,25,{gasLimit:500000})).wait();});
   equal(await token.balanceOf(o),100n);equal(await token.balanceOf(e),0n);
   equal(await token.allowance(o,e),100n);equal(await token.totalSupply(),100n);
   equal((await escrow.deposits(id)).owner,ethers.ZeroAddress);
  }
  await(await token.setModes(false,false,false,false)).wait();
  await(await escrow.lock(id,t,25)).wait();await(await escrow.lock(second,t,10)).wait();
  equal(await token.balanceOf(o),65n);equal(await token.balanceOf(e),35n);
  const deposit=await escrow.deposits(id);equal(deposit.owner,o);equal(deposit.token,t);equal(deposit.amount,25n);equal(deposit.released,false);
  await reject(()=>escrow.lock.staticCall(id,t,1));await reject(()=>escrow.connect(outsider).unlock.staticCall(id));
  for(const modes of [[true,false,false,false],[false,true,false,false],[false,false,true,false]]){
   await(await token.setModes(...modes)).wait();
   await reject(async()=>{await(await escrow.unlock(id,{gasLimit:500000})).wait();});
   equal((await escrow.deposits(id)).released,false);equal(await token.balanceOf(o),65n);equal(await token.balanceOf(e),35n);
  }
  await(await token.setModes(false,false,false,false)).wait();await(await escrow.unlock(id)).wait();
  equal(await token.balanceOf(o),90n);equal((await escrow.deposits(id)).released,true);
  equal((await escrow.deposits(second)).released,false);equal(await token.balanceOf(e),10n);
  await reject(()=>escrow.unlock.staticCall(id));await reject(()=>escrow.lock.staticCall(id,t,25));
  await(await escrow.unlock(second)).wait();equal(await token.balanceOf(o),100n);equal(await token.balanceOf(e),0n);
  console.log(JSON.stringify({contract:name,assertions:assertions-before,scope:'IN_MEMORY_EVM_ONLY'}));
 }
 console.log(JSON.stringify({passed:true,assertions,compiler:solc.version(),nativeDeployment:false,sdkWiring:false}));
}
main().catch(error=>{console.error(error);process.exitCode=1;});
