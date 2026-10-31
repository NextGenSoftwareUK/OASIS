// Compile the native TVM contract with pinned, already restored tooling.
// Virtual compiler files keep every generated artifact out of source/docs.
const fs = require('node:fs');
const path = require('node:path');
const {createRequire} = require('node:module');
const root = path.resolve(__dirname, '../..');
const sdk = path.join(root, 'Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.TONOASIS/SdkBridge');
const load = createRequire(path.join(sdk, 'package.json'));
const compiler = load('@tact-lang/compiler');
const ts = load('typescript');
const output = path.join(root, 'TestResults/TonCqlCurrent/compiled');

(async () => {
    const files = {'TonHolonStore.tact': fs.readFileSync(path.join(sdk, '../Contracts/TonHolonStore.tact')).toString('base64')};
    const result = await compiler.build({
        config: {name: 'TonHolonStore', path: 'TonHolonStore.tact', output: 'output', options: {debug: false, external: false}},
        project: compiler.createVirtualFileSystem('/project', files, false),
        stdlib: '/stdlib'
    });
    if (!result.ok) throw new Error('Native TON contract compilation failed.');
    fs.mkdirSync(output, {recursive: true});
    for (const [name, content] of Object.entries(files)) {
        if (!name.startsWith('output/')) continue;
        const target = path.join(output, path.basename(name));
        const bytes = Buffer.from(content, 'base64');
        fs.writeFileSync(target, bytes);
        if (name.endsWith('.ts')) {
            const compiled = ts.transpileModule(bytes.toString('utf8'), {
                compilerOptions: {module: ts.ModuleKind.CommonJS, target: ts.ScriptTarget.ES2022}, reportDiagnostics: true
            });
            if (compiled.diagnostics?.some(d => d.category === ts.DiagnosticCategory.Error))
                throw new Error('Generated TON binding transpilation failed.');
            fs.writeFileSync(target.replace(/\.ts$/, '.cjs'), compiled.outputText);
        }
    }
    console.log('Native TVM contract compiled; no network, account, deployment or token purchase.');
})().catch(error => {console.error(error); process.exitCode = 1;});
