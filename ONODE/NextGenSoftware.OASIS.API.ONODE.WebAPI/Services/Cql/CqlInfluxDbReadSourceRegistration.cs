using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NextGenSoftware.OASIS.API.Providers.InfluxDBOASIS;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

/// <summary>Wires the shipping InfluxDBOASIS provider as a hosted read-only CQL source; query logic stays in the provider.</summary>
public static class CqlInfluxDbReadSourceRegistration
{
    public static void AddInfluxDbReadSource(this IServiceCollection services, IConfiguration configuration)
    {
        const string prefix = "CQL_INFLUXDB_QUERY_";
        var endpoint = configuration[prefix + "ORIGIN"];
        var organization = configuration[prefix + "ORGANIZATION"];
        var bucket = configuration[prefix + "BUCKET"];
        var reader = configuration[prefix + "READER_TOKEN"];
        var probe = configuration[prefix + "PROBE_ID"];
        if (endpoint == null && organization == null && bucket == null && reader == null && probe == null) return;
        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var origin) || origin.Scheme != Uri.UriSchemeHttps ||
            origin.IsLoopback || origin.AbsolutePath != "/" || origin.UserInfo.Length != 0 ||
            origin.Query.Length != 0 || origin.Fragment.Length != 0)
            throw new InvalidOperationException("InfluxDB CQL requires an explicit credential-free HTTPS origin.");
        if (string.IsNullOrWhiteSpace(organization) || string.IsNullOrWhiteSpace(bucket) || string.IsNullOrWhiteSpace(reader) ||
            !Guid.TryParseExact(probe, "D", out var id) || id == Guid.Empty)
            throw new InvalidOperationException("InfluxDB CQL requires an organization, bucket, restricted reader token and existing probe.");
        services.AddSingleton<CqlReadSourceProfile>(_ => new CqlReadSourceProfile(
            new InfluxDBOASIS(origin.AbsoluteUri.TrimEnd('/'), reader, organization, bucket, id)));
    }
}
