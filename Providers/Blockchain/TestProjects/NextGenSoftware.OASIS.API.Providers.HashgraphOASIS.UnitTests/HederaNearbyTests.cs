using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.HashgraphOASIS.UnitTests;

[TestClass]
public sealed class HederaNearbyTests
{
    private sealed class Records : HashgraphOASIS
    {
        public int Activations, Reads;
        public Exception? Failure;
        public Avatar Avatar = new() { Id = Guid.NewGuid(), IsActive = true,
            MetaData = new() { ["Latitude"] = "30.0005", ["Longitude"] = "20" } };
        public Holon Holon = new() { Id = Guid.NewGuid(), IsActive = true, HolonType = HolonType.Park,
            MetaData = new() { ["Latitude"] = "30.0005", ["Longitude"] = "20" } };
        public override Task<OASISResult<bool>> ActivateProviderAsync() {
            Activations++; IsProviderActivated = true; return Task.FromResult(new OASISResult<bool> { Result = true });
        }
        public override Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0) {
            Reads++; return Task.FromResult(new OASISResult<IEnumerable<IAvatar>> { Result = Failure == null ? new[] { Avatar } : null,
                IsError = Failure != null, Exception = Failure, ErrorCode = Failure == null ? null : "CONTROLLED_READ", IsLoaded = Failure == null });
        }
        public override Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type = HolonType.All, bool loadChildren = true,
            bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true,
            bool loadChildrenFromProvider = false, int version = 0) {
            Reads++; return Task.FromResult(new OASISResult<IEnumerable<IHolon>> { Result = Failure == null ? new[] { Holon } : null,
                IsError = Failure != null, Exception = Failure, ErrorCode = Failure == null ? null : "CONTROLLED_READ", IsLoaded = Failure == null });
        }
    }

    [DataTestMethod]
    [DataRow(false, false)] [DataRow(false, true)] [DataRow(true, false)] [DataRow(true, true)]
    public async Task NearbyUsesStoredRecordsInsteadOfEmptyMirrorPlaceholder(bool avatars, bool synchronous)
    {
        var provider = new Records();
        if (avatars) {
            var result = synchronous ? provider.GetAvatarsNearMe(30000000,20000000,100)
                : await provider.GetAvatarsNearMeAsync(30000000,20000000,100);
            Assert.IsFalse(result.IsError, result.Message); Assert.IsTrue(result.IsLoaded);
            Assert.AreSame(provider.Avatar, result.Result.Single());
        } else {
            var result = synchronous ? provider.GetHolonsNearMe(30000000,20000000,100,HolonType.Park)
                : await provider.GetHolonsNearMeAsync(30000000,20000000,100,HolonType.Park);
            Assert.IsFalse(result.IsError, result.Message); Assert.IsTrue(result.IsLoaded);
            Assert.AreSame(provider.Holon, result.Result.Single());
        }
        Assert.AreEqual(1, provider.Activations); Assert.AreEqual(1, provider.Reads);
    }

    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public async Task InvalidRadiusFailsBeforeActivationOrStorage(bool avatars)
    {
        var provider = new Records();
        var cause = avatars ? (await provider.GetAvatarsNearMeAsync(0,0,-1)).Exception
            : (await provider.GetHolonsNearMeAsync(0,0,-1)).Exception;
        Assert.IsInstanceOfType(cause, typeof(ArgumentOutOfRangeException));
        Assert.AreEqual(0, provider.Activations); Assert.AreEqual(0, provider.Reads);
    }

    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public async Task StoredReadFailureRetainsOriginalEvidence(bool avatars)
    {
        var cause = new System.IO.IOException("controlled storage unavailable"); var provider = new Records { Failure = cause };
        if (avatars) {
            var result = await provider.GetAvatarsNearMeAsync(30000000,20000000,100);
            Assert.IsTrue(result.IsError); Assert.AreSame(cause, result.Exception); Assert.AreEqual("CONTROLLED_READ", result.ErrorCode); Assert.IsNull(result.Result);
        } else {
            var result = await provider.GetHolonsNearMeAsync(30000000,20000000,100);
            Assert.IsTrue(result.IsError); Assert.AreSame(cause, result.Exception); Assert.AreEqual("CONTROLLED_READ", result.ErrorCode); Assert.IsNull(result.Result);
        }
    }
}
