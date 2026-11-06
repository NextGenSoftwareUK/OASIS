using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NextGenSoftware.OASIS.API.Providers.TursoOASIS;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

public static class CqlTursoReadSourceRegistration
{
    public static void AddTursoReadSource(this IServiceCollection services, IConfiguration configuration)
    {
        const string prefix = "CQL_TURSO_QUERY_";
        var address = configuration[prefix + "ORIGIN"];
        var token = configuration[prefix + "READER_TOKEN"];
        var probe = configuration[prefix + "PROBE_ID"];
        if (address == null && token == null && probe == null) return;
        if (address is not { Length: <= 262 } || !Uri.TryCreate(address, UriKind.Absolute, out var origin) ||
            origin.AbsoluteUri != address || !Guid.TryParseExact(probe, "D", out var id) || id.ToString("D") != probe ||
            !TursoQuerySource.IsValidConfiguration(origin, token, id))
            throw new InvalidOperationException("Turso CQL requires a reviewed HTTPS database, read token and canonical probe.");

        services.AddSingleton<CqlReadSourceProfile>(_ => new CqlReadSourceProfile(new TursoQuerySource(origin, token, id)));
    }
}
