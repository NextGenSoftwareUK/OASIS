using System.Text.Json;
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.API.Providers.ActivityPubOASIS.IntegrationTests;

/// <summary>Opt-in public read-only native HTTP proof. No token, account creation, posting or deletion.</summary>
public class PublicMastodonAccountTests
{
    [Theory][InlineData(false)][InlineData(true)]
    public async Task PublicReplyGraphMatchesIndependentNativeContext(bool synchronous)
    {
        Assert.Equal("1",Environment.GetEnvironmentVariable("OASIS_ACTIVITYPUB_PUBLIC_LIVE"));
        const string instance="https://mastodon.social";const string id="117015608750780726";
        using var client=new HttpClient{Timeout=TimeSpan.FromSeconds(15)};
        using var nativeResponse=await client.GetAsync(instance+"/api/v1/statuses/"+id+"/context");nativeResponse.EnsureSuccessStatusCode();
        using var context=JsonDocument.Parse(await nativeResponse.Content.ReadAsStringAsync());
        var descendants=context.RootElement.GetProperty("descendants").EnumerateArray().ToArray();
        Assert.NotEmpty(descendants);
        // Retained public fixture is a small direct-reply thread; fail if its scope changes.
        Assert.All(descendants,status=>Assert.Equal(id,status.GetProperty("in_reply_to_id").GetString()));
        using var provider=new ActivityPubOASIS(instance,"");
        var result=synchronous?provider.LoadHolon(id,loadChildrenFromProvider:true)
            :await provider.LoadHolonAsync(id,loadChildrenFromProvider:true).WaitAsync(TimeSpan.FromSeconds(30));
        Assert.False(result.IsError,result.Exception?.ToString());
        Assert.Equal(descendants.Select(status=>status.GetProperty("id").GetString()).OrderBy(value=>value),
            result.Result.Children.Select(status=>status.ProviderUniqueStorageKey[ProviderType.ActivityPubOASIS]).OrderBy(value=>value));
        foreach(var child in result.Result.Children)
        {
            Assert.Equal(result.Result.Id,child.ParentHolonId);Assert.Empty(child.Children);
            var native=descendants.Single(status=>status.GetProperty("id").GetString()==child.ProviderUniqueStorageKey[ProviderType.ActivityPubOASIS]);
            Assert.Equal(native.GetProperty("uri").GetString(),child.MetaData["MastodonObjectUri"]?.ToString());
            Assert.Equal(native.GetProperty("content").GetString(),child.MetaData["MastodonContentHtml"]?.ToString());
        }
    }

    [Theory][InlineData(false)][InlineData(true)]
    public async Task PublicStatusMatchesIndependentNativeResponse(bool synchronous)
    {
        Assert.Equal("1",Environment.GetEnvironmentVariable("OASIS_ACTIVITYPUB_PUBLIC_LIVE"));
        const string instance="https://mastodon.social";
        using var client=new HttpClient{Timeout=TimeSpan.FromSeconds(15)};
        using var accountResponse=await client.GetAsync(instance+"/api/v1/accounts/lookup?acct=Mastodon");accountResponse.EnsureSuccessStatusCode();
        using var account=JsonDocument.Parse(await accountResponse.Content.ReadAsStringAsync());var accountId=account.RootElement.GetProperty("id").GetString()!;
        using var statusesResponse=await client.GetAsync(instance+"/api/v1/accounts/"+Uri.EscapeDataString(accountId)+"/statuses?limit=1");statusesResponse.EnsureSuccessStatusCode();
        using var statuses=JsonDocument.Parse(await statusesResponse.Content.ReadAsStringAsync());Assert.True(statuses.RootElement.GetArrayLength()>0);
        var id=statuses.RootElement[0].GetProperty("id").GetString()!;
        using var statusResponse=await client.GetAsync(instance+"/api/v1/statuses/"+Uri.EscapeDataString(id));statusResponse.EnsureSuccessStatusCode();
        using var status=JsonDocument.Parse(await statusResponse.Content.ReadAsStringAsync());
        using var provider=new ActivityPubOASIS(instance,"");
        var result=synchronous?provider.LoadHolon(id,loadChildren:false):await provider.LoadHolonAsync(id,loadChildren:false).WaitAsync(TimeSpan.FromSeconds(20));
        Assert.False(result.IsError,result.Exception?.ToString());Assert.NotEqual(Guid.Empty,result.Result.Id);
        Assert.Equal(id,result.Result.ProviderUniqueStorageKey[ProviderType.ActivityPubOASIS]);
        Assert.Equal(status.RootElement.GetProperty("uri").GetString(),result.Result.MetaData["MastodonObjectUri"]);
        Assert.Equal(status.RootElement.GetProperty("content").GetString(),result.Result.MetaData["MastodonContentHtml"]);
        Assert.Equal(status.RootElement.GetProperty("account").GetProperty("id").GetString(),result.Result.MetaData["MastodonAuthorId"]);
    }
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
