using System.Net;
using System.Text.Json;
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.API.Providers.ActivityPubOASIS.IntegrationTests;

public class NativeMastodonOwnedStatusTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DeletesAnOwnedPrivateStatusAndNativeReadConfirmsAbsence(bool synchronous)
    {
        const string origin = "https://mastodon.oasis.test";
        Assert.Equal("true", Environment.GetEnvironmentVariable("GITHUB_ACTIONS"));
        Assert.Equal(origin, Environment.GetEnvironmentVariable("OASIS_ACTIVITYPUB_SEARCH_INSTANCE"));
        var token = Environment.GetEnvironmentVariable("OASIS_ACTIVITYPUB_STATUS_FIXTURE_TOKEN");
        Assert.False(string.IsNullOrWhiteSpace(token));

        using var client = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false })
        {
            BaseAddress = new Uri(origin),
            Timeout = TimeSpan.FromSeconds(20)
        };
        client.DefaultRequestHeaders.Authorization = new("Bearer", token);
        using var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["status"] = $"OASIS disposable status {Guid.NewGuid():N}",
            ["visibility"] = "private"
        });
        using var created = await client.PostAsync("/api/v1/statuses", form);
        created.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await created.Content.ReadAsStringAsync());
        var status = document.RootElement;
        Assert.Equal("private", status.GetProperty("visibility").GetString());
        Assert.Equal("oasisreader", status.GetProperty("account").GetProperty("acct").GetString());
        var id = status.GetProperty("id").GetString();
        Assert.Matches("^[0-9]+$", id!);
        var path = $"/api/v1/statuses/{id}";
        using var before = await client.GetAsync(path);
        Assert.Equal(HttpStatusCode.OK, before.StatusCode);

        using var provider = new ActivityPubOASIS(origin, token!);
        var deleted = synchronous ? provider.DeleteHolon(id!) : await provider.DeleteHolonAsync(id!);
        Assert.False(deleted.IsError, deleted.Message);
        Assert.True(deleted.IsDeleted);
        Assert.Equal(1, deleted.DeletedCount);
        Assert.NotNull(deleted.Result);
        Assert.Equal(id, deleted.Result.ProviderUniqueStorageKey[ProviderType.ActivityPubOASIS]);
        using var after = await client.GetAsync(path);
        Assert.Equal(HttpStatusCode.NotFound, after.StatusCode);
    }
}
