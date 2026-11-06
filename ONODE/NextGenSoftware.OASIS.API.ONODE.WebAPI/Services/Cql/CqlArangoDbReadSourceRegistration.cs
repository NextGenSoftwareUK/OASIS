using System;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NextGenSoftware.OASIS.API.Core.Objects.NativeData;
using ArangoProvider = NextGenSoftware.OASIS.API.Providers.ArangoDBOASIS.ArangoDBOASIS;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

public static class CqlArangoDbReadSourceRegistration
{
    private const string ClientKey = "cql-arangodb-source";
    private static readonly string[] Settings = { "ORIGIN", "READER_USERNAME", "READER_PASSWORD", "PROBE_ID" };

    public static void AddArangoDbReadSource(this IServiceCollection services, IConfiguration configuration)
    {
        var values = Settings.Select(name => configuration["CQL_ARANGODB_QUERY_" + name]).ToArray();
        if (values.All(value => value == null)) return;
        if (!Uri.TryCreate(values[0], UriKind.Absolute, out var origin) || origin.Scheme != Uri.UriSchemeHttps ||
            origin.IsLoopback || origin.AbsolutePath != "/" || origin.UserInfo.Length != 0 ||
            origin.Query.Length != 0 || origin.Fragment.Length != 0)
            throw new InvalidOperationException("ArangoDB CQL requires a credential-free HTTPS origin.");
        if (values.Skip(1).Take(2).Any(value => string.IsNullOrWhiteSpace(value) || value.Length > 1024 || value.Any(char.IsControl)) ||
            values[1].Contains(':') || !Guid.TryParseExact(values[3], "D", out var probe) || probe == Guid.Empty)
            throw new InvalidOperationException("ArangoDB CQL requires restricted credentials and an existing probe.");

        services.AddKeyedSingleton<HttpClient>(ClientKey, (_, _) =>
        {
            var transport = (SocketsHttpHandler)NativeConnectionGuard.CreateGuardedHttpHandler();
            transport.UseProxy = false;
            transport.UseCookies = false;
            return new HttpClient(new ExactReadHandler(origin, transport));
        });
        services.AddSingleton<CqlReadSourceProfile>(provider => new CqlReadSourceProfile(new ArangoProvider(
            origin.AbsoluteUri, values[1], values[2], provider.GetRequiredKeyedService<HttpClient>(ClientKey), probe)));
    }

    internal sealed class ExactReadHandler(Uri origin, HttpMessageHandler transport) : DelegatingHandler(transport)
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var uri = request.RequestUri;
            var content = request.Content;
            if (request.Method != HttpMethod.Post || uri is not { IsAbsoluteUri: true } ||
                !string.Equals(uri.GetLeftPart(UriPartial.Authority), origin.GetLeftPart(UriPartial.Authority), StringComparison.Ordinal) ||
                uri.UserInfo.Length != 0 || uri.Query.Length != 0 || uri.Fragment.Length != 0 ||
                uri.AbsolutePath != "/_db/oasis/_api/cursor" || request.Headers.Host != null ||
                content?.Headers.ContentType?.MediaType != "application/json" ||
                content.Headers.ContentLength is not (> 0 and <= 8192))
                throw new InvalidOperationException("ArangoDB CQL admits only the native cursor endpoint at its configured origin.");

            using var document = JsonDocument.Parse(await content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false));
            var query = document.RootElement;
            if (query.ValueKind != JsonValueKind.Object || query.EnumerateObject().Count() != 4 ||
                !query.TryGetProperty("query", out var aql) || aql.ValueKind != JsonValueKind.String ||
                aql.GetString() != ArangoProvider.NativeIdentityQuery ||
                !query.TryGetProperty("batchSize", out var batch) || !HasNumber(batch, 101) ||
                !query.TryGetProperty("options", out var options) || !HasNativeLimits(options) ||
                !query.TryGetProperty("bindVars", out var bindings) || !HasCanonicalKeys(bindings))
                throw new InvalidOperationException("ArangoDB CQL admits only the bounded native document lookup.");
            return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }

        private static bool HasNumber(JsonElement value, int expected) =>
            value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var number) && number == expected;

        private static bool HasNativeLimits(JsonElement options) =>
            options.ValueKind == JsonValueKind.Object && options.EnumerateObject().Count() == 2 &&
            options.TryGetProperty("maxRuntime", out var runtime) && HasNumber(runtime, 15) &&
            options.TryGetProperty("failOnWarning", out var strict) && strict.ValueKind == JsonValueKind.True;

        private static bool HasCanonicalKeys(JsonElement bindings) =>
            bindings.ValueKind == JsonValueKind.Object && bindings.EnumerateObject().Count() == 1 &&
            bindings.TryGetProperty("keys", out var keys) && keys.ValueKind == JsonValueKind.Array &&
            keys.GetArrayLength() is >= 1 and <= 100 && keys.EnumerateArray().All(IsCanonicalKey) &&
            keys.EnumerateArray().Select(key => key.GetString()).Distinct(StringComparer.Ordinal).Count() == keys.GetArrayLength();

        private static bool IsCanonicalKey(JsonElement value) =>
            value.ValueKind == JsonValueKind.String && value.GetString() is string key &&
            Guid.TryParseExact(key, "D", out var id) && id != Guid.Empty && key == id.ToString("D");
    }
}
