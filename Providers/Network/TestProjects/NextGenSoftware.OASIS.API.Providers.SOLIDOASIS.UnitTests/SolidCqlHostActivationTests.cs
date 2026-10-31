using System.Net;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.SOLIDOASIS.UnitTests;

public class SolidCqlHostActivationTests
{
    [Theory]
    [InlineData("ready")]
    [InlineData("unauthorized")]
    [InlineData("server-error")]
    [InlineData("redirect")]
    [InlineData("cancelled-before")]
    [InlineData("cancelled-during")]
    public async Task ReadOnlyActivationUsesTheScopedRootWithoutReadingOrWritingRecords(string scenario)
    {
        var handler = new RootTransport(scenario);
        using var provider = new SOLIDOASIS("https://pod.invalid/demo/", "", new byte[32], handler);
        Assert.IsAssignableFrom<ICosmicReadOnlyQueryActivation>(provider);
        using var cancel = new CancellationTokenSource();
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
        Assert.Equal(scenario == "cancelled-before" ? 0 : 1, handler.Calls);
        Assert.False(handler.Content.WasRead);
    }

    private sealed class RootTransport(string scenario) : HttpMessageHandler
    {
        public int Calls;
        public NeverReadContent Content { get; } = new();
        public TaskCompletionSource<bool> Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            Calls++;
            Assert.Equal(HttpMethod.Get, request.Method);
            Assert.Equal("https://pod.invalid/demo/", request.RequestUri!.AbsoluteUri);
            Entered.TrySetResult(true);
            if (scenario == "cancelled-during") await Task.Delay(Timeout.Infinite, token);
            return new HttpResponseMessage(scenario switch
            {
                "unauthorized" => HttpStatusCode.Unauthorized,
                "server-error" => HttpStatusCode.ServiceUnavailable,
                "redirect" => HttpStatusCode.TemporaryRedirect,
                _ => HttpStatusCode.OK
            }) { Content = Content };
        }
    }

    private sealed class NeverReadContent : HttpContent
    {
        public bool WasRead;
        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context)
        {
            WasRead = true;
            throw new InvalidOperationException("Health activation must not buffer a Pod's record listing.");
        }
        protected override bool TryComputeLength(out long length) { length = 0; return false; }
    }
}
