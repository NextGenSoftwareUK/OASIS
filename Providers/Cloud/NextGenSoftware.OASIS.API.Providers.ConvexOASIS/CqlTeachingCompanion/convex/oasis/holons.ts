import { internalMutation, internalQuery } from "../_generated/server";
import { v } from "convex/values";

const canonicalId = /^[0-9a-f]{8}-(?:[0-9a-f]{4}-){3}[0-9a-f]{12}$/;
const emptyId = "00000000-0000-0000-0000-000000000000";

export const readTeachingHolon = internalQuery({
  args: { id: v.string() },
  handler: async (ctx, { id }) => {
    if (!canonicalId.test(id) || id === emptyId) throw new Error("A canonical OASIS ID is required");
    const row = await ctx.db.query("oasis_holons").withIndex("by_oasis_id", (q) => q.eq("id", id)).unique();
    if (!row || row.isDeleted) return { status: "absent" } as const;
    const data = JSON.parse(row.dataJson);
    if (data.id !== id) throw new Error("Stored Holon identity mismatch");
    if (data.isPublic !== true) return { status: "denied" } as const;
    return { status: "ok", dataJson: row.dataJson } as const;
  },
});

export const seedTeachingHolon = internalMutation({
  args: { id: v.string(), isDeleted: v.boolean(), dataJson: v.string() },
  handler: async (ctx, { id, isDeleted, dataJson }) => {
    if (!canonicalId.test(id) || id === emptyId || JSON.parse(dataJson).id !== id)
      throw new Error("A canonical matching teaching identity is required");
    const existing = await ctx.db.query("oasis_holons").withIndex("by_oasis_id", (q) => q.eq("id", id)).unique();
    if (existing) throw new Error("The teaching identity already exists");
    return ctx.db.insert("oasis_holons", { id, isDeleted, dataJson, parentHolonId: emptyId, holonType: 0 });
  },
});
