using System;
using NextGenSoftware.OASIS.API.Core.Managers;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public class AvatarLoginTests
{
    [Theory]
    [InlineData("alice@example.com", true, false)]
    [InlineData("alice", false, false)]
    [InlineData("nobody-probe", false, false)]
    [InlineData("0x71C7656EC7ab88b098defB751B7401B5f6d8976F", false, true)]
    [InlineData("7xKXtg2CW87d97TXJSDpbD5jBkheTqA83TZRuJosgAsU", false, true)]
    [InlineData("", false, false)]
    [InlineData(null, false, false)]
    public void IdentifierShapeDecidesWhichLookupsRun(string identifier, bool email, bool publicKey)
    {
        Assert.Equal(email, AvatarLoginIdentifier.LooksLikeEmail(identifier));
        Assert.Equal(publicKey, AvatarLoginIdentifier.LooksLikePublicKey(identifier));
    }

    [Fact]
    public void AnOrdinaryUsernameNeverRunsTheLoadEveryAvatarPublicKeyLookup()
    {
        Assert.False(AvatarLoginIdentifier.LooksLikePublicKey("a-regular-username"));
        Assert.False(AvatarLoginIdentifier.LooksLikePublicKey("short"));
    }

    [Fact]
    public void BreakerTripsForItsWindowThenRecoversAndResetsOnSuccess()
    {
        var now = new DateTime(2026, 11, 18, 0, 0, 0, DateTimeKind.Utc);
        var breaker = new AvatarLoginBreaker(() => now, TimeSpan.FromSeconds(45));
        Assert.False(breaker.IsTripped);

        breaker.Trip();
        Assert.True(breaker.IsTripped);

        now = now.AddSeconds(44);
        Assert.True(breaker.IsTripped);
        now = now.AddSeconds(2);
        Assert.False(breaker.IsTripped);

        breaker.Trip();
        Assert.True(breaker.IsTripped);
        breaker.Reset();
        Assert.False(breaker.IsTripped);
    }

    [Fact]
    public void LoginTimeoutDefaultsToTenSeconds()
        => Assert.Equal(10, new NextGenSoftware.OASIS.API.DNA.StorageProviderSettings().AvatarLoginTimeoutSeconds);
}
