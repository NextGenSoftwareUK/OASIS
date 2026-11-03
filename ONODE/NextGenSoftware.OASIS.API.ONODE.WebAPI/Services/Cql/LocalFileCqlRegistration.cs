using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NextGenSoftware.OASIS.API.Providers.LocalFileOASIS;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

public static class LocalFileCqlRegistration
{
    public const string ConfigurationKey = "CQL_LOCALFILE_PUBLIC_SOURCE_JSON";

    public static void AddConfigured(IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        if (configuration[ConfigurationKey] is not string json) return;
        services.AddSingleton<CqlReadSourceProfile>(_ => new CqlReadSourceProfile(LocalFilePublicCqlProfile.FromJson(json)));
        // Query tariff, public read grant and presets remain separate explicit
        // host configuration. This must never register/switch writable storage.
    }
}
