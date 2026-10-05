namespace Application.Contracts;

/// <summary>
/// Factory for creating blockchain-specific funding rate publishers
/// </summary>
public interface IOnChainFundingPublisherFactory
{
    /// <summary>
    /// Get publisher for a specific blockchain provider
    /// </summary>
    IOnChainFundingPublisher GetPublisher(BlockchainProviderType providerType);
    
    /// <summary>
    /// Get publisher from configuration (primary provider)
    /// </summary>
    IOnChainFundingPublisher GetPrimaryPublisher();
    
    /// <summary>
    /// Get all configured publishers (for multi-chain publishing)
    /// </summary>
    IEnumerable<IOnChainFundingPublisher> GetAllPublishers();
    
    /// <summary>
    /// Check if a provider is configured and available
    /// </summary>
    bool IsProviderAvailable(BlockchainProviderType providerType);
}

