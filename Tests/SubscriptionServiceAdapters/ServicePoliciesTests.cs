using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using Xunit;

public sealed class ServicePoliciesTests
{
    private static UsageEndpointPolicy Resolve(int service, HttpContext context) => service switch
    {
        5 => NextGenSoftware.OASIS.STAR.WebAPI.Middleware.SubscriptionPolicy.Resolve(context),
        7 => NextGenSoftware.OASIS.Web7.WebAPI.Middleware.SubscriptionPolicy.Resolve(context),
        8 => NextGenSoftware.OASIS.Web8.WebAPI.Middleware.SubscriptionPolicy.Resolve(context),
        9 => NextGenSoftware.OASIS.Web9.WebAPI.Middleware.SubscriptionPolicy.Resolve(context),
        10 => NextGenSoftware.OASIS.Web10.WebAPI.Middleware.SubscriptionPolicy.Resolve(context),
        _ => throw new ArgumentOutOfRangeException(nameof(service))
    };
    public static IEnumerable<object[]> Cases() => from service in new[] { 5, 7, 8, 9, 10 }
        from route in new[] { "/api/resource", "/graphql", "/mcp", "/grpc.Service/Execute" }
        select new object[] { service, route };

    [Theory, MemberData(nameof(Cases))]
    public void EveryBusinessTransportUsesExplicitIncludedRequestPolicy(int service, string route)
    {
        var context = new DefaultHttpContext(); context.Request.Path = route;
        context.SetEndpoint(new Endpoint(_ => Task.CompletedTask, new EndpointMetadataCollection(), "business"));
        var policy = Resolve(service, context);
        Assert.NotNull(policy); Assert.Equal("api.request", policy.MeterCategory);
        Assert.Equal(1, policy.ReservedUnits); Assert.Equal(0, policy.EstimatedCostUsd); Assert.Equal(0, policy.FixedCostUsd);
        Assert.Equal($"web{service}-included-request-v1", policy.PricingCatalogueVersion);
    }

    [Theory]
    [InlineData(5)] [InlineData(7)] [InlineData(8)] [InlineData(9)] [InlineData(10)]
    public void PublicIdentityRoutesAndOperationalHealthDoNotNeedSubscriberQuota(int service)
    {
        var context = new DefaultHttpContext(); context.Request.Path = "/identity/register";
        context.SetEndpoint(new Endpoint(_ => Task.CompletedTask, new EndpointMetadataCollection(new AllowAnonymousAttribute()), "registration"));
        Assert.Null(Resolve(service, context));
        context.SetEndpoint(new Endpoint(_ => Task.CompletedTask, new EndpointMetadataCollection(), "health"));
        context.Request.Path = "/api/health"; Assert.Null(Resolve(service, context));
        context.Request.Path = "/swagger/v1/swagger.json"; Assert.Null(Resolve(service, context));
        context.Request.Path = "/api/healthcare/create";
        Assert.NotNull(Resolve(service, context)); // Segment boundary: a business route must not inherit health exemption.
        context.SetEndpoint(null); Assert.Null(Resolve(service, context));
    }
}
