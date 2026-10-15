using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using Xunit;

public sealed class ReadAvailabilityTests
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

    public static IEnumerable<object[]> OrdinaryReads() =>
        from service in new[] { 5, 7, 8, 9, 10 }
        from path in new[] { "/api/quests", "/api/quests/all-for-avatar/game", "/api/quests/11111111-1111-1111-1111-111111111111",
            "/api/geohotspots/eligible", "/api/geohotspots/11111111-1111-1111-1111-111111111111",
            "/api/geonfts", "/api/geonfts/load-all-for-avatar" }
        select new object[] { service, path };

    [Theory, MemberData(nameof(OrdinaryReads))]
    public async Task OrdinaryReadSucceedsWithoutUsageAuthorityOrIdempotencyHeader(int service, string path)
    {
        var context = Context("GET", path);
        var endpointCalls = 0;
        var middleware = new Web4UsageLedgerMiddleware(async ctx =>
        {
            endpointCalls++;
            await ctx.Response.WriteAsync("real-read-result");
        }, NullLogger<Web4UsageLedgerMiddleware>.Instance);
        // An unavailable execution dependency cannot block a route classified as an ordinary read.
        await middleware.InvokeAsync(context, null!, new Web4UsageLedgerOptions { EndpointPolicy = ctx => Resolve(service, ctx) });
        Assert.Equal(200, context.Response.StatusCode);
        Assert.Equal(1, endpointCalls);
        context.Response.Body.Position = 0;
        Assert.Equal("real-read-result", await new StreamReader(context.Response.Body).ReadToEndAsync());
        Assert.False(context.Response.Headers.ContainsKey("X-OASIS-Usage-Settlement"));
    }

    [Theory]
    [InlineData("/star/ignite")] [InlineData("/api/star/ignite")]
    [InlineData("/star/extinguish")] [InlineData("/api/star/extinguish")]
    public void LifecycleOperationsDoNotRequireSettlement(string path) =>
        Assert.Null(Resolve(5, Context("POST", path)));

    [Theory]
    [InlineData(5)] [InlineData(7)] [InlineData(8)] [InlineData(9)] [InlineData(10)]
    public async Task MeteredWritesFailClosedDuringAuthorityOutage(int service)
    {
        var context = Context("POST", "/api/resource");
        var avatar = Guid.NewGuid().ToString("D");
        context.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, avatar) }, "Bearer"));
        context.Request.Headers.Authorization = "Bearer test-bearer";
        context.Request.Headers["Idempotency-Key"] = Guid.NewGuid().ToString("D");
        using var http = new HttpClient(new AuthorityOutage());
        var options = new Web4UsageLedgerOptions { Service = "WEB" + service, EndpointPolicy = ctx => Resolve(service, ctx) };
        var execution = new Web4UsageExecution(new Web4SubscriptionAuthorizationClient(http,
            "https://authority.test", options.Service, new string('s', 32)), null!, options);
        var endpointCalls = 0;
        var middleware = new Web4UsageLedgerMiddleware(_ => { endpointCalls++; return Task.CompletedTask; },
            NullLogger<Web4UsageLedgerMiddleware>.Instance);
        await middleware.InvokeAsync(context, execution, options);
        Assert.Equal(503, context.Response.StatusCode);
        Assert.Equal(0, endpointCalls);
    }

    [Theory]
    [InlineData("/graphql")] [InlineData("/mcp")]
    public void ReadVerbCannotExemptProviderCapableTransports(string path) =>
        Assert.NotNull(Resolve(5, Context("GET", path)));

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task DuplicateKeysAreRejectedBeforeAuthorityEvenWhenHttpCoalescesThem(bool coalesced)
    {
        var context = Context("POST", "/api/resource");
        context.User = new ClaimsPrincipal(new ClaimsIdentity(new[] {
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString("D")) }, "Bearer"));
        context.Request.Headers.Authorization = "Bearer test-bearer";
        context.Request.Headers["Idempotency-Key"] = coalesced
            ? new Microsoft.Extensions.Primitives.StringValues("first-operation-key, second-operation-key")
            : new Microsoft.Extensions.Primitives.StringValues(new[] { "first-operation-key", "second-operation-key" });
        var called = false;
        var middleware = new Web4UsageLedgerMiddleware(_ => { called = true; return Task.CompletedTask; },
            NullLogger<Web4UsageLedgerMiddleware>.Instance);
        await middleware.InvokeAsync(context, null!, new Web4UsageLedgerOptions { EndpointPolicy = ctx => Resolve(5, ctx) });
        Assert.Equal(400, context.Response.StatusCode);
        Assert.False(called);
        context.Response.Body.Position = 0;
        Assert.Contains("IDEMPOTENCY_KEY_REQUIRED", await new StreamReader(context.Response.Body).ReadToEndAsync());
    }

    private static DefaultHttpContext Context(string method, string path)
    {
        var context = new DefaultHttpContext();
        context.Request.Method = method;
        context.Request.Path = path;
        context.Response.Body = new MemoryStream();
        context.SetEndpoint(new Endpoint(_ => Task.CompletedTask, new EndpointMetadataCollection(), "business"));
        return context;
    }

    private sealed class AuthorityOutage : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable) { Content = new StringContent("{}") });
    }
}
