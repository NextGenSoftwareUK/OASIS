const fs=require('fs');
const path=require('path');
const assert=require('assert/strict');
const root=path.resolve(__dirname,'../..');
const {ethers}=require(path.join(root,'Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS/node_modules/ethers'));
async function main(){
  const provider=new ethers.JsonRpcProvider('http://127.0.0.1:18545',undefined,{cacheTimeout:-1,pollingInterval:100});
  assert.equal((await provider.getNetwork()).chainId,31337n,'Refusing non-fixture chain');
  const fixture=JSON.parse(fs.readFileSync(path.join(root,'TestResults/Web3CoreCurrent/web3-storage.json'),'utf8'));
  const signer=await provider.getSigner(0);
  const contract=await new ethers.ContractFactory(fixture.abi,fixture.bytecode,signer).deploy();
  await contract.waitForDeployment();
  let assertions=0;
  for(const [create,remove,page,count] of [
    ['createAvatar','deleteAvatar','getAvatarsPage','getAvatarsCount'],
    ['createAvatarDetail','deleteAvatarDetail','getAvatarDetailsPage','getAvatarDetailsCount'],
    ['createHolon','deleteHolon','getHolonsPage','getHolonsCount']]){
    for(let id=1;id<=5;id++) await (await contract[create](id,ethers.id('owned-'+id),'0x0102')).wait();
    assert.equal(await contract[count](),5n);assertions++;
    const block=Number(BigInt(await provider.send('eth_blockNumber',[])));
    const first=await contract[page](0,2,{blockTag:block});
    assert.deepEqual(first.map(row=>Number(row.EntityId)),[1,2]);assertions++;
    const second=await contract[page](2,2,{blockTag:block});
    assert.deepEqual(second.map(row=>Number(row.EntityId)),[3,4]);assertions++;
    assert.equal((await contract[page](4,2)).length,1);assertions++;
    assert.equal((await contract[page](100,2)).length,0);assertions++;
    await assert.rejects(contract[page](0,0));assertions++;
    await assert.rejects(contract[page](0,257));assertions++;
    await assert.rejects(contract[create](1,ethers.id('duplicate'),'0x00'));assertions++;
    await (await contract[remove](2)).wait();
    assert.equal(await contract[count](),4n);assertions++;
    assert.deepEqual((await contract[page](0,256)).map(row=>Number(row.EntityId)).sort(),[1,3,4,5]);assertions++;
    assert.deepEqual((await contract[page](0,2,{blockTag:block})).map(row=>Number(row.EntityId)),[1,2]);assertions++;
    await (await contract[create](2,ethers.id('recreated'),'0x0304')).wait();
    assert.equal(await contract[count](),5n);assertions++;
    assert.equal((await contract[page](0,256)).filter(row=>row.EntityId===2n).length,1);assertions++;
  }
  const other=contract.connect(await provider.getSigner(1));
  await assert.rejects(other.createAvatar(99,ethers.id('unauthorized'),'0x00'));assertions++;
  assert.equal(await contract.getAvatarsCount(),5n);assertions++;
  const result={passed:true,assertions,collections:3,chainId:31337,contract:await contract.getAddress()};
  fs.writeFileSync(path.join(root,'TestResults/Web3CoreCurrent/web3-pages-current.json'),JSON.stringify(result));
  console.log(JSON.stringify(result));await provider.destroy();
}
main().catch(error=>{console.error(error.message);process.exitCode=1;});
