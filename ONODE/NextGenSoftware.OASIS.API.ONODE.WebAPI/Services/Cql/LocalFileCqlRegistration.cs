using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NextGenSoftware.OASIS.API.Providers.LocalFileOASIS;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

public static class LocalFileCqlRegistration
{
    public const string ConfigurationKey = "CQL_LOCALFILE_PUBLIC_SOURCE_JSON";
    public const string BootstrapConfigurationKey = "CQL_LOCALFILE_PUBLIC_BOOTSTRAP_JSON";

    public static void AddConfigured(IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        var json = configuration[ConfigurationKey];
        var bundle = configuration[BootstrapConfigurationKey];
        if (json == null)
        {
            if (bundle != null) throw new ArgumentException("LocalFile bootstrap requires an explicit source profile.");
            return;
        }
        var source = LocalFilePublicCqlProfile.FromJson(json);
        LocalFilePublicBootstrap bootstrap = null;
        if (bundle != null)
        {
            if (!bool.TryParse(configuration["OASIS_SANDBOX"], out var sandbox) || !sandbox)
                throw new ArgumentException("Public teaching-data bootstrap requires explicit sandbox mode.");
            bootstrap = LocalFilePublicBootstrap.FromJson(source, bundle);
        }
        services.AddSingleton<CqlReadSourceProfile>(_ => new CqlReadSourceProfile(source));
        if (bootstrap != null) services.AddSingleton<IHostedService>(_ => bootstrap);
        // Query tariff, public read grant and presets remain separate explicit
        // host configuration. This must never register/switch writable storage.
    }
}
