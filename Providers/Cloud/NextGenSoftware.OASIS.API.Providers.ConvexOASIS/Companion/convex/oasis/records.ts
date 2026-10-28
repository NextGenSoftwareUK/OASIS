import { internalMutationGeneric as mutation, internalQueryGeneric as query } from "convex/server";
import { ConvexError, v } from "convex/values";

const table = "oasis_records";
const recordRevision = (doc: { _id: string; revision: number }) => `${doc._id}:${doc.revision}`;
function requireKey(key: string) {
  if (!key || key.length > 1024) throw new Error("Invalid OASIS storage key");
}
function conflict(): never { throw new ConvexError({ code: "OASIS_STORAGE_CONFLICT" }); }

// Internal visibility requires deployment-admin authorization for direct API
// calls; ordinary public clients cannot read or mutate this storage namespace.
export const verify = query({
  args: {},
  handler: async (ctx) => {
    for (const legacy of ["oasis_avatars", "oasis_avatar_details", "oasis_holons"]) {
      if (await ctx.db.query(legacy).first() !== null)
        throw new Error("Legacy OASIS records exist; preserve data and review migration");
    }
    return { protocol: "oasis-private-records-v1" };
  },
});

export const get = query({
  args: { key: v.string() },
  handler: async (ctx, { key }) => {
    requireKey(key);
    const doc = await ctx.db.query(table).withIndex("by_key", q => q.eq("key", key)).unique();
    return doc === null ? null : { value: doc.value, revision: recordRevision(doc) };
  },
});

export const put = mutation({
  args: { key: v.string(), value: v.string(), mode: v.union(v.literal("put"), v.literal("create"), v.literal("replace")), revision: v.optional(v.string()) },
  handler: async (ctx, { key, value, mode, revision }) => {
    requireKey(key);
    if ((mode === "replace") !== (revision !== undefined)) throw new Error("Invalid revision selection");
    const previous = await ctx.db.query(table).withIndex("by_key", q => q.eq("key", key)).unique();
    if (mode === "create" && previous !== null) conflict();
    if (mode === "replace" && (previous === null || recordRevision(previous) !== revision)) conflict();
    const next = previous === null ? 1 : previous.revision + 1;
    if (!Number.isSafeInteger(next) || next < 1) throw new Error("Storage revision exhausted");
    const id = previous === null
      ? await ctx.db.insert(table, { key, value, revision: next })
      : previous._id;
    if (previous !== null) await ctx.db.replace(id, { key, value, revision: next });
    return { key, revision: `${id}:${next}` };
  },
});

export const remove = mutation({
  args: { key: v.string(), revision: v.optional(v.string()) },
  handler: async (ctx, { key, revision }) => {
    requireKey(key);
    const previous = await ctx.db.query(table).withIndex("by_key", q => q.eq("key", key)).unique();
    if (revision !== undefined && (previous === null || recordRevision(previous) !== revision)) conflict();
    if (previous !== null) await ctx.db.delete(previous._id);
    return { key, deleted: previous !== null };
  },
});

export const list = query({
  args: { prefix: v.string(), cursor: v.union(v.string(), v.null()) },
  handler: async (ctx, { prefix, cursor }) => {
    const page = await ctx.db.query(table).withIndex("by_key", q => q.gte("key", prefix))
      .paginate({ cursor, numItems: 100 });
    const keys = page.page.filter(doc => doc.key.startsWith(prefix)).map(doc => doc.key);
    const complete = page.isDone || keys.length !== page.page.length;
    return { keys, complete, cursor: complete ? null : page.continueCursor };
  },
});
