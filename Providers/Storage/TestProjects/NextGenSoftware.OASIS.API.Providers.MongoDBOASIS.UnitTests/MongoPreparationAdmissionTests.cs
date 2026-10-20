using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.UnitTests;

[TestClass]
public class MongoPreparationAdmissionTests
{
    [DataTestMethod]
    [DataRow(false, "missing")] [DataRow(true, "missing")]
    [DataRow(false, "authority")] [DataRow(true, "authority")]
    [DataRow(false, "unactivated")] [DataRow(true, "unactivated")]
    public async Task PreparationAdmissionFailsBeforeAbsentRepositoryAccess(bool synchronous, string fault)
    {
        var provider = new MongoDBOASIS("mongodb://127.0.0.1:1/?serverSelectionTimeoutMS=1", "unactivated-preparation-admission-only");
        var candidate = fault == "missing" ? null : new HolonWritePreparation(Guid.NewGuid(), Guid.NewGuid(),
            fault == "authority" ? ProviderType.SQLLiteDBOASIS : ProviderType.MongoDBOASIS,
            new[] { new ProtectedHolonPreparation(Guid.NewGuid(), "oasis-preparation1:opaque") });
        var result = synchronous ? provider.ClaimHolonWritePreparation(candidate!) : await provider.ClaimHolonWritePreparationAsync(candidate!);
        Assert.IsTrue(result.IsError); Assert.IsNull(result.Result); Assert.IsFalse(provider.IsProviderActivated);
        Assert.AreEqual(fault == "missing" ? "COSMIC_PREPARATION_INVALID" : fault == "authority" ? "COSMIC_AUTHORITY_MISMATCH" : "COSMIC_PREPARATION_PROVIDER_NOT_READY", result.ErrorCode);
        Assert.IsFalse(result.Exception is NullReferenceException);
    }
    [TestMethod]
    public async Task CancelledPreparationReturnsAnExplicitResultWithoutActivatingStorage()
    {
        var provider = new MongoDBOASIS("mongodb://127.0.0.1:1/?serverSelectionTimeoutMS=1", "unactivated-preparation-admission-only");
        using var cancellation = new CancellationTokenSource(); cancellation.Cancel();
        var candidate = new HolonWritePreparation(Guid.NewGuid(), Guid.NewGuid(), ProviderType.MongoDBOASIS,
            new[] { new ProtectedHolonPreparation(Guid.NewGuid(), "oasis-preparation1:opaque") });
        var result = await provider.ClaimHolonWritePreparationAsync(candidate, cancellation.Token);
        Assert.IsTrue(result.IsError); Assert.IsNull(result.Result); Assert.IsFalse(provider.IsProviderActivated);
        Assert.AreEqual("COSMIC_PREPARATION_CANCELLED", result.ErrorCode); Assert.IsInstanceOfType<OperationCanceledException>(result.Exception);
    }
}
