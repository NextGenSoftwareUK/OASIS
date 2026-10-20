using System.Linq.Expressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.UnitTests;

[TestClass]
public class MongoCqlAdmissionTests
{
    private static CosmicQueryPlan<Holon> Plan(CosmicQuery<Holon> query,ProviderType provider=ProviderType.MongoDBOASIS,CosmicQueryCapabilities? capabilities=null)
    {
        var result=CosmicQueryPlanner.Plan(query,provider,new[]{new CosmicQueryTarget(provider,capabilities??MongoDBOASIS.CqlCapabilities)});
        Assert.IsFalse(result.IsError,result.Message);return result.Result;
    }
    [DataTestMethod]
    [DataRow("missing",100)] [DataRow("wrong-provider",100)] [DataRow("limit",0)]
    [DataRow("limit",10001)] [DataRow("cancelled",100)] [DataRow("inactive",100)]
    public async Task InvalidExecutionNeverOpensAnUnactivatedRepository(string scenario,int maximum)
    {
        var provider=new MongoDBOASIS("mongodb://127.0.0.1:1/?serverSelectionTimeoutMS=1","cql-admission-only");
        Assert.IsInstanceOfType(provider,typeof(ICosmicQueryStorageProvider));
        var plan=scenario=="missing"?null:Plan(CosmicQuery<Holon>.Create(),scenario=="wrong-provider"?ProviderType.SQLLiteDBOASIS:ProviderType.MongoDBOASIS);
        using var cancellation=new CancellationTokenSource();if(scenario=="cancelled")cancellation.Cancel();
        var result=await provider.ExecuteCosmicQueryAsync(plan!,maximum,cancellation.Token);
        Assert.IsTrue(result.IsError);Assert.IsNull(result.Result);Assert.IsFalse(provider.IsProviderActivated);
        Assert.AreEqual(scenario is "missing" or "wrong-provider"?"CQL_PLAN_INVALID":scenario=="limit"?"CQL_QUERY_LIMIT"
            :scenario=="cancelled"?"CQL_QUERY_CANCELLED":"CQL_PROVIDER_NOT_READY",result.ErrorCode);
    }
    [TestMethod]
    public async Task CallerCapabilityProfileCannotEnableUnsupportedDecimalMetadataExecution()
    {
        var provider=new MongoDBOASIS("mongodb://127.0.0.1:1/?serverSelectionTimeoutMS=1","cql-admission-only");
        var built=CosmicQuery<Holon>.Create().Where(h=>CosmicQueryMetadata.Value<decimal>(h,"price")>1m);
        Assert.IsFalse(built.IsError,built.Message);
        var broad=new CosmicQueryCapabilities(CosmicQueryFeatures.Metadata,Array.Empty<CosmicQueryFieldCapability>(),
            new[]{new CosmicQueryMetadataCapability(typeof(decimal),Enum.GetValues<CosmicQueryComparisonOperator>())});
        var result=await provider.ExecuteCosmicQueryAsync(Plan(built.Result,capabilities:broad));
        Assert.AreEqual("CQL_CAPABILITY_UNSUPPORTED",result.ErrorCode);Assert.IsFalse(provider.IsProviderActivated);
    }
    [TestMethod]
    public async Task InvalidNativeFieldNameRejectsBeforeRepositoryAccess()
    {
        var query=CosmicQuery<Holon>.Create().Where(h=>CosmicQueryMetadata.Value<string>(h,"bad\0key")=="value");
        Assert.IsFalse(query.IsError,query.Message);
        var provider=new MongoDBOASIS("mongodb://127.0.0.1:1/?serverSelectionTimeoutMS=1","cql-admission-only");
        Assert.AreEqual("CQL_CAPABILITY_UNSUPPORTED",(await provider.ExecuteCosmicQueryAsync(Plan(query.Result))).ErrorCode);
    }
    [TestMethod]
    public async Task CombinedLiteralPayloadRejectsBeforeRepositoryAccess()
    {
        var values=Enumerable.Range(0,128).Select(index=>new string('x',4090)+index.ToString()).ToArray();
        var first=CosmicQuery<Holon>.Create().Where(h=>values.Contains(h.Name));Assert.IsFalse(first.IsError,first.Message);
        var second=first.Result.Where(h=>values.Contains(h.Description));Assert.IsFalse(second.IsError,second.Message);
        var third=second.Result.Where(h=>values.Contains(h.Name));Assert.IsFalse(third.IsError,third.Message);
        var provider=new MongoDBOASIS("mongodb://127.0.0.1:1/?serverSelectionTimeoutMS=1","cql-admission-only");
        Assert.AreEqual("CQL_QUERY_LIMIT",(await provider.ExecuteCosmicQueryAsync(Plan(third.Result))).ErrorCode);
    }
    [DataTestMethod]
    [DataRow("canonical")] [DataRow("metadata")] [DataRow("membership")] [DataRow("null")]
    [DataRow("guid")] [DataRow("nullable-guid")] [DataRow("guid-membership")]
    public async Task SupportedExpressionsAreCompletelyTranslatedBeforeRepositoryAccess(string form)
    {
        var ids=new[]{Guid.NewGuid(),Guid.NewGuid()};
        var target=ids[0];
        Expression<Func<Holon,bool>> predicate=form switch{
            "canonical"=>h=>h.Version>=2&&h.Name.StartsWith("$where",StringComparison.Ordinal)&&!h.IsPublic,
            "metadata"=>h=>CosmicQueryMetadata.Value<int>(h,"a.b","$value")>=7&&CosmicQueryMetadata.Exists(h,"literal.dot"),
            "membership"=>h=>ids.Contains(h.Id)||h.Description.EndsWith("\0coffee",StringComparison.Ordinal),
            "guid"=>h=>CosmicQueryMetadata.Value<Guid>(h,"guid")==target,
            "nullable-guid"=>h=>CosmicQueryMetadata.Value<Guid?>(h,"guid")!=null,
            "guid-membership"=>h=>ids.Contains(CosmicQueryMetadata.Value<Guid>(h,"guid")),
            _=>h=>CosmicQueryMetadata.IsNull(h,"nullable")||CosmicQueryMetadata.Value<string>(h,"name")!=null};
        var built=CosmicQuery<Holon>.Create().Where(predicate);Assert.IsFalse(built.IsError,built.Message);
        var provider=new MongoDBOASIS("mongodb://127.0.0.1:1/?serverSelectionTimeoutMS=1","cql-admission-only");
        var result=await provider.ExecuteCosmicQueryAsync(Plan(built.Result));
        Assert.AreEqual("CQL_PROVIDER_NOT_READY",result.ErrorCode,result.Message+result.Exception);Assert.IsFalse(provider.IsProviderActivated);
    }
}
