using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Subscription;

public class LocalUsageReservationAuthorityTests
{
    [Fact]
    public void RegisteredAuthorityIsSingletonAndDoesNotCaptureARequestScope()
    {
        var services=new ServiceCollection();services.AddHttpContextAccessor();
        services.AddSingleton(new Mock<ISubscriptionService>(MockBehavior.Strict).Object);
        services.AddSingleton(new Mock<ISubscriptionUsageRepository>(MockBehavior.Strict).Object);
        LocalUsageAuthorityRegistration.AddLocalUsageAuthority(services,(_,_)=>Task.FromResult(0));
        using var provider=services.BuildServiceProvider(new ServiceProviderOptions{ValidateScopes=true,ValidateOnBuild=true});
        var authority=provider.GetRequiredService<IUsageReservationAuthority>();
        Assert.IsType<LocalUsageReservationAuthority>(authority);
        using var scope=provider.CreateScope();Assert.Same(authority,scope.ServiceProvider.GetRequiredService<IUsageReservationAuthority>());
    }
    [Fact]
    public async Task DurableDeliveryUsesExactReservationWithoutHttpContext()
    {
        var user=Guid.NewGuid().ToString("D");var operation=Guid.NewGuid().ToString("D");
        var service=new Mock<ISubscriptionService>(MockBehavior.Strict);
        var repository=new Mock<ISubscriptionUsageRepository>(MockBehavior.Strict);
        var start=new UsageStartRequest{UserId=user,OperationId=operation,ConsumingService="WEB4"};
        var settlement=new UsageSettlementRequest{UserId=user,OperationId=operation,ConsumingService="WEB4"};
        var started=new UsageStartResult{OperationId=operation,Started=true};
        var settled=new UsageSettlementResult{OperationId=operation,Settled=true};
        repository.Setup(x=>x.StartAsync(start,CancellationToken.None)).ReturnsAsync(started);
        service.Setup(x=>x.SettleUsageAsync(user,settlement,CancellationToken.None)).ReturnsAsync(settled);
        var authority=new LocalUsageReservationAuthority(service.Object,repository.Object,new HttpContextAccessor(),
            (_,_)=>throw new InvalidOperationException("Background delivery must not reload avatar karma."));
        Assert.Same(started,await authority.StartUsageAsync(start));
        Assert.Same(settled,await authority.SettleUsageAsync(settlement));
        repository.VerifyAll();service.VerifyAll();
        using var canceled=new CancellationTokenSource();canceled.Cancel();
        await Assert.ThrowsAsync<OperationCanceledException>(async()=>await authority.StartUsageAsync(start,canceled.Token));
        await Assert.ThrowsAsync<OperationCanceledException>(async()=>await authority.SettleUsageAsync(settlement,canceled.Token));
        repository.Verify(x=>x.StartAsync(It.IsAny<UsageStartRequest>(),It.IsAny<CancellationToken>()),Times.Once);
        service.Verify(x=>x.SettleUsageAsync(It.IsAny<string>(),It.IsAny<UsageSettlementRequest>(),It.IsAny<CancellationToken>()),Times.Once);
    }
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
