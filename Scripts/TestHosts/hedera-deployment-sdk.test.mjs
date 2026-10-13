import test from 'node:test';
import assert from 'node:assert/strict';
import {Client,AccountId,AccountInfoQuery,Hbar} from './hedera-deployment-sdk.mjs';
import {restoreKey} from '../../Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.HashgraphOASIS/SdkBridge/bridge.mjs';

test('deployment client accepts provider bridge key and account instances without CJS identity mismatch',()=>{
  // Public, disposable deterministic test vector; never a funded identity.
  const key=restoreKey({keyType:'ecdsa',privateKey:'1'.padStart(64,'0')});
  const client=Client.forTestnet();
  try {
    const account=AccountId.fromString('0.0.1234');
    client.setOperator(account,key);
    assert.equal(client.getOperator().accountId.toString(),account.toString());
    assert.equal(client.getOperator().publicKey.toStringDer(),key.publicKey.toStringDer());
    assert.doesNotThrow(()=>new AccountInfoQuery().setAccountId(account).setMaxQueryPayment(Hbar.fromTinybars('100000000')));
  } finally {client.close();}
});
