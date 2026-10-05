using API.Infrastructure.Workers.CorporateAction;
using API.Infrastructure.Workers.ExchangeRate;
using API.Infrastructure.Workers.RiskManagement;

namespace API.Infrastructure.DI;

/// <summary>
/// This class provides an extension method for configuring background worker services in a WebApplicationBuilder.
/// It registers the services required for performing background tasks, such as updating exchange rates at regular intervals.
/// </summary>
public static class WorkerRegister
{
    /// <summary>
    /// Registers worker services for background tasks in the application.
    /// This includes registering the `ExchangeRateWorker` as a hosted service that runs in the background,
    /// and the `ExchangeRateUpdaterService` to update exchange rates periodically.
    /// Also registers the `CorporateActionWorker` for daily corporate action updates.
    /// </summary>
    /// <param name="builder">The WebApplicationBuilder instance used to register services.</param>
    /// <returns>The WebApplicationBuilder instance to allow method chaining.</returns>
    public static WebApplicationBuilder AddWorkerService(this WebApplicationBuilder builder)
    {
        // Exchange Rate Worker
        builder.Services.AddHostedService<ExchangeRateWorker>();
        builder.Services.AddScoped<ExchangeRateUpdaterService>();
        
        // Corporate Action Worker
        builder.Services.AddScoped<CorporateActionUpdaterService>();
        builder.Services.AddHostedService<CorporateActionWorker>();
        
        // Funding Rate On-Chain Publisher Worker
        builder.Services.AddHostedService<API.Infrastructure.Workers.FundingRate.FundingRateOnChainPublisherWorker>();
        
        // Risk Management Worker
        builder.Services.AddScoped<RiskManagementUpdaterService>();
        builder.Services.AddHostedService<RiskManagementWorker>();
        
        return builder;
    }
}