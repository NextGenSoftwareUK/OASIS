using System;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NextGenSoftware.OASIS.API.Providers.NetlifyBlobsOASIS;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

public static class CqlNetlifyBlobsReadSourceRegistration
{
    private static readonly Regex ObjectOrigin = new(
        @"\Ahttps://(?:[a-z0-9](?:[a-z0-9-]{0,61}[a-z0-9])?\.)+[a-z][a-z0-9]{0,61}[a-z0-9]\z",
        RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100));

    public static void AddNetlifyBlobsReadSource(this IServiceCollection services, IConfiguration configuration)
    {
        const string prefix = "CQL_NETLIFY_BLOBS_QUERY_";
        var site = configuration[prefix + "SITE_ID"];
        var store = configuration[prefix + "STORE"];
        var originText = configuration[prefix + "OBJECT_ORIGIN"];
        var token = configuration[prefix + "READER_TOKEN"];
        var probe = configuration[prefix + "PROBE_ID"];
        if (site == null && store == null && originText == null && token == null && probe == null) return;
        if (!TryParseId(site, out var siteId) || !TryParseId(probe, out var probeId) ||
            store is not { Length: > 0 and <= 64 } || store.Any(c => !char.IsAsciiLetterOrDigit(c) && c is not ('-' or '_')) ||
            originText is not { Length: > 0 and <= 261 } || !ObjectOrigin.IsMatch(originText) ||
            !Uri.TryCreate(originText, UriKind.Absolute, out var origin) || origin.IsLoopback ||
            token is not { Length: > 0 and <= 1024 } || token.Any(c => c <= ' ' || c > '~'))
            throw new InvalidOperationException("Netlify Blobs CQL requires a reviewed site, store, object origin, reader and probe.");

        services.AddSingleton<CqlReadSourceProfile>(_ =>
            new CqlReadSourceProfile(new NetlifyBlobsQuerySource(siteId, token, store, origin, probeId)));
    }

    private static bool TryParseId(string value, out Guid id) =>
        Guid.TryParseExact(value, "D", out id) && id != Guid.Empty && value == id.ToString("D");
}
