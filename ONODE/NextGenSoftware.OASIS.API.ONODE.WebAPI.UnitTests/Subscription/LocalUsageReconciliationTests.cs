using System.Text.Json;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.SubscriptionReconciliation;

public class LocalUsageReconciliationTests
{
    [Fact]
    public void Web4KnownRejectionRetainsExactInternalEvidenceShape()
    {
        var id=Guid.NewGuid().ToString("D");
        var receipt=JsonSerializer.SerializeToElement(new {Provider="WEB4",ProviderRequestId=id,
            CostSource="not-executed",PricingCatalogueVersion="no-provider-execution-v1",
            PromptTokens=0,CompletionTokens=0,Units=0,ActualCostUsd=0});
        Assert.True(UsageReconciliationMath.IsInternalReceipt(receipt,"WEB4",id,"failed"));
        Assert.False(UsageReconciliationMath.IsInternalReceipt(receipt,"WEB4",id,"succeeded"));
        Assert.False(UsageReconciliationMath.IsInternalReceipt(receipt,"WEB4",Guid.NewGuid().ToString("D"),"failed"));
        Assert.False(UsageReconciliationMath.IsInternalReceipt(receipt,"untrusted-service",id,"failed"));
    }
    [Fact]
    public void Web4ExternalEvidenceUsesExistingRequiredIdentityValidation()
    {
        var receipt=new UsageExternalReceipt { Source="provider",UserId=Guid.NewGuid().ToString("D"),Month="2026-10",
            OperationId=Guid.NewGuid().ToString("D"),ConsumingService="WEB4",Provider="SuiOASIS",
            ProviderRequestId="original-digest",ExternalId="original-digest",EvidenceSha256=new string('a',64),Actor="test-auditor" };
        receipt.Validate(); Assert.Equal("provider:WEB4:SuiOASIS:original-digest",receipt.Id);
        receipt.ConsumingService="untrusted-service";
        Assert.Throws<ArgumentException>(()=>receipt.Validate());
    }
}
