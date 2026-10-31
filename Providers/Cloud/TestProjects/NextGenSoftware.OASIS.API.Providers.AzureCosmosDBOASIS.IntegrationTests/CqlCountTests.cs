using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.Infrastructure;

namespace NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.IntegrationTests;

[TestClass]
public class CqlCountTests
{
    [TestMethod]
    public void WindowCountUsesOneNativeSourceAggregate()
    {
        var query = CosmicQuery<Holon>.Create().Where(h => h.IsActive).Skip(2).Take(3).Count();
        Assert.IsFalse(query.IsError, query.Message);
        var plan = CosmicScalarPlanner.Plan(query.Result, ProviderType.AzureCosmosDBOASIS,
            new[] { new CosmicScalarTarget(ProviderType.AzureCosmosDBOASIS, AzureCosmosDBOASIS.CqlCapabilities, AzureCosmosDBOASIS.CqlScalarCapabilities) });
        Assert.IsFalse(plan.IsError, plan.Message);
        var command = CosmosCqlCount.Compile(plan.Result);
        StringAssert.Contains(command.QueryText, "COUNT(1)");
        Assert.IsFalse(command.QueryText.Contains("SELECT *"));
        Assert.IsFalse(command.QueryText.Contains("IIF(COUNT"));
        Assert.AreEqual(3L, CosmosCqlCount.ApplyWindow(10, 2, 3));
        Assert.AreEqual(0L, CosmosCqlCount.ApplyWindow(1, 2, 3));
        Assert.AreEqual(0L, CosmosCqlCount.ApplyWindow(10, 2, 0));
        Assert.AreEqual(8L, CosmosCqlCount.ApplyWindow(10, 2, null));
    }

    [DataTestMethod]
    [DataRow("{\"count\":-1}")]
    [DataRow("{\"count\":1.5}")]
    [DataRow("{\"count\":\"1\"}")]
    [DataRow("{\"count\":9007199254740992}")]
    [DataRow("{\"count\":1,\"extra\":true}")]
    public void InvalidScalarAcknowledgementRejects(string document)
        => Assert.ThrowsException<InvalidDataException>(() => CosmosCqlCount.Decode(new[] { document }));

    [TestMethod]
    public void CountRequiresExactlyOneCompleteAcknowledgement()
    {
        Assert.ThrowsException<InvalidDataException>(() => CosmosCqlCount.Decode(Array.Empty<string>()));
        Assert.ThrowsException<InvalidDataException>(() => CosmosCqlCount.Decode(new[] { "{\"count\":1}", "{\"count\":1}" }));
        Assert.AreEqual(0L, CosmosCqlCount.Decode(new[] { "{\"count\":0}" }));
    }
}
