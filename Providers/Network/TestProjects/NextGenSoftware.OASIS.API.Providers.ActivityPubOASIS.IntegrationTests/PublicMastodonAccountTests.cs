using System.Text.Json;
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.API.Providers.ActivityPubOASIS.IntegrationTests;

/// <summary>Opt-in public read-only native HTTP proof. No token, account creation, posting or deletion.</summary>
public class PublicMastodonAccountTests
{
    [Theory][InlineData(false)][InlineData(true)]
    public async Task PublicProfileMatchesIndependentNativeResponse(bool synchronous)
    {
        Assert.Equal("1",Environment.GetEnvironmentVariable("OASIS_ACTIVITYPUB_PUBLIC_LIVE"));
        const string instance="https://mastodon.social";const string handle="Mastodon";
        using var client=new HttpClient{Timeout=TimeSpan.FromSeconds(15)};
        using var native=await client.GetAsync(instance+"/api/v1/accounts/lookup?acct="+Uri.EscapeDataString(handle));
        native.EnsureSuccessStatusCode();using var document=JsonDocument.Parse(await native.Content.ReadAsStringAsync());
        var id=document.RootElement.GetProperty("id").GetString()!;
        var acct=document.RootElement.GetProperty("acct").GetString()!;
        var profile=document.RootElement.GetProperty("url").GetString()!;
        using var provider=new ActivityPubOASIS(instance,"");
        var byName=synchronous?provider.LoadAvatarByUsername(handle):await provider.LoadAvatarByUsernameAsync(handle).WaitAsync(TimeSpan.FromSeconds(20));
        Assert.False(byName.IsError,byName.Exception?.ToString());Assert.NotEqual(Guid.Empty,byName.Result.Id);
        Assert.Equal(acct,byName.Result.Username);Assert.Equal(id,byName.Result.ProviderUniqueStorageKey[ProviderType.ActivityPubOASIS]);
        Assert.Equal(profile,byName.Result.MetaData["MastodonProfileUrl"]);
        Assert.True(string.IsNullOrEmpty(byName.Result.Email));Assert.True(string.IsNullOrEmpty(byName.Result.Password));
        var byId=synchronous?provider.LoadAvatarByProviderKey(id):await provider.LoadAvatarByProviderKeyAsync(id).WaitAsync(TimeSpan.FromSeconds(20));
        Assert.False(byId.IsError,byId.Exception?.ToString());Assert.Equal(byName.Result.Id,byId.Result.Id);
        Assert.Equal(acct,byId.Result.Username);Assert.Equal(id,byId.Result.ProviderUniqueStorageKey[ProviderType.ActivityPubOASIS]);
        var detail=synchronous?provider.LoadAvatarDetailByUsername(handle):await provider.LoadAvatarDetailByUsernameAsync(handle).WaitAsync(TimeSpan.FromSeconds(20));
        Assert.False(detail.IsError,detail.Exception?.ToString());Assert.Equal(byName.Result.Id,detail.Result.Id);
        Assert.Equal(acct,detail.Result.Username);Assert.Equal(id,detail.Result.ProviderUniqueStorageKey[ProviderType.ActivityPubOASIS]);
        Assert.Equal(profile,detail.Result.MetaData["MastodonProfileUrl"]);Assert.True(string.IsNullOrEmpty(detail.Result.Email));
    }
}
