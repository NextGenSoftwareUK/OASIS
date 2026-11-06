using System;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NextGenSoftware.OASIS.API.Core.Objects.NativeData;
using UpstashProvider = NextGenSoftware.OASIS.API.Providers.UpstashOASIS.UpstashOASIS;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

public static class CqlUpstashReadSourceRegistration
{
    private const string ClientKey = "cql-upstash-source";

    public static void AddUpstashReadSource(this IServiceCollection services, IConfiguration configuration)
    {
        const string prefix = "CQL_UPSTASH_QUERY_";
        var endpoint = configuration[prefix + "ORIGIN"];
        var token = configuration[prefix + "READER_TOKEN"];
        var probe = configuration[prefix + "PROBE_ID"];
        if (endpoint == null && token == null && probe == null) return;
        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var origin) ||
            origin.Scheme != Uri.UriSchemeHttps || !origin.IsDefaultPort ||
            !origin.Host.EndsWith(".upstash.io", StringComparison.Ordinal) ||
            origin.AbsolutePath != "/" || origin.UserInfo.Length != 0 ||
            origin.Query.Length != 0 || origin.Fragment.Length != 0)
            throw new InvalidOperationException("Upstash CQL requires a credential-free native HTTPS origin.");
        if (token is not { Length: > 0 and <= 4096 } || token.Any(character => character is < '!' or > '~') ||
            !Guid.TryParseExact(probe, "D", out var id) || id == Guid.Empty)
            throw new InvalidOperationException("Upstash CQL requires a restricted REST token and existing probe.");

        services.AddKeyedSingleton<HttpClient>(ClientKey, (_, _) =>
        {
            var transport = (SocketsHttpHandler)NativeConnectionGuard.CreateGuardedHttpHandler();
            transport.UseProxy = false;
            transport.UseCookies = false;
            return new HttpClient(new ExactReadHandler(origin, transport))
            {
                Timeout = TimeSpan.FromSeconds(15),
                MaxResponseContentBufferSize = 8 * 1024 * 1024
            };
        });
        services.AddSingleton<CqlReadSourceProfile>(provider => new CqlReadSourceProfile(new UpstashProvider(
            origin.AbsoluteUri, token, provider.GetRequiredKeyedService<HttpClient>(ClientKey), id)));
    }

    internal sealed class ExactReadHandler(Uri origin, HttpMessageHandler transport) : DelegatingHandler(transport)
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var uri = request.RequestUri;
            var content = request.Content;
            if (request.Method != HttpMethod.Post || uri is not { IsAbsoluteUri: true } ||
                !string.Equals(uri.GetLeftPart(UriPartial.Authority), origin.GetLeftPart(UriPartial.Authority), StringComparison.Ordinal) ||
                uri.UserInfo.Length != 0 || uri.Fragment.Length != 0 || uri.Query.Length != 0 || uri.AbsolutePath != "/" ||
                request.Headers.Host != null || content?.Headers.ContentType?.MediaType != "application/json" ||
                content.Headers.ContentLength is not (> 0 and <= 4608))
                throw new InvalidOperationException("Upstash CQL transport admits only native JSON reads at its configured origin.");

            using var document = JsonDocument.Parse(await content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false));
            var command = document.RootElement;
            if (command.ValueKind != JsonValueKind.Array || command.GetArrayLength() is < 2 or > 101 ||
                command[0].ValueKind != JsonValueKind.String || command[0].GetString() != "MGET" ||
                command.EnumerateArray().Skip(1).Any(key => !IsCanonicalHolonKey(key)))
                throw new InvalidOperationException("Upstash CQL transport admits only bounded native MGET Holon keys.");
            return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }

        private static bool IsCanonicalHolonKey(JsonElement value)
        {
            if (value.ValueKind != JsonValueKind.String || value.GetString() is not { Length: 42 } key ||
                !key.StartsWith("holon:", StringComparison.Ordinal) ||
                !Guid.TryParseExact(key.AsSpan(6), "D", out var id) || id == Guid.Empty)
                return false;
            return key.AsSpan(6).SequenceEqual(id.ToString("D"));
        }
    }
}
