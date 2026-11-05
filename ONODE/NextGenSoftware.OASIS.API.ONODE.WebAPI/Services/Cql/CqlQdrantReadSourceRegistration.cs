using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NextGenSoftware.OASIS.API.Core.Objects.NativeData;
using QdrantProvider = NextGenSoftware.OASIS.API.Providers.QdrantOASIS.QdrantOASIS;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

public static class CqlQdrantReadSourceRegistration
{
    private const string ClientKey = "cql-qdrant-source";

    public static void AddQdrantReadSource(this IServiceCollection services, IConfiguration configuration)
    {
        var endpoint = configuration["CQL_QDRANT_QUERY_ORIGIN"];
        var apiKey = configuration["CQL_QDRANT_QUERY_API_KEY"];
        var probeValue = configuration["CQL_QDRANT_QUERY_PROBE_ID"];
        if (endpoint == null && apiKey == null && probeValue == null) return;
        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var origin) || origin.Scheme != Uri.UriSchemeHttps ||
            origin.IsLoopback || origin.AbsolutePath != "/" || origin.UserInfo.Length != 0 ||
            origin.Query.Length != 0 || origin.Fragment.Length != 0)
            throw new InvalidOperationException("Qdrant CQL requires an explicit credential-free HTTPS origin.");
        if (string.IsNullOrWhiteSpace(apiKey) || apiKey.Length > 4096 || apiKey.Any(c => char.IsWhiteSpace(c) || char.IsControl(c)))
            throw new InvalidOperationException("Qdrant CQL requires an explicit restricted API key.");
        if (!Guid.TryParseExact(probeValue, "D", out var probe) || probe == Guid.Empty)
            throw new InvalidOperationException("Qdrant CQL requires an existing public teaching probe identity.");
        services.AddKeyedSingleton<HttpClient>(ClientKey, (_, _) => new HttpClient(
            new ExactReadHandler(origin, NativeConnectionGuard.CreateGuardedHttpHandler())));
        services.AddSingleton<CqlReadSourceProfile>(provider => new CqlReadSourceProfile(new QdrantProvider(
            provider.GetRequiredKeyedService<HttpClient>(ClientKey), origin.AbsoluteUri, apiKey, probe)));
    }

    internal sealed class ExactReadHandler(Uri origin, HttpMessageHandler transport) : DelegatingHandler(transport)
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var uri = request.RequestUri;
            if (request.Method != HttpMethod.Post || uri is not { IsAbsoluteUri: true } ||
                !string.Equals(uri.GetLeftPart(UriPartial.Authority), origin.GetLeftPart(UriPartial.Authority), StringComparison.Ordinal) ||
                uri.UserInfo.Length != 0 || uri.Fragment.Length != 0 ||
                uri.PathAndQuery != "/collections/oasis_holons/points?timeout=15" ||
                request.Content?.Headers.ContentType?.MediaType != "application/json")
                throw new InvalidOperationException("Qdrant CQL transport admits only bounded exact point reads.");
            await request.Content.LoadIntoBufferAsync(8192);
            using var document = JsonDocument.Parse(await request.Content.ReadAsByteArrayAsync(cancellationToken));
            var body = document.RootElement;
            if (body.ValueKind != JsonValueKind.Object || body.EnumerateObject().Count() != 3 ||
                !body.TryGetProperty("with_payload", out var payload) || payload.ValueKind != JsonValueKind.True ||
                !body.TryGetProperty("with_vector", out var vector) || vector.ValueKind != JsonValueKind.False ||
                !body.TryGetProperty("ids", out var ids) || ids.ValueKind != JsonValueKind.Array ||
                ids.GetArrayLength() is < 1 or > 100)
                throw new InvalidOperationException("Qdrant CQL transport admits only bounded exact point reads.");
            var seen = new HashSet<Guid>();
            foreach (var key in ids.EnumerateArray())
                if (key.ValueKind != JsonValueKind.String || !Guid.TryParseExact(key.GetString(), "D", out var id) ||
                    id == Guid.Empty || key.GetString() != id.ToString("D") || !seen.Add(id))
                    throw new InvalidOperationException("Qdrant CQL transport requires distinct canonical point identities.");
            return await base.SendAsync(request, cancellationToken);
        }
    }
}
