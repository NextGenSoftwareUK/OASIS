namespace API.Infrastructure.Workers.ExchangeRate;

/// <summary>
/// A service responsible for fetching and updating exchange rates between two cryptocurrencies
/// (SOL and XRD) from an external API and saving them into the database. 
/// This service interacts with the KuCoin API to retrieve market tickers and calculates the exchange 
/// rates by comparing prices against USDT (Tether) to determine the conversion rates between the tokens.
/// </summary>
public sealed class ExchangeRateUpdaterService(DataContext context, HttpClient client)
{
    private const string Sol = "SOL";
    private const string Xrd = "XRD";
    private const string Usdt = "-USDT";

    /// <summary>
    /// Asynchronously fetches exchange rates for SOL and XRD and saves them to the database.
    /// This method retrieves the current exchange rate between SOL and XRD from the KuCoin API, 
    /// then saves the corresponding exchange rates to the database for later use.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token used to propagate cancellation signals.</param>
    public async Task UpdateExchangeRatesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Fetching exchange rates for SOL -> XRD and XRD -> SOL
            decimal solToXrd = await GetExchangeRate(Sol, Xrd);
            decimal xrdToSol = await GetExchangeRate(Xrd, Sol);

            // Saving exchange rates to the database
            await SaveExchangeRateAsync(Sol, Xrd, solToXrd, cancellationToken);
            await SaveExchangeRateAsync(Xrd, Sol, xrdToSol, cancellationToken);
        }
        catch (Exception ex)
        {
            // Logging error message in case of failure
            Console.WriteLine($"Error updating exchange rates: {ex.Message}");
        }
    }

    /// <summary>
    /// Fetches the exchange rate for a given pair of cryptocurrencies from the KuCoin API.
    /// It retrieves market tickers and calculates the exchange rate based on the prices of the 
    /// given currencies against USDT (Tether). If the rate cannot be determined, an exception is thrown.
    /// </summary>
    /// <param name="fromSymbol">The symbol of the source cryptocurrency (e.g., "SOL").</param>
    /// <param name="toSymbol">The symbol of the target cryptocurrency (e.g., "XRD").</param>
    /// <returns>The exchange rate between the source and target cryptocurrencies.</returns>
    /// <exception cref="Exception">Thrown if the API response is invalid or if exchange rates cannot be fetched.</exception>
    private async Task<decimal> GetExchangeRate(string fromSymbol, string toSymbol)
    {
        // The URL for fetching all tickers from the KuCoin API
        string url = "https://api.kucoin.com/api/v1/market/allTickers";

        // Sending GET request to KuCoin API to retrieve tickers
        HttpResponseMessage response = await client.GetAsync(url);
        response.EnsureSuccessStatusCode();
        string responseBody = await response.Content.ReadAsStringAsync();

        // Parsing the response into a JSON object
        JObject json = JObject.Parse(responseBody);
        if (json["data"]?["ticker"] is not JArray tickers)
        {
            // If 'ticker' field is missing or invalid, an exception is thrown
            throw new Exception("Unexpected API response: 'data.ticker' field is missing or invalid.");
        }

        // Fetching the price of both cryptocurrencies in terms of USDT
        decimal fromUsdt = GetPrice(tickers, fromSymbol + Usdt);
        decimal toUsdt = GetPrice(tickers, toSymbol + Usdt);

        // If the price for any currency is missing, an exception is thrown
        if (fromUsdt == 0 || toUsdt == 0)
        {
            throw new Exception($"Failed to fetch exchange rates for {fromSymbol} or {toSymbol}.");
        }

        // Calculating and returning the exchange rate
        return fromUsdt / toUsdt;
    }

    /// <summary>
    /// Retrieves the price of a given cryptocurrency against USDT from the list of tickers.
    /// </summary>
    /// <param name="tickers">The array of tickers retrieved from the KuCoin API response.</param>
    /// <param name="symbol">The symbol of the cryptocurrency (e.g., "SOL-USDT").</param>
    /// <returns>The price of the cryptocurrency in terms of USDT.</returns>
    private static decimal GetPrice(JArray tickers, string symbol)
    {
        // Iterating through the list of tickers to find the price for the given symbol
        foreach (JToken ticker in tickers)
        {
            // If the symbol matches, return the price
            if (ticker["symbol"]?.ToString() == symbol &&
                decimal.TryParse(ticker["last"]?.ToString(), out decimal price))
            {
                return price;
            }
        }

        // If the price for the given symbol is not found, return 0
        return 0;
    }

    /// <summary>
    /// Saves the calculated exchange rate into the database.
    /// It stores the exchange rate along with the related token IDs and the source URL of the data.
    /// If any of the tokens are not found in the database, the rate is not saved.
    /// </summary>
    /// <param name="fromSymbol">The symbol of the source cryptocurrency (e.g., "SOL").</param>
    /// <param name="toSymbol">The symbol of the target cryptocurrency (e.g., "XRD").</param>
    /// <param name="rate">The exchange rate between the source and target cryptocurrencies.</param>
    /// <param name="cancellationToken">The cancellation token used to propagate cancellation signals.</param>
    private async Task SaveExchangeRateAsync(string fromSymbol, string toSymbol, decimal rate,
        CancellationToken cancellationToken)
    {
        // Fetching token data from the database
        NetworkToken? fromToken =
            await context.NetworkTokens.FirstOrDefaultAsync(t => t.Symbol == fromSymbol, cancellationToken);
        NetworkToken? toToken =
            await context.NetworkTokens.FirstOrDefaultAsync(t => t.Symbol == toSymbol, cancellationToken);

        // If either token is missing, log the error and skip saving the rate
        if (fromToken == null || toToken == null)
        {
            Console.WriteLine($"Skipping saving exchange rate: Missing token info for {fromSymbol} or {toSymbol}");
            return;
        }

        // Creating a new exchange rate entry
        Domain.Entities.ExchangeRate exchangeRate = new Domain.Entities.ExchangeRate
        {
            FromTokenId = fromToken.Id,
            ToTokenId = toToken.Id,
            Rate = rate,
            SourceUrl = "https://api.kucoin.com/api/v1/market/allTickers",
        };

        // Saving the new exchange rate to the database
        await context.ExchangeRates.AddAsync(exchangeRate, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }
}