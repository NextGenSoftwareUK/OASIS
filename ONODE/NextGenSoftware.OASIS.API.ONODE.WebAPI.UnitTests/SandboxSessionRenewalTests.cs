using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;
using CoreAvatar = NextGenSoftware.OASIS.API.Core.Holons.Avatar;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Helpers;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

public sealed class SandboxSessionRenewalTests
{
    [Theory]
    [InlineData(true, 3)]
    [InlineData(false, 3)]
    [InlineData(true, 0)]
    [InlineData(true, -1)]
    public void SharedCookieOptionsPreserveExpiryAndTransportBoundary(bool secure, int days)
    {
        var now = new DateTime(2026, 11, 1, 12, 0, 0, DateTimeKind.Utc);
        var options = AvatarSessionCookie.CreateOptions(secure, days, now);
        Assert.True(options.HttpOnly);
        Assert.Equal(secure, options.Secure);
        Assert.Equal(Microsoft.AspNetCore.Http.SameSiteMode.Lax, options.SameSite);
        Assert.Equal("/", options.Path);
        Assert.Equal(now.AddDays(days > 0 ? days : 7), options.Expires!.Value.UtcDateTime);
    }

    [Fact]
    public void SuccessfulDemoPublishesTheSameAvatarAndItsRefreshCookie()
    {
        var context = new DefaultHttpContext();
        context.Request.Scheme = "https";
        var avatar = new CoreAvatar { Id = Guid.NewGuid(), Username = "sandbox-controlled",
            JwtToken = "controlled-jwt", RefreshToken = "controlled-refresh" };
        var authenticated = new OASISResult<IAvatar> { Result = avatar };
        var response = SandboxController.CompleteSession(context, authenticated);
        var published = Assert.IsType<OkObjectResult>(response.Result);
        Assert.Same(authenticated, published.Value);
        var cookie = SetCookieHeaderValue.Parse(Assert.Single(context.Response.Headers.SetCookie));
        Assert.Equal("refreshToken", cookie.Name.ToString());
        Assert.Equal("controlled-refresh", cookie.Value.ToString());
        Assert.True(cookie.HttpOnly);
        Assert.True(cookie.Secure);
        Assert.Equal("lax", cookie.SameSite.ToString().ToLowerInvariant());
        Assert.Equal("/", cookie.Path.ToString());
        Assert.True(cookie.Expires > DateTimeOffset.UtcNow);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void IncompleteAuthenticationCannotReleaseABearerOrCookie(bool missingJwt)
    {
        var context = new DefaultHttpContext();
        var result = new OASISResult<IAvatar> { Result = new CoreAvatar {
            JwtToken = missingJwt ? null : "controlled-jwt",
            RefreshToken = missingJwt ? "controlled-refresh" : null } };
        var response = Assert.IsType<ObjectResult>(SandboxController.CompleteSession(context, result).Result);
        Assert.Equal(503, response.StatusCode);
        var error = Assert.IsType<OASISResult<IAvatar>>(response.Value);
        Assert.True(error.IsError);
        Assert.Null(error.Result);
        Assert.Equal("AUTH_SESSION_INCOMPLETE", error.ErrorCode);
        Assert.Equal(0, context.Response.Headers.SetCookie.Count);
    }

    [Fact]
    public void FailedAuthenticationDoesNotIssueARefreshCookie()
    {
        var context = new DefaultHttpContext();
        var response = SandboxController.CompleteSession(context, new OASISResult<IAvatar> { IsError = true, Message = "controlled error" });
        Assert.IsType<BadRequestObjectResult>(response.Result);
        Assert.Equal(0, context.Response.Headers.SetCookie.Count);
    }

    [Fact]
    public void MissingRefreshCredentialIsRejectedBeforeCookiePublication()
    {
        var context = new DefaultHttpContext();
        Assert.Throws<ArgumentException>(() => AvatarSessionCookie.Append(context, ""));
        Assert.Equal(0, context.Response.Headers.SetCookie.Count);
    }

    [Fact]
    public void LogoutClearsTheSameRefreshCookieScope()
    {
        var context = new DefaultHttpContext(); context.Request.Scheme = "https";
        AvatarSessionCookie.Clear(context);
        var cookie = SetCookieHeaderValue.Parse(Assert.Single(context.Response.Headers.SetCookie));
        Assert.Equal("refreshToken", cookie.Name.ToString());
        Assert.Equal("/", cookie.Path.ToString());
        Assert.True(cookie.HttpOnly); Assert.True(cookie.Secure);
        Assert.True(cookie.Expires < DateTimeOffset.UtcNow);
    }

    [Theory]
    [InlineData("https://web4.oasisomniverse.one", true)]
    [InlineData("https://third-party.example", false)]
    [InlineData("https://web4.oasisomniverse.one.attacker.example", false)]
    public async Task ActualPreflightAllowsCredentialCookiesOnlyForTheExactFirstParty(string origin, bool credentialed)
    {
        var services = new ServiceCollection();
        services.AddLogging(); services.AddCors();
        await using var provider = services.BuildServiceProvider();
        var app = new ApplicationBuilder(provider);
        Startup.ConfigureBrowserCors(app);
        var invoked = false;
        app.Run(context => { invoked = true; return Task.CompletedTask; });
        var context = new DefaultHttpContext { RequestServices = provider };
        context.Request.Method = "OPTIONS";
        context.Request.Headers.Origin = origin;
        context.Request.Headers.AccessControlRequestMethod = "POST";
        context.Request.Headers.AccessControlRequestHeaders = "authorization,content-type,idempotency-key";
        await app.Build()(context);
        Assert.False(invoked);
        Assert.Equal(204, context.Response.StatusCode);
        Assert.Equal(credentialed ? origin : "*", context.Response.Headers.AccessControlAllowOrigin.ToString());
        Assert.Equal(credentialed ? "true" : "", context.Response.Headers.AccessControlAllowCredentials.ToString());
        Assert.Contains("Origin", context.Response.Headers.Vary.ToString());
    }

    [Fact]
    public void BothBrowserPoliciesExposeTheOriginalOperationIdentity()
    {
        foreach (var configure in new Action<CorsPolicyBuilder>[] {
            Startup.ConfigureCorsPolicy, Startup.ConfigureSessionCookieCorsPolicy })
        {
            var builder = new CorsPolicyBuilder(); configure(builder);
            Assert.Contains("X-OASIS-Operation-Id", builder.Build().ExposedHeaders);
        }
    }
}
