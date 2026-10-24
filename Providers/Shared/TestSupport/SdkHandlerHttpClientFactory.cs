using System;
using System.Net.Http;
using Amazon.Runtime;

namespace NextGenSoftware.OASIS.Providers.TestSupport;

/// <summary>Reuse one controlled handler with the real AWS SDK pipeline; no network listener.</summary>
public sealed class HandlerHttpClientFactory:HttpClientFactory
{
    private readonly HttpMessageHandler handler;
    public HandlerHttpClientFactory(HttpMessageHandler handler)=>this.handler=handler??throw new ArgumentNullException(nameof(handler));
    public override HttpClient CreateHttpClient(IClientConfig clientConfig)=>new(handler,disposeHandler:false);
}
