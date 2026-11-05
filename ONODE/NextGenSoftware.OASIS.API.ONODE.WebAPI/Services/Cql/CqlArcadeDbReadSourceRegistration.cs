using System;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NextGenSoftware.OASIS.API.Core.Objects.NativeData;
using ArcadeProvider = NextGenSoftware.OASIS.API.Providers.ArcadeDBOASIS.ArcadeDBOASIS;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

public static class CqlArcadeDbReadSourceRegistration
{
    private const string ClientKey = "cql-arcadedb-source";

    public static void AddArcadeDbReadSource(this IServiceCollection services, IConfiguration configuration)
    {
        var endpoint = configuration["CQL_ARCADEDB_QUERY_ORIGIN"];
        var username = configuration["CQL_ARCADEDB_QUERY_USERNAME"];
        var password = configuration["CQL_ARCADEDB_QUERY_PASSWORD"];
        var probeValue = configuration["CQL_ARCADEDB_QUERY_PROBE_ID"];
        if (endpoint == null && username == null && password == null && probeValue == null) return;
        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var origin) || origin.Scheme != Uri.UriSchemeHttps ||
            origin.IsLoopback || origin.AbsolutePath != "/" || origin.UserInfo.Length != 0 ||
            origin.Query.Length != 0 || origin.Fragment.Length != 0)
            throw new InvalidOperationException("ArcadeDB CQL requires an explicit credential-free HTTPS origin.");
        if (!Guid.TryParseExact(probeValue, "D", out var probe) || probe == Guid.Empty)
            throw new InvalidOperationException("ArcadeDB CQL requires an existing teaching probe identity.");
        if (string.IsNullOrWhiteSpace(username) || username.Contains(':') || username.Length > 256 || username.Any(char.IsControl) ||
            string.IsNullOrWhiteSpace(password) || password.Length > 1024 || password.Any(char.IsControl))
            throw new InvalidOperationException("ArcadeDB CQL requires an explicit restricted database account.");
        services.AddKeyedSingleton<HttpClient>(ClientKey, (_, _) => new HttpClient(
            new ExactReadHandler(origin, NativeConnectionGuard.CreateGuardedHttpHandler())));
        services.AddSingleton<CqlReadSourceProfile>(provider => new CqlReadSourceProfile(new ArcadeProvider(
            origin.AbsoluteUri, username, password, probe, provider.GetRequiredKeyedService<HttpClient>(ClientKey))));
    }

    internal sealed class ExactReadHandler(Uri origin, HttpMessageHandler transport) : DelegatingHandler(transport)
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var uri = request.RequestUri;
            if (request.Method != HttpMethod.Post || uri is not { IsAbsoluteUri: true } ||
                !string.Equals(uri.GetLeftPart(UriPartial.Authority), origin.GetLeftPart(UriPartial.Authority), StringComparison.Ordinal) ||
                uri.UserInfo.Length != 0 || uri.Query.Length != 0 || uri.Fragment.Length != 0 ||
                uri.AbsolutePath != ArcadeProvider.CqlExactRecordRoute ||
                request.Content?.Headers.ContentType?.MediaType != "application/json")
                throw new InvalidOperationException("ArcadeDB CQL transport admits only exact parameterized reads.");

            await request.Content.LoadIntoBufferAsync(4096);
            using var document = JsonDocument.Parse(await request.Content.ReadAsByteArrayAsync(cancellationToken));
            var body = document.RootElement;
            if (body.ValueKind != JsonValueKind.Object || body.EnumerateObject().Count() != 3 ||
                !IsString(body, "language", "sql") || !IsString(body, "command", ArcadeProvider.CqlExactRecordQuery) ||
                !body.TryGetProperty("params", out var parameters) || parameters.ValueKind != JsonValueKind.Object ||
                parameters.EnumerateObject().Count() != 1 || !parameters.TryGetProperty("id", out var id) ||
                id.ValueKind != JsonValueKind.String || !Guid.TryParseExact(id.GetString(), "D", out var key) ||
                key == Guid.Empty || id.GetString() != key.ToString("D"))
                throw new InvalidOperationException("ArcadeDB CQL transport admits only exact parameterized reads.");
            return await base.SendAsync(request, cancellationToken);
        }

        private static bool IsString(JsonElement element, string name, string value)
            => element.TryGetProperty(name, out var item) && item.ValueKind == JsonValueKind.String && item.GetString() == value;
    }
}
