using System.Text.Json;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Objects.Search;

namespace NextGenSoftware.OASIS.API.Providers.ActivityPubOASIS.IntegrationTests;

/// <summary>Explicit authorized read-only gate. Never provisions tokens or resolves unknown remote actors.</summary>
public class AuthorizedMastodonAccountSearchTests
{
    [Fact]
    public async Task PagedAccountsMatchNativeFirstPageAndCanonicalPointProjection()
    {
        Assert.Equal("1",Environment.GetEnvironmentVariable("OASIS_ACTIVITYPUB_SEARCH_LIVE"));
        var origin=Environment.GetEnvironmentVariable("OASIS_ACTIVITYPUB_SEARCH_INSTANCE")??throw new InvalidOperationException("Explicit test instance required.");
        var token=Environment.GetEnvironmentVariable("OASIS_ACTIVITYPUB_SEARCH_TOKEN")??throw new InvalidOperationException("Protected read:search user token required.");
        var query=Environment.GetEnvironmentVariable("OASIS_ACTIVITYPUB_SEARCH_QUERY")??throw new InvalidOperationException("Narrow public-handle test query required,not a private email.");
        Assert.False(string.IsNullOrWhiteSpace(token));Assert.False(string.IsNullOrWhiteSpace(query));
        using var provider=new ActivityPubOASIS(origin,token);
        using var client=new HttpClient{BaseAddress=new Uri(origin),Timeout=TimeSpan.FromSeconds(20)};
        client.DefaultRequestHeaders.Authorization=new("Bearer",token);
        using var native=await client.GetAsync("/api/v2/search?q="+Uri.EscapeDataString(query)+"&type=accounts&limit=40&offset=0&resolve=false");native.EnsureSuccessStatusCode();
        using var document=JsonDocument.Parse(await native.Content.ReadAsStringAsync());var first=document.RootElement.GetProperty("accounts").EnumerateArray().ToArray();Assert.NotEmpty(first);
        var parameters=new SearchParams{SearchOnlyForCurrentAvatar=false,SearchGroups=new(){new SearchTextGroup{SearchAvatars=true,SearchQuery=query}}};
        var result=await provider.SearchAsync(parameters,loadChildren:false).WaitAsync(TimeSpan.FromSeconds(120));
        Assert.False(result.IsError,result.Exception?.ToString());Assert.True(result.IsLoaded);Assert.Equal(result.Result.SearchResultAvatars.Count,result.LoadedCount);Assert.Empty(result.Result.SearchResultHolons);
        foreach(var account in first){var id=account.GetProperty("id").GetString()!;var projected=Assert.Single(result.Result.SearchResultAvatars.Where(row=>row.ProviderUniqueStorageKey[ProviderType.ActivityPubOASIS]==id));Assert.Equal(account.GetProperty("acct").GetString(),projected.Username);Assert.True(string.IsNullOrEmpty(projected.Email));}
        var selected=first[0].GetProperty("id").GetString()!;var point=await provider.LoadAvatarByProviderKeyAsync(selected);
        Assert.False(point.IsError,point.Exception?.ToString());Assert.Equal(point.Result.Id,Assert.Single(result.Result.SearchResultAvatars.Where(row=>row.ProviderUniqueStorageKey[ProviderType.ActivityPubOASIS]==selected)).Id);
    }
}
