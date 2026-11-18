using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Operations;

/// <summary>Operator pauses for durable background work. Pausing never acknowledges or deletes queued work.</summary>
public static class HostedMongoWorkerControl
{
    public const string PauseHyperDriveKey = "OASIS_PAUSE_HYPERDRIVE_WORKERS";
    public const string PauseReconciliationKey = "OASIS_PAUSE_USAGE_RECONCILIATION";
    public const string PauseMaintenanceKey = "OASIS_PAUSE_APPLICATION_MAINTENANCE";

    public static bool ReconciliationMayStart(IConfiguration configuration)
        => !configuration.GetValue<bool>(PauseReconciliationKey);

    public static void AddMaintenanceWorker<T>(IServiceCollection services, IConfiguration configuration)
        where T : class, IHostedService
    {
        if (!configuration.GetValue<bool>(PauseMaintenanceKey))
            services.AddHostedService<T>();
    }

    public static void AddHyperDriveWorker<T>(IServiceCollection services, IConfiguration configuration)
        where T : class, IHostedService
    {
        if (!configuration.GetValue<bool>(PauseHyperDriveKey))
            services.AddHostedService<T>();
    }
}
