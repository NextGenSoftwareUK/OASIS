using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Stripe;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Subscription;

public sealed class SubscriptionPlanDefinition
{
    public string Id { get; set; }
    public string Name { get; set; }
    public decimal PriceMonthly { get; set; }
    public decimal Price => PriceMonthly;
    public decimal Amount => PriceMonthly;
    public string Interval { get; set; } = "month";
    public string Currency { get; set; } = "USD";
    public string[] Features { get; set; }
    public bool IsContactSales { get; set; }
    public int MaxRequestsPerMonth { get; set; }
    public int MaxStorageGB { get; set; }
    public string SupportLevel { get; set; }
}

public record SubscriptionPlanPrice(decimal Amount, string Currency, string Interval);
public interface ISubscriptionPlanPriceReader
{
    Task<SubscriptionPlanPrice> GetAsync(string planId, CancellationToken cancellationToken);
}

public sealed class StripeSubscriptionPlanPriceReader(IConfiguration configuration) : ISubscriptionPlanPriceReader
{
    public async Task<SubscriptionPlanPrice> GetAsync(string planId, CancellationToken cancellationToken)
    {
        string secret = SubscriptionStripeConfiguration.SecretKey(configuration);
        string id = SubscriptionStripeConfiguration.PriceId(configuration, planId);
        if (string.IsNullOrWhiteSpace(secret) || string.IsNullOrWhiteSpace(id))
            throw new InvalidOperationException($"Stripe key and existing price ID are required for plan '{planId}'.");
        var price = await new PriceService(new StripeClient(secret)).GetAsync(id, cancellationToken: cancellationToken);
        if (!price.Active || price.Recurring?.Interval != "month" || price.Recurring.IntervalCount != 1 || price.UnitAmount == null)
            throw new InvalidOperationException($"The configured Stripe price for '{planId}' must be active, fixed, and monthly.");
        return new(StripeAmount.FromMinorUnits(price.UnitAmount.Value, price.Currency), price.Currency.ToUpperInvariant(), price.Recurring.Interval);
    }
}

public static class StripeAmount
{
    private static readonly HashSet<string> ZeroDecimal = new(StringComparer.OrdinalIgnoreCase)
        { "bif", "clp", "djf", "gnf", "jpy", "kmf", "krw", "mga", "pyg", "rwf", "vnd", "vuv", "xaf", "xof", "xpf" };
    private static readonly HashSet<string> ThreeDecimal = new(StringComparer.OrdinalIgnoreCase)
        { "bhd", "jod", "kwd", "omr", "tnd" };
    public static decimal FromMinorUnits(long amount, string currency)
    {
        if (string.IsNullOrWhiteSpace(currency) || currency.Length != 3 || amount < 0)
            throw new ArgumentException("A nonnegative amount and three-letter Stripe currency are required.");
        return amount / (ZeroDecimal.Contains(currency) ? 1m : ThreeDecimal.Contains(currency) ? 1000m : 100m);
    }
}

public sealed class SubscriptionPlanCatalogue(ISubscriptionPlanPriceReader prices)
{
    public static IReadOnlyList<SubscriptionPlanDefinition> Definitions { get; } = new[]
    {
        new SubscriptionPlanDefinition { Id="free", Name="Free", Features=new[]{"1,000 API requests/month", "100 MB storage", "Community support"}, MaxRequestsPerMonth=1000, MaxStorageGB=0, SupportLevel="Community" },
        new SubscriptionPlanDefinition { Id="bronze", Name="Bronze", Features=new[]{"10,000 API requests/month", "1 GB storage", "Community support", "Basic analytics"}, MaxRequestsPerMonth=10000, MaxStorageGB=1, SupportLevel="Community" },
        new SubscriptionPlanDefinition { Id="silver", Name="Silver", Features=new[]{"100,000 API requests/month", "10 GB storage", "Email support", "Priority processing", "Advanced analytics"}, MaxRequestsPerMonth=100000, MaxStorageGB=10, SupportLevel="Email" },
        new SubscriptionPlanDefinition { Id="gold", Name="Gold", Features=new[]{"1,000,000 API requests/month", "100 GB storage", "Priority support", "Advanced analytics", "Custom integrations", "SLA guarantee"}, MaxRequestsPerMonth=1000000, MaxStorageGB=100, SupportLevel="Priority" },
        new SubscriptionPlanDefinition { Id="enterprise", Name="Enterprise", IsContactSales=true, Features=new[]{"Unlimited API requests", "Unlimited storage", "Dedicated support", "SLA & SSO", "On-premise deployment", "Custom contracts"}, MaxRequestsPerMonth=-1, MaxStorageGB=-1, SupportLevel="Dedicated" }
    };

    public Task<SubscriptionPlanDefinition[]> GetAllAsync(CancellationToken ct = default) =>
        Task.WhenAll(Definitions.Select(async definition =>
        {
            var result = new SubscriptionPlanDefinition
            {
                Id=definition.Id, Name=definition.Name, Features=definition.Features.ToArray(), IsContactSales=definition.IsContactSales,
                MaxRequestsPerMonth=definition.MaxRequestsPerMonth, MaxStorageGB=definition.MaxStorageGB, SupportLevel=definition.SupportLevel
            };
            if (definition.Id != "free" && !definition.IsContactSales)
            {
                var price = await prices.GetAsync(definition.Id, ct);
                result.PriceMonthly=price.Amount; result.Currency=price.Currency; result.Interval=price.Interval;
            }
            return result;
        }));
}
