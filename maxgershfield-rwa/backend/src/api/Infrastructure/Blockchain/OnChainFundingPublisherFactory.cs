using Application.Contracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Blockchain;

/// <summary>
/// Factory for creating blockchain-specific funding rate publishers
/// Uses lazy initialization to avoid circular dependency issues
/// </summary>
public class OnChainFundingPublisherFactory : IOnChainFundingPublisherFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly ILogger<OnChainFundingPublisherFactory> _logger;

    private readonly Dictionary<BlockchainProviderType, Lazy<IOnChainFundingPublisher>> _publishers;

    public OnChainFundingPublisherFactory(
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        ILogger<OnChainFundingPublisherFactory> logger)
    {
        _serviceProvider = serviceProvider;
        _configuration = configuration;
        _logger = logger;
        _publishers = new Dictionary<BlockchainProviderType, Lazy<IOnChainFundingPublisher>>();
        InitializePublishers();
    }

    private void InitializePublishers()
    {
        var enabledProviders = _configuration
            .GetSection("Blockchain:FundingRate:EnabledProviders")
            .Get<string[]>() ?? new[] { "Solana" };

        foreach (var providerName in enabledProviders)
        {
            if (Enum.TryParse<BlockchainProviderType>(providerName, true, out var providerType))
            {
                _publishers[providerType] = new Lazy<IOnChainFundingPublisher>(() =>
                {
                    try
                    {
                        var publisher = CreatePublisher(providerType);
                        _logger.LogInformation("Initialized {ProviderType} funding rate publisher", providerType);
                        return publisher;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to initialize {ProviderType} publisher", providerType);
                        throw;
                    }
                });
            }
        }
    }

    private IOnChainFundingPublisher CreatePublisher(BlockchainProviderType providerType)
    {
        return providerType switch
        {
            BlockchainProviderType.Solana => _serviceProvider.GetRequiredService<Solana.SolanaOnChainFundingPublisher>(),
            // BlockchainProviderType.Ethereum => _serviceProvider.GetRequiredService<Ethereum.EthereumOnChainFundingPublisher>(),
            // BlockchainProviderType.Arbitrum => _serviceProvider.GetRequiredService<Arbitrum.ArbitrumOnChainFundingPublisher>(),
            // BlockchainProviderType.Polygon => _serviceProvider.GetRequiredService<Polygon.PolygonOnChainFundingPublisher>(),
            _ => throw new NotSupportedException($"Provider {providerType} is not supported")
        };
    }

    public IOnChainFundingPublisher GetPublisher(BlockchainProviderType providerType)
    {
        if (_publishers.TryGetValue(providerType, out var lazyPublisher))
            return lazyPublisher.Value;

        throw new InvalidOperationException(
            $"Publisher for {providerType} is not configured or available");
    }

    public IOnChainFundingPublisher GetPrimaryPublisher()
    {
        var primaryProvider = _configuration
            .GetValue<string>("Blockchain:FundingRate:PrimaryProvider") ?? "Solana";

        if (Enum.TryParse<BlockchainProviderType>(primaryProvider, true, out var providerType))
            return GetPublisher(providerType);

        // Fallback to first available
        return _publishers.Values.FirstOrDefault()?.Value
            ?? throw new InvalidOperationException("No funding rate publishers are configured");
    }

    public IEnumerable<IOnChainFundingPublisher> GetAllPublishers()
    {
        return _publishers.Values.Select(lazy => lazy.Value);
    }

    public bool IsProviderAvailable(BlockchainProviderType providerType)
    {
        return _publishers.ContainsKey(providerType);
    }
}

