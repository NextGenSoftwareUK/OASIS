using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Core.Serialization;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

/// <summary>Native child-selection intent, owned by the persisted parent revision.</summary>
public sealed class CosmicQueryChildSelection
{
    public Guid ParentId { get; }
    // Null means the older parent-link representation; empty means a known empty manifest.
    public IReadOnlyList<Guid> MemberIds { get; }
    public HolonType ChildType { get; }
    public int MaximumNodes { get; }
    public int MaximumPayloadBytes { get; }

    internal CosmicQueryChildSelection(Guid parent, Guid[] members, CosmicQueryGraphOptions options,
        int remainingNodes, int remainingBytes)
    {
        ParentId = parent;
        MemberIds = members == null ? null : Array.AsReadOnly(members);
        ChildType = options.ChildType;
        MaximumNodes = remainingNodes;
        MaximumPayloadBytes = remainingBytes;
    }
}

/// <summary>
/// Shared bounded expansion over native, transaction-bound child reads. This
/// assembles memberships; it never evaluates business predicates or reloads
/// through HolonManager. Input records must be exclusively owned native results.
/// </summary>
public static class HolonGraphQueryExpansion
{
    public static async Task<OASISResult<IReadOnlyList<IHolon>>> ExpandAsync(
        IReadOnlyList<IHolon> roots, CosmicQueryGraphOptions options,
        Func<CosmicQueryChildSelection, CancellationToken, Task<IReadOnlyList<IHolon>>> readChildren,
        CancellationToken cancellationToken = default, Action checkNativeBudget = null)
    {
        try
        {
            if (roots == null || options == null || readChildren == null)
                throw Invalid("Graph expansion requires native roots, options and a bound native reader.");
            var rootCount = roots.Count;
            if (rootCount < 0) throw Invalid("The native root collection has an invalid count.");
            if (rootCount > options.MaximumNodes) throw Limit("CQL roots exceed the graph node budget.");
            var result = new IHolon[rootCount];
            var rootIds = new HashSet<Guid>();
            var pending = new Queue<(IHolon Node, int Depth, HashSet<Guid> Seen)>();
            long bytes = 0;
            void Charge(IHolon record)
            {
                record.Children = new List<IHolon>();
                bytes += Encoding.UTF8.GetByteCount(StoredRecordJson.Serialize(record));
                if (bytes > options.MaximumPayloadBytes) throw Limit("CQL graph exceeds its total payload budget.");
            }
            for (var index = 0; index < rootCount; index++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var root = roots[index];
                if (root == null || root.Id == Guid.Empty || !rootIds.Add(root.Id))
                    throw Invalid("CQL roots require nonempty, distinct identities.");
                Charge(root);
                result[index] = root;
                pending.Enqueue((root, 0, new HashSet<Guid> { root.Id }));
            }
            if (roots.Count != rootCount) throw Invalid("Native roots changed during graph capture.");

            var nodes = rootCount;
            while (pending.Count != 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
                checkNativeBudget?.Invoke();
                var (node, depth, seen) = pending.Dequeue();
                var members = Membership(node);
                if (!options.IncludesChildrenAtDepth(depth)) continue;
                if (members != null && members.Length == 0) continue;
                if (members != null && options.ChildType == HolonType.All && members.Length > options.MaximumNodes - nodes)
                    throw Limit("Persisted CQL membership exceeds the remaining graph node budget.");

                var selection = new CosmicQueryChildSelection(node.Id, members, options,
                    options.MaximumNodes - nodes, (int)(options.MaximumPayloadBytes - bytes));
                var selected = await readChildren(selection, cancellationToken).ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();
                checkNativeBudget?.Invoke();
                if (selected == null) throw Invalid("A native child reader returned no collection.");
                var count = selected.Count;
                if (count < 0) throw Invalid("A native child collection has an invalid count.");
                if (count > selection.MaximumNodes) throw Limit("Native children exceed the remaining graph node budget.");
                var children = new Dictionary<Guid, IHolon>();
                for (var index = 0; index < count; index++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var child = selected[index];
                    if (child == null || child.Id == Guid.Empty || child.ParentHolonId != node.Id ||
                        options.ChildType != HolonType.All && child.HolonType != options.ChildType ||
                        !children.TryAdd(child.Id, child) || !seen.Add(child.Id))
                        throw Invalid("A native child is null, duplicated, cyclic, foreign or of an unrequested type.");
                    Charge(child);
                }
                if (selected.Count != count) throw Invalid("Native children changed during graph capture.");
                if (members != null)
                {
                    var expected = new HashSet<Guid>(members);
                    if (children.Keys.Any(id => !expected.Contains(id)) ||
                        options.ChildType == HolonType.All && children.Count != members.Length)
                        throw Invalid("Native children differ from the persisted role membership.");
                }
                var ordered = members == null
                    ? children.Keys.OrderBy(id => id.ToString("D"), StringComparer.Ordinal)
                    : members.Where(children.ContainsKey);
                nodes += count;
                foreach (var id in ordered)
                {
                    var child = children[id];
                    node.Children.Add(child);
                    pending.Enqueue((child, depth + 1, seen));
                }
            }
            cancellationToken.ThrowIfCancellationRequested();
            checkNativeBudget?.Invoke();
            return new OASISResult<IReadOnlyList<IHolon>>(Array.AsReadOnly(result)) { IsLoaded = true };
        }
        catch (Rejected cause)
        {
            return new OASISResult<IReadOnlyList<IHolon>>
            { IsError = true, ErrorCount = 1, ErrorCode = cause.Code, Message = cause.Message, Exception = cause };
        }
    }

    private static Guid[] Membership(IHolon node)
    {
        if (node is not Holon { ChildRoleManifestJson: not null } owner) return null;
        HolonChildRoleManifest manifest;
        try { manifest = HolonChildRoleManifestJson.Deserialize(owner.ChildRoleManifestJson); }
        catch (Exception cause) { throw new Rejected("CQL_DATA_INVALID", "The stored graph role manifest is invalid.", cause); }
        if (manifest.ParentId != node.Id) throw Invalid("The stored graph role manifest belongs to another parent.");
        return manifest.Roles.SelectMany(role => role.ChildIds).ToArray();
    }

    private static Rejected Invalid(string message) => new("CQL_DATA_INVALID", message);
    private static Rejected Limit(string message) => new("CQL_QUERY_LIMIT", message);
    private sealed class Rejected : Exception
    {
        internal string Code { get; }
        internal Rejected(string code, string message, Exception cause = null) : base(message, cause) { Code = code; }
    }
}
