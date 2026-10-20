using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

public enum HolonReplicationRouteAvailability { Starting, Healthy, Degraded, Stopped, Faulted }

/// <summary>Operational status only: no business payload, claim token, endpoint credentials or exception text.</summary>
public sealed class HolonReplicationRouteStatus
{
    public string TargetId {get;}
    public HolonReplicationRouteAvailability Availability {get;}
    public string Code {get;}
    public DateTime ObservedAtUtc {get;}
    internal HolonReplicationRouteStatus(string target,HolonReplicationRouteAvailability availability,string code)
    {TargetId=target;Availability=availability;Code=code;ObservedAtUtc=DateTime.UtcNow;}
}

public sealed class HolonReplicationHostState
{
    private readonly object _gate=new();
    private readonly Dictionary<string,HolonReplicationRouteStatus> _routes=new(StringComparer.Ordinal);
    private bool _running,_failed;
    internal void Initialize(IEnumerable<string> targets)
    {
        lock(_gate)
        {
            if(_routes.Count!=0)throw new InvalidOperationException("A readiness state has one hosted worker owner.");
            foreach(var target in targets)_routes.Add(target,new(target,HolonReplicationRouteAvailability.Starting,"COSMIC_OUTBOX_STARTING"));
        }
    }
    internal void Running(){lock(_gate)_running=true;}
    internal void Update(string target,HolonReplicationRouteAvailability availability,string code=null)
    {
        lock(_gate)
        {
            if(!_routes.ContainsKey(target))throw new InvalidOperationException("Unknown readiness route.");
            if(!_running)return;
            _routes[target]=new(target,availability,code==null || code.Length<=128 ? code:code.Substring(0,127)+"…");
        }
    }
    internal void Terminal(bool failed)
    {
        lock(_gate)
        {
            _running=false;_failed|=failed;
            foreach(var target in _routes.Keys.ToArray())_routes[target]=new(target,_failed ? HolonReplicationRouteAvailability.Faulted:HolonReplicationRouteAvailability.Stopped,
                _failed ? "COSMIC_OUTBOX_HOST_FAULTED":"COSMIC_OUTBOX_STOPPED");
        }
    }
    internal (bool Ready,IReadOnlyList<HolonReplicationRouteStatus> Routes) Capture()
    {
        lock(_gate)return (_running && !_failed && _routes.Count!=0 && _routes.Values.All(route=>route.Availability==HolonReplicationRouteAvailability.Healthy),
            Array.AsReadOnly(_routes.Values.OrderBy(route=>route.TargetId,StringComparer.Ordinal).ToArray()));
    }
    public IReadOnlyList<HolonReplicationRouteStatus> Snapshot()=>Capture().Routes;
    public bool IsReady
    {get{lock(_gate)return _running && !_failed && _routes.Count!=0 && _routes.Values.All(route=>route.Availability==HolonReplicationRouteAvailability.Healthy);}}
}

public sealed class HolonReplicationHealthCheck : IHealthCheck
{
    private readonly HolonReplicationHostState _state;
    public HolonReplicationHealthCheck(HolonReplicationHostState state)=>_state=state ?? throw new ArgumentNullException(nameof(state));
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,CancellationToken cancellationToken=default)
    {
        cancellationToken.ThrowIfCancellationRequested();var snapshot=_state.Capture();var routes=snapshot.Routes;
        var data=routes.ToDictionary(route=>route.TargetId,route=>(object)new{state=route.Availability.ToString(),code=route.Code},StringComparer.Ordinal);
        return Task.FromResult(snapshot.Ready ? HealthCheckResult.Healthy("Configured COSMIC workers and native queue heads are available.",data)
            :HealthCheckResult.Unhealthy("COSMIC replication is starting, stopped, faulted or has an unavailable/blocked route.",data:data));
    }
}
