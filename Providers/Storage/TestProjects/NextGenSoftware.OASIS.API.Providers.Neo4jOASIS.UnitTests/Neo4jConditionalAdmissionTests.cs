using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo4j.Driver;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using Provider=NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.Neo4jOASIS;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.UnitTests;

[TestClass]
public class Neo4jConditionalAdmissionTests
{
    [DataTestMethod]
    [DataRow("authority")] [DataRow("missing-child-condition")] [DataRow("cancelled")]
    public async Task InvalidConditionalIntentNeverActivatesTheDriver(string invalid)
    {
        var driver=DispatchProxy.Create<IDriver,Neo4jLifecycleTests.DriverProbe>();
        var probe=(Neo4jLifecycleTests.DriverProbe)driver;
        var provider=new Provider("bolt://owned.invalid","owned","owned",()=>driver);
        var root=new Holon {Id=Guid.NewGuid()};
        if(invalid=="missing-child-condition")root.Children.Add(new Holon {Id=Guid.NewGuid()});
        var request=new ConditionalHolonWriteRequest(new ConditionalHolonWrite(Guid.NewGuid(),Guid.NewGuid(),
            invalid=="authority" ? ProviderType.MongoDBOASIS:ProviderType.Neo4jOASIS,
            new[]{new HolonWriteCondition(root.Id,null)}),ConditionalHolonOperation.Save,new[]{root});
        using var cancellation=new CancellationTokenSource();if(invalid=="cancelled")cancellation.Cancel();
        var result=await provider.ExecuteConditionalHolonWriteAsync(request,cancellation.Token);
        Assert.IsTrue(result.IsError);Assert.IsFalse(result.IsSaved);Assert.IsNotNull(result.Exception);
        Assert.AreEqual(invalid=="cancelled" ? "COSMIC_CONDITIONAL_WRITE_CANCELLED":"COSMIC_CONDITIONAL_WRITE_INVALID",result.ErrorCode);
        Assert.AreEqual(0,probe.Verifications);Assert.AreEqual(0,probe.SchemaRuns);Assert.IsFalse(provider.IsProviderActivated);
        Assert.IsFalse((await provider.DeActivateProviderAsync()).IsError);
    }
}
