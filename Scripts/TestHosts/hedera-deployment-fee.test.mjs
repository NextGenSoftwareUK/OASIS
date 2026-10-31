import test from 'node:test';import assert from 'node:assert/strict';import {estimatedTinybars,validateDeploymentBudget,quotedDeploymentTinybars} from './hedera-deployment-fee.mjs';
test('fee conversion rounds up exactly using current network rate units',()=>{
 const result=estimatedTinybars('10000000000',{hbar_equivalent:30000,cent_equivalent:283139});
 assert.equal(result,1059550257n);
 assert.ok(result*283139n>=10000000000n*30000n);
 assert.ok((result-1n)*283139n<10000000000n*30000n);
 assert.equal(estimatedTinybars('9007199254740993',{hbar_equivalent:1,cent_equivalent:3}),3002399751580331n);
});
test('observed SDK base-only estimate cannot authorize deployment without gas budget',()=>{
 const quote=quotedDeploymentTinybars('10000000000',{hbar_equivalent:30000,cent_equivalent:263244},2000000,97n,[]);
 assert.deepEqual(quote,{base:1139627115n,gas:194000000n,total:1333627115n});
 assert.throws(()=>validateDeploymentBudget(quote.total,1200000000n,1391606260n),/fee ceiling/);
 assert.ok(1259196297n<=quote.total,'actual retained consensus charge fits this snapshot quote, not the old SDK setting');
});
test('gas quote rejects loss of precision, overflow and duplicate coverage',()=>{
 const rate={hbar_equivalent:1,cent_equivalent:1};
 for(const gas of [0,-1,1.5,Number.MAX_SAFE_INTEGER+1])assert.throws(()=>quotedDeploymentTinybars('1',rate,gas,1n));
 for(const price of [0n,-1n,1,9223372036854775808n])assert.throws(()=>quotedDeploymentTinybars('1',rate,1,price));
 assert.throws(()=>quotedDeploymentTinybars('1',rate,2,9223372036854775807n,[]),/int64/);
 assert.throws(()=>quotedDeploymentTinybars('1',rate,1,1n),/extras/);
 assert.throws(()=>quotedDeploymentTinybars('1',rate,1,1n,[{name:'GAS'}]),/already includes/);
});
test('estimates do not raise ceilings or authorize unfunded deployment',()=>{
 assert.throws(()=>validateDeploymentBudget(1059544624n,100000000n,391606260n),/fee ceiling/);
 assert.throws(()=>validateDeploymentBudget(1059544624n,1500000000n,391606260n),/funding/);
 validateDeploymentBudget(1059544624n,1500000000n,2391606260n);
 assert.throws(()=>estimatedTinybars('1',{hbar_equivalent:0,cent_equivalent:1}));
 assert.throws(()=>estimatedTinybars('1.5',{hbar_equivalent:1,cent_equivalent:1}));
});
