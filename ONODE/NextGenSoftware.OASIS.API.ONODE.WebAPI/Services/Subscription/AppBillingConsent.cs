using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using NextGenSoftware.OASIS.API.DNA;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Subscription;

[JsonConverter(typeof(AppPaygConsentRequestJsonConverter))]
public sealed class AppPaygConsentRequest
{
    public bool Enabled { get; init; }
    public decimal MonthlyCapUsd { get; init; }
    public string PricingVersion { get; init; }
    public string PricingSha256 { get; init; }
    public bool Consent { get; init; }

    public void ValidateOptIn(AppOveragePrices prices)
    {
        if (!Consent) throw new ArgumentException("Consent to the published USD prices and monthly cap is required.");
        if (MonthlyCapUsd < 0.5m || MonthlyCapUsd > 1_000_000m || decimal.Round(MonthlyCapUsd, 2) != MonthlyCapUsd)
            throw new ArgumentException("Choose a monthly USD cap from 0.50 to 1000000.00 in whole cents.");
        if (PricingVersion != prices.Version || PricingSha256 != prices.PricingSha256)
            throw new SubscriptionUsageConflictException("App overage terms changed; review and accept the current published terms.");
    }
}

/// <summary>Financial intent requires an explicit switch and unambiguous, known JSON fields.</summary>
public sealed class AppPaygConsentRequestJsonConverter : JsonConverter<AppPaygConsentRequest>
{
    public override AppPaygConsentRequest Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var document = JsonDocument.ParseValue(ref reader);
        if (document.RootElement.ValueKind != JsonValueKind.Object) throw new JsonException("App billing preferences must be an object.");
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        bool enabled = false, consent = false;
        decimal cap = 0;
        string version = null, fingerprint = null;
        foreach (var field in document.RootElement.EnumerateObject())
        {
            if (!seen.Add(field.Name)) throw new JsonException("Duplicate app billing preference.");
            switch (field.Name.ToLowerInvariant())
            {
                case "enabled":
                    if (field.Value.ValueKind is not (JsonValueKind.True or JsonValueKind.False)) throw new JsonException("Enabled must be a boolean.");
                    enabled = field.Value.GetBoolean();
                    break;
                case "consent":
                    if (field.Value.ValueKind is not (JsonValueKind.True or JsonValueKind.False)) throw new JsonException("Consent must be a boolean.");
                    consent = field.Value.GetBoolean();
                    break;
                case "monthlycapusd":
                    if (field.Value.ValueKind != JsonValueKind.Number || !field.Value.TryGetDecimal(out cap)) throw new JsonException("The USD cap must be a decimal number.");
                    break;
                case "pricingversion":
                case "pricingsha256":
                    if (field.Value.ValueKind is not (JsonValueKind.String or JsonValueKind.Null)) throw new JsonException("Published terms identifiers must be strings.");
                    if (field.Name.Equals("pricingVersion", StringComparison.OrdinalIgnoreCase)) version = field.Value.GetString();
                    else fingerprint = field.Value.GetString();
                    break;
                default: throw new JsonException("Unrecognized app billing preference.");
            }
        }
        if (!seen.Contains("enabled")) throw new JsonException("An explicit enabled preference is required.");
        return new AppPaygConsentRequest { Enabled = enabled, Consent = consent, MonthlyCapUsd = cap,
            PricingVersion = version, PricingSha256 = fingerprint };
    }

    public override void Write(Utf8JsonWriter writer, AppPaygConsentRequest value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteBoolean("enabled", value.Enabled);
        writer.WriteBoolean("consent", value.Consent);
        writer.WriteNumber("monthlyCapUsd", value.MonthlyCapUsd);
        if (value.PricingVersion != null) writer.WriteString("pricingVersion", value.PricingVersion);
        if (value.PricingSha256 != null) writer.WriteString("pricingSha256", value.PricingSha256);
        writer.WriteEndObject();
    }
}

/// <summary>Server-side local identity snapshot, created only after the awaited Stripe readiness check.</summary>
public sealed record AppBillingPayerSnapshot(string AvatarId, string PlanId, string CustomerId,
    string SubscriptionId, long AuthorizationVersion, DateTime VerifiedAtUtc)
{
    public static AppBillingPayerSnapshot AfterVerification(SubscriptionRecord payer, DateTime verifiedAtUtc) =>
        new(payer.UserId, payer.PlanId, payer.StripeCustomerId, payer.StripeSubscriptionId, payer.AuthorizationVersion, verifiedAtUtc);
}

public static class AppBillingConsent
{
    public static bool HasCurrentTerms(AppPaygSettings settings, string plan, AppOveragePrices prices) =>
        settings?.ConsentSchemaVersion == 1 && Guid.TryParseExact(settings.ConsentId, "D", out _) &&
        settings.PricingVersion == prices.Version && settings.PricingSha256 == prices.PricingSha256 &&
        settings.Currency == prices.Currency && prices.HasPrice(plan) && settings.RatePerRequestUsd == prices.Rate(plan) &&
        settings.MonthlyCapUsd >= 0.5m && settings.MonthlyCapUsd <= 1_000_000m && decimal.Round(settings.MonthlyCapUsd, 2) == settings.MonthlyCapUsd &&
        settings.ConsentAtUtc.HasValue && settings.PaymentVerifiedAtUtc.HasValue &&
        settings.PaymentVerifiedAtUtc.Value <= settings.ConsentAtUtc.Value &&
        settings.ConsentAtUtc.Value - settings.PaymentVerifiedAtUtc.Value <= TimeSpan.FromMinutes(5) &&
        settings.BillingAuthorizationVersion > 0 && settings.ConsentBy == settings.BillingAvatarId &&
        Guid.TryParseExact(settings.BillingAvatarId, "D", out _);

    public static string UnavailableReason(AppAccountRecord app, bool nodeEnabled)
    {
        if (!nodeEnabled) return "App overages are disabled on this node.";
        if (!AppAccountRules.PooledQuotasEnabled) return "Pooled app quotas are disabled on this node.";
        if (app.Status != "active") return "This app account is not active.";
        if (!app.PooledQuota || app.PlanId is "enterprise" or "free") return "This app does not use a finite paid shared request pool.";
        return null;
    }

    public static void RequireEligible(AppAccountRecord app)
    {
        var enabled = OASISBootLoader.OASISBootLoader.OASISDNA?.OASIS?.UsageVisibility?.AppPayAsYouGoEnabled
            ?? new UsageVisibilitySettings().AppPayAsYouGoEnabled;
        var reason = UnavailableReason(app, enabled);
        if (reason != null) throw new SubscriptionUsageConflictException(reason);
    }

    public static void RequirePaidPayer(SubscriptionRecord payer, DateTime now)
    {
        if (payer == null || payer.PlanId == "free" || payer.Status is not ("active" or "trialing") ||
            payer.CurrentPeriodEnd.HasValue && payer.CurrentPeriodEnd.Value <= now || payer.OveragePaymentBlocked ||
            string.IsNullOrWhiteSpace(payer.StripeCustomerId) || string.IsNullOrWhiteSpace(payer.StripeSubscriptionId))
            throw new SubscriptionUsageConflictException("An active paid subscription with a verified payment method and no blocked overage payment is required.");
    }
}

public sealed class AppBillingUnavailableException(string message) : Exception(message) { }
