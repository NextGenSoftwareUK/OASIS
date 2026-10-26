using System.Text.Json;
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.API.Providers.ActivityPubOASIS.IntegrationTests;

/// <summary>Opt-in public read-only native HTTP proof. No token, account creation, posting or deletion.</summary>
public class PublicMastodonAccountTests
{
    [Theory][InlineData(false)][InlineData(true)]
    public async Task PublicAccountAndDetailBatchesMatchIndependentNativeResponse(bool synchronous)
    {
        Assert.Equal("1",Environment.GetEnvironmentVariable("OASIS_ACTIVITYPUB_PUBLIC_LIVE"));
        const string instance="https://mastodon.social";
        using var client=new HttpClient{Timeout=TimeSpan.FromSeconds(15)};
        using var lookup=await client.GetAsync(instance+"/api/v1/accounts/lookup?acct=Mastodon");lookup.EnsureSuccessStatusCode();
        using var selected=JsonDocument.Parse(await lookup.Content.ReadAsStringAsync());var id=selected.RootElement.GetProperty("id").GetString()!;
        using var response=await client.GetAsync(instance+"/api/v1/accounts?id%5B%5D="+Uri.EscapeDataString(id));response.EnsureSuccessStatusCode();
        using var native=JsonDocument.Parse(await response.Content.ReadAsStringAsync());Assert.Equal(1,native.RootElement.GetArrayLength());
        var expected=native.RootElement[0];Assert.Equal(id,expected.GetProperty("id").GetString());
        using var provider=new ActivityPubOASIS(instance,"");
        var accounts=synchronous?provider.LoadAccountBatch(new[]{id}):await provider.LoadAccountBatchAsync(new[]{id}).WaitAsync(TimeSpan.FromSeconds(20));
        Assert.False(accounts.IsError,accounts.Exception?.ToString());Assert.True(accounts.IsLoaded);Assert.Equal(1,accounts.LoadedCount);
        var avatar=Assert.Single(accounts.Result);Assert.Equal(id,avatar.ProviderUniqueStorageKey[ProviderType.ActivityPubOASIS]);
        Assert.Equal(expected.GetProperty("acct").GetString(),avatar.Username);Assert.Equal(expected.GetProperty("url").GetString(),avatar.MetaData["MastodonProfileUrl"]);
        Assert.NotEqual(Guid.Empty,avatar.Id);Assert.True(string.IsNullOrEmpty(avatar.Email));Assert.True(string.IsNullOrEmpty(avatar.Password));
        var details=synchronous?provider.LoadAccountDetailBatch(new[]{id}):await provider.LoadAccountDetailBatchAsync(new[]{id}).WaitAsync(TimeSpan.FromSeconds(20));
        Assert.False(details.IsError,details.Exception?.ToString());Assert.True(details.IsLoaded);Assert.Equal(1,details.LoadedCount);
        var detail=Assert.Single(details.Result);Assert.Equal(avatar.Id,detail.Id);Assert.Equal(avatar.Username,detail.Username);
        Assert.Equal(id,detail.ProviderUniqueStorageKey[ProviderType.ActivityPubOASIS]);Assert.Equal(avatar.MetaData["MastodonProfileUrl"],detail.MetaData["MastodonProfileUrl"]);
        Assert.True(string.IsNullOrEmpty(detail.Email));
    }

    [Theory][InlineData(false)][InlineData(true)]
    public async Task PublicStatusBatchMatchesIndependentNativeResponse(bool synchronous)
    {
        Assert.Equal("1",Environment.GetEnvironmentVariable("OASIS_ACTIVITYPUB_PUBLIC_LIVE"));
        const string instance="https://mastodon.social";
        using var client=new HttpClient{Timeout=TimeSpan.FromSeconds(15)};
        using var accountResponse=await client.GetAsync(instance+"/api/v1/accounts/lookup?acct=Mastodon");accountResponse.EnsureSuccessStatusCode();
        using var account=JsonDocument.Parse(await accountResponse.Content.ReadAsStringAsync());
        using var statusesResponse=await client.GetAsync(instance+"/api/v1/accounts/"+account.RootElement.GetProperty("id").GetString()+"/statuses?limit=2");statusesResponse.EnsureSuccessStatusCode();
        using var statuses=JsonDocument.Parse(await statusesResponse.Content.ReadAsStringAsync());
        var ids=statuses.RootElement.EnumerateArray().Select(row=>row.GetProperty("id").GetString()!).ToArray();Assert.Equal(2,ids.Length);
        var query=string.Join("&",ids.Select(id=>"id%5B%5D="+Uri.EscapeDataString(id)));
        using var nativeResponse=await client.GetAsync(instance+"/api/v1/statuses?"+query);nativeResponse.EnsureSuccessStatusCode();
        using var native=JsonDocument.Parse(await nativeResponse.Content.ReadAsStringAsync());
        var expected=native.RootElement.EnumerateArray().ToDictionary(row=>row.GetProperty("id").GetString()!,row=>row.Clone());Assert.Equal(2,expected.Count);
        using var provider=new ActivityPubOASIS(instance,"");
        var result=synchronous?provider.LoadStatusBatch(ids):await provider.LoadStatusBatchAsync(ids).WaitAsync(TimeSpan.FromSeconds(20));
        Assert.False(result.IsError,result.Exception?.ToString());Assert.True(result.IsLoaded);Assert.Equal(expected.Count,result.LoadedCount);
        Assert.Equal(ids.Where(expected.ContainsKey),result.Result.Select(row=>row.ProviderUniqueStorageKey[ProviderType.ActivityPubOASIS]));
        foreach(var row in result.Result){
            var status=expected[row.ProviderUniqueStorageKey[ProviderType.ActivityPubOASIS]];
            Assert.Equal(status.GetProperty("uri").GetString(),row.MetaData["MastodonObjectUri"]);
            Assert.Equal(status.GetProperty("content").GetString(),row.MetaData["MastodonContentHtml"]);
            Assert.Equal(status.GetProperty("account").GetProperty("id").GetString(),row.MetaData["MastodonAuthorId"]);
            Assert.Empty(row.Children);
        }
    }

