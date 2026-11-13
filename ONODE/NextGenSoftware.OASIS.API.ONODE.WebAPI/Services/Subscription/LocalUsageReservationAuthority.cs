using System;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Subscription;

/// <summary>WEB4-only authority transport. Admission uses authenticated request identity; durable delivery uses reservation ownership.</summary>
public sealed class LocalUsageReservationAuthority : IUsageReservationAuthority
{
    private readonly ISubscriptionService _subscriptions;
    private readonly ISubscriptionUsageRepository _repository;
    private readonly IHttpContextAccessor _context;
    private readonly Func<Guid,CancellationToken,Task<int>> _karma;
    private readonly IAppAccountRepository _apps;
    public LocalUsageReservationAuthority(ISubscriptionService subscriptions,ISubscriptionUsageRepository repository,
        IHttpContextAccessor context,Func<Guid,CancellationToken,Task<int>> authoritativeKarma,IAppAccountRepository apps=null)
    { _subscriptions=subscriptions; _repository=repository; _context=context; _karma=authoritativeKarma; _apps=apps; }
    public async Task<SubscriptionAuthorizationResult> AuthorizeUsageAsync(string bearerToken,UsageAuthorizationRequest request,CancellationToken token=default)
    {
        token.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(bearerToken);
        ArgumentNullException.ThrowIfNull(request); RequireLocal(request.ConsumingService);
        var context=_context.HttpContext;
        var claim=context?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value??context?.User.FindFirst("sub")?.Value;
        if(context?.User.Identity?.IsAuthenticated!=true||!Guid.TryParseExact(claim,"D",out var avatarId)
            ||(context.Items.TryGetValue("Avatar",out var value)&&value is IAvatar avatar&&avatar.Id!=avatarId))
            throw new UnauthorizedAccessException("Local WEB4 admission requires the authenticated canonical avatar identity.");
        // Optional app credentials: the app's plan applies if it beats the avatar's own. Absent headers change nothing.
        AppAccountRecord app=null;
        var headers=context.Request.Headers;
        if(headers.ContainsKey(AppAccountRules.AppIdHeader)||headers.ContainsKey(AppAccountRules.AppKeyHeader))
        {
            if(_apps==null)throw new UnauthorizedAccessException("App credentials are not supported by this deployment.");
            app=await _apps.VerifyAppCredentialAsync(headers[AppAccountRules.AppIdHeader].ToString(),headers[AppAccountRules.AppKeyHeader].ToString(),token)
                ??throw new UnauthorizedAccessException("The app credential is invalid or the app account is disabled.");
        }
        return await _subscriptions.AuthorizeUsageAsync(avatarId.ToString("D"),await _karma(avatarId,token),request,app,token);
    }
    public Task<UsageStartResult> StartUsageAsync(UsageStartRequest request,CancellationToken token=default)
    { token.ThrowIfCancellationRequested(); ArgumentNullException.ThrowIfNull(request); RequireLocal(request.ConsumingService); return _repository.StartAsync(request,token); }
    public Task<UsageSettlementResult> SettleUsageAsync(UsageSettlementRequest request,CancellationToken token=default)
    { token.ThrowIfCancellationRequested(); ArgumentNullException.ThrowIfNull(request); RequireLocal(request.ConsumingService); return _subscriptions.SettleUsageAsync(request.UserId,request,token); }
    private static void RequireLocal(string service)
    { if(service!="WEB4")throw new UnauthorizedAccessException("Local authority is restricted to WEB4-owned operations."); }
}

public static class LocalUsageAuthorityRegistration
{
    public static IServiceCollection AddLocalUsageAuthority(IServiceCollection services,Func<Guid,CancellationToken,Task<int>> authoritativeKarma)
    {
        ArgumentNullException.ThrowIfNull(services); ArgumentNullException.ThrowIfNull(authoritativeKarma);
        services.AddSingleton<IUsageReservationAuthority>(provider=>new LocalUsageReservationAuthority(
            provider.GetRequiredService<ISubscriptionService>(),provider.GetRequiredService<ISubscriptionUsageRepository>(),
            provider.GetRequiredService<IHttpContextAccessor>(),authoritativeKarma,provider.GetService<IAppAccountRepository>()));
        return services;
    }
}
