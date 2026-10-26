// Fixed read-only observer for the public C# acceptance deposit. No signing.
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { createHash } from 'node:crypto';
import { ApiPromise } from '@polkadot/api';
import { ReadOnlyHttpProvider } from './read-only-provider.mjs';
import { readNativeTestToken } from './native-psp22-test-token.mjs';
import { readTokenEscrowDeposit } from './psp22-escrow-read.mjs';
assert.equal(process.env.OASIS_POLKADOT_LOCAL_NATIVE, '1');
assert.equal(process.argv.length, 2);
const folder=resolve(import.meta.dirname,'../../../../Docs/WIP/HyperDrive-Providers/NativeEvidence/Polkadot');
const token=JSON.parse(readFileSync(resolve(folder,'psp22-test-token-deployment.completed.json'),'utf8'));
const escrow=JSON.parse(readFileSync(resolve(folder,'psp22-escrow-deployment.completed.json'),'utf8'));
const api=await ApiPromise.create({provider:new ReadOnlyHttpProvider('http://127.0.0.1:19944'),noInitWarn:true});
try {
  assert.equal(api.genesisHash.toHex(),'0xe8cf5d7d636a08721efaa17acc52ed3d895fe2468dccb2520cc8e249ddb6b208');
  const depositId='0x'+createHash('sha256').update('e7b3473198cc497d9f115d5b8dd87fb1').digest('hex');
  console.log(JSON.stringify({token:await readNativeTestToken(api,{contractAddress:token.address,contractCodeHash:token.codeHash,
    origin:token.sender,owner:token.sender,spender:escrow.address}),escrow:await readTokenEscrowDeposit(api,{
    contractAddress:escrow.address,contractCodeHash:escrow.codeHash,origin:token.sender,depositId})}));
} finally {await api.disconnect();}
