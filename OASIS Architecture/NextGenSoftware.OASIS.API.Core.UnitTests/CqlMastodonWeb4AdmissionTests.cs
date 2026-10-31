using System.Text.Json;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public class CqlMastodonWeb4AdmissionTests
{
    [Fact]
    public void ProtocolReadEnrollmentDoesNotEnrollFederationOrStorageTariffs()
    {
        Assert.Equal(ProviderType.ActivityPubOASIS,CqlV1Compiler.RequestedProvider("ActivityPubOASIS",true));
        Assert.False(CqlV1Compiler.Supported(ProviderType.ActivityPubOASIS));
        Assert.Throws<ArgumentException>(()=>CqlV1Compiler.RequestedProvider("ActivityPubOASIS"));
        Assert.Throws<ArgumentException>(()=>StorageAttemptTariff.Capture(
            new[]{new StorageAttemptTariff(ProviderType.ActivityPubOASIS,"SaveHolon",0.001m,"protocol-test")},
            new[]{ProviderType.ActivityPubOASIS}));
    }

    [Fact]
    public void ExplicitNoOrderingRetainsNativeKeyAndServerActorPolicy()
    {
        var actor=Guid.NewGuid();
        var request=new CqlV1Request {Provider="ActivityPubOASIS",OrderBy="None",Take=1,Filters=new[]{
            new CqlV1Filter {MetadataPath=new[]{"MastodonStatusId"},Scalar=CqlV1Scalar.String,
                Operator=CqlV1Operator.Equal,Value=JsonSerializer.SerializeToElement("123")} }};
        var query=CqlV1Compiler.Compile(request,actor,ProviderType.ActivityPubOASIS);
        Assert.Empty(query.Orderings);Assert.Equal(actor,query.ReadAccess.ActorId);
        var capability=CosmicQueryCapabilities.ForStringMetadataPointLookup("MastodonStatusId");
        Assert.Equal("123",capability.StringPointLookup.SelectKey(query).Result);
        Assert.False(CosmicQueryPlanner.Plan(query,ProviderType.ActivityPubOASIS,
            new[]{new CosmicQueryTarget(ProviderType.ActivityPubOASIS,capability)}).IsError);
        request.Descending=true;
        Assert.Throws<ArgumentException>(()=>CqlV1Compiler.Compile(request,actor,ProviderType.ActivityPubOASIS));
        request.Descending=false;
        Assert.Throws<ArgumentException>(()=>CqlV1Compiler.Compile(request,actor,ProviderType.SQLLiteDBOASIS));
    }

    [Fact]
    public void FiveExplicitReadTariffsRequireIndependentPermissionAndDoNotAuthorizeSignedChains()
    {
        var allowed=new[]{ProviderType.MongoDBOASIS,ProviderType.SQLLiteDBOASIS,ProviderType.Neo4jOASIS,
            ProviderType.AzureCosmosDBOASIS,ProviderType.ActivityPubOASIS};
        var tariffs=allowed.Select(p=>new StorageQueryTariff(p,0.001m,"protocol-test")).ToArray();
        Assert.Equal(5,StorageQueryTariff.Capture(tariffs,allowed).Length);
        Assert.Throws<ArgumentException>(()=>StorageQueryTariff.Capture(tariffs,allowed.Take(4)));
        Assert.Throws<ArgumentException>(()=>StorageQueryTariff.Capture(
            new[]{new StorageQueryTariff(ProviderType.SolanaOASIS,0.001m,"protocol-test")},new[]{ProviderType.SolanaOASIS}));
    }
}
