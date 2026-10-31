using System.Text.Json;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;
using NextGenSoftware.OASIS.API.Providers.OracleDBOASIS;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

public class CqlOracleHostedAdmissionTests
{
    [Fact]
    public void NineProviderHostCatalogueAdmitsOracleOnlyWithIndependentPermissionAndTariff()
    {
        var providers = new[] { ProviderType.MongoDBOASIS, ProviderType.SQLLiteDBOASIS, ProviderType.Neo4jOASIS,
            ProviderType.AzureCosmosDBOASIS, ProviderType.ActivityPubOASIS, ProviderType.IPFSOASIS,
            ProviderType.SOLIDOASIS, ProviderType.AWSS3OASIS, ProviderType.OracleDBOASIS };
        var tariffs = providers.Select(provider => new StorageQueryTariff(provider, 0, "sandbox-demo-free-v2")).ToArray();
        var factory = new PricedStorageAdmissionFactory(null, Array.Empty<NativeStorageAdmissionBinding>(),
            providers.Take(4), tariffs, null, providers.Skip(4));
        Assert.Equal(providers, factory.QueryProviders);
        Assert.Contains(ProviderType.OracleDBOASIS, factory.QueryOnlyProviders);
        Assert.Throws<ArgumentException>(() => new PricedStorageAdmissionFactory(null,
            Array.Empty<NativeStorageAdmissionBinding>(), providers.Take(8), tariffs));
        Assert.Throws<ArgumentException>(() => new PricedStorageAdmissionFactory(null,
            Array.Empty<NativeStorageAdmissionBinding>(), providers, tariffs.Concat(new[] { tariffs[0] })));
    }

    [Theory]
    [InlineData("Name")]
    [InlineData("Description")]
    [InlineData("metadata")]
    public void HttpQueryUsesActualOracleCompilerAndFrozenServerCatalogue(string field)
    {
        var ids = new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
        var actor = Guid.NewGuid();
        var policy = new CqlPublicReadCatalogue(ProviderType.OracleDBOASIS, ids);
        var access = policy.Authorize(actor, ProviderType.OracleDBOASIS).Result;
        const string text = "coffee' OR 1=1 --";
        var filter = new CqlV1Filter { Scalar = CqlV1Scalar.String, Operator = CqlV1Operator.Equal,
            Value = JsonSerializer.SerializeToElement(text) };
        if (field == "metadata") filter.MetadataPath = new[] { "category" };
        else filter.Field = field;
        var request = new CqlV1Request { Provider = "OracleDBOASIS", OrderBy = "None", Take = 8,
            Filters = new[] { filter } };
        var query = CqlV1Compiler.Compile(request, actor, ProviderType.OracleDBOASIS, access);
        var native = OracleCqlCompiler.Compile(query);
        Assert.False(native.IsError, native.Message);
        Assert.Equal(ids.Order(), native.Result.Values.Order());
        Assert.Equal(text, Assert.Single(native.Result.TextValues));
        Assert.DoesNotContain(text, native.Result.Sql);
        Assert.Contains("JSON_EXISTS", native.Result.Sql);
        Assert.Contains("ID IN", native.Result.Sql);
        Assert.Equal(actor, query.ReadAccess.ActorId);
        Assert.True(OracleCqlCompiler.Compile(CqlV1Compiler.Compile(request, actor, ProviderType.OracleDBOASIS)).IsError);
    }
}
