using System.Text.Json;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public class CqlPodWeb4AdmissionTests
{
    [Theory]
    [InlineData(ProviderType.IPFSOASIS,"IPFSRecordCid","Qmbb8wjanm2brssuLy6gjHGJEW5J552uhnsHkRBkZRL2GL")]
    [InlineData(ProviderType.SOLIDOASIS,"SolidRecordId","a1234567-b123-4123-8123-123456789abc")]
    public void ExplicitProtocolRowRequestBindsActorAndPointKeyWithoutFederationOrCrudEnrollment(ProviderType provider,string key,string value)
    {
        Assert.Equal(provider,CqlV1Compiler.RequestedProvider(provider.ToString(),true));
        Assert.False(CqlV1Compiler.Supported(provider));
        Assert.Throws<ArgumentException>(()=>CqlV1Compiler.RequestedProvider(provider.ToString()));
        Assert.Throws<ArgumentException>(()=>StorageAttemptTariff.Capture(
            new[]{new StorageAttemptTariff(provider,"SaveHolon",0.01m,"point-test")},new[]{provider}));
        var actor=Guid.NewGuid();
        var request=new CqlV1Request{Provider=provider.ToString(),OrderBy="None",Take=1,Filters=new[]{
            new CqlV1Filter{MetadataPath=new[]{key},Scalar=CqlV1Scalar.String,Operator=CqlV1Operator.Equal,Value=JsonSerializer.SerializeToElement(value)}}};
        var query=CqlV1Compiler.Compile(request,actor,provider);
        Assert.Equal(actor,query.ReadAccess.ActorId);Assert.Empty(query.Orderings);
        var capabilities=CosmicQueryCapabilities.ForStringMetadataPointLookup(key);
        Assert.Equal(value,capabilities.StringPointLookup.SelectKey(query).Result);
        Assert.False(CosmicQueryPlanner.Plan(query,provider,new[]{new CosmicQueryTarget(provider,capabilities)}).IsError);
        request.Descending=true;Assert.Throws<ArgumentException>(()=>CqlV1Compiler.Compile(request,actor,provider));
    }

    [Fact]
    public void SevenReadTariffsStillRequireIndependentPermissionUniqueProvidersAndUnsignedEnrollment()
    {
        var allowed=new[]{ProviderType.MongoDBOASIS,ProviderType.SQLLiteDBOASIS,ProviderType.Neo4jOASIS,
            ProviderType.AzureCosmosDBOASIS,ProviderType.ActivityPubOASIS,ProviderType.IPFSOASIS,ProviderType.SOLIDOASIS};
        var rows=allowed.Select(p=>new StorageQueryTariff(p,0.001m,"point-test")).ToArray();
        Assert.Equal(7,StorageQueryTariff.Capture(rows,allowed).Length);
        Assert.Throws<ArgumentException>(()=>StorageQueryTariff.Capture(rows,allowed.Take(6)));
        Assert.Throws<ArgumentException>(()=>StorageQueryTariff.Capture(rows.Append(rows[0]),allowed));
        Assert.Throws<ArgumentException>(()=>StorageQueryTariff.Capture(new[]{new StorageQueryTariff(ProviderType.SolanaOASIS,0.001m,"point-test")},new[]{ProviderType.SolanaOASIS}));
    }
}
