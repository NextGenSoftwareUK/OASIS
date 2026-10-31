using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public sealed class NativeResourceCaptureAccountingTests
{
    [Theory]
    [InlineData("open")][InlineData("empty")][InlineData("gap")][InlineData("orphan")]
    [InlineData("foreign-provider")][InlineData("missing-receipt")][InlineData("incomplete")]
    [InlineData("omitted-row")][InlineData("changed-native-id")][InlineData("changed-quantity")]
    [InlineData("changed-time")][InlineData("valid")][InlineData("ordinary")]
    public void SettlementRequiresClosedCompleteAndUnalteredOriginalResourceCapture(string mode)
    {
        var row=new NativeResourceUsageObservation("AWSOASIS","original","sdk-original","region/table/original","read-capacity-units","Total",0.5m,DateTime.UtcNow);
        if(mode=="incomplete")row=row with{Quantity=null,NativeRequestId=null};
        var attempt=new UsageProviderAttempt{Provider="AWSOASIS",ClientRequestId="route",ProviderRequestId="original",ResourceCaptureExpected=true,ResourceCaptureCount=1};
        var batch=new UsageResourceObservationBatch{Provider="AWSOASIS",ClientRequestId="route",ProviderRequestId="original",Sequence=1,NativeOperation="GetItem",Observations=new(){row}};
        var operation=new UsageOutboxOperation{ProviderAttempts=new(){attempt},ResourceObservations=new(){batch}};
        var receipt=new UsageProviderReceipt{Provider="AWSOASIS",ClientRequestId="route",ProviderRequestId="original",NativeResourceUsage=new(){row}};
        if(mode=="open")attempt.ResourceCaptureCount=null;
        if(mode=="empty"){attempt.ResourceCaptureCount=0;operation.ResourceObservations.Clear();}
        if(mode=="gap")batch.Sequence=2;
        if(mode=="orphan")operation.ProviderAttempts.Clear();
        if(mode=="foreign-provider")batch.Provider="foreign";
        if(mode=="omitted-row")batch.Observations.Add(row with{ResourceScope="region/table/second"});
        if(mode=="changed-native-id")receipt.NativeResourceUsage[0]=row with{NativeRequestId="replacement"};
        if(mode=="changed-quantity")receipt.NativeResourceUsage[0]=row with{Quantity=0};
        if(mode=="changed-time")receipt.NativeResourceUsage[0]=row with{ObservedAtUtc=row.ObservedAtUtc.AddTicks(1)};
        if(mode=="ordinary"){attempt.ResourceCaptureExpected=false;attempt.ResourceCaptureCount=null;operation.ResourceObservations.Clear();receipt.NativeResourceUsage=null;}
        void Validate()=>UsageReceiptAccounting.ValidateCapturedResources(operation,mode=="missing-receipt"?Array.Empty<UsageProviderReceipt>():new[]{receipt});
        if(mode is "valid" or "ordinary")Validate();else Assert.Throws<UsageProtocolException>(Validate);
    }
}
