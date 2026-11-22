using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;
using NextGenSoftware.OASIS.Common;
namespace NextGenSoftware.OASIS.API.Core.UnitTests;
public partial class CqlV1Web4Tests
{
    [Fact]
    public async Task SelectedSetEndpointPublishesOnlyExplicitCellsThroughOneMeteredAttempt()
    {
        var provider=ProviderType.SQLLiteDBOASIS;var source=new Source(provider);
        var adapter=source.Storage.As<ICosmicQueryProjectedSetStorageProvider>();
        var stages=new CosmicProjectionCompositionCapabilities(CosmicSelectedStageFeatures.Paging,
            CosmicQueryFeatures.None,Array.Empty<CosmicQueryMetadataCapability>(),Array.Empty<Type>());
        adapter.SetupGet(value=>value.ProjectionCapabilities).Returns(new CosmicProjectionCapabilities(
            new[]{new CosmicProjectionFieldCapability("Name",typeof(string))},Array.Empty<Type>()));
        adapter.SetupGet(value=>value.CompositionCapabilities).Returns(stages);
        adapter.SetupGet(value=>value.ProjectedSetCapabilities).Returns(new CosmicProjectedSetCapabilities(
            CosmicSetQueryFeatures.Concat,new[]{typeof(string)},new[]{typeof(string)},stages));
        var identity=Guid.NewGuid();
        CosmicProjectionCompositionPacket Input()=>new(
            new[]{new CosmicProjectionInputRow(identity,false,Actor,new object[]{"chosen"},Array.Empty<object>())},
            Array.Empty<CosmicProjectionStageProof>(),new[]{new CosmicProjectionCompositionRow(identity,"chosen")},1,0);
        adapter.Setup(value=>value.ExecuteCosmicProjectedSetAsync(
            It.IsAny<CosmicProjectedSetPlan<Holon,CosmicProjectionRecord>>(),
            It.IsAny<CosmicProjectedSetExecutionLimits>(),It.IsAny<CancellationToken>()))
            .Returns((CosmicProjectedSetPlan<Holon,CosmicProjectionRecord> plan,CosmicProjectedSetExecutionLimits limits,CancellationToken token)=>{
                Assert.Equal(Actor,plan.Query.Left.Source.ReadAccess.ActorId);
                Assert.Equal(Actor,plan.Query.Right.Source.ReadAccess.ActorId);
                return Task.FromResult(new OASISResult<CosmicProjectedSetPacket>(new(
                    Input(),Input(),new(new[]{new CosmicProjectedSetOccurrence(false,0),new CosmicProjectedSetOccurrence(true,0)}),
                    Array.Empty<CosmicProjectedSetStageProof>(),new[]{new CosmicProjectionRow("chosen"),new CosmicProjectionRow("chosen")},2)){IsLoaded=true});
            });
        var admitted=Admission(provider);
        var response=await admitted.Policy.RunAsync(()=>CqlV1Endpoint.ExecuteSetAsync(admitted.Http,new CqlSetV1Request{
            Provider=provider.ToString(),Kind="Concat",
            Left=new(){OrderBy="None",Columns=new[]{new CqlProjectionV1Column{Field="Name",Alias="label"}}},
            Right=new(){OrderBy="None",Columns=new[]{new CqlProjectionV1Column{Field="Name",Alias="other"}}}},source));
        using var json=JsonDocument.Parse(Assert.IsType<FileContentResult>(response).FileContents);
        var result=json.RootElement.GetProperty("result");
        Assert.Equal("cql-projected-set-v1",result.GetProperty("version").GetString());
        Assert.Equal(2,result.GetProperty("rows").GetArrayLength());
        foreach(var row in result.GetProperty("rows").EnumerateArray()){
            Assert.Equal(1,row.GetArrayLength());Assert.Equal("chosen",row[0].GetString());
        }
        Assert.Null(source.Observed);Assert.Equal(1,source.Managers);
        Assert.Equal(0.002m,Assert.Single(admitted.Receipts).ActualCostUsd);
        admitted.Store.Verify(value=>value.BeginProviderAttemptAsync(It.IsAny<string>(),It.IsAny<string>(),
            It.IsAny<Services.Subscriptions.UsageProviderAttempt>(),It.IsAny<CancellationToken>()),Times.Once);
    }
}
