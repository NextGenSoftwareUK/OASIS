using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Reflection;
using Neo4j.Driver;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.UnitTests;

[TestClass]
public class CosmicQueryEnumDomainTests
{
    [TestMethod]
    public void CanonicalHolonTypeHasNativeAdmissionAndProjection()
    {
        var built=CosmicQuery<Holon>.Create().Where(holon=>holon.HolonType==HolonType.Quest);
        Assert.IsFalse(built.IsError,built.Message);
        Assert.IsFalse(Neo4jOASIS.CqlCapabilities.Admit(built.Result).IsError);
        var projection=NeoCqlProjection.Build(NextGenSoftware.OASIS.API.Core.Serialization.StoredRecordJson.Serialize(
            new Holon{Id=Guid.NewGuid(),HolonType=HolonType.Quest}));
        Assert.AreEqual((long)HolonType.Quest,projection["f_HolonType"]);
        Assert.AreEqual(3L,projection["SchemaVersion"]);
    }

    [TestMethod]
    public void DefinedHolonTypeMetadataHasExactAdmission()
    {
        var built=CosmicQuery<Holon>.Create().Where(h=>CosmicQueryMetadata.Value<HolonType>(h,"kind")==HolonType.Holon);
        Assert.IsFalse(built.IsError,built.Message);
        var admitted=Neo4jOASIS.CqlCapabilities.Admit(built.Result);
        Assert.IsFalse(admitted.IsError,admitted.Message);
    }

    [TestMethod]
    public void NullableProviderMetadataHasExactAdmission()
    {
        var built=CosmicQuery<Holon>.Create().Where(h=>CosmicQueryMetadata.Value<ProviderType?>(h,"provider")==ProviderType.Neo4jOASIS);
        Assert.IsFalse(built.IsError,built.Message);
        var admitted=Neo4jOASIS.CqlCapabilities.Admit(built.Result);
        Assert.IsFalse(admitted.IsError,admitted.Message);
    }

    [TestMethod]
    public async Task CallerProfileCannotEnableUnregisteredEnumNativeIo()
    {
        var built=CosmicQuery<Holon>.Create().Where(h=>CosmicQueryMetadata.Value<Unregistered>(h,"kind")==Unregistered.First);
        Assert.IsFalse(built.IsError,built.Message);
        var forged=new CosmicQueryCapabilities(CosmicQueryFeatures.Metadata,new[]{typeof(Unregistered)},new[]{CosmicQueryComparisonOperator.Equal});
        var plan=CosmicQueryPlanner.Plan(built.Result,ProviderType.Neo4jOASIS,new[]{new CosmicQueryTarget(ProviderType.Neo4jOASIS,forged)});
        Assert.IsFalse(plan.IsError,plan.Message);
        var driver=DispatchProxy.Create<IDriver,CosmicQueryAdmissionTests.NoIo>();
        var provider=new Neo4jOASIS("bolt://owned.invalid","owned","owned",()=>driver){IsProviderActivated=true};
        var result=await provider.ExecuteCosmicQueryAsync(plan.Result);
        Assert.IsTrue(result.IsError);Assert.AreEqual("CQL_CAPABILITY_UNSUPPORTED",result.ErrorCode);
        Assert.IsNull(result.Result);Assert.AreEqual(0,((CosmicQueryAdmissionTests.NoIo)driver).Calls);
    }

    private enum Unregistered { First=1 }
    [TestMethod]
    public void ArbitraryEnumsRemainUnregistered()
    {
        var built=CosmicQuery<Holon>.Create().Where(h=>CosmicQueryMetadata.Value<Unregistered>(h,"kind")==Unregistered.First);
        Assert.IsFalse(built.IsError,built.Message);
        Assert.IsTrue(Neo4jOASIS.CqlCapabilities.Admit(built.Result).IsError);
    }
}
