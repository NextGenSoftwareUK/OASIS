import { httpRouter } from "convex/server";
import { internal } from "./_generated/api";
import { httpAction } from "./_generated/server";

const http = httpRouter();

http.route({
  path: "/oasis/cql/holon",
  method: "GET",
  handler: httpAction(async (ctx, request) => {
    const reader = process.env.OASIS_CQL_READER_TOKEN;
    if (!reader || request.headers.get("Authorization") !== `Bearer ${reader}`)
      return new Response(null, { status: 401 });
    const params = new URL(request.url).searchParams;
    const id = params.get("id");
    if (!id || params.size !== 1 || !/^[0-9a-f]{8}-(?:[0-9a-f]{4}-){3}[0-9a-f]{12}$/.test(id)
      || id === "00000000-0000-0000-0000-000000000000") return new Response(null, { status: 400 });
    const result = await ctx.runQuery(internal.oasis.holons.readTeachingHolon, { id });
    if (result.status === "denied") return new Response(null, { status: 403 });
    return new Response(result.status === "absent" ? "null" : result.dataJson, {
      headers: { "Content-Type": "application/json", "Cache-Control": "no-store" },
    });
  }),
});

export default http;
