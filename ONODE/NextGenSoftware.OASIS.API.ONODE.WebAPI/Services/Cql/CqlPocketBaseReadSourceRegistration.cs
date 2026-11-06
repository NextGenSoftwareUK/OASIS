using System;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NextGenSoftware.OASIS.API.Core.Objects.NativeData;
using NextGenSoftware.OASIS.API.Providers.PocketBaseOASIS;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

public static class CqlPocketBaseReadSourceRegistration
{
    private const string ClientKey = "cql-pocketbase-source";

    public static void AddPocketBaseReadSource(this IServiceCollection services, IConfiguration configuration)
    {
        const string prefix = "CQL_POCKETBASE_QUERY_";
        var endpoint = configuration[prefix + "ORIGIN"];
        var token = configuration[prefix + "TOKEN"];
        var probe = configuration[prefix + "PROBE_ID"];
        if (endpoint == null && token == null && probe == null) return;
        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var origin) || origin.Scheme != Uri.UriSchemeHttps ||
            origin.IsLoopback || origin.AbsolutePath != "/" || origin.UserInfo.Length != 0 ||
            origin.Query.Length != 0 || origin.Fragment.Length != 0)
            throw new InvalidOperationException("PocketBase CQL requires an explicit credential-free HTTPS origin.");
        if (token == null || token.Length is < 16 or > 8192 ||
            token.Any(character => character < 33 || character > 126) ||
            !Guid.TryParseExact(probe, "D", out var id) || id == Guid.Empty)
            throw new InvalidOperationException("PocketBase CQL requires a restricted reader token and an existing probe.");

        services.AddKeyedSingleton<HttpClient>(ClientKey, (_, _) => new HttpClient(
            new ExactReadHandler(origin, NativeConnectionGuard.CreateGuardedHttpHandler()))
            { Timeout = TimeSpan.FromSeconds(15) });
        services.AddSingleton<CqlReadSourceProfile>(provider => new CqlReadSourceProfile(new PocketBaseQuerySource(
            origin.AbsoluteUri, provider.GetRequiredKeyedService<HttpClient>(ClientKey), token, id)));
    }

    internal sealed class ExactReadHandler(Uri origin, HttpMessageHandler transport) : DelegatingHandler(transport)
    {
        private const string Key = "oasis_id='[0-9a-f]{8}(?:-[0-9a-f]{4}){3}-[0-9a-f]{12}'";
        private static readonly Regex ExactSelection = new(
            @"\A\?perPage=100&page=1&sort=oasis_id&skipTotal=false&filter=\(" + Key + @"(?:\|\|" + Key
            + @"){0,99}\)&&is_deleted=false\z", RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100));

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var uri = request.RequestUri;
            if (request.Method != HttpMethod.Get || uri is not { IsAbsoluteUri: true } ||
                !string.Equals(uri.GetLeftPart(UriPartial.Authority), origin.GetLeftPart(UriPartial.Authority), StringComparison.Ordinal) ||
                uri.UserInfo.Length != 0 || uri.Fragment.Length != 0 || uri.AbsolutePath != "/api/collections/oasis_holons/records" ||
                uri.Query.Length > 16384 || !ExactSelection.IsMatch(Uri.UnescapeDataString(uri.Query)))
                throw new InvalidOperationException("PocketBase CQL transport admits only bounded teaching record reads.");
            return base.SendAsync(request, cancellationToken);
        }
    }
}
