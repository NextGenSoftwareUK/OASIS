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
using NextGenSoftware.OASIS.API.Providers.SupabaseOASIS;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

public static class CqlSupabaseReadSourceRegistration
{
    private const string ClientKey = "cql-supabase-source";

    public static void AddSupabaseReadSource(this IServiceCollection services, IConfiguration configuration)
    {
        const string prefix = "CQL_SUPABASE_QUERY_";
        var endpoint = configuration[prefix + "ORIGIN"];
        var key = configuration[prefix + "GATEWAY_KEY"];
        var reader = configuration[prefix + "READER_TOKEN"];
        var probe = configuration[prefix + "PROBE_ID"];
        if (endpoint == null && key == null && reader == null && probe == null) return;
        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var origin) || origin.Scheme != Uri.UriSchemeHttps ||
            origin.IsLoopback || origin.AbsolutePath != "/" || origin.UserInfo.Length != 0 ||
            origin.Query.Length != 0 || origin.Fragment.Length != 0)
            throw new InvalidOperationException("Supabase CQL requires an explicit credential-free HTTPS origin.");
        if (!IsPublicGatewayKey(key) || !HasRole(reader, "authenticated") ||
            !Guid.TryParseExact(probe, "D", out var id) || id == Guid.Empty)
            throw new InvalidOperationException("Supabase CQL requires a public gateway key, restricted reader JWT and existing probe.");

        services.AddKeyedSingleton<HttpClient>(ClientKey, (_, _) => new HttpClient(
            new ExactReadHandler(origin, NativeConnectionGuard.CreateGuardedHttpHandler())));
        services.AddSingleton<CqlReadSourceProfile>(provider => new CqlReadSourceProfile(new SupabaseQuerySource(
            origin.AbsoluteUri, key, reader, provider.GetRequiredKeyedService<HttpClient>(ClientKey), id)));
    }

    private static bool IsPublicGatewayKey(string value) =>
        (value is { Length: > 15 and <= 256 } && value.StartsWith("sb_publishable_", StringComparison.Ordinal)
            && value.All(character => char.IsAsciiLetterOrDigit(character) || character is '_' or '-'))
        || HasRole(value, "anon");

    // Reject misconfigured privileged credentials before sending them. Supabase
    // still owns JWT validation and all record authorization through row policies.
    private static bool HasRole(string token, string expected)
    {
        if (token is not { Length: > 0 and <= 8192 } ||
            token.Any(character => !(char.IsAsciiLetterOrDigit(character) || character is '_' or '-' or '.')))
            return false;
        var parts = token.Split('.');
        if (parts.Length != 3 || parts.Any(string.IsNullOrEmpty)) return false;
        try
        {
            var payload = parts[1].Replace('-', '+').Replace('_', '/');
            using var json = JsonDocument.Parse(Convert.FromBase64String(payload.PadRight((payload.Length + 3) / 4 * 4, '=')));
            if (json.RootElement.ValueKind != JsonValueKind.Object) return false;
            var roles = json.RootElement.EnumerateObject().Where(property => property.NameEquals("role")).ToArray();
            return roles.Length == 1 && roles[0].Value.ValueKind == JsonValueKind.String
                && roles[0].Value.GetString() == expected;
        }
        catch (FormatException) { return false; }
        catch (JsonException) { return false; }
    }

    internal sealed class ExactReadHandler(Uri origin, HttpMessageHandler transport) : DelegatingHandler(transport)
    {
        private const string Key = "[0-9a-f]{8}(?:-[0-9a-f]{4}){3}-[0-9a-f]{12}";
        private static readonly Regex ExactSelection = new(
            @"\A\?id=in\.\(" + Key + "(?:," + Key + @"){0,99}\)&is_deleted=eq.false&order=id.asc&limit=100\z",
            RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100));

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var uri = request.RequestUri;
            if (request.Method != HttpMethod.Get || uri is not { IsAbsoluteUri: true } ||
                !string.Equals(uri.GetLeftPart(UriPartial.Authority), origin.GetLeftPart(UriPartial.Authority), StringComparison.Ordinal) ||
                uri.UserInfo.Length != 0 || uri.Fragment.Length != 0 || uri.AbsolutePath != "/rest/v1/oasis_holons" ||
                uri.Query.Length > 8192 || !ExactSelection.IsMatch(uri.Query))
                throw new InvalidOperationException("Supabase CQL transport admits only bounded teaching record reads.");
            return base.SendAsync(request, cancellationToken);
        }
    }
}
