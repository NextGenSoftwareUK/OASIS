using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

public class CqlBrowserOperationHeaderTests
{
    [Theory]
    [InlineData("GET")]
    [InlineData("POST")]
    public void ActualHostCorsPolicyExposesTheAcknowledgedOperationToTheSandboxBrowser(string method)
    {
        var context = new DefaultHttpContext();
        context.Request.Method = method;
        context.Request.Headers.Origin = "https://web4.oasisomniverse.one";
        var operation = Guid.NewGuid().ToString();
        context.Response.Headers["X-OASIS-Operation-Id"] = operation;
        var builder = new CorsPolicyBuilder();
        Startup.ConfigureCorsPolicy(builder);
        var service = new CorsService(Options.Create(new CorsOptions()), NullLoggerFactory.Instance);

        var result = service.EvaluatePolicy(context, builder.Build());
        service.ApplyResult(result, context.Response);

        Assert.True(result.IsOriginAllowed);
        Assert.Equal("X-OASIS-Operation-Id", context.Response.Headers.AccessControlExposeHeaders.ToString());
        Assert.Equal(operation, context.Response.Headers["X-OASIS-Operation-Id"].ToString());
        Assert.False(result.SupportsCredentials);
    }

    [Fact]
    public void OperationHeaderExposurePreservesAuthenticatedQueryPreflight()
    {
        var context = new DefaultHttpContext();
        context.Request.Method = "OPTIONS";
        context.Request.Headers.Origin = "https://web4.oasisomniverse.one";
        context.Request.Headers.AccessControlRequestMethod = "POST";
        context.Request.Headers.AccessControlRequestHeaders = "authorization,content-type,idempotency-key";
        var builder = new CorsPolicyBuilder();
        Startup.ConfigureCorsPolicy(builder);
        var service = new CorsService(Options.Create(new CorsOptions()), NullLoggerFactory.Instance);

        var result = service.EvaluatePolicy(context, builder.Build());
        service.ApplyResult(result, context.Response);

        Assert.True(result.IsPreflightRequest);
        Assert.True(result.IsOriginAllowed);
        Assert.Contains("POST", result.AllowedMethods);
        Assert.Contains("authorization", result.AllowedHeaders);
        Assert.Contains("content-type", result.AllowedHeaders);
        Assert.Contains("idempotency-key", result.AllowedHeaders);
        Assert.False(result.SupportsCredentials);
    }
}
