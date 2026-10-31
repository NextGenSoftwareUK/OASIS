using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public partial class CqlV1Web4Tests
{
    [Theory]
    [InlineData(null, false)] [InlineData("false", false)] [InlineData("true", true)]
    public void DeploymentRequirementIsCapturedRatherThanReadingMutableConfiguration(string? value, bool expected)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            { [CqlReadPolicyEnrollment.ConfigurationKey] = value }).Build();
        using var provider = new ServiceCollection().AddConfiguredCqlReadAccessPolicy(configuration).BuildServiceProvider();
        configuration[CqlReadPolicyEnrollment.ConfigurationKey] = expected ? "false" : "true";
        Assert.Equal(expected, provider.GetRequiredService<CqlReadPolicyRequirement>().Required);
    }

    [Theory]
    [InlineData("")] [InlineData("yes")] [InlineData("1")]
    public void InvalidDeploymentFlagCannotSilentlyDisableTheRequirement(string value)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            { [CqlReadPolicyEnrollment.ConfigurationKey] = value }).Build();
        Assert.Throws<InvalidOperationException>(() => new ServiceCollection().AddConfiguredCqlReadAccessPolicy(configuration));
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task RequiredPolicyReadinessRejectsMissingOrNullFactories(bool nullFactory)
    {
        var services = new ServiceCollection();
        if (nullFactory) services.AddScoped<ICqlReadAccessPolicy>(_ => null!);
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        var readiness = new CqlReadPolicyReadiness(provider, new CqlReadPolicyRequirement(true));
        await Assert.ThrowsAsync<InvalidOperationException>(() => readiness.StartAsync(CancellationToken.None));
    }

    [Fact]
    public async Task ReadinessSupportsScopedPolicyWithoutAuthorizingAnActor()
    {
        var constructions = 0;
        var service = new HostedReadPolicy((_, _) => throw new InvalidOperationException("Startup must not authorize."));
        using var provider = new ServiceCollection().AddScoped<ICqlReadAccessPolicy>(_ => { constructions++; return service; })
            .BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        await new CqlReadPolicyReadiness(provider, new CqlReadPolicyRequirement(true)).StartAsync(CancellationToken.None);
        Assert.Equal(1, constructions);
        Assert.Equal(0, service.Calls);
    }

    [Fact]
    public async Task UnrequiredOrCancelledReadinessDoesNotResolveThePolicy()
    {
        using var provider = new ServiceCollection().AddScoped<ICqlReadAccessPolicy>(_ => throw new InvalidOperationException("Do not resolve."))
            .BuildServiceProvider();
        await new CqlReadPolicyReadiness(provider, new CqlReadPolicyRequirement(false)).StartAsync(CancellationToken.None);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            new CqlReadPolicyReadiness(provider, new CqlReadPolicyRequirement(true)).StartAsync(new CancellationToken(true)));
    }

    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)] [InlineData(4)] [InlineData(5)]
    public async Task RequiredPolicyCannotBeSkippedByHostingAnEndpointWithoutStartupReadiness(int route)
    {
        var source = new Source(ProviderType.MongoDBOASIS);
        var admitted = Admission(ProviderType.MongoDBOASIS);
        using var services = new ServiceCollection().AddSingleton(new CqlReadPolicyRequirement(true)).BuildServiceProvider();
        admitted.Http.RequestServices = services;
        Task<IActionResult> Run() => route switch
        {
            0 => CqlV1Endpoint.ExecuteAsync(admitted.Http, new CqlV1Request(), source),
            1 => CqlV1Endpoint.ExecuteScalarAsync(admitted.Http, new CqlScalarV1Request { Operation = "Count" }, source),
            2 => CqlV1Endpoint.ExecuteProjectionAsync(admitted.Http, new CqlProjectionV1Request(), source),
            3 => CqlV1Endpoint.ExecuteGroupedAsync(admitted.Http, new CqlGroupedV1Request(), source),
            4 => CqlV1Endpoint.ExecuteJoinAsync(admitted.Http, new CqlJoinV1Request(), source),
            _ => CqlV1Endpoint.ExecuteCompositionAsync(admitted.Http, new CqlCompositionV1Request(), source)
        };
        var result = await admitted.Policy.RunAsync(Run);
        Assert.Equal("CQL_ACCESS_POLICY_REQUIRED", Code(result));
        Assert.Equal(503, Assert.IsType<ObjectResult>(result).StatusCode);
        Assert.Equal(0, source.Lookups);
        Assert.Equal(0, source.Managers);
        Assert.Empty(admitted.Receipts);
    }
}
