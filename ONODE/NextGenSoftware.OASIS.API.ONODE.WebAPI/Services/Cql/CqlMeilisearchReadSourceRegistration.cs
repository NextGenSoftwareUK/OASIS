using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NextGenSoftware.OASIS.API.Core.Objects.NativeData;
using MeilisearchProvider = NextGenSoftware.OASIS.API.Providers.MeilisearchOASIS.MeilisearchOASIS;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

public static class CqlMeilisearchReadSourceRegistration
{
    private const string ClientKey = "cql-meilisearch-source";

    public static void AddMeilisearchReadSource(this IServiceCollection services, IConfiguration configuration)
    {
        var endpoint = configuration["CQL_MEILISEARCH_QUERY_ORIGIN"];
        var key = configuration["CQL_MEILISEARCH_QUERY_API_KEY"];
        if (endpoint == null && key == null) return;
        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var origin) || origin.Scheme != Uri.UriSchemeHttps ||
            origin.IsLoopback || origin.AbsolutePath != "/" || origin.UserInfo.Length != 0 ||
            origin.Query.Length != 0 || origin.Fragment.Length != 0)
            throw new InvalidOperationException("Meilisearch CQL requires an explicit credential-free HTTPS origin.");
        if (key == null || key.Length is < 16 or > 256 || !key.All(char.IsAsciiHexDigit))
            throw new InvalidOperationException("Meilisearch CQL requires an explicit restricted API key.");

        services.AddKeyedSingleton<HttpClient>(ClientKey, (_, _) => new HttpClient(
            new ExactReadHandler(origin, NativeConnectionGuard.CreateGuardedHttpHandler())));
        services.AddSingleton<CqlReadSourceProfile>(provider => new CqlReadSourceProfile(new MeilisearchProvider(
            origin.AbsoluteUri, key, provider.GetRequiredKeyedService<HttpClient>(ClientKey))));
    }

    internal sealed class ExactReadHandler(Uri origin, HttpMessageHandler transport) : DelegatingHandler(transport)
    {
        private const string IndexPath = "/indexes/oasis_holons";
        private const string DocumentPrefix = IndexPath + "/documents/";

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var uri = request.RequestUri;
            if (request.Method != HttpMethod.Get || uri is not { IsAbsoluteUri: true } ||
                !string.Equals(uri.GetLeftPart(UriPartial.Authority), origin.GetLeftPart(UriPartial.Authority), StringComparison.Ordinal) ||
                uri.UserInfo.Length != 0 || uri.Query.Length != 0 || uri.Fragment.Length != 0 || !IsExactRead(uri.AbsolutePath))
                throw new InvalidOperationException("Meilisearch CQL transport admits only exact reads from the configured index.");
            return base.SendAsync(request, cancellationToken);
        }

        private static bool IsExactRead(string path)
        {
            if (path == IndexPath) return true;
            if (!path.StartsWith(DocumentPrefix, StringComparison.Ordinal)) return false;
            var key = path[DocumentPrefix.Length..];
            return Guid.TryParseExact(key, "D", out var id) && id != Guid.Empty && key == id.ToString("D");
        }
    }
}
