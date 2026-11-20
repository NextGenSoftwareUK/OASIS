using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Subscription;

/// <summary>Projects the canonical billing ledger for settings clients; Holon preferences never grant a plan.</summary>
public sealed class SubscriptionSettingsAuthority(
    ISubscriptionBillingRepository billing, ISubscriptionUsageRepository usage) : ISubscriptionSettingsAuthority
{
    public async Task<OASISResult<Dictionary<string, object>>> GetAsync(Guid avatarId, CancellationToken cancellationToken = default)
    {
        if (avatarId == Guid.Empty)
            return new() { IsError = true, ErrorCode = "SUBSCRIPTION_SETTINGS_INVALID", Message = "A non-empty avatar ID is required." };
        try
        {
            var owner = avatarId.ToString("D");
            var record = await billing.GetSubscriptionAsync(owner, cancellationToken);
            var settings = new Dictionary<string, object>
            {
                ["hasSubscription"] = record != null,
                ["currentPlan"] = null,
                ["planType"] = null,
                ["status"] = null,
                ["isActive"] = false,
                ["expiresAt"] = null,
                ["autoRenew"] = false,
                ["payAsYouGoEnabled"] = false
            };
            if (record != null)
            {
                if (record.UserId != owner)
                    throw new InvalidOperationException("The subscription record does not belong to the requested avatar.");
                var plan = SubscriptionPlanCatalogue.Definitions.SingleOrDefault(item => item.Id == record.PlanId);
                if (plan == null)
                    return new() { IsError = true, ErrorCode = "SUBSCRIPTION_PLAN_UNKNOWN", Message = "The stored subscription references an unknown plan." };
                var now = DateTime.UtcNow;
                var aggregate = await usage.GetAggregateForPeriodAsync(owner,
                    new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc), cancellationToken);
                settings["currentPlan"] = record.PlanId;
                settings["planType"] = record.PlanId;
                settings["status"] = record.Status;
                settings["isActive"] = record.IsActiveAt(now);
                settings["expiresAt"] = record.CurrentPeriodEnd;
                settings["autoRenew"] = !record.CancelAtPeriodEnd && !string.IsNullOrEmpty(record.StripeSubscriptionId);
                settings["cancelAtPeriodEnd"] = record.CancelAtPeriodEnd;
                settings["payAsYouGoEnabled"] = record.PayAsYouGoEnabled;
                settings["maxRequestsPerMonth"] = plan.MaxRequestsPerMonth;
                settings["maxStorageGB"] = plan.MaxStorageGB;
                settings["requestsUsedThisMonth"] = aggregate.MonthlyRequests;
                settings["updatedAt"] = record.UpdatedAt;
            }
            return new(settings) { Message = "Subscription settings read from the billing authority." };
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception cause)
        {
            return new() { IsError = true, ErrorCode = "SUBSCRIPTION_SETTINGS_READ_FAILED",
                Message = "The billing authority could not read subscription settings.", Exception = cause };
        }
    }

    public async Task<OASISResult<bool>> UpdateAsync(Guid avatarId, Dictionary<string, object> settings,
        CancellationToken cancellationToken = default)
    {
        if (avatarId == Guid.Empty || settings == null || settings.Count != 1 ||
            !settings.TryGetValue("payAsYouGoEnabled", out var value) || !TryBoolean(value, out var enabled))
            return new() { IsError = true, ErrorCode = "SUBSCRIPTION_SETTINGS_INVALID",
                Message = "Settings updates accept only the Boolean payAsYouGoEnabled preference. Plans, payments and renewal changes use the subscription billing flow." };
        if (enabled)
            return new() { IsError = true, ErrorCode = "SUBSCRIPTION_CONSENT_REQUIRED",
                Message = "Enable paid overages through the verified subscription consent flow." };
        try
        {
            await billing.SetPayAsYouGoAsync(avatarId.ToString("D"), false, cancellationToken);
            return new(true) { IsSaved = true, Message = "Paid overages disabled in the billing authority." };
        }
        catch (OperationCanceledException) { throw; }
        catch (KeyNotFoundException cause)
        {
            return new() { IsError = true, ErrorCode = "SUBSCRIPTION_REQUIRED", Message = cause.Message };
        }
        catch (Exception cause)
        {
            return new() { IsError = true, ErrorCode = "SUBSCRIPTION_SETTINGS_SAVE_FAILED",
                Message = "The billing authority could not save the subscription preference.", Exception = cause };
        }
    }

    private static bool TryBoolean(object value, out bool result)
    {
        if (value is bool boolean) { result = boolean; return true; }
        if (value is JsonElement json && json.ValueKind is JsonValueKind.True or JsonValueKind.False)
        { result = json.GetBoolean(); return true; }
        result = false;
        return false;
    }
}
