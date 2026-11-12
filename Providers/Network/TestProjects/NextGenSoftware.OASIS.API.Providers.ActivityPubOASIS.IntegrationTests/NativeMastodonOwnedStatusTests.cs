using System.Net;
using System.Text.Json;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive;
using NextGenSoftware.OASIS.API.DNA;

namespace NextGenSoftware.OASIS.API.Providers.ActivityPubOASIS.IntegrationTests;

public class NativeMastodonOwnedStatusTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task DeletesAnOwnedPrivateStatusAndNativeReadConfirmsAbsence(bool synchronous)
        => VerifyAsync(synchronous, routed: false);

    [Theory, InlineData(false), InlineData(true)]
    public Task RoutesInstanceScopedStatusReadAndDelete(bool synchronous)
        => VerifyAsync(synchronous, routed: true);

    private static async Task VerifyAsync(bool synchronous, bool routed)
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
        var sourceText = $"OASIS disposable status {Guid.NewGuid():N}";
        using var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["status"] = sourceText,
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
        var deleted = routed
            ? await RouteStatusAsync(provider, id!, sourceText, synchronous)
            : synchronous ? provider.DeleteHolon(id!) : await provider.DeleteHolonAsync(id!);
        Assert.False(deleted.IsError, deleted.Message);
        Assert.True(deleted.IsDeleted);
        Assert.Equal(1, deleted.DeletedCount);
        Assert.NotNull(deleted.Result);
        Assert.Equal(id, deleted.Result.ProviderUniqueStorageKey[ProviderType.ActivityPubOASIS]);
        Assert.Equal(sourceText, deleted.Result.MetaData["MastodonSourceText"]);
        using var after = await client.GetAsync(path);
        Assert.Equal(HttpStatusCode.NotFound, after.StatusCode);
    }

    private static async Task<NextGenSoftware.OASIS.Common.OASISResult<IHolon>> RouteStatusAsync(
        ActivityPubOASIS provider, string id, string sourceText, bool synchronous)
    {
        var dna = new OASISDNA
        {
            OASIS = new NextGenSoftware.OASIS.API.DNA.OASIS
            {
                HyperDriveMode = HyperDriveModes.V2,
                StorageProviders = new StorageProviderSettings()
            }
        };
        dna.OASIS.SubscriptionConfig.PlanType = "Gold";
        var manager = new ProviderManager(null, dna);
        manager.RegisterProvider(provider);
        var router = new OASISHyperDrive(manager);
        Task<NextGenSoftware.OASIS.Common.OASISResult<IHolon>> Route(StorageOperationRequest request)
            => synchronous ? Task.FromResult(router.RouteRequest<IHolon>(request)) : router.RouteRequestAsync<IHolon>(request);
        var loaded = await Route(new StorageOperationRequest
        {
            Operation = "LoadHolonByProviderKey",
            ProviderKey = id,
            PreferredProvider = ProviderType.ActivityPubOASIS,
            LoadChildren = false
        });
        Assert.False(loaded.IsError, loaded.Message);
        Assert.NotNull(loaded.Result);
        Assert.Equal(id, loaded.Result.ProviderUniqueStorageKey[ProviderType.ActivityPubOASIS]);
        Assert.Contains(sourceText, loaded.Result.MetaData["MastodonContentHtml"], StringComparison.Ordinal);
        using var route = JsonDocument.Parse(loaded.MetaData["hyperDriveRoute"]);
        Assert.Equal("ActivityPubOASIS", route.RootElement.GetProperty("ServingProvider").GetString());

        return await Route(new StorageOperationRequest
        {
            Operation = "DeleteHolonByProviderKey",
            ProviderKey = id,
            PreferredProvider = ProviderType.ActivityPubOASIS,
            SoftDelete = false
        });
    }
}
