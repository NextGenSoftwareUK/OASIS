const assert=require('node:assert/strict');
const fs=require('node:fs');
const path=require('node:path');
const vm=require('node:vm');
const root=path.resolve(__dirname,'../..');
const engine=vm.createContext({});
new vm.Script(fs.readFileSync(path.join(root,'ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI/Services/Cql/Text/cql-text.js'),'utf8')).runInContext(engine);
function compile(text){engine.text=text;return JSON.parse(vm.runInContext('JSON.stringify(CqlText.compile(text))',engine));}
let checks=0;
for(const provider of ['MongoDBOASIS','SQLLiteDBOASIS','Neo4jOASIS']) {
 const a='(SELECT * FROM '+provider+' LIMIT 2)',b='(SELECT * FROM '+provider+' LIMIT 3)',c='(SELECT * FROM '+provider+' LIMIT 4)';
 let q=compile(a+' UNION ALL '+b+' EXCEPT '+c+' LIMIT 1');
 assert.equal(q.body.kind,'Except');assert.equal(q.body.left.set.kind,'Concat');
 assert.equal(q.body.left.set.left.take,2);assert.equal(q.body.left.set.right.take,3);assert.equal(q.body.take,1);
 assert.equal(q.body.left.set.provider,undefined);assert.equal(q.body.left.set.maximumResults,undefined);checks++;
 q=compile(a+' UNION '+b+' INTERSECT '+c);assert.equal(q.body.kind,'Union');assert.equal(q.body.right.set.kind,'Intersect');checks++;
 const selected='(SELECT Name, Description FROM '+provider+' LIMIT 2) UNION ALL (SELECT Name, Description FROM '+provider+' LIMIT 2)';
 q=compile('WITH q AS ('+selected+') SELECT COUNT(*) AS total FROM q');
 assert.equal(q.body.reduction,'LongCount');assert.equal(q.scalarAlias,'total');assert.equal(q.body.selectedStages,undefined);checks++;
 q=compile("WITH q AS ("+selected+") SELECT Name AS Label FROM q WHERE Description = 'x' ORDER BY Name LIMIT 3");
 assert.deepEqual(q.body.selectedStages.map(s=>s.operation),['Where','OrderBy','Take','Select']);
 assert.deepEqual(q.body.selectedStages.at(-1).columns,[{alias:'Label',sourceAlias:'Name'}]);checks++;
}
assert.throws(()=>compile('SELECT * FROM MongoDBOASIS UNION SELECT * FROM SQLLiteDBOASIS'),/same provider/);checks++;
assert.throws(()=>compile('WITH q AS ((SELECT Name FROM MongoDBOASIS) UNION (SELECT Name FROM MongoDBOASIS)) SELECT Name FROM q'),/explicit TOP|LIMIT|FETCH/);checks++;
console.log(JSON.stringify({serverEngineOnly:true,checks,providers:3}));
