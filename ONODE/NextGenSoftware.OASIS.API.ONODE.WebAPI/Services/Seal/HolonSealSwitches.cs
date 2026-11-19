using System;
using System.Collections.Generic;
using System.Linq;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Seal;

/// <summary>Publishes complete seal switches atomically; a refresh cannot overwrite a newer local setting.</summary>
public sealed class HolonSealSwitches
{
    private sealed record Snapshot(HashSet<Guid> Owners, HashSet<string> Apps, long Revision);
    private readonly object _gate = new object();
    private volatile Snapshot _snapshot = new Snapshot(new HashSet<Guid>(), Apps(Array.Empty<string>()), 0);

    public long Revision => _snapshot.Revision;
    public bool OwnerEnabled(Guid owner) => _snapshot.Owners.Contains(owner);
    public bool AppEnabled(string app) => !string.IsNullOrEmpty(app) && _snapshot.Apps.Contains(app);

    public bool IsEnabled(Guid owner, string app)
    {
        var snapshot = _snapshot;
        return (owner != Guid.Empty && snapshot.Owners.Contains(owner)) ||
               (!string.IsNullOrEmpty(app) && snapshot.Apps.Contains(app));
    }

    public void SetOwner(Guid owner, bool enabled)
    {
        lock (_gate)
        {
            var current = _snapshot;
            var owners = new HashSet<Guid>(current.Owners);
            if (enabled) owners.Add(owner); else owners.Remove(owner);
            _snapshot = new Snapshot(owners, current.Apps, checked(current.Revision + 1));
        }
    }

    public bool TryReplace(long expectedRevision, IEnumerable<Guid> owners, IEnumerable<string> apps)
    {
        // Fully enumerate both sources before publishing either. Enumeration failures leave the policy intact.
        var nextOwners = new HashSet<Guid>(owners);
        var nextApps = Apps(apps);
        lock (_gate)
        {
            var current = _snapshot;
            if (current.Revision != expectedRevision) return false;
            _snapshot = new Snapshot(nextOwners, nextApps, checked(current.Revision + 1));
            return true;
        }
    }

    public void ReplaceOwners(IEnumerable<Guid> owners)
    {
        var next = new HashSet<Guid>(owners);
        lock (_gate)
        {
            var current = _snapshot;
            _snapshot = new Snapshot(next, current.Apps, checked(current.Revision + 1));
        }
    }

    public void ReplaceApps(IEnumerable<string> apps)
    {
        var next = Apps(apps);
        lock (_gate)
        {
            var current = _snapshot;
            _snapshot = new Snapshot(current.Owners, next, checked(current.Revision + 1));
        }
    }

    private static HashSet<string> Apps(IEnumerable<string> apps)
        => new HashSet<string>(apps.Where(app => !string.IsNullOrEmpty(app)), StringComparer.OrdinalIgnoreCase);
}
