const fs = require('node:fs');
const vm = require('node:vm');
const assert = require('node:assert/strict');
const page = fs.readFileSync('Examples/CosmicCqlDemo/V1DemoPresentation.cs', 'utf8');
const source = page.slice(page.indexOf('<script>') + 8, page.indexOf('</script>'));
const elements = new Map();
const element = id => {
  if (!elements.has(id)) elements.set(id, { value: '', textContent: '', disabled: true });
  return elements.get(id);
};
const context = vm.createContext({ document: { getElementById: element }, fetch: () => new Promise(() => {}) });
new vm.Script(source).runInContext(context);
vm.runInContext("session = { fixtureTag: 'owned-check', ids: [], tokens: {} };", context);
element('provider').value = 'MongoDBOASIS';
const lower = '1.0000000000000000000000000002';
const upper = '1.0000000000000000000000000004';
for (const mode of ['decimalSource', 'decimalGroups', 'doubleSource', 'doubleGroups']) {
  assert.ok(page.includes('<option value="' + mode + '">'), mode + ' must be selectable in the page');
  element('example').value = mode;
  vm.runInContext('preset()', context);
  const body = JSON.parse(element('query').value);
  const grouped = mode.endsWith('Groups');
  const floating = mode.startsWith('double');
  const bounds = floating ? ['1.0000000000000004', '1.0000000000000009'] : [lower, upper];
  assert.equal(vm.runInContext('queryRoute', context), '/api/data/query-cql/' + (grouped ? 'grouped' : 'projection'));
  const filters = grouped ? body.resultFilters : body.filters.slice(1);
  assert.deepEqual(filters.map(filter => filter.value), bounds);
  assert.ok(filters.every(filter => typeof filter.value === 'string'));
  assert.equal(grouped ? body.keys[0].valueType : body.columns[1].valueType, floating ? 'Double' : 'Decimal');
  if (grouped) {
    assert.equal(body.resultOrder[0].alias, floating ? 'Reading' : 'Price');
    assert.equal(body.resultTake, 2);
  }
}
const raw = '{"price":"' + lower + '","integer":9007199254740993}';
context.raw = raw;
const formatted = vm.runInContext('pretty(raw)', context);
assert.ok(formatted.includes('"' + lower + '"'));
assert.ok(formatted.includes('9007199254740993'));
const result = { status: 'passed', presets: 4, lexicalExactness: true, runtime: process.version,
  mode: 'offline-script-behavior-no-browser-or-network' };
fs.writeFileSync('TestResults/CosmicCqlDemo/v1-decimal-ui-check.json', JSON.stringify(result));
console.log(JSON.stringify(result));
