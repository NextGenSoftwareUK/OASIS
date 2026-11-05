using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NextGenSoftware.OASIS.API.Core.Objects.NativeData;
using ElasticsearchProvider = NextGenSoftware.OASIS.API.Providers.ElasticsearchOASIS.ElasticsearchOASIS;
using OpenSearchProvider = NextGenSoftware.OASIS.API.Providers.OpenSearchOASIS.OpenSearchOASIS;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

public static class CqlSearchDocumentReadSources
{
    public static void AddSearchDocumentReadSources(this IServiceCollection services, IConfiguration configuration)
    {
        var elastic = ReadSettings(configuration, "ELASTICSEARCH", "API_KEY");
        var open = ReadSettings(configuration, "OPENSEARCH", "USERNAME", "PASSWORD");
        if (elastic != null && !Convert.TryFromBase64String(elastic.Credentials[0], stackalloc byte[1024], out _))
            throw new InvalidOperationException("Elasticsearch CQL requires an encoded API key.");
        if (open != null && open.Credentials[0].Contains(':'))
            throw new InvalidOperationException("OpenSearch CQL requires a valid read account.");
        if (elastic != null)
        {
            var key = elastic.Credentials[0];
            AddClient(services, "cql-elasticsearch-source", elastic.Origin, "oasis-holons");
            services.AddSingleton<CqlReadSourceProfile>(provider => new CqlReadSourceProfile(new ElasticsearchProvider(
                elastic.Origin.AbsoluteUri, null, null, elastic.Probe,
                provider.GetRequiredKeyedService<HttpClient>("cql-elasticsearch-source"), key)));
        }

        if (open != null)
        {
            AddClient(services, "cql-opensearch-source", open.Origin, "oasis_holons");
            services.AddSingleton<CqlReadSourceProfile>(provider => new CqlReadSourceProfile(new OpenSearchProvider(
                open.Origin.AbsoluteUri, open.Credentials[0], open.Credentials[1], open.Probe,
                provider.GetRequiredKeyedService<HttpClient>("cql-opensearch-source"))));
        }
    }

    private sealed record Source(Uri Origin, Guid Probe, string[] Credentials);

    private static Source ReadSettings(IConfiguration configuration, string provider, params string[] credentialNames)
    {
        var prefix = "CQL_" + provider + "_QUERY_";
        var endpoint = configuration[prefix + "ORIGIN"];
        var probeValue = configuration[prefix + "PROBE_ID"];
        var credentials = credentialNames.Select(name => configuration[prefix + name]).ToArray();
        if (endpoint == null && probeValue == null && credentials.All(value => value == null)) return null;
        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var origin) || origin.Scheme != Uri.UriSchemeHttps ||
            origin.IsLoopback || origin.AbsolutePath != "/" || origin.UserInfo.Length != 0 ||
            origin.Query.Length != 0 || origin.Fragment.Length != 0)
            throw new InvalidOperationException(provider + " CQL requires an explicit credential-free HTTPS origin.");
        if (!Guid.TryParseExact(probeValue, "D", out var probe) || probe == Guid.Empty)
            throw new InvalidOperationException(provider + " CQL requires an existing teaching probe identity.");
        if (credentials.Any(value => string.IsNullOrWhiteSpace(value) || value.Length > 1024 || value.Any(char.IsControl)))
            throw new InvalidOperationException(provider + " CQL requires explicit restricted credentials.");
        return new Source(origin, probe, credentials);
    }

    private static void AddClient(IServiceCollection services, string key, Uri origin, string index)
        => services.AddKeyedSingleton<HttpClient>(key, (_, _) => new HttpClient(
            new ExactReadHandler(origin, index, NativeConnectionGuard.CreateGuardedHttpHandler())));

    internal sealed class ExactReadHandler(Uri origin, string index, HttpMessageHandler transport) : DelegatingHandler(transport)
    {
        private readonly string _prefix = "/" + index + "/_doc/";

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var uri = request.RequestUri;
            if (request.Method != HttpMethod.Get || uri is not { IsAbsoluteUri: true } ||
                !string.Equals(uri.GetLeftPart(UriPartial.Authority), origin.GetLeftPart(UriPartial.Authority), StringComparison.Ordinal) ||
                uri.UserInfo.Length != 0 || uri.Fragment.Length != 0 || uri.Query != "?realtime=true&_source=true" ||
                !uri.AbsolutePath.StartsWith(_prefix, StringComparison.Ordinal) ||
                !Guid.TryParseExact(uri.AbsolutePath[_prefix.Length..], "D", out var id) || id == Guid.Empty ||
                uri.AbsolutePath != _prefix + id.ToString("D"))
                throw new InvalidOperationException("Search CQL transport admits only exact reads from its configured index.");
            return base.SendAsync(request, cancellationToken);
        }
    }
}
