using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Subscription;
using Xunit;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests.Subscription;

/// <summary>
/// Administrator plan grants: argument validation and the authorisation / status-code behaviour of the endpoint.
/// These run without a database (every rejection happens before the transaction opens). The transactional write
/// itself needs a MongoDB replica set and is covered separately.
/// </summary>
public sealed class PlanGrantTests
{
    private const string Admin = "11111111-1111-1111-1111-111111111111";
    private const string Target = "22222222-2222-2222-2222-222222222222";

    private static MongoSubscriptionUsageRepository Repository()
    {
        // No constructor: it would connect to MongoDB. Every path under test rejects before touching the database.
        return (MongoSubscriptionUsageRepository)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(MongoSubscriptionUsageRepository));
    }

    private static SubscriptionAdminController Controller(bool authenticatedAdmin, string allowedIds = Admin)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new[] { new System.Collections.Generic.KeyValuePair<string, string>("SUBSCRIPTION_ADMIN_AVATAR_IDS", allowedIds) })
            .Build();
        var controller = new SubscriptionAdminController(configuration, Repository());
        var http = new DefaultHttpContext();
        if (authenticatedAdmin)
            http.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, Admin), new Claim("oasis.subscription.admin", "true")
            }, "test"));
        controller.ControllerContext = new ControllerContext { HttpContext = http };
        return controller;
    }

    private static SubscriptionAdminController.GrantPlanRequest Request(string userId = Target, string planId = "enterprise",
        string reason = "PIPER production account", DateTime? expires = null) =>
        new() { UserId = userId, PlanId = planId, Reason = reason, ExpiresAtUtc = expires };

    [Theory]
    [InlineData(null)] [InlineData("")] [InlineData("not-a-guid")] [InlineData("00000000000000000000000000000001")]
    public void RejectsAnAvatarIdThatIsNotAHyphenatedUuid(string userId) =>
        Assert.Throws<ArgumentException>(() => MongoSubscriptionUsageRepository.ValidateGrantRequest(userId, "enterprise", Admin, "valid reason"));

    [Theory]
    [InlineData(null)] [InlineData("")] [InlineData("platinum")] [InlineData("Enterprise")] [InlineData("ENTERPRISE")]
    public void RejectsAnUnknownPlan(string plan) =>
        Assert.Throws<ArgumentException>(() => MongoSubscriptionUsageRepository.ValidateGrantRequest(Target, plan, Admin, "valid reason"));

    [Theory]
    [InlineData(null)] [InlineData("")] [InlineData("   ")] [InlineData("abcd")]
    public void RequiresAReasonOfAtLeastFiveCharacters(string reason) =>
        Assert.Throws<ArgumentException>(() => MongoSubscriptionUsageRepository.ValidateGrantRequest(Target, "enterprise", Admin, reason));

    [Fact]
    public void RejectsAReasonOverFiveHundredCharacters() =>
        Assert.Throws<ArgumentException>(() => MongoSubscriptionUsageRepository.ValidateGrantRequest(Target, "enterprise", Admin, new string('x', 501)));

    [Theory]
    [InlineData(null)] [InlineData("")] [InlineData("  ")]
    public void RequiresTheGrantingAdministrator(string actor) =>
        Assert.Throws<ArgumentException>(() => MongoSubscriptionUsageRepository.ValidateGrantRequest(Target, "enterprise", actor, "valid reason"));

    [Fact]
    public async Task AnUnauthenticatedCallerGets403AndNothingIsGranted()
    {
        var result = await Controller(authenticatedAdmin: false).GrantPlan(Request());
        Assert.Equal(403, Assert.IsType<ObjectResult>(result).StatusCode);
    }

    [Fact]
    public async Task AnAuthenticatedAvatarThatIsNotAnApprovedAdministratorGets403()
    {
        var result = await Controller(authenticatedAdmin: true, allowedIds: "33333333-3333-3333-3333-333333333333").GrantPlan(Request());
        Assert.Equal(403, Assert.IsType<ObjectResult>(result).StatusCode);
    }

    [Fact]
    public async Task AnAdministratorGets400ForAnEmptyBody()
    {
        var result = await Controller(authenticatedAdmin: true).GrantPlan(null);
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Theory]
    [InlineData("bad-id", "enterprise", "valid reason")]
    [InlineData(Target, "platinum", "valid reason")]
    [InlineData(Target, "enterprise", "no")]
    public async Task AnAdministratorGets400ForAnInvalidRequest(string userId, string plan, string reason)
    {
        var result = await Controller(authenticatedAdmin: true).GrantPlan(Request(userId, plan, reason));
        Assert.IsType<BadRequestObjectResult>(result);
    }
}
