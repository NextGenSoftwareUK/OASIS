using System.Globalization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.CosmosBlockChainOASIS.UnitTests;

[TestClass]
public sealed class CosmosNearbyTests
{
    private sealed class Records : CosmosBlockChainOASIS
    {
        public Avatar Avatar = new() { Id = Guid.NewGuid(), IsActive = true,
            MetaData = new() { ["Latitude"] = "30.0005", ["Longitude"] = "20" } };
        public Holon Holon = new() { Id = Guid.NewGuid(), IsActive = true, HolonType = HolonType.Park,
            MetaData = new() { ["Latitude"] = "30.0005", ["Longitude"] = "20" } };
        public Records() { IsProviderActivated = true; }
        public override OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version = 0)
            => new() { Result = new[] { Avatar,new Avatar{Id=Guid.NewGuid(),IsActive=true,MetaData=new(){{"Latitude","40"},{"Longitude","20"}}} }, IsLoaded = true,LoadedCount=2 };
        public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType type = HolonType.All, bool loadChildren = true,
            bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true,
            bool loadChildrenFromProvider = false, int version = 0)
            => new() { Result = new[] { Holon,new Holon{Id=Guid.NewGuid(),IsActive=true,HolonType=HolonType.Park,MetaData=new(){{"Latitude","40"},{"Longitude","20"}}} }, IsLoaded = true,LoadedCount=2 };
    }

    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public void NearbyUsesInvariantLocationAndPreservesRecord(bool avatars)
    {
        using var provider = new Records(); var net = (IOASISNETProvider)provider;
        var prior = CultureInfo.CurrentCulture;
        try {
            CultureInfo.CurrentCulture = new CultureInfo("fr-FR");
            if (avatars) {
                var result = net.GetAvatarsNearMe(30000000,20000000,100);
                Assert.IsFalse(result.IsError); Assert.IsTrue(result.IsLoaded); Assert.AreSame(provider.Avatar, result.Result.Single());
                Assert.AreEqual(1,result.LoadedCount);
                provider.Avatar.IsActive = false;var empty=net.GetAvatarsNearMe(30000000,20000000,100);Assert.AreEqual(0,empty.Result.Count());Assert.AreEqual(0,empty.LoadedCount);
            } else {
                var result = net.GetHolonsNearMe(30000000,20000000,100,HolonType.Park);
                Assert.IsFalse(result.IsError); Assert.IsTrue(result.IsLoaded); Assert.AreSame(provider.Holon, result.Result.Single());
                Assert.AreEqual(1,result.LoadedCount);
                var empty=net.GetHolonsNearMe(30000000,20000000,100,HolonType.Holon);Assert.AreEqual(0,empty.Result.Count());Assert.AreEqual(0,empty.LoadedCount);
            }
        } finally { CultureInfo.CurrentCulture = prior; }
    }

    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public void PartialCoordinatesAreNotSuccessfulAbsence(bool avatars)
    {
        using var provider = new Records(); var net = (IOASISNETProvider)provider;
        (avatars ? provider.Avatar.MetaData : provider.Holon.MetaData).Remove("Longitude");
        var cause = avatars ? net.GetAvatarsNearMe(30000000,20000000,100).Exception
            : net.GetHolonsNearMe(30000000,20000000,100,HolonType.All).Exception;
        Assert.IsInstanceOfType(cause, typeof(System.IO.InvalidDataException));
    }

    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public void InvalidRadiusRejectedBeforeProviderActivation(bool avatars)
    {
        using var provider = new CosmosBlockChainOASIS(); var net = (IOASISNETProvider)provider;
        var cause = avatars ? net.GetAvatarsNearMe(0,0,-1).Exception : net.GetHolonsNearMe(0,0,-1,HolonType.All).Exception;
        Assert.IsInstanceOfType(cause, typeof(ArgumentOutOfRangeException)); Assert.IsFalse(provider.IsProviderActivated);
    }
}
