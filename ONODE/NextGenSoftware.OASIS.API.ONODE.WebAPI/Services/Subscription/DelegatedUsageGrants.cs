using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Subscription
{
    public interface IDelegatedUsageGrantRepository
    {
        Task<DelegatedUsageGrant> CreateGrantAsync(string userId, CreateDelegatedUsageGrantRequest request, CancellationToken cancellationToken);
        Task<IReadOnlyList<DelegatedUsageGrant>> ListGrantsAsync(string userId, CancellationToken cancellationToken);
        Task<DelegatedUsageGrant> GetGrantAsync(string grantId, CancellationToken cancellationToken);
        Task<DelegatedUsageGrant> RevokeGrantAsync(string userId, string grantId, CancellationToken cancellationToken);
    }

    /// <summary>The single definition of what a delegated grant may authorize. Used at creation and inside the reservation transaction.</summary>
    public static class DelegatedUsageGrantValidation
    {
        public const int MaxEndpoints = 32;
        public static readonly TimeSpan MaxLifetime = TimeSpan.FromDays(366);

        public static void ValidateCreate(string userId, CreateDelegatedUsageGrantRequest request, DateTime now)
        {
            if (!Guid.TryParseExact(userId, "D", out _)) throw new ArgumentException("A canonical avatar id is required.");
            if (request == null) throw new ArgumentException("A grant request is required.");
            string service = request.ConsumingService?.Trim().ToUpperInvariant();
            // WEB4 never consumes its own grants: delegation exists for downstream services without the avatar's bearer.
            if (!SubscriptionServiceIdentity.IsService(service) || service == "WEB4")
                throw new ArgumentException("ConsumingService must be one of WEB5-WEB10.");
            if (string.IsNullOrWhiteSpace(request.Scope) || request.Scope.Length > 256)
                throw new ArgumentException("Scope is required and must be at most 256 characters.");
            if (request.Endpoints == null || request.Endpoints.Count == 0 || request.Endpoints.Count > MaxEndpoints ||
                request.Endpoints.Any(e => string.IsNullOrWhiteSpace(e) || e.Length > 2048))
                throw new ArgumentException($"Between 1 and {MaxEndpoints} explicit endpoints are required; wildcard grants are not supported.");
            if (request.MonthlyBudgetUsd <= 0 || request.MaxOperationCostUsd <= 0 || request.MaxOperationCostUsd > request.MonthlyBudgetUsd)
                throw new ArgumentException("MonthlyBudgetUsd and MaxOperationCostUsd must be positive, and the per-operation cap cannot exceed the monthly budget.");
            var expires = DateTime.SpecifyKind(request.ExpiresAtUtc, DateTimeKind.Utc);
            if (expires <= now || expires - now > MaxLifetime)
                throw new ArgumentException("ExpiresAtUtc must be in the future and at most 366 days away.");
        }

        public static void EnsureReservable(DelegatedUsageGrant grant, SubscriptionUsageEvent usageEvent)
        {
            if (grant.UserId != usageEvent.UserId || grant.ConsumingService != usageEvent.ConsumingService)
                throw new SubscriptionUsageLimitException("GRANT_SCOPE_MISMATCH", "The delegated grant does not belong to this avatar and consuming service.", 403);
            if (grant.RevokedAtUtc != null)
                throw new SubscriptionUsageLimitException("GRANT_REVOKED", "The delegated grant was revoked.", 403);
            if (grant.ExpiresAtUtc <= usageEvent.AuthorizedAtUtc)
                throw new SubscriptionUsageLimitException("GRANT_EXPIRED", "The delegated grant has expired.", 403);
            if (!grant.Endpoints.Contains(usageEvent.Endpoint, StringComparer.Ordinal))
                throw new SubscriptionUsageLimitException("GRANT_ENDPOINT_NOT_ALLOWED", "The delegated grant does not cover this endpoint.", 403);
            if (usageEvent.ReservedCostUsd > grant.MaxOperationCostUsd)
                throw new SubscriptionUsageLimitException("GRANT_OPERATION_CAP_EXCEEDED", "This reservation exceeds the grant's per-operation USD cap.", 402);
            if (checked(grant.MonthReservedCostUsd + grant.MonthSettledCostUsd + usageEvent.ReservedCostUsd) > grant.MonthlyBudgetUsd)
                throw new SubscriptionUsageLimitException("GRANT_BUDGET_EXCEEDED", "This reservation would exceed the grant's monthly USD budget.", 402);
        }
    }
}
