using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

/// <summary>Immutable deployment requirement, never supplied by a query DTO.</summary>
public sealed class CqlReadPolicyRequirement(bool required)
{
    public bool Required { get; } = required;
}

public static class CqlReadPolicyEnrollment
{
    public const string ConfigurationKey = "CQL_REQUIRE_AUTHORITATIVE_READ_POLICY";

    public static IServiceCollection AddConfiguredCqlReadAccessPolicy(this IServiceCollection services,
        IConfiguration configuration)
    {
        if (services == null || configuration == null) throw new ArgumentNullException();
        var value = configuration[ConfigurationKey];
        var required = false;
        if (value != null && !bool.TryParse(value, out required))
            throw new InvalidOperationException(ConfigurationKey + " must be true or false.");
        services.AddSingleton(new CqlReadPolicyRequirement(required));
        services.AddHostedService<CqlReadPolicyReadiness>();
        return services;
    }
}

/// <summary>Verify required composition in a scope without authorizing an actor
/// or acquiring a storage provider. Scoped application policies are supported.</summary>
public sealed class CqlReadPolicyReadiness(IServiceProvider services, CqlReadPolicyRequirement requirement) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (requirement.Required)
        {
            using var scope = services.CreateScope();
            if (scope.ServiceProvider.GetService<ICqlReadAccessPolicy>() == null)
                throw new InvalidOperationException(CqlReadPolicyEnrollment.ConfigurationKey
                    + " requires a non-null application ICqlReadAccessPolicy.");
        }
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
