using Moq;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Sandbox;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Subscription;

public class SandboxSubscriptionLifecycleTests
{
    private static readonly DateTime Now = new(2026, 10, 30, 12, 0, 0, DateTimeKind.Utc);
    private sealed class Clock : TimeProvider { public override DateTimeOffset GetUtcNow() => new(Now); }
    private static Avatar Demo() => new() {
        Id = Guid.NewGuid(), Username = "sandbox-test", Email = "sandbox-test@sandbox.example.com",
        AvatarType = new(AvatarType.User), CreatedDate = Now.AddSeconds(-1).AddTicks(1234), Verified = Now, IsActive = true
    };
    private static SandboxSubscriptionLifecycle Lifecycle(Func<ISandboxSubscriptionStore> store, bool enabled = true, bool admitted = true) =>
        new(store, () => admitted, () => new(enabled, "sandbox-", "sandbox.example.com", TimeSpan.FromHours(24)), new Clock());

    [Theory]
    [InlineData(false, true)] [InlineData(true, false)]
    public async Task DisabledFeatureDoesNotResolveBillingOrMutateAnything(bool enabled, bool admitted)
    {
        var service = Lifecycle(() => throw new Exception("Billing must remain lazy."), enabled, admitted);
        await service.ProvisionAsync(null!, CancellationToken.None);
        Assert.Equal(new SandboxSubscriptionSweep(0, 0), await service.SweepAsync(50, CancellationToken.None));
    }

    [Fact]
    public async Task VerifiedDemoIsIssuedOnceWithAvatarBoundExpiryAndBsonPrecision()
    {
        var avatar = Demo();
        var created = Now.AddSeconds(-1);
        var store = new Mock<ISandboxSubscriptionStore>(MockBehavior.Strict);
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        store.Setup(value => value.CreateSandboxSubscriptionAsync(avatar.Id.ToString("D"), created, created.AddHours(24), CancellationToken.None))
            .Returns(completion.Task);
        var pending = Lifecycle(() => store.Object).ProvisionAsync(avatar, CancellationToken.None);
        Assert.False(pending.IsCompleted); // The session must await durable issuance before authentication.
        completion.SetResult(); await pending;
        store.VerifyAll();
    }

    [Theory]
    [InlineData("id")] [InlineData("type")] [InlineData("username")] [InlineData("email")]
    [InlineData("inactive")] [InlineData("unverified")] [InlineData("expired")] [InlineData("future")]
    public async Task InvalidOrNonDemoIdentityNeverReachesTheBillingStore(string mode)
    {
        var avatar = Demo();
        if (mode == "id") avatar.Id = Guid.Empty;
        if (mode == "type") avatar.AvatarType = new(AvatarType.Wizard);
        if (mode == "username") avatar.Username = "real-user";
        if (mode == "email") avatar.Email = "other@sandbox.example.com";
        if (mode == "inactive") avatar.IsActive = false;
        if (mode == "unverified") avatar.Verified = null;
        if (mode == "expired") avatar.CreatedDate = Now.AddDays(-2);
        if (mode == "future") avatar.CreatedDate = Now.AddMinutes(1);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Lifecycle(() => throw new Exception("Must not resolve billing.")).ProvisionAsync(avatar, CancellationToken.None));
    }

    [Fact]
    public async Task IssuanceFailurePropagatesAndCleanupRetainsItsActualReport()
    {
        var store = new Mock<ISandboxSubscriptionStore>(MockBehavior.Strict);
        store.Setup(value => value.CreateSandboxSubscriptionAsync(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new SubscriptionUsageConflictException("Opening state required."));
        store.Setup(value => value.RetireSandboxSubscriptionsAsync(50, CancellationToken.None)).ReturnsAsync(new SandboxSubscriptionSweep(2, 1));
        var service = Lifecycle(() => store.Object);
        await Assert.ThrowsAsync<SubscriptionUsageConflictException>(() => service.ProvisionAsync(Demo(), CancellationToken.None));
        Assert.Equal(new SandboxSubscriptionSweep(2, 1), await service.SweepAsync(50, CancellationToken.None));
        store.VerifyAll();
    }
}
