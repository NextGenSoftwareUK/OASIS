using Application.Contracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace API.Infrastructure.Workers.FundingRate;

/// <summary>
/// Background worker service that publishes funding rates to blockchain(s) on a scheduled interval
/// </summary>
public sealed class FundingRateOnChainPublisherWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly ILogger<FundingRateOnChainPublisherWorker> _logger;
    private readonly TimeSpan _publishInterval;

    public FundingRateOnChainPublisherWorker(
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        ILogger<FundingRateOnChainPublisherWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _configuration = configuration;
        _logger = logger;

        // Get publish interval from config (default: 1 hour)
        var intervalMinutes = _configuration.GetValue<int>("Blockchain:FundingRate:PublishIntervalMinutes", 60);
        _publishInterval = TimeSpan.FromMinutes(intervalMinutes);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Funding Rate On-Chain Publisher Worker started. Publish interval: {Interval}", _publishInterval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PublishFundingRatesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in funding rate on-chain publisher worker");
            }

            // Wait for the next publish interval
            await Task.Delay(_publishInterval, stoppingToken);
        }

        _logger.LogInformation("Funding Rate On-Chain Publisher Worker stopped");
    }

    private async Task PublishFundingRatesAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        
        var fundingRateService = scope.ServiceProvider.GetRequiredService<IFundingRateService>();
        var publisherFactory = scope.ServiceProvider.GetRequiredService<IOnChainFundingPublisherFactory>();
        
        // Get list of symbols to publish (from config or database)
        var symbolsToPublish = GetSymbolsToPublish();

        _logger.LogInformation("Publishing funding rates for {Count} symbols", symbolsToPublish.Count);

        var publishToAllChains = _configuration.GetValue<bool>("Blockchain:FundingRate:PublishToAllChains", false);

        foreach (var symbol in symbolsToPublish)
        {
            try
            {
                // Get current funding rate
                var rateResult = await fundingRateService.GetCurrentFundingRateAsync(symbol, cancellationToken);
                if (rateResult.IsError || rateResult.Result == null)
                {
                    _logger.LogWarning("Failed to get funding rate for {Symbol}: {Error}", 
                        symbol, rateResult.Message);
                    continue;
                }

                var rate = rateResult.Result;

                if (publishToAllChains)
                {
                    // Publish to all configured blockchains
                    var publishers = publisherFactory.GetAllPublishers();
                    foreach (var publisher in publishers)
                    {
                        try
                        {
                            var result = await publisher.PublishFundingRateAsync(symbol, rate, cancellationToken);
                            if (result.Success)
                            {
                                _logger.LogInformation(
                                    "Published funding rate for {Symbol} to {Provider}: {TransactionHash}",
                                    symbol, publisher.ProviderType, result.TransactionHash);
                            }
                            else
                            {
                                _logger.LogWarning(
                                    "Failed to publish funding rate for {Symbol} to {Provider}: {Error}",
                                    symbol, publisher.ProviderType, result.ErrorMessage);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, 
                                "Exception publishing {Symbol} to {Provider}",
                                symbol, publisher.ProviderType);
                        }
                    }
                }
                else
                {
                    // Publish to primary blockchain only
                    var publisher = publisherFactory.GetPrimaryPublisher();
                    var result = await publisher.PublishFundingRateAsync(symbol, rate, cancellationToken);
                    
                    if (result.Success)
                    {
                        _logger.LogInformation(
                            "Published funding rate for {Symbol} to {Provider}: {TransactionHash}",
                            symbol, publisher.ProviderType, result.TransactionHash);
                        
                        // Update database with transaction hash
                        await UpdateFundingRateWithTransactionHash(symbol, result, scope.ServiceProvider, cancellationToken);
                    }
                    else
                    {
                        _logger.LogWarning(
                            "Failed to publish funding rate for {Symbol}: {Error}",
                            symbol, result.ErrorMessage);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception publishing funding rate for {Symbol}", symbol);
            }
        }
    }

    private List<string> GetSymbolsToPublish()
    {
        // Get from configuration, or could query database for tracked symbols
        var symbols = _configuration
            .GetSection("Blockchain:FundingRate:TrackedSymbols")
            .Get<string[]>() ?? new[] { "AAPL", "MSFT", "GOOGL" };

        return symbols.ToList();
    }

    private async Task UpdateFundingRateWithTransactionHash(
        string symbol,
        OnChainPublishResult result,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
    {
        try
        {
            // TODO: Update FundingRate entity with OnChainTransactionHash
            // This would require injecting DataContext or FundingRateService
            // For now, this is a placeholder
            _logger.LogDebug("Transaction hash {Hash} for {Symbol} should be saved to database", 
                result.TransactionHash, symbol);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update funding rate with transaction hash for {Symbol}", symbol);
        }
    }
}

