using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NextGenSoftware.OASIS.API.Core.Objects.NativeData;
using NextGenSoftware.OASIS.API.Providers.CouchDBOASIS;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

public static class CqlCouchDbReadSourceRegistration
{
    public static void AddCouchDbReadSource(IServiceCollection services, IConfiguration configuration)
    {
        const string prefix = "CQL_COUCHDB_QUERY_";
        var endpoint = configuration[prefix + "ENDPOINT"];
        var username = configuration[prefix + "USERNAME"];
        var password = configuration[prefix + "PASSWORD"];
        var probe = configuration[prefix + "PROBE_ID"];
        if (endpoint == null && username == null && password == null && probe == null) return;

        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var origin) || origin.Scheme != Uri.UriSchemeHttps ||
            origin.IsLoopback || origin.AbsolutePath != "/" || origin.UserInfo.Length != 0 ||
            origin.Query.Length != 0 || origin.Fragment.Length != 0)
            throw new InvalidOperationException("CouchDB CQL requires a credential-free HTTPS origin.");
        if (string.IsNullOrWhiteSpace(username) || username.Contains(':') || string.IsNullOrWhiteSpace(password) ||
            !Guid.TryParseExact(probe, "D", out var id) || id == Guid.Empty)
            throw new InvalidOperationException("CouchDB CQL requires restricted reader credentials and an existing public probe.");

        const string clientKey = "cql-couchdb-source";
        services.AddKeyedSingleton<HttpClient>(clientKey, (_, _) =>
            new HttpClient(new ExactReadHandler(origin, NativeConnectionGuard.CreateGuardedHttpHandler())));
        services.AddSingleton<CqlReadSourceProfile>(provider => new CqlReadSourceProfile(new CouchDbQuerySource(
            origin.AbsoluteUri, username, password, provider.GetRequiredKeyedService<HttpClient>(clientKey), id)));
    }

    internal sealed class ExactReadHandler(Uri origin, HttpMessageHandler transport) : DelegatingHandler(transport)
    {
        private const string DocumentPath = "/oasis_holons/";

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var uri = request.RequestUri;
            if (request.Method != HttpMethod.Get || uri is not { IsAbsoluteUri: true } ||
                !string.Equals(uri.GetLeftPart(UriPartial.Authority), origin.GetLeftPart(UriPartial.Authority), StringComparison.Ordinal) ||
                uri.UserInfo.Length != 0 || uri.Fragment.Length != 0 || uri.Query != "?conflicts=true" ||
                !uri.AbsolutePath.StartsWith(DocumentPath, StringComparison.Ordinal) ||
                !Guid.TryParseExact(uri.AbsolutePath.AsSpan(DocumentPath.Length), "D", out var id) || id == Guid.Empty)
                throw new InvalidOperationException("CouchDB CQL transport admits only exact teaching document reads.");

            return base.SendAsync(request, cancellationToken);
        }
    }
}
