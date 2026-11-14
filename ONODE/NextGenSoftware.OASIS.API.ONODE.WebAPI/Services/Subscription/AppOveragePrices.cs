using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Subscription
{
    /// <summary>Reads Configuration/app-overage-prices.json. It ships with NO prices, so pay-as-you-go for app pools cannot be enabled until David fills them in.</summary>
    public sealed class AppOveragePrices
    {
        public string Version { get; private set; } = "app-overages-unset";
        private readonly Dictionary<string, decimal> _rates = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        public decimal Rate(string planId) => planId != null && _rates.TryGetValue(planId, out var r) ? r : 0m;
        public bool HasPrice(string planId) => planId != null && _rates.TryGetValue(planId, out var r) && r > 0;

        public static AppOveragePrices Load()
        {
            var prices = new AppOveragePrices();
            try
            {
                var path = Path.Combine(AppContext.BaseDirectory, "Configuration", "app-overage-prices.json");
                if (!System.IO.File.Exists(path)) return prices;
                using var doc = JsonDocument.Parse(System.IO.File.ReadAllText(path));
                if (doc.RootElement.TryGetProperty("version", out var v)) prices.Version = v.GetString() ?? prices.Version;
                if (doc.RootElement.TryGetProperty("requestRatesUsd", out var rates) && rates.ValueKind == JsonValueKind.Object)
                    foreach (var p in rates.EnumerateObject()) if (p.Value.TryGetDecimal(out var d)) prices._rates[p.Name] = d;
            }
            catch (Exception) { /* unreadable catalogue = no prices = disabled */ }
            return prices;
        }
    }
}
