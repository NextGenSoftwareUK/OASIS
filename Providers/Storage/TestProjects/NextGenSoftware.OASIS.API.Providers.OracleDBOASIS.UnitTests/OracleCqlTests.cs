using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.OracleDBOASIS.UnitTests;

[TestClass]
public sealed class OracleCqlTests
{
    private static CosmicQuery<Holon> Query(Guid[] ids)=>CosmicQuery<Holon>.Create(
        CosmicQueryScope.Named(ProviderType.OracleDBOASIS).Result)
        .WithReadAccess(CosmicQueryReadAccess.AuthoritativeRecords(Guid.NewGuid(),ProviderType.OracleDBOASIS,ids).Result).Result;

    [TestMethod]
    public void IdentityMembershipBindsNativeGuidValuesAndFrozenAuthorization()
    {
        var ids=new[]{Guid.NewGuid(),Guid.NewGuid(),Guid.NewGuid()};var original=ids.ToArray();
        var query=Query(ids).Where(row=>ids.Contains(row.Id)).Result;
        ids[0]=Guid.NewGuid();
        var result=OracleCqlCompiler.Compile(query);
        Assert.IsFalse(result.IsError,result.Message);
        StringAssert.Contains(result.Result.Sql,"ID IN (:cql");
        StringAssert.Contains(result.Result.Sql,"IS_DELETED=0");
        Assert.AreEqual(6,result.Result.Values.Count);
        CollectionAssert.AreEquivalent(original,result.Result.Values.Take(3).ToArray());
        CollectionAssert.AreEqual(original,result.Result.Values.Skip(3).ToArray());
        foreach(var id in original)Assert.IsFalse(result.Result.Sql.Contains(id.ToString()));
    }

    [TestMethod]
    public void EmptyTrustedCatalogueCompilesToNativeFalseWithoutAnyIdParameters()
    {
        var result=OracleCqlCompiler.Compile(Query(Array.Empty<Guid>()));
        Assert.IsFalse(result.IsError,result.Message);StringAssert.Contains(result.Result.Sql,"1=0");
        Assert.AreEqual(0,result.Result.Values.Count);
    }

    [DataTestMethod]
    [DataRow("ordering")] [DataRow("graph")]
    public void UnenrolledBusinessFeaturesRejectBeforeSqlDispatch(string feature)
    {
        var query=Query(new[]{Guid.NewGuid()});
        query=feature switch{
            "name"=>query.Where(row=>row.Name=="coffee").Result,
            "metadata"=>query.Where(row=>CosmicQueryMetadata.Value<string>(row,"category")=="coffee").Result,
            "ordering"=>query.OrderBy(row=>row.Id).Result,
            _=>query.WithChildren(CosmicQueryGraphOptions.Create(maxChildDepth:1).Result).Result};
        var result=OracleCqlCompiler.Compile(query);
        Assert.IsTrue(result.IsError);Assert.AreEqual("CQL_CAPABILITY_UNSUPPORTED",result.ErrorCode);Assert.IsNull(result.Result);
    }

