using FluentAssertions;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests;

public sealed partial class MongoSqliteRealProviderRoutingTests
{
    private static async Task VerifyDimensionReplicationAsync(MongoDBOASIS mongo,SQLLiteDBOASIS sqlite,OASISHyperDrive router,bool synchronous,bool mongoPrimary)
    {
        var id=Guid.NewGuid();var version=Guid.NewGuid();var previous=Guid.NewGuid();
        IHolon payload=new Holon {Id=id,VersionId=version,PreviousVersionId=previous,CustomHolonType="owned-custom-type",Name="dimension round trip",IsPublic=true,DimensionLevel=DimensionLevel.Fifth,SubDimensionLevel=SubDimensionLevel.Seventh};
        payload.CreatedProviderType=new EnumValue<ProviderType>(ProviderType.EthereumOASIS);payload.CreatedOASISType=new EnumValue<OASISType>(OASISType.OurWorld);
        try
        {
            for(var pass=0;pass<2;pass++)
            {
                var request=new StorageOperationRequest {Operation="SaveHolon",Payload=payload,SaveChildren=false,
                    PreferredProvider=mongoPrimary ? ProviderType.MongoDBOASIS:ProviderType.SQLLiteDBOASIS};
                var result=synchronous ? router.RouteRequest<IHolon>(request):await router.RouteRequestAsync<IHolon>(request);
                result.IsError.Should().BeFalse(result.Message);result.IsWarning.Should().BeFalse(result.Message);
                result.Result.DimensionLevel.Should().Be(DimensionLevel.Fifth);result.Result.SubDimensionLevel.Should().Be(SubDimensionLevel.Seventh);
                result.Result.IsPublic.Should().Be(pass==0);
                result.Result.VersionId.Should().Be(version);result.Result.PreviousVersionId.Should().Be(previous);result.Result.CustomHolonType.Should().Be("owned-custom-type");
                result.Result.CreatedProviderType?.Value.Should().Be(ProviderType.EthereumOASIS);result.Result.CreatedOASISType?.Value.Should().Be(OASISType.OurWorld);
                foreach(var store in new IOASISStorageProvider[] {mongo,sqlite})
                {
                    var loaded=synchronous ? store.LoadHolon(id,loadChildren:false):await store.LoadHolonAsync(id,loadChildren:false);
                    loaded.IsError.Should().BeFalse(loaded.Message);loaded.Result.DimensionLevel.Should().Be(DimensionLevel.Fifth);
                    loaded.Result.SubDimensionLevel.Should().Be(SubDimensionLevel.Seventh);
                    loaded.Result.IsPublic.Should().Be(pass==0);
                    loaded.Result.VersionId.Should().Be(version);loaded.Result.PreviousVersionId.Should().Be(previous);loaded.Result.CustomHolonType.Should().Be("owned-custom-type");
                    loaded.Result.CreatedProviderType?.Value.Should().Be(ProviderType.EthereumOASIS);loaded.Result.CreatedOASISType?.Value.Should().Be(OASISType.OurWorld);
                }
                payload=result.Result;payload.Name="edited dimension round trip";payload.IsPublic=false;
                payload.CreatedProviderType=null;payload.CreatedOASISType=null;
            }
        }
        finally
        {
            (await mongo.DeleteHolonAsync(id)).IsError.Should().BeFalse();
            (await sqlite.DeleteHolonAsync(id)).IsError.Should().BeFalse();
        }
    }
}
