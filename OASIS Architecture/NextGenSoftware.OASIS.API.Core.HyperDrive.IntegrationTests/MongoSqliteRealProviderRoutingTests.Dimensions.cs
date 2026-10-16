using FluentAssertions;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS;

namespace NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests;

public sealed partial class MongoSqliteRealProviderRoutingTests
{
    private static async Task VerifyDimensionReplicationAsync(MongoDBOASIS mongo,SQLLiteDBOASIS sqlite,OASISHyperDrive router,bool synchronous,bool mongoPrimary)
    {
        var id=Guid.NewGuid();IHolon payload=new Holon {Id=id,Name="dimension round trip",DimensionLevel=DimensionLevel.Fifth,SubDimensionLevel=SubDimensionLevel.Seventh};
        try
        {
            for(var pass=0;pass<2;pass++)
            {
                var request=new StorageOperationRequest {Operation="SaveHolon",Payload=payload,SaveChildren=false,
                    PreferredProvider=mongoPrimary ? ProviderType.MongoDBOASIS:ProviderType.SQLLiteDBOASIS};
                var result=synchronous ? router.RouteRequest<IHolon>(request):await router.RouteRequestAsync<IHolon>(request);
                result.IsError.Should().BeFalse(result.Message);result.IsWarning.Should().BeFalse(result.Message);
                result.Result.DimensionLevel.Should().Be(DimensionLevel.Fifth);result.Result.SubDimensionLevel.Should().Be(SubDimensionLevel.Seventh);
                foreach(var store in new IOASISStorageProvider[] {mongo,sqlite})
                {
                    var loaded=synchronous ? store.LoadHolon(id,loadChildren:false):await store.LoadHolonAsync(id,loadChildren:false);
                    loaded.IsError.Should().BeFalse(loaded.Message);loaded.Result.DimensionLevel.Should().Be(DimensionLevel.Fifth);
                    loaded.Result.SubDimensionLevel.Should().Be(SubDimensionLevel.Seventh);
                }
                payload=result.Result;payload.Name="edited dimension round trip";
            }
        }
        finally
        {
            (await mongo.DeleteHolonAsync(id)).IsError.Should().BeFalse();
            (await sqlite.DeleteHolonAsync(id)).IsError.Should().BeFalse();
        }
    }
}
