using System;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NextGenSoftware.OASIS.API.Providers.VercelKVOASIS;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

public static class CqlVercelKvReadSourceRegistration
{
    public static void AddVercelKvReadSource(this IServiceCollection services, IConfiguration configuration)
    {
        const string prefix = "CQL_VERCEL_KV_QUERY_";
        var address = configuration[prefix + "ENDPOINT"];
        var token = configuration[prefix + "READER_TOKEN"];
        var probe = configuration[prefix + "PROBE_ID"];
        if (address == null && token == null && probe == null) return;
        if (address is not { Length: <= 262 } ||
            !Regex.IsMatch(address, @"\Ahttps://(?:[a-z0-9](?:[a-z0-9-]{0,61}[a-z0-9])?\.)+upstash\.io/\z", RegexOptions.CultureInvariant) ||
            !Uri.TryCreate(address, UriKind.Absolute, out var endpoint) || endpoint.AbsoluteUri != address ||
            token is not { Length: > 0 and <= 1024 } || token.Any(c => c <= ' ' || c > '~') ||
            !Guid.TryParseExact(probe, "D", out var id) || id == Guid.Empty || id.ToString("D") != probe)
            throw new InvalidOperationException("Migrated Vercel KV CQL requires a reviewed Upstash HTTPS endpoint, reader token and canonical probe.");

        services.AddSingleton<CqlReadSourceProfile>(_ => new CqlReadSourceProfile(new VercelKvQuerySource(endpoint, token, id)));
    }
}
