using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo4j.Driver;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using Provider=NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.Neo4jOASIS;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.UnitTests;

[TestClass]
public class Neo4jPreparationAdmissionTests
{
    [DataTestMethod]
    [DataRow(false,"missing")] [DataRow(true,"missing")]
    [DataRow(false,"authority")] [DataRow(true,"authority")]
    [DataRow(false,"inactive")] [DataRow(true,"inactive")]
    [DataRow(false,"cancelled")] [DataRow(true,"cancelled")]
    public async Task InvalidPreparationDoesNotActivateOrSubmit(bool synchronous,string scenario)
    {
        var driver=DispatchProxy.Create<IDriver,Neo4jLifecycleTests.DriverProbe>();
        var probe=(Neo4jLifecycleTests.DriverProbe)driver;
        var provider=new Provider("bolt://owned.invalid","owned","owned",()=>driver);
        Assert.IsInstanceOfType(provider,typeof(IHolonWritePreparationStorageProvider),
            "Neo4j must own the shared durable preparation capability.");
        var storage=(IHolonWritePreparationStorageProvider)(object)provider;
        var candidate=scenario=="missing" ? null : new HolonWritePreparation(Guid.NewGuid(),Guid.NewGuid(),
            scenario=="authority" ? ProviderType.MongoDBOASIS:ProviderType.Neo4jOASIS,
            new[]{new ProtectedHolonPreparation(Guid.NewGuid(),"oasis-preparation1:owned-test-envelope")});
        using var cancellation=new CancellationTokenSource();
        if(scenario=="cancelled")cancellation.Cancel();
        // The sync contract has no cancellation argument; its inactive admission still must avoid I/O.
        var result=synchronous ? storage.ClaimHolonWritePreparation(candidate!)
            :await storage.ClaimHolonWritePreparationAsync(candidate!,cancellation.Token);
        Assert.IsTrue(result.IsError);
        Assert.AreEqual(scenario=="missing" ? "COSMIC_PREPARATION_INVALID"
            :scenario=="authority" ? "COSMIC_AUTHORITY_MISMATCH"
            :scenario=="cancelled"&&!synchronous ? "COSMIC_PREPARATION_CANCELLED"
            :"COSMIC_PROVIDER_NOT_READY",result.ErrorCode);
        Assert.AreEqual(0,probe.Verifications);
        Assert.AreEqual(0,probe.SchemaRuns);
        Assert.IsFalse(provider.IsProviderActivated);
        Assert.IsFalse((await provider.DeActivateProviderAsync()).IsError);
    }
}
