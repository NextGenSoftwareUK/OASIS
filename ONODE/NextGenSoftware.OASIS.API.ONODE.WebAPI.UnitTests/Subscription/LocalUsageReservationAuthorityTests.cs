using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Moq;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Subscription;

public class LocalUsageReservationAuthorityTests
{
    [Fact]
    public async Task AdmissionUsesAuthenticatedAvatarAndAuthoritativeKarma()
    {
        var id=Guid.NewGuid();var context=new DefaultHttpContext();
        context.User=new ClaimsPrincipal(new ClaimsIdentity(new[]{new Claim(ClaimTypes.NameIdentifier,id.ToString("D"))},"test"));
        var service=new Mock<ISubscriptionService>(MockBehavior.Strict);
        var request=new UsageAuthorizationRequest{ConsumingService="WEB4"};
        var decision=new SubscriptionAuthorizationResult{Allowed=true};
        service.Setup(x=>x.AuthorizeUsageAsync(id.ToString("D"),42,request,It.IsAny<CancellationToken>())).ReturnsAsync(decision);
        var authority=new LocalUsageReservationAuthority(service.Object,new Mock<ISubscriptionUsageRepository>(MockBehavior.Strict).Object,
            new HttpContextAccessor{HttpContext=context},(avatar,token)=>{Assert.Equal(id,avatar);return Task.FromResult(42);});
        Assert.Same(decision,await authority.AuthorizeUsageAsync("authenticated-token",request));
        service.VerifyAll();
        request.ConsumingService="WEB5";
        await Assert.ThrowsAsync<UnauthorizedAccessException>(()=>authority.AuthorizeUsageAsync("token",request));
        request.ConsumingService="WEB4";context.User=new ClaimsPrincipal();
        await Assert.ThrowsAsync<UnauthorizedAccessException>(()=>authority.AuthorizeUsageAsync("token",request));
    }
    [Fact]
    public void PublicServiceHeadersCannotImpersonateLocalAuthority()
    {
        var context=new DefaultHttpContext();context.Request.Headers["X-OASIS-Service"]="WEB4";
        context.Request.Headers["X-OASIS-Service-Key"]=new string('a',32);
        var config=new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>{
            ["SUBSCRIPTION_SERVICE_KEY_WEB4"]=new string('a',32)}).Build();
        Assert.True(SubscriptionServiceIdentity.IsService("WEB4"));
        Assert.Throws<UnauthorizedAccessException>(()=>SubscriptionServiceIdentity.RequireService(context,config,"WEB4"));
    }
}
