using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NextGenSoftware.OASIS.API.Providers.PlanetScaleOASIS;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

public static class CqlPlanetScaleReadSourceRegistration
{
    public static void AddPlanetScaleReadSource(IServiceCollection services, IConfiguration configuration)
    {
        var connection = configuration["CQL_PLANETSCALE_QUERY_CONNECTION_STRING"];
        var probe = configuration["CQL_PLANETSCALE_QUERY_PROBE_ID"];
        if (connection == null && probe == null) return;

        if (string.IsNullOrWhiteSpace(connection) || !Guid.TryParseExact(probe, "D", out var id)
            || id == Guid.Empty || probe != id.ToString("D"))
            throw new InvalidOperationException("PlanetScale CQL requires a restricted reader and a canonical public probe ID.");

        PlanetScaleQuerySource source;
        try { source = new PlanetScaleQuerySource(connection, id); }
        catch (ArgumentException)
        {
            throw new InvalidOperationException("PlanetScale CQL requires a valid reader configuration with verified TLS.");
        }
        services.AddSingleton(new CqlReadSourceProfile(source));
    }
}
