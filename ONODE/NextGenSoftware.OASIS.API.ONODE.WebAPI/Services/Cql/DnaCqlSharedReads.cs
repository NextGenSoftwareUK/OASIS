using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.DNA;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

/// <summary>
/// Feeds CQL the settings in the DNA (<c>OASIS.Cql</c>), the caller's groups, and what the chosen provider can do.
/// Group lists are reused for <c>GroupCacheSeconds</c> so a query does not pay the lookup every time; a membership change
/// takes effect within that time.
/// </summary>
public sealed class DnaCqlSharedReads : ICqlSharedReads
{
    private const int MaximumCachedActors = 10000;
    private readonly ConcurrentDictionary<Guid, (DateTime At, Guid[] Groups)> _groups = new();

    private static CqlSettings Settings => OASISBootLoader.OASISBootLoader.OASISDNA?.OASIS?.Cql ?? new CqlSettings();

    public bool SharedReads => Settings.SharedReads;
    public bool InheritedReads => Settings.InheritedReads;

    public IReadOnlyCollection<Guid> GroupsOf(Guid actor)
    {
        if (actor == Guid.Empty) return Array.Empty<Guid>();
        var lifetime = TimeSpan.FromSeconds(Math.Max(0, Settings.GroupCacheSeconds));
        if (_groups.TryGetValue(actor, out var hit) && DateTime.UtcNow - hit.At < lifetime) return hit.Groups;
        try
        {
            var providers = new CqlV1Providers();
            var groups = providers.CreateManager(providers.Resolve(null)).GroupIdsOf(actor).ToArray();
            if (_groups.Count >= MaximumCachedActors) _groups.Clear();
            _groups[actor] = (DateTime.UtcNow, groups);
            return groups;
        }
        catch
        {
            // A failed lookup shares less, never more. It is not cached, so the next query tries again.
            return Array.Empty<Guid>();
        }
    }

    public bool ProviderWalksAncestors(ProviderType? provider)
    {
        try
        {
            return new CqlV1Providers().Resolve(provider) is ICosmicQueryStorageProvider query
                && (query.QueryCapabilities.Features & CosmicQueryFeatures.AncestorExists) != 0;
        }
        catch { return false; }
    }
}
