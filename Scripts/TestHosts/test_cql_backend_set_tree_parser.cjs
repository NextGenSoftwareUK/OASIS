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
 q=compile('WITH q AS ((SELECT Version AS Number FROM '+provider+' LIMIT 2) UNION ALL (SELECT Version AS Number FROM '+provider+' LIMIT 2)) SELECT SUM(Number) AS Total FROM q');
 assert.equal(q.body.reduction,'Sum');assert.equal(q.body.reductionColumn,'Number');assert.equal(q.scalarAlias,'Total');checks++;
 q=compile('WITH q AS ((SELECT Version AS Number FROM '+provider+' LIMIT 2) UNION ALL (SELECT Version AS Number FROM '+provider+' LIMIT 2)) SELECT AVG(Number) AS Mean FROM q WHERE Number > 0');
 assert.equal(q.body.reduction,'Average');assert.equal(q.body.reductionColumn,'Number');assert.equal(q.body.selectedStages[0].operation,'Where');checks++;
 q=compile('WITH q AS ('+selected+') SELECT MIN(Name) AS First FROM q');
 assert.equal(q.body.reduction,'Min');assert.equal(q.body.reductionColumn,'Name');assert.equal(q.scalarAlias,'First');checks++;
 q=compile('WITH q AS ('+selected+') SELECT MAX(Description) AS Last FROM q WHERE Name IS NOT NULL');
 assert.equal(q.body.reduction,'Max');assert.equal(q.body.reductionColumn,'Description');assert.equal(q.body.selectedStages[0].operation,'Where');checks++;
 q=compile('WITH q AS ('+selected+') SELECT COUNT(Description) AS Present FROM q');
 assert.equal(q.body.reduction,'LongCount');assert.equal(q.body.reductionColumn,undefined);
 assert.deepEqual(q.body.selectedStages.at(-1),{operation:'Where',filters:[{alias:'Description',operator:'NotEqual',value:null}]});checks++;
 q=compile('WITH q AS ('+selected+') SELECT COUNT(DISTINCT Name) AS UniqueNames FROM q');
 assert.equal(q.body.reduction,'LongCount');assert.deepEqual(q.body.selectedStages.slice(-2),[{operation:'Select',columns:[{alias:'Name',sourceAlias:'Name'}]},{operation:'Distinct'}]);checks++;
 q=compile('(SELECT Name AS Label FROM (SELECT * FROM '+provider+' ORDER BY Id LIMIT 2) AS sample WHERE Version > 1) UNION ALL (SELECT Name AS Label FROM '+provider+' LIMIT 2)');
 assert.equal(q.path,'data/query-cql/set');assert.equal(q.body.left.orderBy,'Id');assert.equal(q.body.left.take,2);
 assert.equal(q.body.left.entityStages[0].operation,'Where');assert.equal(q.body.left.entityStages[0].where.filter.field,'Version');assert.equal(q.body.left.where,undefined);checks++;
 q=compile('WITH sample AS (SELECT * FROM '+provider+' LIMIT 2), narrowed AS (SELECT * FROM sample WHERE Version > 0), labels AS (SELECT Name AS Label FROM narrowed) SELECT DISTINCT Label FROM labels UNION ALL (SELECT Name AS Label FROM '+provider+' LIMIT 2)');
 assert.equal(q.body.left.take,2);assert.equal(q.body.left.entityStages.length,1);assert.equal(q.body.left.stages.at(-1).operation,'Distinct');assert.equal(q.body.left.columns[0].alias,'Label');checks++;
 assert.throws(()=>compile('SELECT Name FROM (SELECT * FROM '+provider+' LIMIT 2) AS sample WHERE Version > 0'),/set operand/);checks++;


}
assert.throws(()=>compile('SELECT * FROM MongoDBOASIS UNION SELECT * FROM SQLLiteDBOASIS'),/same provider/);checks++;
assert.throws(()=>compile('WITH q AS ((SELECT Name FROM MongoDBOASIS) UNION (SELECT Name FROM MongoDBOASIS)) SELECT Name FROM q'),/explicit TOP|LIMIT|FETCH/);checks++;
console.log(JSON.stringify({serverEngineOnly:true,checks,providers:3}));
