using System.Net;
using System.Text.Json;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.Common;

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
            ? await RouteStatusAsync(client, provider, id!, sourceText, synchronous)
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

    private static async Task VerifyPublicObjectResolutionAsync(HttpClient client, Func<StorageOperationRequest, Task<OASISResult<IHolon>>> route)
    {
        using var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["status"] = $"OASIS disposable public object {Guid.NewGuid():N}",
            ["visibility"] = "public"
        });
        using var created = await client.PostAsync("/api/v1/statuses", form);
        created.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await created.Content.ReadAsStringAsync());
        var id = document.RootElement.GetProperty("id").GetString();
        Assert.Matches("^[0-9]+$", id!);
        var path = $"/api/v1/statuses/{id}";
        var uri = document.RootElement.GetProperty("uri").GetString();
        Task<OASISResult<IHolon>> ReadPublicObject() =>
            route(new StorageOperationRequest
            {
                Operation = "LoadHolonByProviderKey",
                ProviderKey = uri!,
                PreferredProvider = ProviderType.ActivityPubOASIS,
                LoadChildren = false
            });

        try
        {
            var resolved = await ReadPublicObject();
            Assert.False(resolved.IsError, resolved.Message);
            Assert.True(resolved.IsLoaded);
            using var routeEvidence = JsonDocument.Parse(resolved.MetaData["hyperDriveRoute"]);
            Assert.Equal("ActivityPubOASIS", routeEvidence.RootElement.GetProperty("ServingProvider").GetString());
            Assert.Equal(uri, resolved.Result.MetaData["MastodonObjectUri"]);
            Assert.Equal(id, resolved.Result.ProviderUniqueStorageKey[ProviderType.ActivityPubOASIS]);
        }
        finally
        {
            using var deleted = await client.DeleteAsync(path);
            deleted.EnsureSuccessStatusCode();
            using var absent = await client.GetAsync(path);
            Assert.Equal(HttpStatusCode.NotFound, absent.StatusCode);
        }

        var missing = await ReadPublicObject();
        Assert.Equal(OASISResultErrorCodes.RecordNotFound, missing.ErrorCode);
        Assert.False(missing.IsLoaded);
        Assert.Null(missing.Result);
    }

    private static async Task<OASISResult<IHolon>> RouteStatusAsync(
        HttpClient client, ActivityPubOASIS provider, string id, string sourceText, bool synchronous)
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
        Task<OASISResult<IHolon>> Route(StorageOperationRequest request)
            => synchronous ? Task.FromResult(router.RouteRequest<IHolon>(request)) : router.RouteRequestAsync<IHolon>(request);
        foreach (var invalidKey in new[] { ".", ".." })
        {
            var rejected = await Route(new StorageOperationRequest
            {
                Operation = "LoadHolonByProviderKey",
                ProviderKey = invalidKey,
                PreferredProvider = ProviderType.ActivityPubOASIS,
                LoadChildren = false
            });
            Assert.True(rejected.IsError);
            Assert.False(rejected.IsLoaded);
            Assert.Null(rejected.Result);
            Assert.IsType<ArgumentException>(rejected.Exception);
        }
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
        Assert.Contains(sourceText, Assert.IsType<string>(loaded.Result.MetaData["MastodonContentHtml"]), StringComparison.Ordinal);
        using var route = JsonDocument.Parse(loaded.MetaData["hyperDriveRoute"]);
        Assert.Equal("ActivityPubOASIS", route.RootElement.GetProperty("ServingProvider").GetString());

        await VerifyPublicObjectResolutionAsync(client, Route);

        return await Route(new StorageOperationRequest
        {
            Operation = "DeleteHolonByProviderKey",
            ProviderKey = id,
            PreferredProvider = ProviderType.ActivityPubOASIS,
            SoftDelete = false
        });
    }
}
