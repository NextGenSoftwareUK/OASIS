// Execute the actual demo JavaScript against a small DOM/HTTP harness.
// No browser install, network request, database or dependency restore is needed.
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');

async function main() {
    const root = path.resolve(__dirname, '../..');
    const page = fs.readFileSync(path.join(root, 'Examples/CosmicCqlDemo/V1DemoPresentation.cs'), 'utf8');
    const start = page.indexOf('<script>') + '<script>'.length;
    const script = page.slice(start, page.indexOf('</script>', start));
    const bootstrap = script.indexOf("fetch('/demo/session')");
    assert.ok(bootstrap > 0, 'The demo session bootstrap must be present.');

    const elements = new Map();
    const element = id => {
        if (!elements.has(id)) elements.set(id, { value: '', textContent: '', disabled: false });
        return elements.get(id);
    };
    const requests = [];
    let sequence = 0;
    const context = vm.createContext({
        document: { getElementById: element },
        crypto: { randomUUID: () => `logical-operation-${++sequence}` },
        fetch: async (url, options) => {
            requests.push({ url, options });
            return { status: 200, headers: { get: () => 'settled' }, text: async () => '{"result":[]}' };
        }
    });
    vm.runInContext(script.slice(0, bootstrap), context);
    vm.runInContext('session = { fixtureTag: "owned-fixture", tokens: { Alice: "synthetic", Visitor: "visitor" } };', context);

    let presets = 0;
    for (const provider of ['MongoDBOASIS', 'SQLLiteDBOASIS', 'Neo4jOASIS', '']) {
        element('provider').value = provider;
        for (const example of ['compositionFilter', 'compositionTake', 'compositionExact', 'leftJoin', 'innerJoin']) {
            element('example').value = example;
            vm.runInContext('preset()', context);
            const body = JSON.parse(element('query').value);
            assert.equal(body.provider, provider || undefined);
            if (example.startsWith('composition')) {
                assert.equal(element('route').textContent, '/api/data/query-cql/composition');
                assert.equal(body.source.provider, undefined);
                assert.equal(body.source.filters[0].value, 'owned-fixture');
                assert.equal(body.source.skip, 1);
                assert.equal(body.source.take, 2);
                assert.deepEqual(body.source.columns.map(column => column.alias), ['Name', 'Precision']);
                const expected = example === 'compositionFilter' ? ['Where', 'OrderBy', 'Take']
                    : example === 'compositionTake' ? ['Take', 'Where'] : ['Where', 'OrderBy', 'Skip', 'Take'];
                assert.deepEqual(body.stages.map(stage => stage.operation), expected);
                if (example === 'compositionExact') assert.equal(body.stages[0].filters[0].value, '9007199254740993');
            } else {
                assert.equal(element('route').textContent, '/api/data/query-cql/join');
                assert.equal(body.kind, example === 'leftJoin' ? 'Left' : 'Inner');
                assert.equal(body.right.filters[0].value, 'owned-fixture');
            }
            presets++;
        }
    }

    // Changing the current UI cannot alter an intentional repeat's route,
    // actor, body or idempotency key.
    element('provider').value = 'MongoDBOASIS';
    element('example').value = 'compositionExact';
    element('actor').value = 'Alice';
    vm.runInContext('preset()', context);
    await vm.runInContext('run(false)', context);
    element('example').value = 'leftJoin';
    element('actor').value = 'Visitor';
    vm.runInContext('preset()', context);
    await vm.runInContext('run(true)', context);
    assert.equal(requests.length, 2);
    assert.equal(requests[0].url, '/api/data/query-cql/composition');
    assert.deepEqual(requests[1], requests[0]);
    assert.match(vm.runInContext('pretty(\'{"exact":9007199254740993}\')', context), /9007199254740993/);
    process.stdout.write(`Passed: ${presets} native-query browser presets, repeat ownership and exact JSON formatting.\n`);
}

main().catch(error => { process.stderr.write(error.stack + '\n'); process.exitCode = 1; });
