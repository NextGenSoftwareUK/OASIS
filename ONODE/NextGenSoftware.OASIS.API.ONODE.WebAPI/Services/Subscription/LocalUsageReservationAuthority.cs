using System;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
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
    public LocalUsageReservationAuthority(ISubscriptionService subscriptions,ISubscriptionUsageRepository repository,
        IHttpContextAccessor context,Func<Guid,CancellationToken,Task<int>> authoritativeKarma)
    { _subscriptions=subscriptions; _repository=repository; _context=context; _karma=authoritativeKarma; }
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
        return await _subscriptions.AuthorizeUsageAsync(avatarId.ToString("D"),await _karma(avatarId,token),request,token);
    }
    public Task<UsageStartResult> StartUsageAsync(UsageStartRequest request,CancellationToken token=default)
    { token.ThrowIfCancellationRequested(); ArgumentNullException.ThrowIfNull(request); RequireLocal(request.ConsumingService); return _repository.StartAsync(request,token); }
    public Task<UsageSettlementResult> SettleUsageAsync(UsageSettlementRequest request,CancellationToken token=default)
    { token.ThrowIfCancellationRequested(); ArgumentNullException.ThrowIfNull(request); RequireLocal(request.ConsumingService); return _subscriptions.SettleUsageAsync(request.UserId,request,token); }
    private static void RequireLocal(string service)
    { if(service!="WEB4")throw new UnauthorizedAccessException("Local authority is restricted to WEB4-owned operations."); }
}
