using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests;

// Shared behavioral checks; callers must actually persist/delete these identities
// through their native provider. This is not a simulated geographic backend.
internal static class NativeNearbyAcceptance
{
    internal static void Verify(IOASISNETProvider provider,Guid avatarId,Guid holonId,bool present)
    {
        // API microdegrees vs persisted degrees, including ~0.11m across the date line.
        foreach(var point in new[]{(Longitude:179999999L,Radius:0,Match:present),
            (Longitude:-180000000L,Radius:1,Match:present),
            (Longitude:-180000000L,Radius:0,Match:false),(Longitude:0L,Radius:1,Match:false)}){
            var avatars=provider.GetAvatarsNearMe(0,point.Longitude,point.Radius);
            Assert.False(avatars.IsError,avatars.Message);Assert.NotNull(avatars.Result);
            Assert.Equal(point.Match,avatars.Result.Any(value=>value.Id==avatarId));
            var holons=provider.GetHolonsNearMe(0,point.Longitude,point.Radius,HolonType.All);
            Assert.False(holons.IsError,holons.Message);Assert.NotNull(holons.Result);
            Assert.Equal(point.Match,holons.Result.Any(value=>value.Id==holonId));
        }
        var wrongType=provider.GetHolonsNearMe(0,179999999,0,HolonType.Avatar);
        Assert.False(wrongType.IsError,wrongType.Message);Assert.NotNull(wrongType.Result);
        Assert.DoesNotContain(wrongType.Result,value=>value.Id==holonId);
    }

    internal static void RejectInvalidInputs(IOASISNETProvider provider)
    {
        Assert.True(provider.GetAvatarsNearMe(90000001,0,1).IsError);
        Assert.True(provider.GetAvatarsNearMe(0,180000001,1).IsError);
        Assert.True(provider.GetAvatarsNearMe(0,0,-1).IsError);
        Assert.True(provider.GetHolonsNearMe(0,0,1,(HolonType)int.MaxValue).IsError);
    }
}
