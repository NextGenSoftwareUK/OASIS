using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Subscription;

/// <summary>Reviewed USD customer rates, separate from GBP monthly plans and provider costs.</summary>
public sealed class PaidOveragePricing
{
    public string Version { get; set; }
    public string Currency { get; set; }
    public decimal AiMargin { get; set; }
    public Dictionary<string, decimal> RequestRatesUsd { get; set; } = new();
    public string PricingSha256 => Convert.ToHexStringLower(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(new {Version,Currency,AiMargin,Rates=RequestRatesUsd.OrderBy(x=>x.Key,StringComparer.Ordinal).ToArray()})));
    public static PaidOveragePricing Load()
    {
        var path=Path.Combine(AppContext.BaseDirectory,"Configuration","subscription-overage-prices.json");
        var pricing=JsonSerializer.Deserialize<PaidOveragePricing>(System.IO.File.ReadAllText(path),new JsonSerializerOptions {PropertyNameCaseInsensitive=true});
        pricing.Validate();
        return pricing;
    }
    public void Validate()
    {
        if(string.IsNullOrWhiteSpace(Version) || Currency != "USD" || AiMargin != 0 || RequestRatesUsd.Count != 4)
            throw new InvalidOperationException("Overages require versioned USD rates and the agreed zero AI margin.");
        foreach(var plan in new[]{"bronze","silver","gold","enterprise"})
            if(!RequestRatesUsd.TryGetValue(plan,out var rate) || rate<=0 || rate>1)
                throw new InvalidOperationException("Each paid plan requires an approved positive USD request rate.");
    }
    public decimal Rate(string plan) => RequestRatesUsd.TryGetValue(plan ?? "",out var rate)
        ? rate : throw new InvalidOperationException("Paid overages are available only for the configured paid plans.");
    public static bool HasAcceptedTerms(SubscriptionRecord subscriber) => subscriber!=null && subscriber.PlanId!="free" &&
        !string.IsNullOrWhiteSpace(subscriber.StripeCustomerId) && !string.IsNullOrWhiteSpace(subscriber.StripeSubscriptionId) &&
        !string.IsNullOrWhiteSpace(subscriber.PaidOverageTermsVersion) && subscriber.PaidOverageTermsSha256?.Length==64 &&
        subscriber.PaidOverageTermsAcceptedAtUtc.HasValue && subscriber.MonthlyOverageLimitUsd>0;
    public static bool HasConsent(SubscriptionRecord subscriber) => subscriber?.PayAsYouGoEnabled==true && HasAcceptedTerms(subscriber);
    public static decimal Excess(decimal amount, decimal included) => included<=0 ? 0 : Math.Max(0,amount-included);
    public static decimal RequestReservation(SubscriptionUsagePolicy policy,SubscriptionUsageAggregate usage,decimal rate)
        => policy.MonthlyRequestLimit>=0 && usage.MonthlyRequests>=policy.MonthlyRequestLimit ? rate : 0;
    public static decimal AiSettlement(SubscriptionUsagePolicy policy,SubscriptionUsageAggregate usage,decimal measured)
        => checked(Excess(usage.SettledCostUsd+measured,policy.MonthlyBudgetUsd)-Excess(usage.SettledCostUsd,policy.MonthlyBudgetUsd));
    public static long MinorUnits(decimal amount) => checked((long)Math.Round(amount*100m,0,MidpointRounding.AwayFromZero));
}
