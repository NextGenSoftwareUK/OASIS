// Existing solc/Hardhat only. This tests EVM invariants, not live HTS consensus.
const fs = require('node:fs');
const path = require('node:path');
const assert = require('node:assert/strict');
const { createRequire } = require('node:module');
const root = path.resolve(__dirname, '../..');
const existing = createRequire(path.join(root, 'Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS/package.json'));
const solc = existing('solc');
const { ethers } = existing('ethers');
process.env.HARDHAT_CONFIG = path.join(root, 'Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS/hardhat.config.cjs');
process.env.HARDHAT_NETWORK = 'hardhat';
const hardhat = existing('hardhat');
const source = fs.readFileSync(path.join(root, 'Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.HashgraphOASIS/Contracts/OASISHederaNftEscrow.sol'), 'utf8');
const fixture = `pragma solidity ^0.8.0;
contract FixtureNFT {
 mapping(uint256=>address) public ownerOf;
 mapping(uint256=>address) public approved;
 mapping(address=>bool) private associated;
 bool public failTransfer;
 bool public failAssociation;
 constructor(){ownerOf[1]=msg.sender;ownerOf[2]=msg.sender;}
 function setFailures(bool transferFailure,bool associationFailure) external {failTransfer=transferFailure;failAssociation=associationFailure;}
 function isAssociated() external view returns(bool){return associated[msg.sender];}
 function associate() external returns(uint256){if(failAssociation)return 1;associated[msg.sender]=true;return 22;}
 function approve(address spender,uint256 serial) external {require(ownerOf[serial]==msg.sender);approved[serial]=spender;}
 function transferFrom(address from,address to,uint256 serial) external {
  require(!failTransfer);require(ownerOf[serial]==from);require(msg.sender==from||approved[serial]==msg.sender);
  ownerOf[serial]=to;delete approved[serial];
 }
}`;
async function main() {
 const compiled=JSON.parse(solc.compile(JSON.stringify({language:'Solidity',sources:{'Escrow.sol':{content:source},'Fixture.sol':{content:fixture}},settings:{evmVersion:'paris',outputSelection:{'*':{'*':['abi','evm.bytecode.object']}}}})));
 const errors=(compiled.errors||[]).filter(e=>e.severity==='error');
 assert.equal(errors.length,0,errors.map(e=>e.formattedMessage).join('\n'));
 const provider=new ethers.BrowserProvider(hardhat.network.provider);provider.pollingInterval=50;
 const owner=await provider.getSigner(0), outsider=await provider.getSigner(1);
 async function deploy(file,name){const c=compiled.contracts[file][name];const deployed=await new ethers.ContractFactory(c.abi,c.evm.bytecode.object,owner).deploy();await deployed.waitForDeployment();return deployed;}
 const escrow=await deploy('Escrow.sol','OASISHederaNftEscrow'), nft=await deploy('Fixture.sol','FixtureNFT');
 const e=await escrow.getAddress(), t=await nft.getAddress(), o=await owner.getAddress();
 let assertions=0;
 async function rejects(action){await assert.rejects(action);assertions++;}
 await rejects(()=>escrow.unlockNFT.staticCall(t,1));
 await rejects(()=>escrow.lockNFT.staticCall(t,0));
 await rejects(()=>escrow.lockNFT.staticCall(t,9223372036854775808n));
 await rejects(()=>escrow.connect(outsider).lockNFT.staticCall(t,1));
 await rejects(()=>escrow.lockNFT.staticCall(t,1));
 await (await nft.approve(e,1)).wait();
 await (await nft.setFailures(false,true)).wait();
 await rejects(async()=>{await(await escrow.lockNFT(t,1,{gasLimit:400000})).wait();});
 assert.equal(await escrow.nftDepositor(t,1),ethers.ZeroAddress);assertions++;
 assert.equal(await nft.ownerOf(1),o);assertions++;
 await (await nft.setFailures(true,false)).wait();
 await rejects(async()=>{await(await escrow.lockNFT(t,1,{gasLimit:400000})).wait();});
 assert.equal(await escrow.nftDepositor(t,1),ethers.ZeroAddress);assertions++;
 await (await nft.setFailures(false,false)).wait();
 await (await escrow.lockNFT(t,1)).wait();
 assert.equal(await escrow.nftDepositor(t,1),o);assertions++;
 assert.equal(await nft.ownerOf(1),e);assertions++;
 await rejects(()=>escrow.connect(outsider).unlockNFT.staticCall(t,1));
 await rejects(()=>escrow.lockNFT.staticCall(t,1));
 await (await nft.setFailures(true,false)).wait();
 await rejects(async()=>{await(await escrow.unlockNFT(t,1,{gasLimit:400000})).wait();});
 assert.equal(await escrow.nftDepositor(t,1),o);assertions++;
 assert.equal(await nft.ownerOf(1),e);assertions++;
 await (await nft.setFailures(false,false)).wait();
 await (await escrow.unlockNFT(t,1)).wait();
 assert.equal(await escrow.nftDepositor(t,1),ethers.ZeroAddress);assertions++;
 assert.equal(await nft.ownerOf(1),o);assertions++;
 await (await nft.approve(e,2)).wait();
 await (await escrow.lockNFT(t,2)).wait();
 await (await escrow.unlockNFT(t,2)).wait();
 assert.equal(await nft.ownerOf(2),o);assertions++;
 console.log(JSON.stringify({compiler:solc.version(),runtime:'existing in-process Hardhat',assertions,liveHedera:false}));
}
main().catch(error=>{console.error(error);process.exitCode=1;});
