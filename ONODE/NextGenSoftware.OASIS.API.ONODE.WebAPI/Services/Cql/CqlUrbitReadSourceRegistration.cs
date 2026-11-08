using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Objects.NativeData;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Providers.UrbitOASIS;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

/// <summary>Explicit private, query-only book source. Never logs in, imports or enables canonical storage.</summary>
public static class CqlUrbitReadSourceRegistration
{
    private const string ClientKey = "cql-urbit-private-book";
    private const string Prefix = "CQL_URBIT_QUERY_";

    public static void AddUrbitReadSource(this IServiceCollection services, IConfiguration configuration)
    {
        var names = new[] { "ORIGIN", "SHIP", "SESSION_TOKEN", "OWNER", "CONNECTION_ID", "PAGE_IDS" };
        var values = names.ToDictionary(name => name, name => configuration[Prefix + name]);
        if (values.Values.All(value => value == null)) return;
        if (values.Values.Any(string.IsNullOrEmpty)) throw InvalidConfiguration();
        if (!Uri.TryCreate(values["ORIGIN"], UriKind.Absolute, out var origin)
            || origin.Scheme != Uri.UriSchemeHttps || origin.IsLoopback || origin.AbsolutePath != "/"
            || origin.UserInfo.Length != 0 || origin.Query.Length != 0 || origin.Fragment.Length != 0)
            throw InvalidConfiguration();
        var ship = values["SHIP"];
        var token = values["SESSION_TOKEN"];
        if (ship.Length > 64 || !Regex.IsMatch(ship, @"\A~(?:[a-z]{3}|[a-z]{6}(?:-{1,2}[a-z]{6}){0,7})\z")
            || token.Length is < 3 or > 256 || !Regex.IsMatch(token, @"\A0v[0-9a-v]+(?:\.[0-9a-v]+)*\z")
            || !Guid.TryParseExact(values["OWNER"], "D", out var owner) || owner == Guid.Empty
            || !Guid.TryParseExact(values["CONNECTION_ID"], "D", out var connection) || connection == Guid.Empty)
            throw InvalidConfiguration();
        string[] cids;
        Guid[] ids;
        try
        {
            if (values["PAGE_IDS"].Length > 4096) throw InvalidConfiguration();
            cids = JsonSerializer.Deserialize<string[]>(values["PAGE_IDS"]);
            if (cids == null || cids.Length is < 1 or > 32 || cids.Distinct(StringComparer.Ordinal).Count() != cids.Length)
                throw InvalidConfiguration();
            ids = cids.Select(cid => UrbitContactBookQuerySource.LogicalId(owner, connection, cid)).ToArray();
        }
        catch (JsonException) { throw InvalidConfiguration(); }
        catch (ArgumentException) { throw InvalidConfiguration(); }

        services.AddKeyedSingleton<HttpClient>(ClientKey, (_, _) =>
        {
            var transport = (SocketsHttpHandler)NativeConnectionGuard.CreateGuardedHttpHandler();
            transport.UseProxy = false;
            transport.UseCookies = false;
            return new HttpClient(new ExactReadHandler(origin, cids, "urbauth-" + ship + "=" + token, transport));
        });
        services.AddSingleton<CqlReadSourceProfile>(provider => new CqlReadSourceProfile(
            new UrbitContactBookQuerySource(new UrbitContactsV1Client(origin,
                provider.GetRequiredKeyedService<HttpClient>(ClientKey)), owner, connection, cids, ownsClient: true)));
        services.AddKeyedSingleton<ICqlReadAccessPolicy>(ProviderType.UrbitOASIS,
            (_, _) => new OwnerPolicy(owner, ids));
    }

    private static InvalidOperationException InvalidConfiguration() =>
        new("Urbit private CQL requires a credential-free HTTPS origin, ship/session, owner, stable connection and bounded CID catalogue.");

    internal sealed class OwnerPolicy(Guid owner, IEnumerable<Guid> ids) : ICqlReadAccessPolicy
    {
        private readonly Guid[] catalogue = ids.ToArray();
        public OASISResult<CosmicQueryReadAccess> Authorize(Guid authenticatedActor, ProviderType authority)
        {
            if (authenticatedActor != owner || authority != ProviderType.UrbitOASIS)
                return new() { IsError = true, ErrorCount = 1, ErrorCode = "CQL_ACCESS_FORBIDDEN",
                    Message = "The configured private book belongs to another source owner or authority." };
            return CosmicQueryReadAccess.AuthoritativeRecords(owner, authority, catalogue);
        }
    }

    internal sealed class ExactReadHandler : DelegatingHandler
    {
        private readonly Uri origin;
        private readonly HashSet<string> paths;
        private readonly string cookie;
        internal ExactReadHandler(Uri origin, IEnumerable<string> cids, string cookie, HttpMessageHandler transport) : base(transport)
        {
            this.origin = origin;
            this.cookie = cookie;
            paths = cids.Select(cid => "/~/scry/contacts/v1/book/id/" + cid + ".json").ToHashSet(StringComparer.Ordinal);
        }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var uri = request.RequestUri;
            if (request.Method != HttpMethod.Get || request.Content != null || uri is not { IsAbsoluteUri: true }
                || uri.GetLeftPart(UriPartial.Authority) != origin.GetLeftPart(UriPartial.Authority)
                || uri.UserInfo.Length != 0 || uri.Query.Length != 0 || uri.Fragment.Length != 0
                || !paths.Contains(uri.AbsolutePath) || request.Headers.Contains("Cookie") || request.Headers.Authorization != null)
                throw new InvalidOperationException("Private Urbit transport admits only configured book-page reads.");
            request.Headers.Add("Cookie", cookie);
            return base.SendAsync(request, cancellationToken);
        }
    }
}
