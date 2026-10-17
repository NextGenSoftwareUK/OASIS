using System.Globalization;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.TRONOASIS.UnitTests;

public sealed class TronNearbyTests
{
    private sealed class Records : TRONOASIS
    {
        public Avatar Avatar = new() { Id = Guid.NewGuid(), IsActive = true,
            MetaData = new() { ["Latitude"] = "30.0005", ["Longitude"] = "20", ["proof"] = "preserved" } };
        public Holon Holon = new() { Id = Guid.NewGuid(), IsActive = true, HolonType = HolonType.Park,
            MetaData = new() { ["Latitude"] = "30.0005", ["Longitude"] = "20", ["proof"] = "preserved" } };
        public Records() { IsProviderActivated = true; }
        public override OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version = 0)
            => new() { Result = new[] { Avatar }, IsLoaded = true };
        public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType type = HolonType.All, bool loadChildren = true,
            bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true,
            bool loadChildrenFromProvider = false, int version = 0)
            => new() { Result = new[] { Holon }, IsLoaded = true };
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void NearbyUsesMicrodegreesAndInvariantMetadata(bool avatars)
    {
        var provider = new Records(); var prior = CultureInfo.CurrentCulture;
        try {
            CultureInfo.CurrentCulture = new CultureInfo("fr-FR");
            if (avatars) {
                var result = provider.GetAvatarsNearMe(30000000, 20000000, 100);
                Assert.False(result.IsError, result.Message); Assert.True(result.IsLoaded);
                Assert.Same(provider.Avatar, Assert.Single(result.Result));
            } else {
                var result = provider.GetHolonsNearMe(30000000, 20000000, 100, HolonType.Park);
                Assert.False(result.IsError, result.Message); Assert.True(result.IsLoaded);
                Assert.Same(provider.Holon, Assert.Single(result.Result));
                Assert.Empty(provider.GetHolonsNearMe(30000000, 20000000, 100, HolonType.Holon).Result);
            }
        } finally { CultureInfo.CurrentCulture = prior; }
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void CorruptLocationCannotBecomeSuccessfulEmptyResult(bool avatars)
    {
        var provider = new Records();
        (avatars ? provider.Avatar.MetaData : provider.Holon.MetaData)["Latitude"] = "NaN";
        var cause = avatars ? provider.GetAvatarsNearMe(30000000,20000000,100).Exception
            : provider.GetHolonsNearMe(30000000,20000000,100,HolonType.All).Exception;
        Assert.IsType<System.IO.InvalidDataException>(cause);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void InvalidRadiusRejectedBeforeActivation(bool avatars)
    {
        var provider = new TRONOASIS();
        var cause = avatars ? provider.GetAvatarsNearMe(0,0,-1).Exception
            : provider.GetHolonsNearMe(0,0,-1,HolonType.All).Exception;
        Assert.IsType<ArgumentOutOfRangeException>(cause);
        Assert.False(provider.IsProviderActivated);
    }
}
