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


/// <summary>Physical source admission and recovery use the same immutable expectation.
/// Uses existing recorded primary operations;does not create a second dataset.</summary>
public static class CosmicPrimarySourceBindingProbe
{
    private static CosmicStorageIdentity Wrong(CosmicStorageIdentity value) => new(value.Authority,
        value.EndpointFingerprint==new string('A',64)?new string('B',64):new string('A',64),value.StoreId);

    public static async Task RejectWrongAsync(HolonReplicationEnrollment enrollment,
        Func<HolonReplicationEnrollment,Task<NextGenSoftware.OASIS.Common.OASISResult<IEnumerable<IHolon>>>> execute)
    {
        var identity=enrollment.SourceStorageIdentity;
        if(identity==null)throw new InvalidOperationException("Bound source proof requires native identity capture.");
        foreach(var wrong in new[]{Wrong(identity),new CosmicStorageIdentity(identity.Authority,identity.EndpointFingerprint,Guid.NewGuid())})
        {
            var denied=await execute(new HolonReplicationEnrollment(enrollment.SourceId,enrollment.TargetIds,wrong));
            if(!denied.IsError || denied.IsSaved || denied.ErrorCode!="COSMIC_SOURCE_STORAGE_BINDING_MISMATCH")
                throw new InvalidOperationException("A wrong native source was not refused before write/replay: "+denied.ErrorCode+denied.Message+denied.Exception);
        }
    }

    private static ConditionalHolonWriteRequest Original(ConditionalHolonWriteRequest original,HolonReplicationEnrollment enrollment)
        =>new(new ConditionalHolonWrite(original.Write.OperationId,original.Write.ActorId,original.Write.Authority,original.Write.Conditions,enrollment),
            original.Operation,original.Roots,original.SaveChildren,original.Recursive,original.MaxChildDepth,original.SubmittedRelationships);

    public static async Task VerifyCommittedAsync(IConditionalHolonWriteRecoveryStorageProvider provider,
        ConditionalHolonWriteRequest original,HolonReplicationEnrollment enrollment,
        Func<HolonReplicationEnrollment,Task<NextGenSoftware.OASIS.Common.OASISResult<IEnumerable<IHolon>>>> execute)
    {
        var verified=await provider.ReadConditionalHolonWriteOutcomeAsync(Original(original,enrollment));
        if(verified.IsError || verified.Result?.ReceiptState!=ConditionalHolonReceiptState.Committed)
            throw new InvalidOperationException("Bound original outcome was not verified: "+verified.Message+verified.Exception);
        var wrong=new HolonReplicationEnrollment(enrollment.SourceId,enrollment.TargetIds,Wrong(enrollment.SourceStorageIdentity));
        var rejected=await provider.ReadConditionalHolonWriteOutcomeAsync(Original(original,wrong));
        if(!rejected.IsError || rejected.IsLoaded || rejected.Result!=null)
            throw new InvalidOperationException("Recovery accepted a receipt under a different physical source expectation.");
        await RejectWrongAsync(enrollment,execute);
        var downgrade=await execute(new HolonReplicationEnrollment(enrollment.SourceId,enrollment.TargetIds));
        if(!downgrade.IsError || downgrade.ErrorCode!="COSMIC_OPERATION_ID_REUSED")
            throw new InvalidOperationException("Receipt silently downgraded bound enrollment to an unbound operation.");
    }
}
