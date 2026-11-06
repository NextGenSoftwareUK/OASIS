using System;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NextGenSoftware.OASIS.API.Core.Objects.NativeData;
using NextGenSoftware.OASIS.API.Providers.LanceDBOASIS;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

public static class CqlLanceDbReadSourceRegistration
{
    private const string ClientKey = "cql-lancedb-reader";
    private const string QuotedUuid = "'[0-9a-f]{8}(?:-[0-9a-f]{4}){3}-[0-9a-f]{12}'";
    private static readonly Regex Predicate = new(
        @"\Aid IN \(" + QuotedUuid + "(?:," + QuotedUuid + @"){0,99}\)\z",
        RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100));

    public static void AddLanceDbReadSource(this IServiceCollection services, IConfiguration configuration)
    {
        const string prefix = "CQL_LANCEDB_QUERY_";
        var endpoint = configuration[prefix + "ORIGIN"];
        var database = configuration[prefix + "DATABASE"];
        var key = configuration[prefix + "READER_KEY"];
        var probe = configuration[prefix + "PROBE_ID"];
        if (endpoint == null && database == null && key == null && probe == null) return;
        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var origin) || origin.Scheme != Uri.UriSchemeHttps ||
            origin.HostNameType != UriHostNameType.Dns || origin.Host.Length > 253 ||
            !origin.Host.EndsWith(".api.lancedb.com", StringComparison.Ordinal) ||
            origin.Host.Split('.').Any(label => label.Length is < 1 or > 63 || label[0] == '-' || label[^1] == '-' ||
                label.Any(c => !(char.IsAsciiLetterLower(c) || char.IsAsciiDigit(c) || c == '-'))) ||
            origin.AbsolutePath != "/" || !origin.IsDefaultPort || origin.UserInfo.Length != 0 ||
            origin.Query.Length != 0 || origin.Fragment.Length != 0 || endpoint != origin.GetLeftPart(UriPartial.Authority) ||
            database is not { Length: > 0 and <= 63 } ||
            !char.IsAsciiLetterLower(database[0]) ||
            database.Any(c => !(char.IsAsciiLetterLower(c) || char.IsAsciiDigit(c) || c is '-' or '_')) ||
            key is not { Length: > 0 and <= 1024 } || key.Any(c => char.IsControl(c) || char.IsWhiteSpace(c)) ||
            !Guid.TryParseExact(probe, "D", out var id) || id == Guid.Empty || probe != id.ToString("D"))
            throw new InvalidOperationException("LanceDB CQL requires a canonical origin, database, restricted key and probe.");

        services.AddKeyedSingleton<HttpClient>(ClientKey, (_, _) =>
        {
            var transport = (SocketsHttpHandler)NativeConnectionGuard.CreateGuardedHttpHandler();
            transport.UseProxy = false;
            transport.UseCookies = false;
            return new HttpClient(new ExactReadHandler(origin, database, transport));
        });
        services.AddSingleton<CqlReadSourceProfile>(provider => new CqlReadSourceProfile(new LanceDbQuerySource(
            provider.GetRequiredKeyedService<HttpClient>(ClientKey), origin, key, database, id)));
    }

    internal sealed class ExactReadHandler(Uri origin, string database, HttpMessageHandler transport) : DelegatingHandler(transport)
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var uri = request.RequestUri;
            var content = request.Content;
            if (request.Method != HttpMethod.Post || request.Headers.Host != null ||
                uri is not { IsAbsoluteUri: true } || uri.UserInfo.Length != 0 || uri.Query.Length != 0 || uri.Fragment.Length != 0 ||
                !string.Equals(uri.GetLeftPart(UriPartial.Authority), origin.GetLeftPart(UriPartial.Authority), StringComparison.Ordinal) ||
                uri.AbsolutePath != "/v1/table/oasis_holons/query/" ||
                !request.Headers.TryGetValues("x-lancedb-database", out var databases) ||
                !databases.SequenceEqual(new[] { database }, StringComparer.Ordinal) ||
                request.Headers.Accept.Count != 1 || request.Headers.Accept.Single().MediaType != "application/vnd.apache.arrow.file" ||
                content?.Headers.ContentType?.MediaType != "application/json" || content.Headers.ContentLength is not (> 0 and <= 8192))
                throw new InvalidOperationException("LanceDB CQL admits only bounded Arrow queries at the reviewed table.");

            using var document = JsonDocument.Parse(await content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false));
            var body = document.RootElement;
            if (body.ValueKind != JsonValueKind.Object || body.EnumerateObject().Count() != 7 ||
                !body.TryGetProperty("filter", out var filter) || filter.ValueKind != JsonValueKind.String ||
                filter.GetString() is not { Length: > 0 and <= 4000 } predicate || !Predicate.IsMatch(predicate) ||
                !body.TryGetProperty("prefilter", out var prefilter) || prefilter.ValueKind != JsonValueKind.True ||
                !body.TryGetProperty("vector", out var vector) || vector.ValueKind != JsonValueKind.Array || vector.GetArrayLength() != 0 ||
                !body.TryGetProperty("version", out var version) || version.ValueKind != JsonValueKind.Null ||
                !body.TryGetProperty("offset", out var offset) || offset.ValueKind != JsonValueKind.Number ||
                !offset.TryGetInt32(out var skip) || skip != 0 ||
                !body.TryGetProperty("k", out var k) || k.ValueKind != JsonValueKind.Number || !k.TryGetInt32(out var limit) ||
                !body.TryGetProperty("columns", out var columns) || columns.ValueKind != JsonValueKind.Array ||
                columns.GetArrayLength() != 2 || columns[0].ValueKind != JsonValueKind.String || columns[0].GetString() != "id" ||
                columns[1].ValueKind != JsonValueKind.String || columns[1].GetString() != "doc")
                throw new InvalidOperationException("LanceDB CQL requires the exact UUID filter and Arrow projection.");

            var keys = predicate[7..^1].Split(',').Select(value => Guid.ParseExact(value.AsSpan(1, 36), "D")).ToArray();
            if (limit != keys.Length + 1 || keys.Contains(Guid.Empty) || keys.Distinct().Count() != keys.Length)
                throw new InvalidOperationException("LanceDB CQL requires a bounded limit and distinct nonempty UUIDs.");
            return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }
    }
}