    [TestMethod]
    public async Task ExplicitActivationIsRequiredAndUnsupportedShapeDoesNotOpenAConnection()
    {
        var provider=new OracleDBOASIS("not-a-native-connection");
        var query=Query(new[]{Guid.NewGuid()});
        var plan=CosmicQueryPlanner.Plan(query,ProviderType.OracleDBOASIS,
            new[]{new CosmicQueryTarget(ProviderType.OracleDBOASIS,provider.QueryCapabilities)});
        Assert.IsFalse(plan.IsError,plan.Message);
        var result=await provider.ExecuteCosmicQueryAsync(plan.Result);
        Assert.AreEqual("CQL_PROVIDER_INACTIVE",result.ErrorCode);Assert.IsNull(result.Result);
        using var cancel=new CancellationTokenSource();cancel.Cancel();
        result=await provider.ExecuteCosmicQueryAsync(plan.Result,cancellationToken:cancel.Token);
        Assert.AreEqual("CQL_QUERY_CANCELLED",result.ErrorCode);Assert.IsNull(result.Result);
    }
    [TestMethod]
    public void ContentValuesAreCapturedBindsInsideTheOriginalAuthorityGuard()
    {
        var allowed=Guid.NewGuid();var forbidden=Guid.NewGuid();
        var text="coffee%_' OR 1=1 --";
        var query=Query(new[]{allowed}).Where(h=>(h.Id==forbidden||h.Description.Contains(text))&&
            CosmicQueryMetadata.Value<string>(h,"category")==text).Result;
        text="changed after capture";
        var result=OracleCqlCompiler.Compile(query);
        Assert.IsFalse(result.IsError,result.Message);
        Assert.AreEqual(2,result.Result.TextValues.Count);
        Assert.AreEqual("coffee%_' OR 1=1 --",result.Result.TextValues[0]);
        Assert.IsFalse(result.Result.Sql.Contains("coffee%_"));
        StringAssert.Contains(result.Result.Sql,"JSON_EXISTS(DATA_JSON");
        StringAssert.Contains(result.Result.Sql,"PASSING :cqltext");
        StringAssert.Contains(result.Result.Sql,"ERROR ON ERROR");
        StringAssert.Contains(result.Result.Sql,".type() == \"object\"");
        StringAssert.Contains(result.Result.Sql,".type() == \"string\"");
        CollectionAssert.AreEqual(new[]{allowed,forbidden},result.Result.Values.ToArray());
        Assert.ThrowsException<NotSupportedException>(()=>((IList<string>)result.Result.TextValues)[0]="replacement");
    }

    [TestMethod]
    public void MetadataKeyEncodingCannotEscapeTheOracleSqlLiteral()
    {
        var key="category'\"; DROP TABLE OASIS_HOLONS;--";
        var query=Query(new[]{Guid.NewGuid()}).Where(h=>CosmicQueryMetadata.Value<string>(h,key)=="coffee").Result;
        var result=OracleCqlCompiler.Compile(query);
        Assert.IsFalse(result.IsError,result.Message);
        var outsideLiterals=System.Text.RegularExpressions.Regex.Replace(result.Result.Sql,@"'(?:''|[^'])*'","''");
        Assert.IsFalse(outsideLiterals.Contains("DROP TABLE"));
        Assert.AreEqual("coffee",result.Result.TextValues.Single());
    }

    [TestMethod]
    public void EmptyStringRetainsItsJsonStringIntent()
    {
        var result=OracleCqlCompiler.Compile(Query(new[]{Guid.NewGuid()}).Where(h=>h.Name=="").Result);
        Assert.IsFalse(result.IsError,result.Message);
        Assert.AreEqual("",result.Result.TextValues.Single());
        StringAssert.Contains(result.Result.Sql,".type() == \"string\"");
    }

    [DataTestMethod]
    [DataRow("starts")]
    [DataRow("numeric")]
    [DataRow("null")]
    [DataRow("nul")]
    [DataRow("utf8-bound")]
    public void UnprovedContentSemanticsRejectBeforeNativeDispatch(string feature)
    {
        var query=Query(new[]{Guid.NewGuid()});
        var oversized=new string('\u20ac',1334); //4002UTF8 bytes, within the shared character limit.
        var capture=feature switch {
            "starts"=>query.Where(h=>h.Name.StartsWith("C",StringComparison.Ordinal)),
            "numeric"=>query.Where(h=>CosmicQueryMetadata.Value<int>(h,"score")>1),
            "null"=>query.Where(h=>h.Name==null),
            "nul"=>query.Where(h=>h.Name=="invalid\0value"),
            _=>query.Where(h=>h.Name==oversized)
        };
        Assert.IsFalse(capture.IsError,capture.Message);
        var compiled=OracleCqlCompiler.Compile(capture.Result);
        Assert.IsTrue(compiled.IsError);
        Assert.AreEqual("CQL_CAPABILITY_UNSUPPORTED",compiled.ErrorCode);
        Assert.IsNull(compiled.Result);
    }

}
