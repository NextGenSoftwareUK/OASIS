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

/// <summary>Exercises bound factories and native lease admission in the existing owned primary fixture.</summary>
public static class CosmicOutboxSourceBindingProbe
{
    public static async Task VerifyAsync(IBoundHolonReplicationOutboxSource provider,
        CosmicStorageIdentity expected, string target, Guid operation)
    {
        var captured = provider.GetBoundHolonReplicationOutbox(expected);
        if (captured.IsError || captured.Result == null)
            throw new InvalidOperationException("Bound source factory failed: " + captured.Message, captured.Exception);
        var good = captured.Result;
        var pending = await good.PeekNextAsync(target);
        if (pending.IsError || pending.Result?.OperationId != operation)
            throw new InvalidOperationException("The bound factory must retain its original native obligation.", pending.Exception);
        var claimed = await good.ClaimNextAsync(target,"native-source-binding-probe",TimeSpan.FromMinutes(5));
        if (claimed.IsError || claimed.Result?.OperationId != operation)
            throw new InvalidOperationException("The real native source must claim its original obligation.", claimed.Exception);
        var claim = claimed.Result;
        var before = await good.InspectAsync(claim.Sequence);
        var replacement = new CosmicStorageIdentity(expected.Authority,expected.EndpointFingerprint,Guid.NewGuid());
        var rejectedFactory = provider.GetBoundHolonReplicationOutbox(replacement);
        if (rejectedFactory.IsError || rejectedFactory.Result == null)
            throw new InvalidOperationException("A syntactically valid expectation is checked by native queue admission, not a preflight.");
        var bad = rejectedFactory.Result;
        void Rejected<T>(NextGenSoftware.OASIS.Common.OASISResult<T> result)
        {
            if (!result.IsError || result.ErrorCode != "COSMIC_OUTBOX_SOURCE_STORAGE_BINDING_MISMATCH"
                || !EqualityComparer<T>.Default.Equals(result.Result,default))
                throw new InvalidOperationException("Native source mismatch must refuse access before queue state changes: " + result.Message,result.Exception);
        }
        Rejected(await bad.InspectAsync(claim.Sequence));
        Rejected(await bad.PeekNextAsync(target));
        Rejected(await bad.ReadRouteHeadsAsync());
        Rejected(await bad.ClaimNextAsync(target,"wrong-source",TimeSpan.FromMinutes(1)));
        Rejected(await bad.RenewAsync(claim,TimeSpan.FromMinutes(1)));
        Rejected(await bad.FinishAsync(claim,HolonDeliveryOutcome.Completed));
        Rejected(await bad.FinishWithRecoveryAsync(claim,HolonDeliveryOutcome.ReconciliationRequired,
            new HolonDeliveryRecoveryEvidence(true,new[]{"native-source-probe"}),"UNKNOWN"));
        var after = await good.InspectAsync(claim.Sequence);
        if (before.IsError || after.IsError || after.Result.State != before.Result.State
            || after.Result.AttemptCount != before.Result.AttemptCount || after.Result.OperationId != operation)
            throw new InvalidOperationException("Refused source expectations must retain the original native delivery state.");
        var renewed = await good.RenewAsync(claim,TimeSpan.FromMinutes(1));
        var finished = await good.FinishAsync(claim,HolonDeliveryOutcome.Completed);
        if (renewed.IsError || !renewed.Result || finished.IsError || !finished.Result)
            throw new InvalidOperationException("Only the real native owner may renew and complete the original lease.");
    }
}
