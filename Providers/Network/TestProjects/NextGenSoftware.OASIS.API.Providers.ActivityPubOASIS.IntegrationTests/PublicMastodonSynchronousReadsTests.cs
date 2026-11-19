using System.Net.Http;
using System.Text.Json;
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.API.Providers.ActivityPubOASIS.IntegrationTests;

public sealed class PublicMastodonSynchronousReadsTests
{
    [PublicRead]
    public async Task StatusAndRepliesReadFromLiveMastodonWithoutCallerContext()
    {
        using var discovery = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false })
        {
            BaseAddress = new Uri("https://mastodon.social"),
            Timeout = TimeSpan.FromSeconds(15)
        };
        const string id = "117015608750780726";
        using var original = JsonDocument.Parse(await discovery.GetStringAsync($"/api/v1/statuses/{id}"));

        using var provider = new ActivityPubOASIS("https://mastodon.social", "");
        var previous = SynchronizationContext.Current;
        var context = new TrackingContext();
        try
        {
            SynchronizationContext.SetSynchronizationContext(context);
            var parent = provider.LoadHolon(id, loadChildren: false);
            Assert.False(parent.IsError, parent.Message);
            Assert.True(parent.IsLoaded);
            Assert.Equal(id, parent.Result.ProviderUniqueStorageKey[ProviderType.ActivityPubOASIS]);
            Assert.Equal(original.RootElement.GetProperty("uri").GetString(), parent.Result.MetaData["MastodonObjectUri"]);

            var replies = provider.LoadHolonsForParent(id, loadChildren: false);
            Assert.False(replies.IsError, replies.Exception?.Message ?? replies.Message);
            Assert.True(replies.IsLoaded);
            Assert.NotEmpty(replies.Result);
            Assert.All(replies.Result, reply => Assert.Equal(parent.Result.Id, reply.ParentHolonId));
            Assert.Equal(0, context.PostCount);
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(previous);
        }
    }

    private sealed class PublicReadAttribute : FactAttribute
    {
        public PublicReadAttribute()
        {
            if (Environment.GetEnvironmentVariable("OASIS_ACTIVITYPUB_PUBLIC_LIVE") != "1")
                Skip = "Explicitly enable public Mastodon reads.";
        }
    }

    private sealed class TrackingContext : SynchronizationContext
    {
        public int PostCount { get; private set; }

        public override void Post(SendOrPostCallback callback, object? state)
        {
            PostCount++;
            base.Post(callback, state);
        }
    }
}
