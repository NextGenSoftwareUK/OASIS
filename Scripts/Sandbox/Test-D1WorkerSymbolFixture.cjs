const assert = require("node:assert/strict");
const path = require("node:path");
const { createRequire } = require("node:module");

const serve = process.argv[3] === "--serve";
if (process.argv.length !== (serve ? 4 : 3))
  throw new Error("Pass the existing Miniflare runtime directory and optionally --serve.");
const runtime = path.resolve(process.argv[2]);
const runtimeRequire = createRequire(path.join(runtime, "package.json"));
const { Miniflare } = runtimeRequire("miniflare");
process.chdir(runtime);
const keys = ["BTC-USDT", "ETH-USDT", "SOL-USDT"];
const { readFileSync } = require("node:fs");
const script = readFileSync(path.join(__dirname, "D1SymbolWorker.mjs"), "utf8");
const schema = readFileSync(path.join(__dirname, "D1SymbolWorker.sql"), "utf8");
async function main() {
  const mf = new Miniflare({modules:true, host:"127.0.0.1", compatibilityDate:"2026-07-30",
    d1Databases:{DB:"cosmic-d1-recovery-fixture"}, d1Persist:false, script});
  try {
    const db = await mf.getD1Database("DB");
    await db.exec(schema);
    async function read(selected) {
      const url = new URL("/symbols", await mf.ready);
      for (const key of selected) url.searchParams.append("symbol", key);
      const response = await fetch(url);
      assert.equal(response.status, 200);
      const result = await response.json();
      assert.deepEqual(result.symbols, selected);
      return result.symbols;
    }
    const full = await read(keys);
    const subset = await read([keys[0], keys[2]]);
    const endpoint = new URL("/symbols", await mf.ready);
    const invalid = await fetch(new URL("?symbol=BTC-USDT&symbol=BTC-USDT", endpoint));
    const unenrolled = await fetch(new URL("?symbol=BTC-USDT&symbol=UNENROLLED", endpoint));
    const unbounded = await fetch(endpoint);
    const write = await fetch(endpoint, {method:"POST"});
    assert.equal(invalid.status, 400);
    assert.equal(unenrolled.status, 400);
    assert.equal(unbounded.status, 400);
    assert.deepEqual(await invalid.json(), {error:"Invalid symbol selection"});
    assert.deepEqual(await unenrolled.json(), {error:"Invalid symbol selection"});
    assert.deepEqual(await unbounded.json(), {error:"Invalid symbol selection"});
    assert.equal(write.status, 405);
    console.log(JSON.stringify({full, subset, duplicateStatus:invalid.status,
      unenrolledStatus:unenrolled.status, unboundedStatus:unbounded.status, writeStatus:write.status}));
    if (serve) {
      console.log(JSON.stringify({endpoint: endpoint.href}));
      // The parent test owns the process and closes stdin when it finishes.
      process.stdin.resume();
      for await (const ignored of process.stdin) { }
    }
  } finally { await mf.dispose(); }
}
main().catch(error => { console.error(error); process.exitCode = 1; });