    [Theory][InlineData(false)][InlineData(true)]
    public async Task PublicTimelinePageMatchesIndependentNativeCursor(bool synchronous)
    {
        Assert.Equal("1",Environment.GetEnvironmentVariable("OASIS_ACTIVITYPUB_PUBLIC_LIVE"));
        // Explicit public-preview fixture; mastodon.social requires authentication for this endpoint.
        const string instance="https://hachyderm.io";
        using var client=new HttpClient{Timeout=TimeSpan.FromSeconds(15)};
        using var initial=await client.GetAsync(instance+"/api/v1/timelines/public?limit=1&local=true");initial.EnsureSuccessStatusCode();
        using var first=JsonDocument.Parse(await initial.Content.ReadAsStringAsync());
        Assert.NotEqual(0,first.RootElement.GetArrayLength());var cursor=first.RootElement[0].GetProperty("id").GetString()!;
        using var response=await client.GetAsync(instance+"/api/v1/timelines/public?limit=2&local=true&max_id="+cursor);response.EnsureSuccessStatusCode();
        using var native=JsonDocument.Parse(await response.Content.ReadAsStringAsync());var rows=native.RootElement.EnumerateArray().ToArray();Assert.NotEmpty(rows);
        using var provider=new ActivityPubOASIS(instance,"");
        var result=synchronous?provider.LoadPublicTimelinePage(cursor,2,true):await provider.LoadPublicTimelinePageAsync(cursor,2,true).WaitAsync(TimeSpan.FromSeconds(20));
        Assert.False(result.IsError,result.Exception?.ToString());
        Assert.Equal(rows.Select(row=>row.GetProperty("id").GetString()),result.Result.Statuses.Select(row=>row.ProviderUniqueStorageKey[ProviderType.ActivityPubOASIS]));
        Assert.Equal(rows[^1].GetProperty("id").GetString(),result.Result.OlderThanId);
        for(var index=0;index<rows.Length;index++)Assert.Equal(rows[index].GetProperty("uri").GetString(),result.Result.Statuses[index].MetaData["MastodonObjectUri"]);
    }
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
        Assert.True(result.IsLoaded);Assert.Equal(1,result.LoadedCount);
        Assert.Equal(descendants.Select(status=>status.GetProperty("id").GetString()).OrderBy(value=>value),
            result.Result.Children.Select(status=>status.ProviderUniqueStorageKey[ProviderType.ActivityPubOASIS]).OrderBy(value=>value));
        foreach(var child in result.Result.Children)
        {
            Assert.Equal(result.Result.Id,child.ParentHolonId);Assert.Empty(child.Children);
            var native=descendants.Single(status=>status.GetProperty("id").GetString()==child.ProviderUniqueStorageKey[ProviderType.ActivityPubOASIS]);
            Assert.Equal(native.GetProperty("uri").GetString(),child.MetaData["MastodonObjectUri"]?.ToString());
            Assert.Equal(native.GetProperty("content").GetString(),child.MetaData["MastodonContentHtml"]?.ToString());
        }
        var direct=synchronous?provider.LoadHolonsForParent(id,loadChildren:false)
            :await provider.LoadHolonsForParentAsync(id,loadChildren:false).WaitAsync(TimeSpan.FromSeconds(30));
        Assert.False(direct.IsError,direct.Exception?.ToString());
        Assert.True(direct.IsLoaded);Assert.Equal(descendants.Length,direct.LoadedCount);
        Assert.Equal(descendants.Select(status=>status.GetProperty("id").GetString()).OrderBy(value=>value),
            direct.Result.Select(status=>status.ProviderUniqueStorageKey[ProviderType.ActivityPubOASIS]).OrderBy(value=>value));
        Assert.All(direct.Result,status=>{Assert.Equal(result.Result.Id,status.ParentHolonId);Assert.Empty(status.Children);});
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
        var authorId=status.RootElement.GetProperty("account").GetProperty("id").GetString()!;
        var author=synchronous?provider.LoadAvatarByProviderKey(authorId):await provider.LoadAvatarByProviderKeyAsync(authorId).WaitAsync(TimeSpan.FromSeconds(20));
        Assert.False(author.IsError,author.Exception?.ToString());Assert.Equal(author.Result.Id,result.Result.CreatedByAvatarId);
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
