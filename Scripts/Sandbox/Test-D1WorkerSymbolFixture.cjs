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
const script = `
const enrolled = new Set(${JSON.stringify(keys)});
export default {
  async fetch(request, env) {
    const url = new URL(request.url);
    if (url.pathname !== "/symbols") return new Response(null, {status:404});
    if (request.method !== "GET") return new Response(null, {status:405});
    const symbols = url.searchParams.getAll("symbol");
    if ([...url.searchParams.keys()].some(key => key !== "symbol") ||
        symbols.length < 1 || symbols.length > enrolled.size ||
        new Set(symbols).size !== symbols.length || symbols.some(key => !enrolled.has(key)))
      return Response.json({error:"Invalid symbol selection"}, {status:400});
    const placeholders = symbols.map(() => "?").join(",");
    const statement = env.DB.prepare("SELECT symbol FROM demo_symbols WHERE symbol IN (" + placeholders + ") ORDER BY symbol");
    const result = await statement.bind(...symbols).all();
    if (!result.success || result.results.length !== symbols.length)
      return Response.json({error:"D1 source rows unavailable"}, {status:502});
    return Response.json({symbols:result.results.map(row => row.symbol)});
  }
};
`;
async function main() {
  const mf = new Miniflare({modules:true, host:"127.0.0.1", compatibilityDate:"2026-07-30",
    d1Databases:{DB:"cosmic-d1-recovery-fixture"}, d1Persist:false, script});
  try {
    const db = await mf.getD1Database("DB");
    await db.exec("CREATE TABLE demo_symbols (symbol TEXT PRIMARY KEY)");
    for (const key of keys) await db.prepare("INSERT INTO demo_symbols VALUES (?)").bind(key).run();
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
    const write = await fetch(endpoint, {method:"POST"});
    assert.equal(invalid.status, 400);
    assert.equal(write.status, 405);
    console.log(JSON.stringify({full, subset, duplicateStatus:invalid.status, writeStatus:write.status}));
    if (serve) {
      console.log(JSON.stringify({endpoint: endpoint.href}));
      // The parent test owns the process and closes stdin when it finishes.
      process.stdin.resume();
      for await (const ignored of process.stdin) { }
    }
  } finally { await mf.dispose(); }
}
main().catch(error => { console.error(error); process.exitCode = 1; });
