using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.UnitTests;

[TestClass]
public class MongoConditionalAdmissionTests
{
    [DataTestMethod]
    [DataRow("source")] [DataRow("target")] [DataRow("missing")] [DataRow("cancelled")]
    public async Task InvalidReplicationEnrollmentRejectsBeforeRepositoryAccess(string invalid)
    {
        var provider=new MongoDBOASIS("mongodb://127.0.0.1:1/?serverSelectionTimeoutMS=1","unactivated-admission-only");
        var root=new Holon{Id=Guid.NewGuid()};var enrollment=new HolonReplicationEnrollment("source",new[]{"a"});
        var request=new ConditionalHolonWriteRequest(new ConditionalHolonWrite(Guid.NewGuid(),Guid.NewGuid(),ProviderType.MongoDBOASIS,
            new[]{new HolonWriteCondition(root.Id,null)},enrollment),ConditionalHolonOperation.Save,new[]{root});
        var actual=invalid=="missing" ? null:new HolonReplicationEnrollment(invalid=="source" ? "other":"source",new[]{invalid=="target" ? "b":"a"});
        using var cancellation=new CancellationTokenSource();if(invalid=="cancelled")cancellation.Cancel();
        var result=await provider.ExecuteReplicatingHolonWriteAsync(request,actual,cancellation.Token);
        Assert.IsTrue(result.IsError);Assert.IsFalse(result.IsSaved);Assert.IsFalse(provider.IsProviderActivated);
        Assert.IsFalse(result.Exception is NullReferenceException);
        Assert.AreEqual(invalid=="missing" ? "COSMIC_REPLICATION_ENROLLMENT_REQUIRED":invalid=="cancelled" ? "COSMIC_CONDITIONAL_WRITE_CANCELLED":"COSMIC_CONDITIONAL_WRITE_INVALID",result.ErrorCode);
    }

    [DataTestMethod]
    [DataRow("authority")] [DataRow("missing-child-condition")] [DataRow("cancelled")]
    public async Task InvalidGuaranteeFailsBeforeAnUnactivatedRepositoryCanBeUsed(string invalid)
    {
        var provider = new MongoDBOASIS("mongodb://127.0.0.1:1/?serverSelectionTimeoutMS=1", "unactivated-admission-only");
        var root = new Holon { Id = Guid.NewGuid() };
        if (invalid == "missing-child-condition") root.Children.Add(new Holon { Id = Guid.NewGuid() });
        var request = new ConditionalHolonWriteRequest(new ConditionalHolonWrite(Guid.NewGuid(), Guid.NewGuid(),
            invalid == "authority" ? ProviderType.SQLLiteDBOASIS : ProviderType.MongoDBOASIS,
            new[] { new HolonWriteCondition(root.Id, null) }), ConditionalHolonOperation.Save, new[] { root });
        using var cancellation = new CancellationTokenSource();
        if (invalid == "cancelled") cancellation.Cancel();
        var result = await provider.ExecuteConditionalHolonWriteAsync(request, cancellation.Token);
        Assert.IsTrue(result.IsError); Assert.IsFalse(result.IsSaved);
        Assert.AreEqual(invalid == "cancelled" ? "COSMIC_CONDITIONAL_WRITE_CANCELLED" : "COSMIC_CONDITIONAL_WRITE_INVALID", result.ErrorCode);
        Assert.IsNotNull(result.Exception);
        Assert.IsFalse(result.Exception is NullReferenceException, "Admission must precede access to the absent repository.");
        Assert.IsFalse(provider.IsProviderActivated);
    }
}
