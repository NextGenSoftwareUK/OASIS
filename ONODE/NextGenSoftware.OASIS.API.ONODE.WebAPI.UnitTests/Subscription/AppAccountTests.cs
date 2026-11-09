using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Subscription;
using Xunit;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests.Subscription;

/// <summary>
/// App accounts: a product (e.g. PIPER) holds a plan and its users are not limited by their personal free quota.
/// These tests need no database; the transactional storage is exercised separately against MongoDB.
/// </summary>
public sealed class AppAccountTests
{
    private const string Admin = "11111111-1111-1111-1111-111111111111";
    private static readonly Guid Avatar = Guid.Parse("22222222-2222-2222-2222-222222222222");

    // ---- rules --------------------------------------------------------------------------------------------------

    [Theory]
    [InlineData("piper")] [InlineData("piper-prod")] [InlineData("a1b")] [InlineData("0abc")]
    public void AcceptsAWellFormedAppId(string id) => Assert.True(AppAccountRules.IsValidAppId(id));

    [Theory]
    [InlineData(null)] [InlineData("")] [InlineData("ab")] [InlineData("Piper")] [InlineData("-piper")]
    [InlineData("pi per")] [InlineData("pi_per")] [InlineData("piper/../x")]
    public void RejectsAMalformedAppId(string id)
    {
        Assert.False(AppAccountRules.IsValidAppId(id));
        Assert.Throws<ArgumentException>(() => AppAccountRules.ValidateAppId(id));
    }

    [Fact]
    public void RejectsAnAppIdOverFortyCharacters() => Assert.False(AppAccountRules.IsValidAppId(new string('a', 41)));

    [Fact]
    public void GeneratedKeysAreUniqueLongAndPrefixed()
    {
        var a = AppAccountRules.GenerateKey();
        var b = AppAccountRules.GenerateKey();
        Assert.NotEqual(a, b);
        Assert.StartsWith(AppAccountRules.KeyPrefixText, a);
        Assert.True(a.Length >= 40);
        Assert.DoesNotContain("=", a);
    }

    [Fact]
    public void AKeyMatchesItsOwnHashAndNothingElse()
    {
        var key = AppAccountRules.GenerateKey();
        var hash = AppAccountRules.Hash(key);
        Assert.Equal(64, hash.Length);
        Assert.True(AppAccountRules.KeyMatches(key, hash));
        Assert.False(AppAccountRules.KeyMatches(key + "x", hash));
        Assert.False(AppAccountRules.KeyMatches(AppAccountRules.GenerateKey(), hash));
        Assert.False(AppAccountRules.KeyMatches(null, hash));
        Assert.False(AppAccountRules.KeyMatches("", hash));
        Assert.False(AppAccountRules.KeyMatches(key, null));
    }

    [Fact]
    public void TheStoredHashDoesNotContainTheKey()
    {
        var key = AppAccountRules.GenerateKey();
        Assert.DoesNotContain(key, AppAccountRules.Hash(key));
    }

    [Theory]
    [InlineData("free", "enterprise", "enterprise")]
    [InlineData("free", "gold", "gold")]
    [InlineData("gold", "free", "gold")]
    [InlineData("silver", "silver", "silver")]
    [InlineData("enterprise", "bronze", "enterprise")]
    [InlineData("free", "not-a-plan", "free")]
    public void TheMoreGenerousPlanApplies(string avatarPlan, string appPlan, string expected) =>
        Assert.Equal(expected, AppAccountRules.EffectivePlanId(avatarPlan, appPlan));

    [Fact]
    public void AnEnterpriseAppLiftsAFreeUserToUnlimited()
    {
        var provider = new SubscriptionUsagePolicyProvider();
        var personal = provider.GetPolicy("free", 0);
        var viaApp = provider.GetPolicy(AppAccountRules.EffectivePlanId("free", "enterprise"), 0);
        Assert.True(personal.DailyCallLimit > 0);
        Assert.Equal(-1, viaApp.MonthlyRequestLimit);
        Assert.Equal(0, viaApp.DailyCallLimit); // zero means unlimited
    }

    [Fact]
    public void AnAppAccountNeverExposesItsKeyHashWhenRedacted()
    {
        var account = new AppAccountRecord { AppId = "piper", KeyHashSha256 = "abc", PlanId = "enterprise" };
        var redacted = account.Redacted();
        Assert.Null(redacted.KeyHashSha256);
        Assert.Equal("piper", redacted.AppId);
        Assert.Equal("abc", account.KeyHashSha256);
    }

    // ---- admin endpoints ------------------------------------------------------------------------------------------

