using Moq;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public sealed class UsageRequestBindingTests
{
    [Theory]
    [InlineData("correct", true)]
    [InlineData("foreign-key", false)]
    [InlineData("foreign-avatar", false)]
    [InlineData("foreign-service", false)]
    [InlineData("service-case", false)]
    [InlineData("empty-avatar", false)]
    [InlineData("short-key", false)]
    [InlineData("control-key", false)]
    [InlineData("oversized-key", false)]
    [InlineData("null-key", false)]
    public void RequestBindingUsesCapturedDurableOwner(string mode, bool expected)
    {
        var avatar = Guid.NewGuid();
        const string key = "continuation-request-key";
        var operation = new UsageOutboxOperation
        {
            Id = UsageCanonical.OperationId("WEB4", avatar.ToString("D"), key),
            UserId = avatar.ToString("D"),
            Service = "WEB4"
        };
        var store = new Mock<IUsageOutboxStore>(MockBehavior.Strict);
        var authority = new Mock<IUsageReservationAuthority>(MockBehavior.Strict);
        var options = new Web4UsageLedgerOptions { Service = "WEB4" };
        var handle = new UsageExecutionHandle(operation, store.Object, options,
            new SubscriptionAuthorizationResult(), authority.Object);

        // Configuration mutation must not redirect a previously admitted handle.
        options.Service = "WEB6";
        var suppliedKey = mode switch
        {
            "foreign-key" => "other-continuation-key",
            "short-key" => "short",
            "control-key" => "continuation\nkey",
            "oversized-key" => new string('x', 129),
            "null-key" => null,
            _ => key
        };
        var service = mode == "foreign-service" ? "WEB6" : mode == "service-case" ? "web4" : "WEB4";
        var actor = mode == "foreign-avatar" ? Guid.NewGuid() : mode == "empty-avatar" ? Guid.Empty : avatar;

        Assert.Equal(expected, handle.IsBoundToRequest(service, actor, suppliedKey));
        store.VerifyNoOtherCalls();
        authority.VerifyNoOtherCalls();
    }
}
