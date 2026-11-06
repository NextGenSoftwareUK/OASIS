using System;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Runtime;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NextGenSoftware.OASIS.API.Core.Objects.NativeData;
using TigrisProvider = NextGenSoftware.OASIS.API.Providers.TigrisOASIS.TigrisOASIS;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

public static class CqlTigrisReadSourceRegistration
{
    public static void AddTigrisReadSource(this IServiceCollection services, IConfiguration configuration)
    {
        const string prefix = "CQL_TIGRIS_QUERY_";
        var endpoint = configuration[prefix + "ORIGIN"];
        var bucket = configuration[prefix + "BUCKET"];
        var access = configuration[prefix + "ACCESS_KEY"];
        var secret = configuration[prefix + "SECRET_KEY"];
        var probe = configuration[prefix + "PROBE_ID"];
        if (new[] { endpoint, bucket, access, secret, probe }.All(value => value == null)) return;
        if (endpoint is not ("https://t3.storage.dev" or "https://fly.storage.tigris.dev"))
            throw new InvalidOperationException("Tigris CQL requires an explicit native HTTPS service origin.");
        if (bucket is not { Length: >= 3 and <= 63 } || !Regex.IsMatch(bucket, @"\A[a-z0-9][a-z0-9-]+[a-z0-9]\z"))
            throw new InvalidOperationException("Tigris CQL requires a DNS-safe teaching bucket.");
        if (string.IsNullOrWhiteSpace(access) || string.IsNullOrWhiteSpace(secret) ||
            !Guid.TryParseExact(probe, "D", out var id) || id == Guid.Empty)
            throw new InvalidOperationException("Tigris CQL requires read-restricted credentials and an existing teaching probe.");

        var bucketOrigin = new UriBuilder(endpoint) { Host = bucket + "." + new Uri(endpoint).Host }.Uri;
        services.AddSingleton<CqlReadSourceProfile>(_ => new CqlReadSourceProfile(
            new TigrisProvider(access, secret, bucket, endpoint, id, new ExactReadClientFactory(bucketOrigin))));
    }

    private sealed class ExactReadClientFactory(Uri origin) : HttpClientFactory
    {
        public override HttpClient CreateHttpClient(IClientConfig clientConfig)
        {
            var transport = (SocketsHttpHandler)NativeConnectionGuard.CreateGuardedHttpHandler();
            transport.UseProxy = false;
            transport.UseCookies = false;
            return new HttpClient(new ExactReadHandler(origin, transport));
        }
    }

    internal sealed class ExactReadHandler(Uri origin, HttpMessageHandler transport) : DelegatingHandler(transport)
    {
        private static readonly Regex RecordPath = new(@"\A/oasis/holon/[0-9a-f]{32}\z", RegexOptions.CultureInvariant);

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var uri = request.RequestUri;
            if (request.Method != HttpMethod.Get || uri is not { IsAbsoluteUri: true } ||
                !string.Equals(uri.GetLeftPart(UriPartial.Authority), origin.GetLeftPart(UriPartial.Authority), StringComparison.Ordinal) ||
                uri.UserInfo.Length != 0 || uri.Query.Length != 0 || uri.Fragment.Length != 0 ||
                (request.Headers.Host != null && !string.Equals(request.Headers.Host, origin.Authority, StringComparison.OrdinalIgnoreCase)) ||
                !RecordPath.IsMatch(uri.AbsolutePath))
                throw new InvalidOperationException("Tigris CQL transport admits only canonical object reads from its configured bucket.");
            return base.SendAsync(request, cancellationToken);
        }
    }
}
