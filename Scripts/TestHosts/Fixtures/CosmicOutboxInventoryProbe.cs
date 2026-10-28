using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace CosmicNativeAcceptance;

/// <summary>Exercises native route discovery using the already-owned primary fixture.
/// No queue claim/state update and no second copy of the fixture is created.</summary>
public static class CosmicOutboxInventoryProbe
{
    public static async Task VerifyAsync(IHolonReplicationReadinessOutbox source, IEnumerable<string> targets, Guid operation, bool sharedStore=false)
    {
        if(source is not IHolonReplicationRouteInventoryOutbox inventory)
            throw new InvalidOperationException("Native source must expose durable route inventory.");
        var expected=targets.OrderBy(value=>value,StringComparer.Ordinal).ToArray();
        var seen=new List<string>();string after=null;
        do
        {
            var page=await inventory.ReadRouteHeadsAsync(1,after);
            if(page.IsError || page.Result==null || page.Result.Heads.Count!=1)throw new InvalidOperationException("Exact native inventory page missing: "+page.Message+page.Exception);
            var head=page.Result.Heads[0];
            if(expected.Contains(head.TargetId) && (head.OperationId!=operation || head.State!=HolonDeliveryState.Pending || head.AttemptCount!=0))
                throw new InvalidOperationException("Route inventory altered or lost the original queued head.");
            var ready=await source.PeekNextAsync(head.TargetId);
            if(ready.IsError || ready.Result?.Sequence!=head.Sequence || ready.Result.AttemptCount!=head.AttemptCount)
                throw new InvalidOperationException("Inventory and durable target head disagree.");
            seen.Add(head.TargetId);after=page.Result.NextAfterTargetId;
            if(seen.Count>(sharedStore?256:expected.Length))throw new InvalidOperationException("Inventory failed to terminate at the recorded native routes.");
        }while(after!=null);
        if(!(sharedStore?seen.Where(expected.Contains):seen).SequenceEqual(expected))throw new InvalidOperationException("Missing or duplicated native route across pages.");
        var empty=await inventory.ReadRouteHeadsAsync(2,seen.Last());
        if(empty.IsError || empty.Result?.Heads.Count!=0 || empty.Result.NextAfterTargetId!=null)
            throw new InvalidOperationException("Inventory continuation beyond the final route is not empty.");
        if(!(await inventory.ReadRouteHeadsAsync(0)).IsError || !(await inventory.ReadRouteHeadsAsync(257)).IsError
            || !(await inventory.ReadRouteHeadsAsync(1," ")).IsError)
            throw new InvalidOperationException("Unbounded or malformed inventory request was admitted.");
        using var cancelled=new CancellationTokenSource();cancelled.Cancel();
        try {await inventory.ReadRouteHeadsAsync(cancellationToken:cancelled.Token);}
        catch(OperationCanceledException){return;}
        throw new InvalidOperationException("Pre-cancelled inventory did not observe cancellation.");
    }
}
