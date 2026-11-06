using System;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NextGenSoftware.OASIS.API.Core.Objects.NativeData;
using NextGenSoftware.OASIS.API.Providers.InfluxDBOASIS;
using InfluxProvider = NextGenSoftware.OASIS.API.Providers.InfluxDBOASIS.InfluxDBOASIS;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

public static class CqlInfluxDbReadSourceRegistration
{
    private const string ClientKey = "cql-influxdb-source";
    private static readonly string[] Settings = { "ORIGIN", "ORGANIZATION", "BUCKET", "READER_TOKEN", "PROBE_ID" };

    public static void AddInfluxDbReadSource(this IServiceCollection services, IConfiguration configuration)
    {
        var values = Settings.Select(name => configuration["CQL_INFLUXDB_QUERY_" + name]).ToArray();
        if (values.All(value => value == null)) return;
        if (!Uri.TryCreate(values[0], UriKind.Absolute, out var origin) || origin.Scheme != Uri.UriSchemeHttps ||
            origin.IsLoopback || origin.AbsolutePath != "/" || origin.UserInfo.Length != 0 ||
            origin.Query.Length != 0 || origin.Fragment.Length != 0)
            throw new InvalidOperationException("InfluxDB CQL requires a credential-free HTTPS origin.");
        if (values.Skip(1).Take(2).Any(value => value is not { Length: > 0 and <= 128 } ||
                value.Any(character => !(char.IsAsciiLetterOrDigit(character) || character is '-' or '_' or '.'))) ||
            values[3] is not { Length: > 0 and <= 4096 } || values[3].Any(character => character is < '!' or > '~') ||
            !Guid.TryParseExact(values[4], "D", out var probe) || probe == Guid.Empty)
            throw new InvalidOperationException("InfluxDB CQL requires an explicit organization, bucket, restricted token and existing probe.");

        services.AddKeyedSingleton<HttpClient>(ClientKey, (_, _) =>
        {
            var transport = (SocketsHttpHandler)NativeConnectionGuard.CreateGuardedHttpHandler();
            transport.UseProxy = false;
            transport.UseCookies = false;
            return new HttpClient(new ExactReadHandler(origin, values[1], values[2], transport));
        });
        services.AddSingleton<CqlReadSourceProfile>(provider => new CqlReadSourceProfile(new InfluxProvider(
            origin.AbsoluteUri, values[3], values[1], values[2], provider.GetRequiredKeyedService<HttpClient>(ClientKey), probe)));
    }

    internal sealed class ExactReadHandler(Uri origin, string organization, string bucket, HttpMessageHandler transport)
        : DelegatingHandler(transport)
    {
        private static readonly Regex IdentityLiteral = new(
            @"r\.id == ""((?:\\x[0-9a-f]{2}){36})""", RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100));
        private const int MaxResponseBytes = 8 * 1024 * 1024;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var uri = request.RequestUri;
            var content = request.Content;
            if (request.Method != HttpMethod.Post || uri is not { IsAbsoluteUri: true } ||
                !string.Equals(uri.GetLeftPart(UriPartial.Authority), origin.GetLeftPart(UriPartial.Authority), StringComparison.Ordinal) ||
                uri.UserInfo.Length != 0 || uri.Fragment.Length != 0 || uri.AbsolutePath != "/api/v2/query" ||
                uri.Query != "?org=" + organization || request.Headers.Host != null ||
                content?.Headers.ContentType?.MediaType != "application/json" ||
                content.Headers.ContentLength is not (> 0 and <= 64 * 1024))
                throw new InvalidOperationException("InfluxDB CQL transport admits only native query requests to its configured organization.");

            using var document = JsonDocument.Parse(await content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false));
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object ||
                root.EnumerateObject().Any(property => property.Name is not ("query" or "dialect")) ||
                root.EnumerateObject().Count(property => property.Name == "query") != 1 ||
                root.EnumerateObject().Count(property => property.Name == "dialect") > 1 ||
                !root.TryGetProperty("query", out var query) || query.ValueKind != JsonValueKind.String ||
                !MatchesNativeQuery(query.GetString()))
                throw new InvalidOperationException("InfluxDB CQL transport admits only the compiled identity query for its configured bucket.");

            var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
            try
            {
                // The SDK requests headers-first streaming. Bound bytes before handing
                // the response to its CSV parser, including when length is absent.
                await response.Content.LoadIntoBufferAsync(MaxResponseBytes, cancellationToken).ConfigureAwait(false);
                return response;
            }
            catch
            {
                response.Dispose();
                throw;
            }
        }

        private bool MatchesNativeQuery(string flux)
        {
            var matches = IdentityLiteral.Matches(flux);
            if (matches.Count is < 1 or > 100) return false;
            var ids = new Guid[matches.Count];
            Span<char> text = stackalloc char[36];
            for (var index = 0; index < ids.Length; index++)
            {
                var literal = matches[index].Groups[1].ValueSpan;
                for (var offset = 0; offset < text.Length; offset++)
                    text[offset] = (char)byte.Parse(literal.Slice(offset * 4 + 2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                if (!Guid.TryParseExact(text, "D", out ids[index]) || ids[index] == Guid.Empty) return false;
            }
            return ids.Distinct().Count() == ids.Length && flux == InfluxDBCqlCompiler.BuildNativeFlux(bucket, ids);
        }
    }
}