    private static SubscriptionAdminController Controller(bool authenticatedAdmin)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new[] { new KeyValuePair<string, string>("SUBSCRIPTION_ADMIN_AVATAR_IDS", Admin) }).Build();
        // No constructor (it would connect to MongoDB): every path under test rejects before touching the database.
        var repository = (MongoSubscriptionUsageRepository)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(MongoSubscriptionUsageRepository));
        var controller = new SubscriptionAdminController(configuration, repository);
        var http = new DefaultHttpContext();
        if (authenticatedAdmin)
            http.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, Admin), new Claim("oasis.subscription.admin", "true")
            }, "test"));
        controller.ControllerContext = new ControllerContext { HttpContext = http };
        return controller;
    }

    private static SubscriptionAdminController.AppAccountRequest Body(string appId = "piper", string name = "PIPER", string plan = "enterprise",
        string status = null, string reason = "PIPER production app") => new() { AppId = appId, Name = name, PlanId = plan, Status = status, Reason = reason };

    [Fact]
    public async Task EveryAppAccountEndpointRefusesAnUnauthenticatedCaller()
    {
        var c = Controller(false);
        Assert.Equal(403, Assert.IsType<ObjectResult>(await c.AppAccounts()).StatusCode);
        Assert.Equal(403, Assert.IsType<ObjectResult>(await c.CreateAppAccount(Body())).StatusCode);
        Assert.Equal(403, Assert.IsType<ObjectResult>(await c.RotateAppAccountKey("piper", Body())).StatusCode);
        Assert.Equal(403, Assert.IsType<ObjectResult>(await c.UpdateAppAccount("piper", Body())).StatusCode);
    }

    [Fact]
    public async Task CreateReturns400ForABadIdPlanNameOrReasonAndForAnEmptyBody()
    {
        var c = Controller(true);
        Assert.IsType<BadRequestObjectResult>(await c.CreateAppAccount(null));
        Assert.IsType<BadRequestObjectResult>(await c.CreateAppAccount(Body(appId: "Bad Id")));
        Assert.IsType<BadRequestObjectResult>(await c.CreateAppAccount(Body(plan: "platinum")));
        Assert.IsType<BadRequestObjectResult>(await c.CreateAppAccount(Body(name: " ")));
        Assert.IsType<BadRequestObjectResult>(await c.CreateAppAccount(Body(reason: "no")));
    }

    [Fact]
    public async Task RotateAndUpdateReturn400ForBadInput()
    {
        var c = Controller(true);
        Assert.IsType<BadRequestObjectResult>(await c.RotateAppAccountKey("Bad Id", Body()));
        Assert.IsType<BadRequestObjectResult>(await c.RotateAppAccountKey("piper", null));
        Assert.IsType<BadRequestObjectResult>(await c.UpdateAppAccount("piper", Body(plan: null, reason: "valid reason")));
        Assert.IsType<BadRequestObjectResult>(await c.UpdateAppAccount("piper", Body(plan: "platinum")));
        Assert.IsType<BadRequestObjectResult>(await c.UpdateAppAccount("piper", Body(plan: null, status: "paused")));
    }

    // ---- WEB4 authorization path ------------------------------------------------------------------------------------

    private static (LocalUsageReservationAuthority Authority, Mock<ISubscriptionService> Service) Authority(IHeaderDictionary headers, IAppAccountRepository apps)
    {
        var http = new DefaultHttpContext();
        foreach (var header in headers) http.Request.Headers[header.Key] = header.Value;
        http.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, Avatar.ToString("D")) }, "test"));
        var accessor = new Mock<IHttpContextAccessor>();
        accessor.SetupGet(a => a.HttpContext).Returns(http);
        var service = new Mock<ISubscriptionService>();
        service.Setup(s => s.AuthorizeUsageAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<UsageAuthorizationRequest>(), It.IsAny<AppAccountRecord>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SubscriptionAuthorizationResult());
        var authority = new LocalUsageReservationAuthority(service.Object, Mock.Of<ISubscriptionUsageRepository>(), accessor.Object,
            (_, _) => Task.FromResult(0), apps);
        return (authority, service);
    }

    private static UsageAuthorizationRequest Request() => new() { ConsumingService = "WEB4" };

    private static HeaderDictionary Headers(string id = null, string key = null)
    {
        var h = new HeaderDictionary();
        if (id != null) h[AppAccountRules.AppIdHeader] = id;
        if (key != null) h[AppAccountRules.AppKeyHeader] = key;
        return h;
    }

    [Fact]
    public async Task WithoutAppHeadersTheRequestIsMeteredExactlyAsBefore()
    {
        var apps = new Mock<IAppAccountRepository>(MockBehavior.Strict);
        var (authority, service) = Authority(Headers(), apps.Object);
        await authority.AuthorizeUsageAsync("token", Request());
        service.Verify(s => s.AuthorizeUsageAsync(Avatar.ToString("D"), 0, It.IsAny<UsageAuthorizationRequest>(), null, It.IsAny<CancellationToken>()), Times.Once);
        apps.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task AValidAppCredentialPassesTheAppToTheAuthorization()
    {
        var account = new AppAccountRecord { AppId = "piper", PlanId = "enterprise", Status = "active" };
        var apps = new Mock<IAppAccountRepository>();
        apps.Setup(a => a.VerifyAppCredentialAsync("piper", "oak_good", It.IsAny<CancellationToken>())).ReturnsAsync(account);
        var (authority, service) = Authority(Headers("piper", "oak_good"), apps.Object);
        await authority.AuthorizeUsageAsync("token", Request());
        service.Verify(s => s.AuthorizeUsageAsync(Avatar.ToString("D"), 0, It.IsAny<UsageAuthorizationRequest>(), account, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("piper", "oak_wrong")]
    [InlineData("piper", null)]
    [InlineData(null, "oak_good")]
    public async Task AnInvalidOrPartialAppCredentialIsRefusedAndNothingIsAuthorized(string id, string key)
    {
        var apps = new Mock<IAppAccountRepository>();
        apps.Setup(a => a.VerifyAppCredentialAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((AppAccountRecord)null);
        var (authority, service) = Authority(Headers(id, key), apps.Object);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => authority.AuthorizeUsageAsync("token", Request()));
        service.Verify(s => s.AuthorizeUsageAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<UsageAuthorizationRequest>(), It.IsAny<AppAccountRecord>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AppHeadersAreRefusedWhenTheDeploymentHasNoAppAccounts()
    {
        var (authority, service) = Authority(Headers("piper", "oak_good"), null);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => authority.AuthorizeUsageAsync("token", Request()));
        service.Verify(s => s.AuthorizeUsageAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<UsageAuthorizationRequest>(), It.IsAny<AppAccountRecord>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
