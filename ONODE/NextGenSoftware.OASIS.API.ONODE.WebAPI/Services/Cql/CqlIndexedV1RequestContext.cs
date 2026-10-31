using System;
using System.Threading;
using Microsoft.AspNetCore.Http;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

/// <summary>Server-only binding of one frozen indexed preparation to its admitted request handle.</summary>
public static class CqlIndexedV1RequestContext
{
    private static readonly object Key=new();
    private sealed class Bound(CqlIndexedV1Preparation preparation,UsageExecutionHandle execution,string rawFingerprint)
    {
        internal readonly CqlIndexedV1Preparation Preparation=preparation;
        internal readonly UsageExecutionHandle Execution=execution;
        internal readonly string ReservationFingerprint=rawFingerprint;
        internal int Consumed;
    }

    public static OASISResult<bool> Bind(HttpContext http,CqlIndexedV1Preparation preparation,UsageExecutionHandle execution)
        =>BindReservation(http,preparation,execution,preparation?.RequestFingerprint);

    internal static OASISResult<bool> BindReservation(HttpContext http,CqlIndexedV1Preparation preparation,
        UsageExecutionHandle execution,string reservationFingerprint)
    {
        if(http==null||preparation==null||execution==null||http.Items.ContainsKey(Key)
            ||!http.Items.TryGetValue(Web4UsageMeasurement.ExecutionKey,out var current)||!ReferenceEquals(current,execution)
            ||!execution.CoversMeasuredReadReservation(preparation.Actor,reservationFingerprint,
                preparation.MaximumServiceCostUsd,preparation.Binding.Tariffs.MaximumPhysicalReads))
            return new(){IsError=true,ErrorCount=1,ErrorCode="CQL_INDEX_RESERVATION_REQUIRED",
                Message="Bind the exact frozen indexed intent to a sufficient measured-read reservation once."};
        http.Items[Key]=new Bound(preparation,execution,reservationFingerprint);
        return new(true);
    }

    internal static bool TryConsume(HttpContext http,PreparedStorageExecution admission,
        out CqlIndexedV1Preparation preparation)
    {
        preparation=null;
        if(!http.Items.TryGetValue(Key,out var value)||value is not Bound bound
            ||!admission.IsBoundTo(bound.Execution)||!admission.HasIndexedReadPolicy(bound.Preparation.Binding.Tariffs)
            ||!bound.Execution.CoversMeasuredReadReservation(bound.Preparation.Actor,bound.ReservationFingerprint,
                bound.Preparation.MaximumServiceCostUsd,bound.Preparation.Binding.Tariffs.MaximumPhysicalReads)
            ||Interlocked.CompareExchange(ref bound.Consumed,1,0)!=0)return false;
        preparation=bound.Preparation;
        return true;
    }
}
