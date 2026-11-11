using System.Text.Json;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Objects.Search;

namespace NextGenSoftware.OASIS.API.Providers.ActivityPubOASIS.IntegrationTests;

/// <summary>Explicit authorized read-only gate. Never provisions tokens or resolves unknown remote actors.</summary>
public class AuthorizedMastodonAccountSearchTests
{
    [Fact]
    public async Task PagedAccountsMatchCompleteNativeSearchAndCanonicalPointProjection()
    {
        Assert.Equal("1",Environment.GetEnvironmentVariable("OASIS_ACTIVITYPUB_SEARCH_LIVE"));
        var origin=Environment.GetEnvironmentVariable("OASIS_ACTIVITYPUB_SEARCH_INSTANCE")??throw new InvalidOperationException("Explicit test instance required.");
        var token=Environment.GetEnvironmentVariable("OASIS_ACTIVITYPUB_SEARCH_TOKEN")??throw new InvalidOperationException("Protected read:search user token required.");
        var query=Environment.GetEnvironmentVariable("OASIS_ACTIVITYPUB_SEARCH_QUERY")??throw new InvalidOperationException("Narrow public-handle test query required,not a private email.");
        Assert.False(string.IsNullOrWhiteSpace(token));Assert.False(string.IsNullOrWhiteSpace(query));
        using var provider=new ActivityPubOASIS(origin,token);
        Assert.True(Uri.TryCreate(origin, UriKind.Absolute, out var endpoint) &&
            endpoint.Scheme == Uri.UriSchemeHttps && endpoint.AbsolutePath == "/" &&
            string.IsNullOrEmpty(endpoint.UserInfo) && string.IsNullOrEmpty(endpoint.Query) &&
            string.IsNullOrEmpty(endpoint.Fragment), "Configure the HTTPS origin of the authorized test instance.");
        using var client = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false })
        {
            BaseAddress = endpoint,
            Timeout = TimeSpan.FromSeconds(20)
        };
        client.DefaultRequestHeaders.Authorization=new("Bearer",token);
        var expected = new Dictionary<string, string>(StringComparer.Ordinal);
        var exhausted = false;
        using var deadline = new CancellationTokenSource(TimeSpan.FromMinutes(2));
        for (var offset = 0; offset < 4000; offset += 40)
        {
            using var native = await client.GetAsync(
                $"/api/v2/search?q={Uri.EscapeDataString(query)}&type=accounts&limit=40&offset={offset}&resolve=false",
                deadline.Token);
            native.EnsureSuccessStatusCode();
            using var document = JsonDocument.Parse(await native.Content.ReadAsStringAsync(deadline.Token));
            var accounts = document.RootElement.GetProperty("accounts");
            Assert.InRange(accounts.GetArrayLength(), 0, 40);
            if (accounts.GetArrayLength() == 0)
            {
                exhausted = true;
                break;
            }
            foreach (var account in accounts.EnumerateArray())
                Assert.True(expected.TryAdd(account.GetProperty("id").GetString()!, account.GetProperty("acct").GetString()!));
        }
        Assert.True(exhausted, "Native search must reach an empty page within the safety bound.");
        Assert.NotEmpty(expected);
        var parameters=new SearchParams{SearchOnlyForCurrentAvatar=false,SearchGroups=new(){new SearchTextGroup{SearchAvatars=true,SearchQuery=query}}};
        var result=await provider.SearchAsync(parameters,loadChildren:false).WaitAsync(TimeSpan.FromSeconds(120));
        Assert.False(result.IsError,result.Exception?.ToString());Assert.True(result.IsLoaded);Assert.Equal(result.Result.SearchResultAvatars.Count,result.LoadedCount);Assert.Empty(result.Result.SearchResultHolons);
        var projected = result.Result.SearchResultAvatars.ToDictionary(row => row.ProviderUniqueStorageKey[ProviderType.ActivityPubOASIS]);
        Assert.Equal(expected.Count, projected.Count);
        foreach (var account in expected)
        {
            Assert.True(projected.TryGetValue(account.Key, out var row));
            Assert.Equal(account.Value, row!.Username);
            Assert.True(string.IsNullOrEmpty(row.Email));
        }
        var selected = expected.Keys.First();
        var point = await provider.LoadAvatarByProviderKeyAsync(selected);
        Assert.False(point.IsError,point.Exception?.ToString());Assert.Equal(point.Result.Id,Assert.Single(result.Result.SearchResultAvatars.Where(row=>row.ProviderUniqueStorageKey[ProviderType.ActivityPubOASIS]==selected)).Id);
    }
}
