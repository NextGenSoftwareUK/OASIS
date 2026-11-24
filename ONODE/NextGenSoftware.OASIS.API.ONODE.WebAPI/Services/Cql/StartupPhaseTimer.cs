using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

/// <summary>Records how long each HoloOASIS teaching-conductor startup phase took, for one startup log line.</summary>
internal sealed class StartupPhaseTimer
{
    private readonly Stopwatch _total = Stopwatch.StartNew();
    private readonly List<(string Phase, double Seconds)> _phases = new();
    private double _last;

    public void Mark(string phase)
    {
        var now = _total.Elapsed.TotalSeconds;
        _phases.Add((phase, now - _last));
        _last = now;
    }

    public override string ToString() =>
        string.Join(", ", _phases.Select(p => $"{p.Phase} +{p.Seconds:F1}s")) + $" (total {_total.Elapsed.TotalSeconds:F1}s)";
}
