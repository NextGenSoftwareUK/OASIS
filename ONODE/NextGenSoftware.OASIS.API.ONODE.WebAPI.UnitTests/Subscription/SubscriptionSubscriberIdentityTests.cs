using System;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Subscription;
using Xunit;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests.Subscription;

public sealed class SubscriptionSubscriberIdentityTests
{
    [Fact]
    public void BillingReadersRejectUnauthenticatedAndOtherAvatarIds()
    {
        var context = new DefaultHttpContext();
        string id = Guid.NewGuid().ToString("D");
        Assert.Throws<UnauthorizedAccessException>(() => SubscriptionServiceIdentity.RequireSubscriber(context, id));
        context.User = new ClaimsPrincipal(new ClaimsIdentity(new[] {new Claim("sub", id)}, "Bearer"));
        Assert.Equal(id, SubscriptionServiceIdentity.RequireSubscriber(context));
        Assert.Equal(id, SubscriptionServiceIdentity.RequireSubscriber(context, id));
        Assert.Throws<UnauthorizedAccessException>(() => SubscriptionServiceIdentity.RequireSubscriber(context, Guid.NewGuid().ToString("D")));
    }
}
