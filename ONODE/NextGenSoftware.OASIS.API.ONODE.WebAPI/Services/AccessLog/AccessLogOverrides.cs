using System;
using System.Collections.Generic;
using System.Linq;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.AccessLog;

/// <summary>Publishes per-app choices and their enabled summary as one complete in-memory snapshot.</summary>
public sealed class AccessLogOverrides
{
    private sealed record Snapshot(Dictionary<string, bool> Choices, bool AnyEnabled);
    private volatile Snapshot _snapshot = new Snapshot(new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase), false);

    public bool AnyEnabled => _snapshot.AnyEnabled;
    public bool? For(string appId)
        => appId != null && _snapshot.Choices.TryGetValue(appId, out var choice) ? choice : (bool?)null;

    public void Replace(IEnumerable<(string AppId, bool? Choice)> choices)
    {
        var map = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        foreach (var choice in choices)
            if (choice.Choice.HasValue && !string.IsNullOrEmpty(choice.AppId))
                map[choice.AppId] = choice.Choice.Value;
        // Enumerate completely and compute the summary before making either visible.
        _snapshot = new Snapshot(map, map.Values.Any(enabled => enabled));
    }
}
