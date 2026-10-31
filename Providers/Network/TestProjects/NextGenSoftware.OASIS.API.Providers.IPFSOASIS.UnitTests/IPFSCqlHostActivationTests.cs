using System.Net;
using System.Reflection;
using Ipfs.Http;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.DNA;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.IPFSOASIS.UnitTests;

[Collection("Owned IPFS")]
public class IPFSCqlHostActivationTests
{
    [Theory]
    [InlineData("ready")]
    [InlineData("empty")]
    [InlineData("blank-version")]
    [InlineData("unavailable")]
    [InlineData("cancelled-before")]
    [InlineData("cancelled-during")]
    public async Task ReadOnlyActivationUsesOnlyTheConfiguredNativeHealthCheck(string scenario)
    {
        var dna = new OASISDNA
        {
            OASIS = new NextGenSoftware.OASIS.API.DNA.OASIS
            {
                StorageProviders = new StorageProviderSettings
                {
                    IPFSOASIS = new IPFSOASISSettings
                    {
                        ConnectionString = "http://127.0.0.1:1",
                        LookUpIPFSAddress = "unchanged-catalogue"
                    }
                }
            }
        };
        // The fixture does not create a daemon, key, catalogue, or DNA file.
        var provider = new IPFSOASIS(dna, "unused-read-only-activation.json");
        var api = typeof(IpfsClient).GetField("api", BindingFlags.Static | BindingFlags.NonPublic)!;
        var previous = api.GetValue(null);
        using var cancel = new CancellationTokenSource();
        var handler = new HealthTransport(scenario);
        using var http = new HttpClient(handler);
        api.SetValue(null, http);
        try
        {
            Assert.IsAssignableFrom<ICosmicReadOnlyQueryActivation>(provider);
            if (scenario == "cancelled-before") cancel.Cancel();
            var pending = provider.ActivateQuerySourceAsync(cancel.Token);
            if (scenario == "cancelled-during")
            {
                await handler.Entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
                cancel.Cancel();
            }
            var result = await pending.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.Equal(scenario == "ready", result.Result);
            Assert.Equal(scenario != "ready", result.IsError);
            Assert.Equal(scenario == "ready", provider.IsProviderActivated);
            Assert.Equal("unchanged-catalogue", dna.OASIS.StorageProviders.IPFSOASIS.LookUpIPFSAddress);
            Assert.Equal(scenario == "cancelled-before" ? 0 : 1, handler.Calls);
        }
        finally
        {
            await provider.DeActivateProviderAsync();
            api.SetValue(null, previous);
        }
    }

    private sealed class HealthTransport(string scenario) : HttpMessageHandler
    {
        public int Calls;
        public TaskCompletionSource<bool> Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            Calls++;
            Assert.Equal("/api/v0/version", request.RequestUri!.AbsolutePath);
            Assert.Equal("127.0.0.1", request.RequestUri.Host);
            Assert.Equal(1, request.RequestUri.Port);
            Entered.TrySetResult(true);
            if (scenario == "cancelled-during")
                await Task.Delay(Timeout.Infinite, token);
            return new HttpResponseMessage(scenario == "unavailable" ? HttpStatusCode.ServiceUnavailable : HttpStatusCode.OK)
            {
                Content = new StringContent(scenario switch
                {
                    "empty" => "{}",
                    "blank-version" => "{\"Version\":\"\"}",
                    _ => "{\"Version\":\"0.43.1\"}"
                })
            };
        }
    }
}
