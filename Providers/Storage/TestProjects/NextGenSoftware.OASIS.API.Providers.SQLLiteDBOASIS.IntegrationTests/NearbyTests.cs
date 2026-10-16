using System.Globalization;
using FluentAssertions;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.IntegrationTests;

public partial class SQLLiteDBOASISIntegrationTests
{
    [Theory]
    [InlineData(90000001L,0L,1)]
    [InlineData(0L,180000001L,1)]
    [InlineData(0L,0L,-1)]
    public void NearbyRejectsInvalidRequestBeforeActivation(long latitude,long longitude,int radius)
    {
        using var provider=new SQLLiteDBOASIS(TestConnectionString);
        var avatars=provider.GetAvatarsNearMe(latitude,longitude,radius);
        avatars.IsError.Should().BeTrue();avatars.Exception.Should().BeOfType<ArgumentOutOfRangeException>();
        var holons=provider.GetHolonsNearMe(latitude,longitude,radius,HolonType.All);
        holons.IsError.Should().BeTrue();holons.Exception.Should().BeOfType<ArgumentOutOfRangeException>();
        provider.IsProviderActivated.Should().BeFalse();
    }

    [Fact]
    public void NearbyRejectsUndefinedTypeBeforeActivation()
    {
        using var provider=new SQLLiteDBOASIS(TestConnectionString);
        var result=provider.GetHolonsNearMe(0,0,100,(HolonType)int.MaxValue);
        result.IsError.Should().BeTrue();result.Exception.Should().BeOfType<ArgumentOutOfRangeException>();
        provider.IsProviderActivated.Should().BeFalse();
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task NearbyUsesInvariantCoordinatesAndRejectsCorruptStoredLocation(bool avatars)
    {
        using var provider=new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var id=Guid.NewGuid();
        var metadata=new Dictionary<string,object> {{"Latitude","0.0005"},{"Longitude","0"},{"proof","preserved"}};
        if(avatars)
        {
            var saved=await provider.SaveAvatarAsync(new Avatar {Id=id,Username="nearby",Email="nearby@example.invalid",IsActive=true,MetaData=metadata,AvatarType=new NextGenSoftware.Utilities.EnumValue<AvatarType>(AvatarType.User)});
            saved.IsError.Should().BeFalse(saved.Message);
        }
        else (await provider.SaveHolonAsync(new Holon {Id=id,Name="nearby",Description="complete",HolonType=HolonType.Park,IsActive=true,MetaData=metadata},saveChildren:false)).IsError.Should().BeFalse();
        var prior=CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture=new CultureInfo("fr-FR");
            if(avatars)
            {
                var near=provider.GetAvatarsNearMe(0,0,100);
                near.IsError.Should().BeFalse(near.Message);near.IsLoaded.Should().BeTrue();
                near.Result.Single().Id.Should().Be(id);near.Result.Single().MetaData["proof"].ToString().Should().Be("preserved");
                provider.GetAvatarsNearMe(0,0,0).Result.Should().BeEmpty();
                var record=(await provider.LoadAvatarAsync(id)).Result;record.MetaData["Latitude"]="NaN";
                var saved=await provider.SaveAvatarAsync(record);saved.IsError.Should().BeFalse(saved.Message);
                var corrupt=provider.GetAvatarsNearMe(0,0,100);
                corrupt.IsError.Should().BeTrue();corrupt.IsLoaded.Should().BeFalse();corrupt.Result.Should().BeNull();corrupt.Exception.Should().BeOfType<InvalidDataException>();
            }
            else
            {
                var near=provider.GetHolonsNearMe(0,0,100,HolonType.Park);
                near.IsError.Should().BeFalse(near.Message);near.IsLoaded.Should().BeTrue();
                near.Result.Single().Id.Should().Be(id);near.Result.Single().Description.Should().Be("complete");
                provider.GetHolonsNearMe(0,0,100,HolonType.Holon).Result.Should().BeEmpty();
                provider.GetHolonsNearMe(0,0,0,HolonType.All).Result.Should().BeEmpty();
                var record=(await provider.LoadHolonAsync(id,loadChildren:false)).Result;record.MetaData.Remove("Longitude");
                var saved=await provider.SaveHolonAsync(record,saveChildren:false);saved.IsError.Should().BeFalse(saved.Message);
                var corrupt=provider.GetHolonsNearMe(0,0,100,HolonType.All);
                corrupt.IsError.Should().BeTrue();corrupt.IsLoaded.Should().BeFalse();corrupt.Result.Should().BeNull();corrupt.Exception.Should().BeOfType<InvalidDataException>();
            }
        }
        finally {CultureInfo.CurrentCulture=prior;}
    }
}
