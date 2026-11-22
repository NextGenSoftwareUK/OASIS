using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Seal;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Subscription;
using Xunit;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

public class HolonSealPolicyTests
{
    private static HolonSealPolicy Policy(bool enabled, string key = "a-long-random-secret-value-0123456789")
        => new HolonSealPolicy(() => new HolonSealSettings { Enabled = enabled, Key = key });

    [Fact]
    public void DnaDefaultsAreOffAndKeyless()
    {
        var d = new HolonSealSettings();
        Assert.False(d.Enabled); Assert.Equal("", d.Key); Assert.Equal("k1", d.KeyId); Assert.Empty(d.RetiredKeys);
        Assert.False(new OASISDNA().OASIS.HolonSeal.Enabled);
        Assert.Null(new AppUsageOverrides().SealedHistoryEnabled);
    }

    [Fact]
    public void NothingIsSealedByDefault()
    {
        var p = Policy(false);
        var owner = Guid.NewGuid(); p.SetOwner(owner, true); p.ReplaceApps(new[] { "piper" });
        Assert.False(p.IsAvailable);
        Assert.False(p.ShouldSeal(owner, "piper")); // master switch off beats both switches
    }

    [Fact]
    public void MasterOnWithoutAKeyIsUnavailable()
    {
        var p = Policy(true, "");
        var owner = Guid.NewGuid(); p.SetOwner(owner, true);
        Assert.False(p.IsAvailable); Assert.False(p.ShouldSeal(owner, null));
    }

    [Fact]
    public void AvatarOrAppSwitchSealsAndNeitherLowersTheOther()
    {
        var p = Policy(true); var a = Guid.NewGuid(); var b = Guid.NewGuid();
        Assert.False(p.ShouldSeal(a, "piper"));                 // neither
        p.SetOwner(a, true);
        Assert.True(p.ShouldSeal(a, null));                     // avatar only
        Assert.True(p.ShouldSeal(a, "other"));                  // avatar on, app not
        Assert.False(p.ShouldSeal(b, "piper"));                 // other avatar, app not on
        p.ReplaceApps(new[] { "piper" });
        Assert.True(p.ShouldSeal(b, "PIPER"));                  // app only (case-insensitive)
        Assert.True(p.ShouldSeal(a, "piper"));                  // both
        p.SetOwner(a, false);
        Assert.False(p.ShouldSeal(a, "other"));                 // avatar turned off again
        Assert.True(p.ShouldSeal(a, "piper"));                  // app still requires it
    }

    [Fact]
    public void ReplaceOwnersFollowsTheStore()
    {
        var p = Policy(true); var a = Guid.NewGuid(); var b = Guid.NewGuid();
        p.ReplaceOwners(new[] { a }); Assert.True(p.OwnerSwitch(a)); Assert.False(p.OwnerSwitch(b));
        p.ReplaceOwners(new[] { b }); Assert.False(p.OwnerSwitch(a)); Assert.True(p.OwnerSwitch(b));
    }

    [Fact]
    public void RetiredKeysStillVerifyButNeverSign()
    {
        var p = new HolonSealPolicy(() => new HolonSealSettings { Enabled = true, Key = "new-secret", KeyId = "k2", RetiredKeys = new Dictionary<string, string> { ["k1"] = "old-secret" } });
        Assert.Equal("k2", p.SigningKey.KeyId);
        Assert.Equal("old-secret", p.FindKey("k1").Secret);
        Assert.Null(p.FindKey("k9"));
    }
}
