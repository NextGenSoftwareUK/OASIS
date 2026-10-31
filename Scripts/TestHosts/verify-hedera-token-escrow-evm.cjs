// Reuses installed compiler/runtime; ordinary-EVM invariants, not live HTS evidence.
const fs=require('node:fs'),path=require('node:path'),assert=require('node:assert/strict');
const {createRequire}=require('node:module');
const root=path.resolve(__dirname,'../..');
const existing=createRequire(path.join(root,'Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS/package.json'));
const solc=existing('solc'),{ethers}=existing('ethers');
process.env.HARDHAT_CONFIG=path.join(root,'Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS/hardhat.config.cjs');
process.env.HARDHAT_NETWORK='hardhat';
const hardhat=existing('hardhat');
const folder=path.join(root,'Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.HashgraphOASIS/Contracts');
const fixture=`pragma solidity ^0.8.0;
contract FixtureToken {
 mapping(address=>uint256) public balanceOf;
 mapping(address=>mapping(address=>uint256)) public allowance;
 mapping(address=>bool) private associated;
 bool public fail;bool public taxed;bool public associationFailure;
 constructor(){balanceOf[msg.sender]=100;}
 function mintFixture(address to,uint256 amount) external {balanceOf[to]+=amount;}
 function setFailures(bool f,bool t,bool a) external {fail=f;taxed=t;associationFailure=a;}
 function isAssociated() external view returns(bool){return associated[msg.sender];}
 function associate() external returns(uint256){if(associationFailure)return 1;associated[msg.sender]=true;return 22;}
 function approve(address spender,uint256 amount) external returns(bool){allowance[msg.sender][spender]=amount;return true;}
 function transferFrom(address from,address to,uint256 amount) external returns(bool){
  if(fail)return false;require(allowance[from][msg.sender]>=amount);require(balanceOf[from]>=amount);
  allowance[from][msg.sender]-=amount;balanceOf[from]-=amount;balanceOf[to]+=taxed?amount-1:amount;return true;
 }
 function transfer(address to,uint256 amount) external returns(bool){
  if(fail)return false;require(balanceOf[msg.sender]>=amount);balanceOf[msg.sender]-=amount;balanceOf[to]+=taxed?amount-1:amount;return true;
 }
}`;
async function main(){
 const sources=Object.fromEntries(['OASISHederaNftEscrow.sol','OASISHederaTokenEscrow.sol'].map(file=>[file,{content:fs.readFileSync(path.join(folder,file),'utf8')}]));
 sources['Fixture.sol']={content:fixture};
 const output=JSON.parse(solc.compile(JSON.stringify({language:'Solidity',sources,settings:{evmVersion:'paris',outputSelection:{'*':{'*':['abi','evm.bytecode.object','evm.deployedBytecode.object']}}}})));
 const errors=(output.errors||[]).filter(e=>e.severity==='error');assert.equal(errors.length,0,errors.map(e=>e.formattedMessage).join('\n'));
 const provider=new ethers.BrowserProvider(hardhat.network.provider);provider.pollingInterval=50;
 const owner=await provider.getSigner(0),outsider=await provider.getSigner(1);
 async function deploy(file,name){const c=output.contracts[file][name];const result=await new ethers.ContractFactory(c.abi,c.evm.bytecode.object,owner).deploy();await result.waitForDeployment();return result;}
 const escrow=await deploy('OASISHederaTokenEscrow.sol','OASISHederaTokenEscrow'),token=await deploy('Fixture.sol','FixtureToken');
 const e=await escrow.getAddress(),t=await token.getAddress(),o=await owner.getAddress();let assertions=0;
 async function rejects(action){await assert.rejects(action);assertions++;}
 await rejects(()=>escrow.unlock.staticCall(t));await rejects(()=>escrow.lockAll.staticCall(t));
 await(await token.approve(e,100)).wait();
 for(const [failed,taxed,association] of [[false,false,true],[true,false,false],[false,true,false]]){
  await(await token.setFailures(failed,taxed,association)).wait();
  await rejects(async()=>{await(await escrow.lockAll(t,{gasLimit:400000})).wait();});
  assert.equal(await escrow.locked(t,o),0n);assertions++;
  assert.equal(await token.balanceOf(o),100n);assertions++;
  assert.equal(await token.allowance(o,e),100n);assertions++;
 }
 await(await token.setFailures(false,false,false)).wait();await(await escrow.lockAll(t)).wait();
 assert.equal(await escrow.locked(t,o),100n);assertions++;
 assert.equal(await token.balanceOf(e),100n);assertions++;
 assert.equal(await token.balanceOf(o),0n);assertions++;
 await rejects(()=>escrow.connect(outsider).unlock.staticCall(t));
 for(const [failed,taxed] of [[true,false],[false,true]]){
  await(await token.setFailures(failed,taxed,false)).wait();
  await rejects(async()=>{await(await escrow.unlock(t,{gasLimit:400000})).wait();});
  assert.equal(await escrow.locked(t,o),100n);assertions++;
  assert.equal(await token.balanceOf(e),100n);assertions++;
 }
 await(await token.setFailures(false,false,false)).wait();await(await escrow.unlock(t)).wait();
 assert.equal(await escrow.locked(t,o),0n);assertions++;
 assert.equal(await token.balanceOf(o),100n);assertions++;
 assert.equal(await token.balanceOf(e),0n);assertions++;
 // Fixture minting deliberately exercises repeated deposits and independent owners.
 await(await token.approve(e,100)).wait();await(await escrow.lockAll(t)).wait();
 await(await token.mintFixture(o,25)).wait();await(await token.approve(e,25)).wait();await(await escrow.lockAll(t)).wait();
 assert.equal(await escrow.locked(t,o),125n);assertions++;
 const other=await outsider.getAddress();
 await(await token.mintFixture(other,40)).wait();await(await token.connect(outsider).approve(e,40)).wait();
 await(await escrow.connect(outsider).lockAll(t)).wait();
 assert.equal(await escrow.locked(t,other),40n);assertions++;
 assert.equal(await token.balanceOf(e),165n);assertions++;
 await(await escrow.unlock(t)).wait();
 assert.equal(await token.balanceOf(o),125n);assertions++;
 assert.equal(await escrow.locked(t,other),40n);assertions++;
 assert.equal(await token.balanceOf(e),40n);assertions++;
 await(await escrow.connect(outsider).unlock(t)).wait();
 assert.equal(await token.balanceOf(other),40n);assertions++;
 assert.equal(await token.balanceOf(e),0n);assertions++;
 await(await token.approve(e,125)).wait();await(await escrow.lockAll(t)).wait();
 const overflow=9223372036854775807n-124n;
 await(await token.mintFixture(o,overflow)).wait();await(await token.approve(e,overflow)).wait();
 await rejects(async()=>{await(await escrow.lockAll(t,{gasLimit:400000})).wait();});
 assert.equal(await escrow.locked(t,o),125n);assertions++;
 assert.equal(await token.balanceOf(o),overflow);assertions++;
 assert.equal(await token.allowance(o,e),overflow);assertions++;
 // Clean the owned deposit; the synthetic oversized fixture balance is not HTS supply evidence.
 await(await escrow.unlock(t)).wait();
 const runtimeCodeHash=ethers.keccak256('0x'+output.contracts['OASISHederaTokenEscrow.sol'].OASISHederaTokenEscrow.evm.deployedBytecode.object);
 assert.equal(ethers.keccak256(await provider.getCode(e)),runtimeCodeHash);assertions++;
 console.log(JSON.stringify({compiler:solc.version(),runtime:'existing in-process Hardhat',assertions,runtimeCodeHash,liveHedera:false}));
}
main().catch(error=>{console.error(error);process.exitCode=1;});
