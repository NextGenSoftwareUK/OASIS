using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NextGenSoftware.OASIS.API.Core.Objects.NativeData;
using TypesenseProvider = NextGenSoftware.OASIS.API.Providers.TypesenseOASIS.TypesenseOASIS;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

public static class CqlTypesenseReadSourceRegistration
{
    private const string ClientKey = "cql-typesense-source";

    public static void AddTypesenseReadSource(this IServiceCollection services, IConfiguration configuration)
    {
        var endpoint = configuration["CQL_TYPESENSE_QUERY_ORIGIN"];
        var key = configuration["CQL_TYPESENSE_QUERY_API_KEY"];
        if (endpoint == null && key == null) return;
        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var origin) || origin.Scheme != Uri.UriSchemeHttps ||
            origin.IsLoopback || origin.AbsolutePath != "/" || origin.UserInfo.Length != 0 ||
            origin.Query.Length != 0 || origin.Fragment.Length != 0)
            throw new InvalidOperationException("Typesense CQL requires an explicit credential-free HTTPS origin.");
        if (key == null || key.Length is < 16 or > 256 || key.Any(character => character < 33 || character > 126))
            throw new InvalidOperationException("Typesense CQL requires an explicit restricted API key.");

        services.AddKeyedSingleton<HttpClient>(ClientKey, (_, _) => new HttpClient(
            new ExactReadHandler(origin, NativeConnectionGuard.CreateGuardedHttpHandler())));
        services.AddSingleton<CqlReadSourceProfile>(provider => new CqlReadSourceProfile(new TypesenseProvider(
            origin.AbsoluteUri, key, provider.GetRequiredKeyedService<HttpClient>(ClientKey))));
    }

    internal sealed class ExactReadHandler(Uri origin, HttpMessageHandler transport) : DelegatingHandler(transport)
    {
        private const string CollectionPath = "/collections/oasis_holons";
        private const string DocumentPrefix = CollectionPath + "/documents/";

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var uri = request.RequestUri;
            if (request.Method != HttpMethod.Get || uri is not { IsAbsoluteUri: true } ||
                !string.Equals(uri.GetLeftPart(UriPartial.Authority), origin.GetLeftPart(UriPartial.Authority), StringComparison.Ordinal) ||
                uri.UserInfo.Length != 0 || uri.Query.Length != 0 || uri.Fragment.Length != 0 || !IsExactRead(uri.AbsolutePath))
                throw new InvalidOperationException("Typesense CQL transport admits only exact reads from the configured collection.");
            return base.SendAsync(request, cancellationToken);
        }

        private static bool IsExactRead(string path)
        {
            if (path == CollectionPath) return true;
            if (!path.StartsWith(DocumentPrefix, StringComparison.Ordinal)) return false;
            var key = path[DocumentPrefix.Length..];
            return Guid.TryParseExact(key, "D", out var id) && id != Guid.Empty && key == id.ToString("D");
        }
    }
}
