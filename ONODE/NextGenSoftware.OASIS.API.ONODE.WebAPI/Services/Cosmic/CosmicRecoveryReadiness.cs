using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cosmic;

/// <summary>Validate enrolled recovery composition without authorizing an actor,
/// resolving keys, activating storage or performing a native operation.</summary>
public sealed class CosmicRecoveryReadiness(IServiceProvider services) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var request = new DefaultHttpContext();
        request.Request.Method = "POST";
        request.Request.Path = CosmicOperationRecoveryRoutes.Recover;
        if (services.GetService<StorageAdmissionRoutes>()?.Resolve(request) != null)
        {
            using var scope = services.CreateScope();
            if (scope.ServiceProvider.GetService<ICosmicOperationRecoveryPolicy>() == null)
                throw new InvalidOperationException("An enrolled COSMIC recovery route requires an application-authorized recovery policy.");
        }
        return Task.CompletedTask;
    }
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
