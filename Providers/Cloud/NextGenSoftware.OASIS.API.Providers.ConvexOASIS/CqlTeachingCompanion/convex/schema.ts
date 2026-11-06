import { defineSchema, defineTable } from "convex/server";
import { v } from "convex/values";

export default defineSchema({
  oasis_holons: defineTable({
    id: v.string(),
    parentHolonId: v.string(),
    holonType: v.number(),
    isDeleted: v.boolean(),
    dataJson: v.string(),
  }).index("by_oasis_id", ["id"]),
});
