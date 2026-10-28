import { defineSchema, defineTable } from "convex/server";
import { v } from "convex/values";

export default defineSchema({
  oasis_records: defineTable({ key: v.string(), value: v.string(), revision: v.number() }).index("by_key", ["key"]),
});
