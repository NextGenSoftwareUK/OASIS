using System;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NextGenSoftware.OASIS.API.Core.Objects.NativeData;
using NextGenSoftware.OASIS.API.Providers.CouchbaseOASIS;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

public static class CqlCouchbaseReadSourceRegistration
{
    private const string ClientKey = "cql-couchbase-source";
    private static readonly string[] Settings = { "ORIGIN", "BUCKET", "READER_USERNAME", "READER_PASSWORD", "PROBE_ID" };

    public static void AddCouchbaseReadSource(this IServiceCollection services, IConfiguration configuration)
    {
        var values = Settings.Select(name => configuration["CQL_COUCHBASE_QUERY_" + name]).ToArray();
        if (values.All(value => value == null)) return;
        if (!Uri.TryCreate(values[0], UriKind.Absolute, out var origin) || origin.Scheme != Uri.UriSchemeHttps ||
            origin.IsLoopback || origin.AbsolutePath != "/" || origin.UserInfo.Length != 0 ||
            origin.Query.Length != 0 || origin.Fragment.Length != 0)
            throw new InvalidOperationException("Couchbase CQL requires a credential-free HTTPS Query Service origin.");
        if (values.Skip(2).Take(2).Any(value => string.IsNullOrWhiteSpace(value) || value.Length > 1024 || value.Any(char.IsControl)) ||
            values[2].Contains(':') || !Guid.TryParseExact(values[4], "D", out var probe) || probe == Guid.Empty)
            throw new InvalidOperationException("Couchbase CQL requires restricted credentials and an existing probe.");
        var statement = CouchbaseCqlCompiler.NativeIdentityStatement(values[1]);

        services.AddKeyedSingleton<HttpClient>(ClientKey, (_, _) =>
        {
            var transport = (SocketsHttpHandler)NativeConnectionGuard.CreateGuardedHttpHandler();
            transport.UseProxy = false;
            transport.UseCookies = false;
            return new HttpClient(new ExactReadHandler(origin, statement, transport));
        });
        services.AddSingleton<CqlReadSourceProfile>(provider => new CqlReadSourceProfile(new CouchbaseQuerySource(
            origin, values[1], values[2], values[3], provider.GetRequiredKeyedService<HttpClient>(ClientKey), probe)));
    }

    internal sealed class ExactReadHandler(Uri origin, string statement, HttpMessageHandler transport)
        : DelegatingHandler(transport)
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var uri = request.RequestUri;
            var content = request.Content;
            if (request.Method != HttpMethod.Post || uri is not { IsAbsoluteUri: true } ||
                !string.Equals(uri.GetLeftPart(UriPartial.Authority), origin.GetLeftPart(UriPartial.Authority), StringComparison.Ordinal) ||
                uri.UserInfo.Length != 0 || uri.Query.Length != 0 || uri.Fragment.Length != 0 ||
                uri.AbsolutePath != "/query/service" || request.Headers.Host != null ||
                content?.Headers.ContentType?.MediaType != "application/json" ||
                content.Headers.ContentLength is not (> 0 and <= 8192))
                throw new InvalidOperationException("Couchbase CQL admits only native query requests at its configured origin.");

            using var document = JsonDocument.Parse(await content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false));
            var query = document.RootElement;
            if (query.ValueKind != JsonValueKind.Object || query.EnumerateObject().Count() != 4 ||
                !query.TryGetProperty("statement", out var sql) || sql.ValueKind != JsonValueKind.String || sql.GetString() != statement ||
                !query.TryGetProperty("readonly", out var readOnly) || readOnly.ValueKind != JsonValueKind.True ||
                !query.TryGetProperty("timeout", out var timeout) || timeout.ValueKind != JsonValueKind.String || timeout.GetString() != "15s" ||
                !query.TryGetProperty("$keys", out var keys) || keys.ValueKind != JsonValueKind.Array || keys.GetArrayLength() is < 1 or > 100 ||
                keys.EnumerateArray().Any(key => !IsCanonicalId(key)) ||
                keys.EnumerateArray().Select(key => key.GetString()).Distinct(StringComparer.Ordinal).Count() != keys.GetArrayLength())
                throw new InvalidOperationException("Couchbase CQL admits only the bounded read-only statement for its configured bucket.");
            return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }

        private static bool IsCanonicalId(JsonElement value) =>
            value.ValueKind == JsonValueKind.String && value.GetString() is string text &&
            Guid.TryParseExact(text, "D", out var id) && id != Guid.Empty && text == id.ToString("D");
    }
}
