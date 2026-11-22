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
    private static Mock<ICosmicSetQueryStorageProvider> SetAdapter(Source source,ProviderType provider,IHolon[] rows)
    {
        var native=source.Storage.As<ICosmicSetQueryStorageProvider>();
        native.SetupGet(value=>value.SetQueryCapabilities).Returns(new CosmicSetQueryCapabilities(
            CosmicSetQueryFeatures.Union|CosmicSetQueryFeatures.Concat|CosmicSetQueryFeatures.Intersect|CosmicSetQueryFeatures.Except,
            new CosmicEntityCompositionCapabilities(CosmicEntityStageFeatures.Paging|CosmicEntityStageFeatures.Ordering|
                CosmicEntityStageFeatures.Filter|CosmicEntityStageFeatures.Reverse,Capabilities)));
        native.Setup(value=>value.ExecuteCosmicSetQueryAsync(It.IsAny<CosmicSetQueryPlan<Holon>>(),
            It.IsAny<int>(),It.IsAny<CancellationToken>()))
            .Returns((CosmicSetQueryPlan<Holon> plan,int maximum,CancellationToken token)=>{
                Assert.Equal(Actor,plan.Query.Left.ReadAccess.ActorId);
                Assert.Equal(Actor,plan.Query.Right.ReadAccess.ActorId);
                return Task.FromResult(new OASISResult<IReadOnlyList<IHolon>>(rows){IsLoaded=true});
            });
        return native;
    }
    [Fact]
    public async Task SetEndpointPublishesConcatOccurrencesThroughOneMeteredAttempt()
    {
        var provider=ProviderType.SQLLiteDBOASIS;var source=new Source(provider);
        var row=new Holon {Id=Guid.NewGuid(),Name="occurrence",CreatedByAvatarId=Actor};
        SetAdapter(source,provider,new IHolon[]{row,row});
        var admitted=Admission(provider);
        var response=await admitted.Policy.RunAsync(()=>CqlV1Endpoint.ExecuteSetAsync(admitted.Http,
            new CqlSetV1Request {Provider=provider.ToString(),Kind="Concat",Left=new(){Take=2},Right=new(){Take=2}},source));
        using var json=JsonDocument.Parse(Assert.IsType<FileContentResult>(response).FileContents);
        var result=json.RootElement.GetProperty("result");
        Assert.Equal("cql-set-v1",result.GetProperty("version").GetString());
        var rows=result.GetProperty("rows");Assert.Equal(2,rows.GetArrayLength());
        Assert.Equal(row.Id,rows[0].GetProperty("id").GetGuid());Assert.Equal(row.Id,rows[1].GetProperty("id").GetGuid());
        Assert.Null(source.Observed);Assert.Equal(1,source.Managers);
        Assert.Equal(0.002m,Assert.Single(admitted.Receipts).ActualCostUsd);
        admitted.Store.Verify(value=>value.BeginProviderAttemptAsync(It.IsAny<string>(),It.IsAny<string>(),
            It.IsAny<Services.Subscriptions.UsageProviderAttempt>(),It.IsAny<CancellationToken>()),Times.Once);
    }
    [Fact]
    public async Task SetEndpointCannotPublishForeignRowsOrDispatchWithoutTariff()
    {
        var provider=ProviderType.SQLLiteDBOASIS;var source=new Source(provider);
        SetAdapter(source,provider,new IHolon[]{new Holon {Id=Guid.NewGuid(),CreatedByAvatarId=Guid.NewGuid(),IsPublic=false}});
        var admitted=Admission(provider);
        var denied=await admitted.Policy.RunAsync(()=>CqlV1Endpoint.ExecuteSetAsync(admitted.Http,new(),source));
        Assert.Equal("CQL_VISIBILITY_VIOLATION",Code(denied));Assert.Single(admitted.Receipts);
        var withoutTariff=Admission(provider,tariff:false);var managers=source.Managers;
        var refused=await withoutTariff.Policy.RunAsync(()=>CqlV1Endpoint.ExecuteSetAsync(withoutTariff.Http,new(),source));
        Assert.Equal("CQL_PROVIDER_NOT_ELIGIBLE",Code(refused));
        Assert.Equal(managers,source.Managers);Assert.Empty(withoutTariff.Receipts);
    }
}
