using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NextGenSoftware.OASIS.API.Providers.ConvexOASIS;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

public static class CqlConvexReadSourceRegistration
{
    public static void AddConvexReadSource(this IServiceCollection services, IConfiguration configuration)
    {
        const string prefix = "CQL_CONVEX_QUERY_";
        var address = configuration[prefix + "SITE"];
        var token = configuration[prefix + "READER_TOKEN"];
        var probe = configuration[prefix + "PROBE_ID"];
        if (address == null && token == null && probe == null) return;
        if (address is not { Length: <= 264 } || !Uri.TryCreate(address, UriKind.Absolute, out var site) ||
            site.AbsoluteUri != address || !Guid.TryParseExact(probe, "D", out var id) || id.ToString("D") != probe ||
            !ConvexQuerySource.IsValidConfiguration(site, token, id))
            throw new InvalidOperationException("Convex CQL requires a reviewed HTTPS site, teaching reader token and canonical probe.");

        services.AddSingleton<CqlReadSourceProfile>(_ => new CqlReadSourceProfile(new ConvexQuerySource(site, token, id)));
    }
}
