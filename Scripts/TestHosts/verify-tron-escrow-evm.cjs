// Reuses installed Web3Core solc/Hardhat/ethers. No downloads, public chain or filesystem ledger.
const fs = require('node:fs');
const path = require('node:path');
const assert = require('node:assert/strict');
const { createRequire } = require('node:module');
const root = path.resolve(__dirname, '../..');
const requireRuntime = createRequire(path.join(root, 'Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS/package.json'));
const solc = requireRuntime('solc');
const { ethers } = requireRuntime('ethers');
process.env.HARDHAT_CONFIG = path.join(root, 'Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS/hardhat.config.cjs');
process.env.HARDHAT_NETWORK = 'hardhat'; // Never select a configured public network for this test.
const hardhat = requireRuntime('hardhat');
const source = fs.readFileSync(path.join(root, 'Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.TRONOASIS/Contracts/OASISTokenEscrow.sol'), 'utf8');
const fixture = `pragma solidity ^0.8.0;
contract TestToken {
 mapping(address=>uint256) public balanceOf;
 mapping(address=>mapping(address=>uint256)) public allowance;
 bool public fail;
 bool public taxed;
 constructor(){balanceOf[msg.sender]=100;}
 function setFail(bool value) external {fail=value;}
 function setTaxed(bool value) external {taxed=value;}
 function approve(address spender,uint256 amount) external returns(bool){allowance[msg.sender][spender]=amount;return true;}
 function transferFrom(address from,address to,uint256 amount) external returns(bool){
  if(fail)return false;require(allowance[from][msg.sender]>=amount);allowance[from][msg.sender]-=amount;
  require(balanceOf[from]>=amount);balanceOf[from]-=amount;balanceOf[to]+=taxed?amount-1:amount;return true;
 }
 function transfer(address to,uint256 amount) external returns(bool){
  if(fail)return false;require(balanceOf[msg.sender]>=amount);balanceOf[msg.sender]-=amount;balanceOf[to]+=taxed?amount-1:amount;return true;
 }
}
contract TestNFT {
 mapping(uint256=>address) public ownerOf;
 mapping(uint256=>address) public approved;
 bool public fail;
 constructor(){ownerOf[0]=msg.sender;ownerOf[type(uint256).max]=msg.sender;}
 function approve(address spender,uint256 id) external {require(ownerOf[id]==msg.sender);approved[id]=spender;}
 function setFail(bool value) external {fail=value;}
 function transferFrom(address from,address to,uint256 id) external {
  require(!fail);require(ownerOf[id]==from);require(msg.sender==from||approved[id]==msg.sender);
  ownerOf[id]=to;delete approved[id];
 }
}`;

