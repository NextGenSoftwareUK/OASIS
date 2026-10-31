using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Moq;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

// Real MVC barriers reproduce request-local settings escaping into the singleton
// and older cleanup overwriting a newer global publication. No timing sleeps.
public sealed partial class CosmicDataControllerHttpTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AConditionalWriteCannotUseAllProvidersAsItsAuthority(bool bulk)
    {
        await WithHost(false, async (client, actor, provider) =>
        {
            object body = bulk
                ? new { holons = new[] { new { name = "invalid authority" } }, operationId = Guid.NewGuid(), providerType = "All" }
                : new { holon = new { name = "invalid authority" }, operationId = Guid.NewGuid(), providerType = "All" };
            using var response = await client.PostAsJsonAsync(bulk ? "/api/data/save-holons" : "/api/data/save-holon", body);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Empty(provider.Invocations.Where(call => call.Method.Name.Contains("Load") ||
                call.Method.Name.Contains("Save") || call.Method.Name.Contains("Conditional")));
        });
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task LocalProviderListsAndNullModesRemainRequestOwned(bool bulk)
    {
        await WithHost(false, async (client, actor, provider) =>
        {
            var globals = ProviderManager.Instance;
            var before = globals.GetProviderAutoFailOverListAsString();
            object body = bulk
                ? new { holons = new[] { new { name = "list scope" } }, operationId = Guid.NewGuid(), saveChildren = false,
                    providerType = "sqllitedboasis", autoFailOverProviders = "MongoDBOASIS,SQLLiteDBOASIS", autoFailOverMode = (string?)null }
                : new { holon = new { name = "list scope" }, operationId = Guid.NewGuid(), saveChildren = false,
                    providerType = "sqllitedboasis", autoFailOverProviders = "MongoDBOASIS,SQLLiteDBOASIS", autoFailOverMode = (string?)null };
            using var response = await client.PostAsJsonAsync(bulk ? "/api/data/save-holons" : "/api/data/save-holon", body);

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            Assert.Equal("MongoDBOASIS,SQLLiteDBOASIS", json.RootElement.GetProperty("autoFailOverProviders").GetString());
            Assert.Equal("SQLLiteDBOASIS", json.RootElement.GetProperty("currentOASISProvider").GetString());
            Assert.Equal(before, globals.GetProviderAutoFailOverListAsString());
        });
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, false)]
    [InlineData(2, false)]
    [InlineData(0, true)]
    [InlineData(1, true)]
    [InlineData(2, true)]
    public async Task OverlappingRequestSettingsNeverLeakOrRestoreOverAnExplicitGlobalUpdate(int feature, bool globalUpdate)
    {
        await WithHost(false, async (client, actor, provider) =>
        {
            var globals = ProviderManager.Instance;
            var original = (globals.IsAutoReplicationEnabled, globals.IsAutoFailOverEnabled, globals.IsAutoLoadBalanceEnabled);
            bool Observe() => feature switch
            {
                0 => globals.IsAutoReplicationEnabled,
                1 => globals.IsAutoFailOverEnabled,
                _ => globals.IsAutoLoadBalanceEnabled
            };
            var firstEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var secondEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var releaseFirst = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var releaseSecond = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var pending = new List<Task<HttpResponseMessage>>();
            globals.IsAutoReplicationEnabled = globals.IsAutoFailOverEnabled = globals.IsAutoLoadBalanceEnabled = false;
            provider.As<IConditionalHolonStorageProvider>()
                .Setup(value => value.ExecuteConditionalHolonWriteAsync(It.IsAny<ConditionalHolonWriteRequest>(), It.IsAny<CancellationToken>()))
                .Returns(async (ConditionalHolonWriteRequest request, CancellationToken cancellation) =>
                {
                    var first = request.Roots.Single().Name == "first";
                    (first ? firstEntered : secondEntered).TrySetResult();
                    await (first ? releaseFirst : releaseSecond).Task.WaitAsync(cancellation);
                    return new OASISResult<IEnumerable<IHolon>> { IsError = true, ErrorCode = CosmicConcurrencyException.Code };
                });

            Task<HttpResponseMessage> Send(string name, string mode, bool globally)
            {
                var payload = new Dictionary<string, object>
                {
                    ["holon"] = new { name }, ["operationId"] = Guid.NewGuid(),
                    ["saveChildren"] = false, ["setGlobally"] = globally,
                    [feature switch { 0 => "autoReplicationMode", 1 => "autoFailOverMode", _ => "autoLoadBalanceMode" }] = mode
                };
                var request = client.PostAsJsonAsync("/api/data/save-holon", payload);
                pending.Add(request);
                return request;
            }

            try
            {
                var first = Send("first", globalUpdate ? "OFF" : "ON", false);
                await firstEntered.Task.WaitAsync(TimeSpan.FromSeconds(10));
                var duringFirst = Observe();

                var second = Send("second", globalUpdate ? "ON" : "OFF", globalUpdate);
                await secondEntered.Task.WaitAsync(TimeSpan.FromSeconds(10));
                var duringSecond = Observe();

                // Finish the older temporary scope while the second request is
                // still active. No timing sleeps or nondeterministic race loops.
                releaseFirst.TrySetResult();
                using var firstResponse = await first.WaitAsync(TimeSpan.FromSeconds(10));
                var afterFirst = Observe();
                releaseSecond.TrySetResult();
                using var secondResponse = await second.WaitAsync(TimeSpan.FromSeconds(10));

                Assert.Equal(HttpStatusCode.Conflict, firstResponse.StatusCode);
                Assert.Equal(HttpStatusCode.Conflict, secondResponse.StatusCode);
                Assert.False(duringFirst);
                Assert.Equal(globalUpdate, duringSecond);
                Assert.Equal(globalUpdate, afterFirst);
                Assert.Equal(globalUpdate, Observe());
                var settingName = feature switch
                { 0 => "autoReplicationEnabled", 1 => "autoFailOverEnabled", _ => "autoLoadBalanceEnabled" };
                using var firstBody = JsonDocument.Parse(await firstResponse.Content.ReadAsStringAsync());
                using var secondBody = JsonDocument.Parse(await secondResponse.Content.ReadAsStringAsync());
                Assert.Equal(!globalUpdate, firstBody.RootElement.GetProperty(settingName).GetBoolean());
                Assert.Equal(globalUpdate, secondBody.RootElement.GetProperty(settingName).GetBoolean());
            }
            finally
            {
                releaseFirst.TrySetResult();
                releaseSecond.TrySetResult();
                try
                {
                    foreach (var request in pending)
                        using (await request.WaitAsync(TimeSpan.FromSeconds(10))) { }
                }
                finally
                {
                    globals.IsAutoReplicationEnabled = original.Item1;
                    globals.IsAutoFailOverEnabled = original.Item2;
                    globals.IsAutoLoadBalanceEnabled = original.Item3;
                }
            }
        });
    }
}
