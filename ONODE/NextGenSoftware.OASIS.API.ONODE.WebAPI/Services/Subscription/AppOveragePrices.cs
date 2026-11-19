using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Subscription;

/// <summary>Immutable, reviewed app-pool USD terms from the shipping catalogue, loaded once per process.</summary>
public sealed class AppOveragePrices
{
    private static readonly Lazy<AppOveragePrices> Published = new(() =>
        Parse(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Configuration", "app-overage-prices.json"))));
    private readonly Dictionary<string, decimal> _rates;
    public string Version { get; }
    public string PricingSha256 { get; }
    public string Currency => "USD";

    private AppOveragePrices(PaidOveragePricing pricing)
    {
        Version = pricing.Version;
        PricingSha256 = pricing.PricingSha256;
        _rates = new Dictionary<string, decimal>(pricing.RequestRatesUsd, StringComparer.Ordinal);
    }

    public static AppOveragePrices Load() => Published.Value;

    public static AppOveragePrices Parse(byte[] utf8)
    {
        ArgumentNullException.ThrowIfNull(utf8);
        using var document = JsonDocument.Parse(utf8);
        if (document.RootElement.ValueKind != JsonValueKind.Object)
            throw new InvalidOperationException("The published app overage catalogue must be an object.");
        var fields = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var field in document.RootElement.EnumerateObject())
        {
            if (!fields.Add(field.Name)) throw new InvalidOperationException("Duplicate app overage catalogue field.");
            if (string.Equals(field.Name, "requestRatesUsd", StringComparison.OrdinalIgnoreCase))
            {
                if (field.Value.ValueKind != JsonValueKind.Object)
                    throw new InvalidOperationException("Published app request rates are required.");
                var plans = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var plan in field.Value.EnumerateObject())
                    if (!plans.Add(plan.Name)) throw new InvalidOperationException("Duplicate app request rate.");
            }
        }
        var pricing = JsonSerializer.Deserialize<PaidOveragePricing>(utf8,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new InvalidOperationException("The published app overage catalogue is required.");
        if (pricing.RequestRatesUsd == null) throw new InvalidOperationException("Published app request rates are required.");
        pricing.Validate();
        return new AppOveragePrices(pricing);
    }

    public bool HasPrice(string plan) => plan != null && _rates.ContainsKey(plan);
    public decimal Rate(string plan) => HasPrice(plan) ? _rates[plan]
        : throw new ArgumentException("App overages require a configured paid plan.");
}
