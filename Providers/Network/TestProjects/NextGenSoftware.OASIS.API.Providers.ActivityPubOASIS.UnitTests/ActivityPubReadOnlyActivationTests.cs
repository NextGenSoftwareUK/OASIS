using System.Net;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.ActivityPubOASIS.UnitTests;

public class ActivityPubReadOnlyActivationTests
{
    private sealed class Transport : HttpMessageHandler
    {
        public int Calls;
        public bool Block, Oversize, Invalid;
        public readonly TaskCompletionSource Sent = new(TaskCreationOptions.RunContinuationsAsynchronously);
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            Calls++;
            Assert.Equal(HttpMethod.Get, request.Method);
            Assert.Equal("https://example.invalid/api/v2/instance", request.RequestUri!.AbsoluteUri);
            Assert.Null(request.Headers.Authorization);
            Sent.TrySetResult();
            if (Block) await Task.Delay(Timeout.Infinite, token);
            return new(HttpStatusCode.OK) { Content = new StringContent(Oversize ? new string('x', 1024 * 1024 + 1)
                : Invalid ? "{}" : "{\"domain\":\"example.invalid\",\"title\":\"Read source\",\"version\":\"4.5.0\",\"configuration\":{}}") };
        }
    }

    [Fact]
    public async Task ReadOnlyContractPublishesActivationAfterOneInstanceGetWithoutProvisioning()
    {
        var transport = new Transport();
        using var provider = new ActivityPubOASIS("https://example.invalid", "", transport);
        var result = await ((ICosmicReadOnlyQueryActivation)provider).ActivateQuerySourceAsync();
        Assert.False(result.IsError); Assert.True(result.Result);
        Assert.True(provider.IsProviderActivated); Assert.Equal(1, transport.Calls);
    }

    [Fact]
    public async Task PreCancelledActivationDoesNotContactInstanceOrBecomeActive()
    {
        var transport = new Transport();
        using var provider = new ActivityPubOASIS("https://example.invalid", "", transport);
        using var cancellation = new CancellationTokenSource(); cancellation.Cancel();
        var result = await provider.ActivateQuerySourceAsync(cancellation.Token);
        Assert.True(result.IsError); Assert.False(provider.IsProviderActivated); Assert.Equal(0, transport.Calls);
    }

    [Fact]
    public async Task CancellationStopsTheOwningNativeRequestBeforeActivationPublication()
    {
        var transport = new Transport { Block = true };
        using var provider = new ActivityPubOASIS("https://example.invalid", "", transport);
        using var cancellation = new CancellationTokenSource();
        var activation = provider.ActivateQuerySourceAsync(cancellation.Token);
        await transport.Sent.Task.WaitAsync(TimeSpan.FromSeconds(5)); cancellation.Cancel();
        var result = await activation.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.True(result.IsError); Assert.False(provider.IsProviderActivated); Assert.Equal(1, transport.Calls);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ExcessiveOrInvalidInstanceReplyNeverActivatesOrFallsBack(bool oversize)
    {
        var transport = new Transport { Oversize = oversize, Invalid = !oversize };
        using var provider = new ActivityPubOASIS("https://example.invalid", "", transport);
        var result = await provider.ActivateQuerySourceAsync();
        Assert.True(result.IsError); Assert.False(provider.IsProviderActivated); Assert.Equal(1, transport.Calls);
    }
}
