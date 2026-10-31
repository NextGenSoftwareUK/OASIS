using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public partial class CqlV1Web4Tests
{
    private static CqlJoinV1Request JoinRequest()=>new()
    {
        Kind="Left",Keys=new[]{new CqlJoinV1Key{Alias="name",Left=new(){Field="Name"},Right=new(){Field="Name"}}},
        Columns=new[]{new CqlJoinV1Column{Alias="name",Selection=new(){Field="Name"}},
            new CqlJoinV1Column{Alias="stock",Source="Right",Selection=new(){MetadataPath=new[]{"stock"},ValueType="Int64"},AllowMissing=true},
            new CqlJoinV1Column{Alias="present",Source="RightPresence"}}
    };
    private static readonly Guid JoinL1=Guid.Parse("10000000-0000-0000-0000-000000000001");
    private static readonly Guid JoinL2=Guid.Parse("10000000-0000-0000-0000-000000000002");
    private static readonly Guid JoinR=Guid.Parse("20000000-0000-0000-0000-000000000001");
    private static CosmicJoinPacket JoinPacket(bool privateRight=false)=>new(new[]{
        new CosmicJoinInputRow(JoinL1,true,Actor,new object[]{"coffee"},new object[]{"coffee"},new object[]{"coffee",JoinL1}),
        new CosmicJoinInputRow(JoinL2,true,Actor,new object[]{"tea"},new object[]{"tea"},new object[]{"tea",JoinL2})},
        new[]{new CosmicJoinInputRow(JoinR,!privateRight,Guid.NewGuid(),new object[]{"coffee"},new object[]{9007199254740993L},new object[]{"coffee",JoinR})},
        new[]{new CosmicJoinRow(JoinL1,JoinR,"coffee",9007199254740993L,true),new CosmicJoinRow(JoinL2,null,"tea",null!,false)},2,1,2);

    private static Mock<ICosmicQueryJoinStorageProvider> JoinAdapter(Source source,ProviderType provider,CosmicJoinPacket packet)
    {
        var adapter=source.Storage.As<ICosmicQueryJoinStorageProvider>();
        adapter.SetupGet(value=>value.QueryProviderType).Returns(provider);adapter.SetupGet(value=>value.QueryCapabilities).Returns(Capabilities);
        var paths=new CosmicProjectionCapabilities(new[]{new CosmicProjectionFieldCapability("Name",typeof(string))},new[]{typeof(long),typeof(long?)});
        adapter.SetupGet(value=>value.JoinCapabilities).Returns(new CosmicJoinCapabilities(new[]{CosmicJoinKind.Inner,CosmicJoinKind.Left},paths,paths));
        adapter.Setup(value=>value.ExecuteCosmicJoinAsync(
            It.IsAny<CosmicJoinPlan<Holon,Holon,CosmicProjectionRecord,CosmicProjectionRecord>>(),
            It.IsAny<CosmicJoinExecutionLimits>(),It.IsAny<CancellationToken>()))
            .Returns((CosmicJoinPlan<Holon,Holon,CosmicProjectionRecord,CosmicProjectionRecord> plan,CosmicJoinExecutionLimits limits,CancellationToken token)=>
            {
                Assert.Equal(Actor,plan.Query.Join.Left.ReadAccess.ActorId);Assert.Equal(Actor,plan.Query.Join.Right.ReadAccess.ActorId);
                return Task.FromResult(new OASISResult<CosmicJoinPacket>(packet){IsLoaded=true});
            });
        return adapter;
    }

    [Theory]
    [InlineData(ProviderType.MongoDBOASIS)] [InlineData(ProviderType.SQLLiteDBOASIS)] [InlineData(ProviderType.Neo4jOASIS)]
    public async Task JoinHttpRetainsPresenceExactWireValuesAndOneDurableAttempt(ProviderType provider)
    {
        var source=new Source(provider);JoinAdapter(source,provider,JoinPacket());var admitted=Admission(provider);
        var response=await admitted.Policy.RunAsync(()=>CqlV1Endpoint.ExecuteJoinAsync(admitted.Http,JoinRequest(),source));
        using var document=JsonDocument.Parse(Assert.IsType<FileContentResult>(response).FileContents);
        var result=document.RootElement.GetProperty("result");Assert.Equal("cql-join-v1",result.GetProperty("version").GetString());
        Assert.Equal(provider.ToString(),result.GetProperty("provider").GetString());
        var rows=result.GetProperty("rows");Assert.True(rows[0].GetProperty("hasRight").GetBoolean());
        Assert.Equal("9007199254740993",rows[0].GetProperty("values")[1].GetString());
        Assert.False(rows[1].GetProperty("hasRight").GetBoolean());Assert.Equal(JsonValueKind.Null,rows[1].GetProperty("values")[1].ValueKind);
        Assert.Equal("Int64",result.GetProperty("columns")[1].GetProperty("valueType").GetString());
        Assert.True(result.GetProperty("columns")[1].GetProperty("nullable").GetBoolean());
        Assert.Null(source.Observed);
        var receipt=Assert.Single(admitted.Receipts);Assert.Equal(1,receipt.Units);Assert.Equal(0.002m,receipt.ActualCostUsd);
        admitted.Store.Verify(value=>value.BeginProviderAttemptAsync(It.IsAny<string>(),It.IsAny<string>(),
            It.IsAny<Services.Subscriptions.UsageProviderAttempt>(),It.IsAny<CancellationToken>()),Times.Once);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task JoinAdmissionAndMalformedRequestsRejectBeforeProviderLookup(bool malformed)
    {
        var provider=ProviderType.SQLLiteDBOASIS;var source=new Source(provider);
        var admitted=Admission(provider,reservedActor:malformed?null:Guid.NewGuid());var request=JoinRequest();
        if(malformed)request.Kind="Cross";
        var result=await admitted.Policy.RunAsync(()=>CqlV1Endpoint.ExecuteJoinAsync(admitted.Http,request,source));
        Assert.Equal(malformed?"CQL_REQUEST_INVALID":"CQL_ADMISSION_REQUIRED",Code(result));
        Assert.Equal(0,source.Lookups);Assert.Equal(0,source.Managers);Assert.Empty(admitted.Receipts);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task JoinMissingInterfaceOrCapabilitiesCannotCreateManagerOrAttempt(bool interfacePresent)
    {
        var type=ProviderType.MongoDBOASIS;var source=new Source(type);
        if(interfacePresent)JoinAdapter(source,type,JoinPacket()).SetupGet(adapter=>adapter.JoinCapabilities)
            .Returns(new CosmicJoinCapabilities(Array.Empty<CosmicJoinKind>(),new CosmicProjectionCapabilities(Array.Empty<CosmicProjectionFieldCapability>(),Array.Empty<Type>()),
                new CosmicProjectionCapabilities(Array.Empty<CosmicProjectionFieldCapability>(),Array.Empty<Type>())));
        var admitted=Admission(type);var response=await admitted.Policy.RunAsync(()=>CqlV1Endpoint.ExecuteJoinAsync(admitted.Http,JoinRequest(),source));
        Assert.Equal("CQL_CAPABILITY_UNSUPPORTED",Code(response));Assert.Equal(0,source.Managers);Assert.Empty(admitted.Receipts);
    }

    [Fact]
    public async Task JoinRequiresConfiguredQueryTariffBeforeDispatch()
    {
        var type=ProviderType.Neo4jOASIS;var source=new Source(type);JoinAdapter(source,type,JoinPacket());
        var admitted=Admission(type,tariff:false);
        var response=await admitted.Policy.RunAsync(()=>CqlV1Endpoint.ExecuteJoinAsync(admitted.Http,JoinRequest(),source));
        Assert.Equal("CQL_PROVIDER_NOT_ELIGIBLE",Code(response));Assert.Equal(0,source.Managers);Assert.Empty(admitted.Receipts);
    }

    [Fact]
    public async Task ForeignNativeRightRowsCannotBecomeSuccessfulJoinJson()
    {
        var type=ProviderType.SQLLiteDBOASIS;var source=new Source(type);JoinAdapter(source,type,JoinPacket(privateRight:true));
        var admitted=Admission(type);var response=await admitted.Policy.RunAsync(()=>CqlV1Endpoint.ExecuteJoinAsync(admitted.Http,JoinRequest(),source));
        Assert.Equal("CQL_PROVIDER_RESULT_INVALID",Code(response));Assert.Single(admitted.Receipts);
    }

    [Fact]
    public async Task EscapedJoinResponseOverflowPublishesNoFileAndRetainsItsReceipt()
    {
        var type=ProviderType.MongoDBOASIS;var source=new Source(type);var text=new string('\0',80000);
        var packet=new CosmicJoinPacket(new[]{new CosmicJoinInputRow(JoinL1,true,Actor,new object[]{text},new object[]{text,text,text},new object[]{text,JoinL1})},
            new[]{new CosmicJoinInputRow(JoinR,true,Actor,new object[]{text},Array.Empty<object>(),new object[]{text,JoinR})},
            new[]{new CosmicJoinRow(JoinL1,JoinR,text,text,text)},1,1,1);
        JoinAdapter(source,type,packet);var request=JoinRequest();
        request.Columns=Enumerable.Range(0,3).Select(index=>new CqlJoinV1Column{Alias="name"+index,Selection=new(){Field="Name"}}).ToArray();
        var admitted=Admission(type);var response=await admitted.Policy.RunAsync(()=>CqlV1Endpoint.ExecuteJoinAsync(admitted.Http,request,source));
        Assert.Equal("CQL_RESPONSE_LIMIT",Code(response));Assert.Equal(413,Assert.IsType<ObjectResult>(response).StatusCode);Assert.Single(admitted.Receipts);
    }
}
