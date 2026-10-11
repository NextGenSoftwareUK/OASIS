import test from 'node:test';import assert from 'node:assert/strict';import {estimatedTinybars,validateDeploymentBudget} from './hedera-deployment-fee.mjs';
test('fee conversion rounds up exactly using current network rate units',()=>{
 const result=estimatedTinybars('10000000000',{hbar_equivalent:30000,cent_equivalent:283139});
 assert.equal(result,1059550257n);
 assert.ok(result*283139n>=10000000000n*30000n);
 assert.ok((result-1n)*283139n<10000000000n*30000n);
 assert.equal(estimatedTinybars('9007199254740993',{hbar_equivalent:1,cent_equivalent:3}),3002399751580331n);
});
test('estimates do not raise ceilings or authorize unfunded deployment',()=>{
 assert.throws(()=>validateDeploymentBudget(1059544624n,100000000n,391606260n),/fee ceiling/);
 assert.throws(()=>validateDeploymentBudget(1059544624n,1500000000n,391606260n),/funding/);
 validateDeploymentBudget(1059544624n,1500000000n,2391606260n);
 assert.throws(()=>estimatedTinybars('1',{hbar_equivalent:0,cent_equivalent:1}));
 assert.throws(()=>estimatedTinybars('1.5',{hbar_equivalent:1,cent_equivalent:1}));
});
