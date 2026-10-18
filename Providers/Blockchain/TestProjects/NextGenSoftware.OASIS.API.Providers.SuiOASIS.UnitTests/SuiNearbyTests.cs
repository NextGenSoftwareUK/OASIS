using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.SuiOASIS.UnitTests;

[TestClass]
public sealed class SuiNearbyTests
{
    private sealed class Records : SuiOASIS
    {
        public Avatar Avatar = new() { Id = Guid.NewGuid(), IsActive = true,
            MetaData = new() { ["Latitude"] = "30.0005", ["Longitude"] = "20" } };
        public Holon Holon = new() { Id = Guid.NewGuid(), IsActive = true, HolonType = HolonType.Park,
            MetaData = new() { ["Latitude"] = "30.0005", ["Longitude"] = "20" } };
        public override Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0)
            => Task.FromResult(new OASISResult<IEnumerable<IAvatar>> { Result = new[] { Avatar }, IsLoaded = true });
        public override Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type = HolonType.All, bool loadChildren = true,
            bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true,
            bool loadChildrenFromProvider = false, int version = 0)
            => Task.FromResult(new OASISResult<IEnumerable<IHolon>> { Result = new[] { Holon }, IsLoaded = true });
    }

    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public void NearbyPreservesRecordAndExcludesInactiveOrDeleted(bool avatars)
    {
        using var provider = new Records(); var net = (IOASISNETProvider)provider;
        if (avatars) {
            var found = net.GetAvatarsNearMe(30000000,20000000,100);
            Assert.IsFalse(found.IsError); Assert.AreSame(provider.Avatar, found.Result.Single());
            provider.Avatar.IsActive = false;
            Assert.AreEqual(0, net.GetAvatarsNearMe(30000000,20000000,100).Result.Count());
            provider.Avatar.IsActive = true; provider.Avatar.DeletedDate = DateTime.UtcNow;
            Assert.AreEqual(0, net.GetAvatarsNearMe(30000000,20000000,100).Result.Count());
        } else {
            var found = net.GetHolonsNearMe(30000000,20000000,100,HolonType.Park);
            Assert.IsFalse(found.IsError); Assert.AreSame(provider.Holon, found.Result.Single());
            Assert.AreEqual(0, net.GetHolonsNearMe(30000000,20000000,100,HolonType.Holon).Result.Count());
            provider.Holon.IsActive = false;
            Assert.AreEqual(0, net.GetHolonsNearMe(30000000,20000000,100,HolonType.All).Result.Count());
        }
    }

    [DataTestMethod]
    [DataRow(false, "NaN")] [DataRow(true, "NaN")]
    [DataRow(false, "partial")] [DataRow(true, "partial")]
    public void CorruptLocationCannotReturnSuccessfulOrUnfilteredPayload(bool avatars, string value)
    {
        using var provider = new Records(); var net = (IOASISNETProvider)provider;
        var metadata = avatars ? provider.Avatar.MetaData : provider.Holon.MetaData;
        if (value == "partial") metadata.Remove("Longitude"); else metadata["Latitude"] = value;
        if (avatars) {
            var result = net.GetAvatarsNearMe(30000000,20000000,100);
            Assert.IsTrue(result.IsError); Assert.IsFalse(result.IsLoaded); Assert.IsNull(result.Result);
            Assert.IsInstanceOfType(result.Exception, typeof(System.IO.InvalidDataException));
        } else {
            var result = net.GetHolonsNearMe(30000000,20000000,100,HolonType.All);
            Assert.IsTrue(result.IsError); Assert.IsFalse(result.IsLoaded); Assert.IsNull(result.Result);
            Assert.IsInstanceOfType(result.Exception, typeof(System.IO.InvalidDataException));
        }
    }
}