async function main() {
 const compiled = JSON.parse(solc.compile(JSON.stringify({language:'Solidity', sources:{
  'Escrow.sol':{content:source}, 'Fixture.sol':{content:fixture}}, settings:{evmVersion:'paris',
  outputSelection:{'*':{'*':['abi','evm.bytecode.object']}}}})));
 const errors = (compiled.errors || []).filter(e=>e.severity==='error');
 assert.equal(errors.length,0,errors.map(e=>e.formattedMessage).join('\n'));
 const provider = new ethers.BrowserProvider(hardhat.network.provider);
 provider.pollingInterval = 50;
 const owner = await provider.getSigner(0), outsider = await provider.getSigner(1);
 async function deploy(file,name){const c=compiled.contracts[file][name];const result=await new ethers.ContractFactory(c.abi,c.evm.bytecode.object,owner).deploy();await result.waitForDeployment();return result;}
 const escrow=await deploy('Escrow.sol','OASISTokenEscrow'), token=await deploy('Fixture.sol','TestToken');
 const t=await token.getAddress(), e=await escrow.getAddress(), o=await owner.getAddress();
 let assertions=0;
 async function rejects(action){await assert.rejects(action);assertions++;}
 await rejects(()=>escrow.unlock.staticCall(t));
 await rejects(()=>escrow.lockAll.staticCall(t)); // Missing allowance cannot create a deposit.
 await (await token.approve(e,100n)).wait();
 await (await token.setFail(true)).wait();
 await rejects(async()=>{await (await escrow.lockAll(t,{gasLimit:300000})).wait();});
 assert.equal(await escrow.locked(t,o),0n);assertions++;
 await (await token.setFail(false)).wait();
 await (await token.setTaxed(true)).wait();
 await rejects(async()=>{await (await escrow.lockAll(t,{gasLimit:300000})).wait();});
 assert.equal(await escrow.locked(t,o),0n);assertions++;
 assert.equal(await token.balanceOf(o),100n);assertions++;
 assert.equal(await token.allowance(o,e),100n);assertions++;
 await (await token.setTaxed(false)).wait();
 await (await escrow.lockAll(t)).wait();
 assert.equal(await escrow.locked(t,o),100n);assertions++;
 assert.equal(await token.balanceOf(o),0n);assertions++;
 await rejects(()=>escrow.connect(outsider).unlock.staticCall(t));
 await (await token.setFail(true)).wait();
 await rejects(async()=>{await (await escrow.unlock(t,{gasLimit:300000})).wait();});
 assert.equal(await escrow.locked(t,o),100n);assertions++;
 await (await token.setFail(false)).wait();
 await (await token.setTaxed(true)).wait();
 await rejects(async()=>{await (await escrow.unlock(t,{gasLimit:300000})).wait();});
 assert.equal(await escrow.locked(t,o),100n);assertions++;
 assert.equal(await token.balanceOf(e),100n);assertions++;
 assert.equal(await token.balanceOf(o),0n);assertions++;
 await (await token.setTaxed(false)).wait();
 await (await escrow.unlock(t)).wait();
 assert.equal(await token.balanceOf(o),100n);assertions++;
 assert.equal(await escrow.locked(t,o),0n);assertions++;
 await rejects(()=>escrow.unlock.staticCall(t));
 const x=await outsider.getAddress();
 await (await token.transfer(x,25n)).wait();
 await (await token.approve(e,75n)).wait();
 await (await token.connect(outsider).approve(e,25n)).wait();
 await (await escrow.lockAll(t)).wait();
 await (await escrow.connect(outsider).lockAll(t)).wait();
 assert.equal(await escrow.locked(t,o),75n);assertions++;
 assert.equal(await escrow.locked(t,x),25n);assertions++;
 await (await escrow.connect(outsider).unlock(t)).wait();
 assert.equal(await token.balanceOf(x),25n);assertions++;
 assert.equal(await escrow.locked(t,o),75n);assertions++;
 await (await escrow.unlock(t)).wait();
 assert.equal(await token.balanceOf(o),75n);assertions++;
 assert.equal(await token.balanceOf(e),0n);assertions++;
 const nft=await deploy('Fixture.sol','TestNFT'), n=await nft.getAddress();
 await rejects(()=>escrow.lockNFT.staticCall(n,0n)); // Explicit approval is required.
 await (await nft.approve(e,0n)).wait();
 await (await nft.setFail(true)).wait();
 await rejects(async()=>{await (await escrow.lockNFT(n,0n,{gasLimit:300000})).wait();});
 assert.equal(await escrow.nftDepositor(n,0n),ethers.ZeroAddress);assertions++;
 assert.equal(await nft.ownerOf(0n),o);assertions++;
 await (await nft.setFail(false)).wait();
 await (await escrow.lockNFT(n,0n)).wait();
 assert.equal(await escrow.nftDepositor(n,0n),o);assertions++;
 assert.equal(await nft.ownerOf(0n),e);assertions++;
 await rejects(()=>escrow.connect(outsider).unlockNFT.staticCall(n,0n));
 await rejects(()=>escrow.lockNFT.staticCall(n,0n));
 await (await nft.setFail(true)).wait();
 await rejects(async()=>{await (await escrow.unlockNFT(n,0n,{gasLimit:300000})).wait();});
 assert.equal(await escrow.nftDepositor(n,0n),o);assertions++;
 await (await nft.setFail(false)).wait();
 await (await escrow.unlockNFT(n,0n)).wait();
 assert.equal(await nft.ownerOf(0n),o);assertions++;
 assert.equal(await escrow.nftDepositor(n,0n),ethers.ZeroAddress);assertions++;
 await rejects(()=>escrow.unlockNFT.staticCall(n,0n));
 const large=(1n<<256n)-1n;
 await (await nft.approve(e,large)).wait();
 await (await escrow.lockNFT(n,large)).wait();
 await (await escrow.unlockNFT(n,large)).wait();
 assert.equal(await nft.ownerOf(large),o);assertions++;
 console.log(JSON.stringify({compiler:solc.version(),runtime:'existing Hardhat in-process EVM',assertions,
  result:'PASS',scope:'EVM escrow contract execution; not TRON TVM deployment or provider API integration'}));
}
main().catch(error=>{console.error(error.message);process.exitCode=1;});
