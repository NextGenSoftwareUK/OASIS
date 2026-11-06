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
using NextGenSoftware.OASIS.API.Providers.FirebaseOASIS;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

public static class CqlFirebaseReadSourceRegistration
{
    private const string ClientKey = "cql-firebase-source";
    private const string RedactionSwitch = "System.Net.Http.DisableUriRedaction";

    public static void AddFirebaseReadSource(this IServiceCollection services, IConfiguration configuration)
    {
        const string prefix = "CQL_FIREBASE_QUERY_";
        var endpoint = configuration[prefix + "ORIGIN"];
        var project = configuration[prefix + "PROJECT_ID"];
        var reader = configuration[prefix + "READER_TOKEN"];
        var probe = configuration[prefix + "PROBE_ID"];
        if (endpoint == null && project == null && reader == null && probe == null) return;
        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var origin) || origin.Scheme != Uri.UriSchemeHttps ||
            !origin.IsDefaultPort || origin.AbsolutePath != "/" || origin.UserInfo.Length != 0 ||
            origin.Query.Length != 0 || origin.Fragment.Length != 0 || !IsDatabaseHost(origin.IdnHost))
            throw new InvalidOperationException("Firebase CQL requires an explicit managed Realtime Database HTTPS origin.");
        if (project == null || !Regex.IsMatch(project, @"\A[a-z][a-z0-9-]{4,28}[a-z0-9]\z") ||
            !IsProjectIdToken(reader, project) || !Guid.TryParseExact(probe, "D", out var id) || id == Guid.Empty)
            throw new InvalidOperationException("Firebase CQL requires a project-bound reader ID token and existing public probe.");
        var environmentOverride = Environment.GetEnvironmentVariable("DOTNET_SYSTEM_NET_HTTP_DISABLEURIREDACTION");
        if ((AppContext.TryGetSwitch(RedactionSwitch, out var disabled) && disabled) ||
            environmentOverride == "1" || string.Equals(environmentOverride, "true", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Firebase CQL requires HTTP URI query redaction.");

        services.AddKeyedSingleton<HttpClient>(ClientKey, (_, _) =>
        {
            var transport = (SocketsHttpHandler)NativeConnectionGuard.CreateGuardedHttpHandler();
            transport.UseProxy = false;
            return new HttpClient(new ExactReadHandler(origin, reader, transport));
        });
        services.AddSingleton<CqlReadSourceProfile>(provider => new CqlReadSourceProfile(new FirebaseQuerySource(
            origin.AbsoluteUri, reader, provider.GetRequiredKeyedService<HttpClient>(ClientKey), id)));
    }

    private static bool IsDatabaseHost(string host) =>
        Regex.IsMatch(host, @"\A[a-z0-9-]+(?:\.[a-z0-9-]+)?\.(?:firebaseio\.com|firebasedatabase\.app)\z");

    // A configuration check, independent of Firebase's signature validation and Security Rules.
    private static bool IsProjectIdToken(string token, string project)
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
            var claims = json.RootElement.EnumerateObject().ToDictionary(property => property.Name, property => property.Value);
            return claims.TryGetValue("aud", out var audience) && audience.ValueKind == JsonValueKind.String &&
                audience.GetString() == project && claims.TryGetValue("iss", out var issuer) &&
                issuer.ValueKind == JsonValueKind.String && issuer.GetString() == "https://securetoken.google.com/" + project &&
                claims.TryGetValue("sub", out var subject) && subject.ValueKind == JsonValueKind.String &&
                !string.IsNullOrWhiteSpace(subject.GetString());
        }
        catch (Exception cause) when (cause is FormatException or JsonException or InvalidOperationException or ArgumentException)
        { return false; }
    }

    internal sealed class ExactReadHandler(Uri origin, string reader, HttpMessageHandler transport) : DelegatingHandler(transport)
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var uri = request.RequestUri;
            if (request.Method != HttpMethod.Get || uri is not { IsAbsoluteUri: true } ||
                !string.Equals(uri.GetLeftPart(UriPartial.Authority), origin.GetLeftPart(UriPartial.Authority), StringComparison.Ordinal) ||
                uri.UserInfo.Length != 0 || uri.Fragment.Length != 0 || uri.AbsolutePath != "/oasis_holons.json" ||
                !IsExactRead(uri.Query, reader))
                throw new InvalidOperationException("Firebase CQL transport admits only exact teaching record reads.");
            return base.SendAsync(request, cancellationToken);
        }

        private static bool IsExactRead(string query, string reader)
        {
            const string prefix = "?orderBy=%22%24key%22&equalTo=%22";
            const string separator = "%22&auth=";
            if (!query.StartsWith(prefix, StringComparison.Ordinal)) return false;
            var suffix = separator + reader;
            if (!query.EndsWith(suffix, StringComparison.Ordinal) || query.Length != prefix.Length + 36 + suffix.Length)
                return false;
            var value = query.Substring(prefix.Length, 36);
            return Guid.TryParseExact(value, "D", out var id) && id != Guid.Empty && value == id.ToString("D");
        }
    }
}
