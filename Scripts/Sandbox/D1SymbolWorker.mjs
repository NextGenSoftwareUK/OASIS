const enrolled = new Set(["BTC-USDT", "ETH-USDT", "SOL-USDT"]);
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
