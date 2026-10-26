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
for (const mode of ['decimalSource', 'decimalGroups', 'doubleSource', 'doubleGroups', 'singleSource', 'singleGroups']) {
  assert.ok(page.includes('<option value="' + mode + '">'), mode + ' must be selectable in the page');
  element('example').value = mode;
  vm.runInContext('preset()', context);
  const body = JSON.parse(element('query').value);
  const grouped = mode.endsWith('Groups');
  const single = mode.startsWith('single');
  const floating = mode.startsWith('double') || single;
  const bounds = single ? ['1.0000002', '1.0000005'] : floating ? ['1.0000000000000004', '1.0000000000000009'] : [lower, upper];
  assert.equal(vm.runInContext('queryRoute', context), '/api/data/query-cql/' + (grouped ? 'grouped' : 'projection'));
  const filters = grouped ? body.resultFilters : body.filters.slice(1);
  assert.deepEqual(filters.map(filter => filter.value), bounds);
  assert.ok(filters.every(filter => typeof filter.value === 'string'));
  assert.equal(grouped ? body.keys[0].valueType : body.columns[1].valueType, single ? 'Single' : floating ? 'Double' : 'Decimal');
  if (grouped) {
    assert.equal(body.resultOrder[0].alias, floating ? 'Reading' : 'Price');
    assert.equal(body.resultTake, 2);
  }
}
// Numeric presets must retain unsigned input types and exact Decimal thresholds.
for (const mode of ['unsignedSum', 'unsignedAverage', 'unsignedMeanGroups']) {
  assert.ok(page.includes('<option value="' + mode + '">'));
  element('example').value = mode;
  vm.runInContext('preset()', context);
  const body = JSON.parse(element('query').value);
  const grouped = mode === 'unsignedMeanGroups';
  assert.equal(vm.runInContext('queryRoute', context), '/api/data/query-cql/' + (grouped ? 'grouped' : 'scalar'));
  assert.equal(grouped ? body.columns[0].selection.valueType : body.selection.valueType, 'UInt64');
  assert.equal(body.take, mode === 'unsignedSum' ? 1 : 2);
  if (grouped) {
    assert.equal(body.resultFilters[0].value, '18446744073709551612.5');
    assert.equal(body.resultTake, 1);
  }
}
// Decimal arithmetic uses the same scoped source and exact string thresholds.
for (const mode of ['decimalSum', 'decimalAverage', 'decimalNumericGroups']) {
  assert.ok(page.includes('<option value="' + mode + '">'));
  element('example').value = mode;
  vm.runInContext('preset()', context);
  const body = JSON.parse(element('query').value);
  const grouped = mode === 'decimalNumericGroups';
  assert.equal(vm.runInContext('queryRoute', context), '/api/data/query-cql/' + (grouped ? 'grouped' : 'scalar'));
  assert.equal(body.take, 2);
  assert.equal(grouped ? body.columns[0].selection.valueType : body.selection.valueType, 'Decimal');
  if (grouped) {
    assert.deepEqual(body.columns.map(column => column.operation), ['Sum', 'Average']);
    assert.deepEqual(body.resultFilters.map(filter => filter.value),
      ['2.0000000000000000000000000005', '1.0000000000000000000000000002']);
    assert.equal(body.resultOrder[0].alias, 'Total');
    assert.equal(body.resultTake, 1);
  } else assert.equal(body.operation, mode === 'decimalSum' ? 'Sum' : 'Average');
}
// Floating presets must bind the input domain, nullability and exact text.
for (const prefix of ['double', 'single']) {
  for (const operation of ['Sum', 'Average', 'NumericGroups']) {
    const mode = prefix + operation;
    assert.ok(page.includes('<option value="' + mode + '">'));
    element('example').value = mode;
    vm.runInContext('preset()', context);
    const body = JSON.parse(element('query').value);
    const grouped = operation === 'NumericGroups';
    const type = prefix === 'single' ? 'Single' : 'Double';
    assert.equal(body.take, 2);
    assert.equal(vm.runInContext('queryRoute', context), '/api/data/query-cql/' + (grouped ? 'grouped' : 'scalar'));
    if (grouped) {
      assert.deepEqual(body.columns.map(column => column.selection.valueType), [type, type]);
      assert.deepEqual(body.columns.map(column => column.selection.nullable), [false, true]);
      assert.deepEqual(body.resultFilters.map(filter => filter.value), prefix === 'single'
        ? ['2.0000005', '1.0000002'] : ['2.000000000000001', '1.0000000000000004']);
      assert.equal(body.resultOrder[0].alias, 'Mean');
      assert.equal(body.resultTake, 1);
    } else {
      assert.equal(body.operation, operation);
      assert.equal(body.selection.valueType, type);
      assert.equal(body.selection.nullable, operation === 'Average');
    }
  }
}
const raw = '{"price":"' + lower + '","integer":9007199254740993}';
context.raw = raw;
const formatted = vm.runInContext('pretty(raw)', context);
assert.ok(formatted.includes('"' + lower + '"'));
assert.ok(formatted.includes('9007199254740993'));
const result = { status: 'passed', presets: 18, lexicalExactness: true, runtime: process.version,
  mode: 'offline-script-behavior-no-browser-or-network' };
fs.writeFileSync('TestResults/CosmicCqlDemo/v1-decimal-ui-check.json', JSON.stringify(result));
console.log(JSON.stringify(result));
