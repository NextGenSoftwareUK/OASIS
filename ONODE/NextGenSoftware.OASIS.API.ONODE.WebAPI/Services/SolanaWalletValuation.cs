using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Services;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services;

internal static class SolanaWalletValuation
{
    private static readonly HttpClient Market = new(new HttpClientHandler { AllowAutoRedirect = false })
    {
        MaxResponseContentBufferSize = 65536
    };
    private static readonly CoinGeckoExchangeRateService NativeQuotes = CoinGeckoExchangeRateService.ForPublicMarketQuotes(Market);

    internal static async Task<(decimal Value, int Priced, int Unpriced)> ReadAsync(
        IEnumerable<SolanaWalletAsset> balances, CancellationToken cancellationToken)
    {
        var holdings = balances.Where(balance => balance.RawAmount != "0").ToArray();
        var prices = new Dictionary<string, decimal>(StringComparer.Ordinal);
        if (holdings.Any(asset => asset.Mint == null))
        {
            var quote = await NativeQuotes.GetUsdQuoteAsync("SOL", cancellationToken);
            if (!quote.IsError && quote.Result != null) prices["SOL"] = quote.Result.PriceUsd;
        }

        foreach (var batch in holdings.Select(asset => asset.Mint).Where(mint => mint != null).Distinct(StringComparer.Ordinal).Chunk(100))
        {
            var query = string.Join(",", batch.Select(Uri.EscapeDataString));
            using var request = new HttpRequestMessage(HttpMethod.Get,
                "https://api.coingecko.com/api/v3/simple/token_price/solana?vs_currencies=usd&include_last_updated_at=true&precision=full&contract_addresses=" + query);
            request.Headers.UserAgent.ParseAdd("OASIS-MarketQuotes/1.0 (+https://oasisweb4.com)");
            using var response = await Market.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode) break;

            using var content = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var json = await JsonDocument.ParseAsync(content, cancellationToken: cancellationToken);
            if (json.RootElement.ValueKind != JsonValueKind.Object) break;
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            foreach (var mint in batch)
            {
                if (json.RootElement.TryGetProperty(mint, out var quote) && quote.ValueKind == JsonValueKind.Object &&
                    quote.TryGetProperty("usd", out var price) && price.ValueKind == JsonValueKind.Number &&
                    price.TryGetDecimal(out var value) && value > 0 &&
                    quote.TryGetProperty("last_updated_at", out var timestamp) && timestamp.ValueKind == JsonValueKind.Number &&
                    timestamp.TryGetInt64(out var updated) && updated <= now && updated >= now - 300)
                    prices[mint] = value;
            }
        }

        decimal total = 0;
        var priced = 0;
        foreach (var asset in holdings)
        {
            if (!prices.TryGetValue(asset.Mint ?? "SOL", out var price) ||
                !decimal.TryParse(asset.Amount, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var quantity) || quantity <= 0)
                continue;

            var value = quantity * price;
            if (value <= 0) continue;
            total = checked(total + value);
            priced++;
        }
        return (total, priced, holdings.Length - priced);
    }
}
