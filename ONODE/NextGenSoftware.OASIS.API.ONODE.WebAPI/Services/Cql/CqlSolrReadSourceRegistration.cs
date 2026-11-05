using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NextGenSoftware.OASIS.API.Core.Objects.NativeData;
using SolrProvider = NextGenSoftware.OASIS.API.Providers.SolrOASIS.SolrOASIS;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

public static class CqlSolrReadSourceRegistration
{
    private const string ClientKey = "cql-solr-source";

    public static void AddSolrReadSource(this IServiceCollection services, IConfiguration configuration)
    {
        var endpoint = configuration["CQL_SOLR_QUERY_ORIGIN"];
        var username = configuration["CQL_SOLR_QUERY_USERNAME"];
        var password = configuration["CQL_SOLR_QUERY_PASSWORD"];
        if (endpoint == null && username == null && password == null) return;
        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var origin) || origin.Scheme != Uri.UriSchemeHttps ||
            origin.IsLoopback || origin.AbsolutePath != "/" || origin.UserInfo.Length != 0 ||
            origin.Query.Length != 0 || origin.Fragment.Length != 0)
            throw new InvalidOperationException("Solr CQL requires an explicit credential-free HTTPS origin.");
        if (string.IsNullOrWhiteSpace(username) || username.Length > 256 || username.Contains(':') || username.Any(char.IsControl) ||
            string.IsNullOrWhiteSpace(password) || password.Length > 1024 || password.Any(char.IsControl))
            throw new InvalidOperationException("Solr CQL requires explicit restricted credentials.");

        services.AddKeyedSingleton<HttpClient>(ClientKey, (_, _) => new HttpClient(
            new ExactReadHandler(origin, NativeConnectionGuard.CreateGuardedHttpHandler())));
        services.AddSingleton<CqlReadSourceProfile>(provider => new CqlReadSourceProfile(new SolrProvider(
            origin.AbsoluteUri, username, password, provider.GetRequiredKeyedService<HttpClient>(ClientKey))));
    }

    internal sealed class ExactReadHandler(Uri origin, HttpMessageHandler transport) : DelegatingHandler(transport)
    {
        private const string Collection = "/solr/oasis_holons";
        private const string DocumentPrefix = "?id=";
        private const string DocumentSuffix = "&wt=json&omitHeader=false";

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var uri = request.RequestUri;
            if (request.Method != HttpMethod.Get || uri is not { IsAbsoluteUri: true } ||
                !string.Equals(uri.GetLeftPart(UriPartial.Authority), origin.GetLeftPart(UriPartial.Authority), StringComparison.Ordinal) ||
                uri.UserInfo.Length != 0 || uri.Fragment.Length != 0 || !IsExactRead(uri))
                throw new InvalidOperationException("Solr CQL transport admits only exact reads from the configured collection.");
            return base.SendAsync(request, cancellationToken);
        }

        private static bool IsExactRead(Uri uri)
        {
            if (uri.AbsolutePath == Collection + "/schema/uniquekey") return uri.Query == "?wt=json";
            if (uri.AbsolutePath != Collection + "/get" ||
                !uri.Query.StartsWith(DocumentPrefix, StringComparison.Ordinal) ||
                !uri.Query.EndsWith(DocumentSuffix, StringComparison.Ordinal)) return false;
            var key = uri.Query[DocumentPrefix.Length..^DocumentSuffix.Length];
            return Guid.TryParseExact(key, "D", out var id) && id != Guid.Empty && key == id.ToString("D");
        }
    }
}
