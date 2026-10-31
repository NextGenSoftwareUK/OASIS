using System.Globalization;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.SOLIDOASIS.UnitTests;

public class SolidNearbyTests
{
    [Theory][InlineData(false)][InlineData(true)]
    public async Task NearbyQueriesDoNotExposeDeletedRecords(bool avatar)
    {
        var transport=new SolidRecordHttpTests.Transport();using var codec=new SolidPrivateRecordCodec(Root,Key);var id=Guid.NewGuid();
        var metadata=new Dictionary<string,object>{{"Latitude",0d},{"Longitude",0d}};
        if(avatar)transport.Records[codec.Resource<IAvatar>(id).AbsoluteUri]=codec.Encode<IAvatar>(new Avatar{Id=id,DeletedDate=DateTime.UtcNow,MetaData=metadata});
        else transport.Records[codec.Resource<IHolon>(id).AbsoluteUri]=codec.Encode<IHolon>(new Holon{Id=id,DeletedDate=DateTime.UtcNow,MetaData=metadata});
        using var provider=new SOLIDOASIS(Root.AbsoluteUri,"",Key,transport);Assert.False((await provider.ActivateProviderAsync()).IsError);var network=(IOASISNETProvider)provider;
        if(avatar){var result=network.GetAvatarsNearMe(0,0,10);Assert.False(result.IsError,result.Exception?.ToString());Assert.Empty(result.Result);}
        else{var result=network.GetHolonsNearMe(0,0,10,HolonType.All);Assert.False(result.IsError,result.Exception?.ToString());Assert.Empty(result.Result);}
    }
    private static readonly Uri Root=new("https://pod.invalid/private/");
    private static readonly byte[] Key=Enumerable.Range(1,32).Select(x=>(byte)x).ToArray();
    [Theory][InlineData(false)][InlineData(true)]
    public async Task DecimalCoordinatesAreInvariantUnderCommaDecimalCulture(bool avatar)
    {
        var transport=new SolidRecordHttpTests.Transport();using var codec=new SolidPrivateRecordCodec(Root,Key);
        var near=Guid.NewGuid();var far=Guid.NewGuid();
        foreach(var id in new[]{near,far}){
            var metadata=new Dictionary<string,object>{{"Latitude",id==near?"51.5":"50.5"},{"Longitude","-0.125"}};
            if(avatar)transport.Records[codec.Resource<IAvatar>(id).AbsoluteUri]=codec.Encode<IAvatar>(new Avatar{Id=id,MetaData=metadata});
            else transport.Records[codec.Resource<IHolon>(id).AbsoluteUri]=codec.Encode<IHolon>(new Holon{Id=id,MetaData=metadata});
        }
        using var provider=new SOLIDOASIS(Root.AbsoluteUri,"",Key,transport);Assert.False((await provider.ActivateProviderAsync()).IsError);
        var old=CultureInfo.CurrentCulture;
        try{
            CultureInfo.CurrentCulture=CultureInfo.GetCultureInfo("fr-FR");var network=(IOASISNETProvider)provider;
            if(avatar){var result=network.GetAvatarsNearMe(51500000,-125000,10);Assert.False(result.IsError,result.Message);Assert.Equal(near,Assert.Single(result.Result).Id);}
            else{var result=network.GetHolonsNearMe(51500000,-125000,10,HolonType.All);Assert.False(result.IsError,result.Message);Assert.Equal(near,Assert.Single(result.Result).Id);}
        }finally{CultureInfo.CurrentCulture=old;}
    }
    [Theory][InlineData(false,90000001,0,10)][InlineData(true,0,-180000001,10)][InlineData(false,0,0,-1)][InlineData(true,long.MaxValue,0,10)]
    public async Task InvalidLocationFailsBeforeStorageRead(bool avatar,long latitude,long longitude,int radius)
    {
        var transport=new SolidRecordHttpTests.Transport();using var provider=new SOLIDOASIS(Root.AbsoluteUri,"",Key,transport);
        Assert.False((await provider.ActivateProviderAsync()).IsError);var network=(IOASISNETProvider)provider;
        if(avatar){var result=network.GetAvatarsNearMe(latitude,longitude,radius);Assert.True(result.IsError);Assert.IsType<ArgumentOutOfRangeException>(result.Exception);}
        else{var result=network.GetHolonsNearMe(latitude,longitude,radius,HolonType.All);Assert.True(result.IsError);Assert.IsType<ArgumentOutOfRangeException>(result.Exception);}
        Assert.Equal(0,transport.Reads);
    }
    [Theory][InlineData(false)][InlineData(true)]
    public async Task NativeReadFailureRetainsOriginalException(bool avatar)
    {
        var transport=new SolidRecordHttpTests.Transport{Failure="read"};using var provider=new SOLIDOASIS(Root.AbsoluteUri,"",Key,transport);
        Assert.False((await provider.ActivateProviderAsync()).IsError);var network=(IOASISNETProvider)provider;
        var error=avatar?network.GetAvatarsNearMe(0,0,10).Exception:network.GetHolonsNearMe(0,0,10,HolonType.All).Exception;
        Assert.Equal(System.Net.HttpStatusCode.ServiceUnavailable,Assert.IsType<HttpRequestException>(error).StatusCode);
    }
}
