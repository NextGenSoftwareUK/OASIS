using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Enums;
using System.Reflection;

namespace NextGenSoftware.OASIS.API.Providers.NEAROASIS.UnitTests;

[TestClass][DoNotParallelize]
public class NearRejectedSaveTests
{
    private static NEAROASIS Unavailable()=>new("http://127.0.0.1:1","unused","unused","unused","","");

    [TestMethod]
    public async Task StoragePreflightFailureAfterActivationPreservesProviderLinksAndVersions()
    {
        const string setting="OASIS_NEAR_STORAGE_ENCRYPTION_KEY";
        var previous=Environment.GetEnvironmentVariable(setting);
        try
        {
            Environment.SetEnvironmentVariable(setting,null);
            var provider=Unavailable();
            // Controlled activation state: test record preflight, NOT live authentication.
            typeof(NEAROASIS).GetField("_isActivated",BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(provider,true);
            var avatar=new Avatar{Version=7,VersionId=Guid.NewGuid()};
            avatar.ProviderUniqueStorageKey[ProviderType.NEAROASIS]="existing-avatar-link";
            var avatarStamp=avatar.VersionId;
            var avatarResult=await provider.SaveAvatarAsync(avatar);
            Assert.IsTrue(avatarResult.IsError);
            StringAssert.Contains(avatarResult.Exception.Message,"configured separately from the signing key");
            Assert.AreEqual(Guid.Empty,avatar.Id);Assert.AreEqual(7,avatar.Version);
            Assert.AreEqual(avatarStamp,avatar.VersionId);
            Assert.AreEqual("existing-avatar-link",avatar.ProviderUniqueStorageKey[ProviderType.NEAROASIS]);
            var detail=new AvatarDetail{Version=9,VersionId=Guid.NewGuid(),Karma=23};
            detail.ProviderUniqueStorageKey[ProviderType.NEAROASIS]="existing-detail-link";
            var detailStamp=detail.VersionId;
            var detailResult=await provider.SaveAvatarDetailAsync(detail);
            Assert.IsTrue(detailResult.IsError);
            StringAssert.Contains(detailResult.Exception.Message,"configured separately from the signing key");
            Assert.AreEqual(Guid.Empty,detail.Id);Assert.AreEqual(9,detail.Version);
            Assert.AreEqual(detailStamp,detail.VersionId);
            Assert.AreEqual("existing-detail-link",detail.ProviderUniqueStorageKey[ProviderType.NEAROASIS]);
            Assert.AreEqual(23L,detail.Karma);
        }
        finally{Environment.SetEnvironmentVariable(setting,previous);}
    }

    [TestMethod]
    public async Task RejectedAvatarSavePreservesCallerIdentityAndStamps()
    {
        var avatar=new Avatar{Username="rejected-avatar",Version=7,VersionId=Guid.NewGuid()};
        var versionId=avatar.VersionId;
        var result=await Unavailable().SaveAvatarAsync(avatar);
        Assert.IsTrue(result.IsError);
        Assert.AreEqual(Guid.Empty,avatar.Id);
        Assert.AreEqual(7,avatar.Version);Assert.AreEqual(versionId,avatar.VersionId);
    }

    [TestMethod]
    public async Task RejectedDetailSavePreservesCallerIdentityAndStamps()
    {
        var detail=new AvatarDetail{Username="rejected-detail",Version=7,VersionId=Guid.NewGuid(),Karma=23};
        var versionId=detail.VersionId;
        var result=await Unavailable().SaveAvatarDetailAsync(detail);
        Assert.IsTrue(result.IsError);
        Assert.AreEqual(Guid.Empty,detail.Id);
        Assert.AreEqual(7,detail.Version);Assert.AreEqual(versionId,detail.VersionId);
        Assert.AreEqual(23L,detail.Karma);
    }
}
