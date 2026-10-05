using Application.DTOs.FundingRate;

namespace Application.Contracts;

/// <summary>
/// Blockchain-agnostic interface for publishing funding rates on-chain
/// </summary>
public interface IOnChainFundingPublisher
{
    /// <summary>
    /// Blockchain provider type (Solana, Ethereum, Arbitrum, etc.)
    /// </summary>
    BlockchainProviderType ProviderType { get; }
    
    /// <summary>
    /// Publish a single funding rate to the blockchain
    /// </summary>
    Task<OnChainPublishResult> PublishFundingRateAsync(
        string symbol, 
        FundingRateResponse rate, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Publish multiple funding rates in a batch
    /// </summary>
    Task<Dictionary<string, OnChainPublishResult>> PublishBatchFundingRatesAsync(
        Dictionary<string, FundingRateResponse> rates, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Read funding rate from the blockchain
    /// </summary>
    Task<OnChainFundingRate?> GetOnChainFundingRateAsync(
        string symbol, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Read multiple funding rates from the blockchain
    /// </summary>
    Task<Dictionary<string, OnChainFundingRate>> GetOnChainFundingRatesAsync(
        List<string> symbols, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Initialize on-chain storage for a symbol (e.g., create PDA, deploy contract)
    /// </summary>
    Task<bool> InitializeFundingRateAccountAsync(
        string symbol, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Check if funding rate account is initialized
    /// </summary>
    Task<bool> IsFundingRateAccountInitializedAsync(
        string symbol, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get the account/contract address for a symbol
    /// </summary>
    Task<string?> GetFundingRateAccountAddressAsync(
        string symbol, 
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Supported blockchain providers for funding rate publishing
/// </summary>
public enum BlockchainProviderType
{
    Solana,
    Ethereum,
    Arbitrum,
    Polygon,
    Avalanche,
    Base,
    Optimism
}

/// <summary>
/// Result of publishing a funding rate on-chain
/// </summary>
public class OnChainPublishResult
{
    public bool Success { get; set; }
    public string? TransactionHash { get; set; }
    public string? AccountAddress { get; set; }
    public DateTime PublishedAt { get; set; }
    public string? ErrorMessage { get; set; }
    public int Confirmations { get; set; }
    public BlockchainProviderType ProviderType { get; set; }
}

/// <summary>
/// On-chain funding rate data structure
/// </summary>
public class OnChainFundingRate
{
    public string Symbol { get; set; } = string.Empty;
    public BlockchainProviderType ProviderType { get; set; }
    public decimal Rate { get; set; }
    public decimal HourlyRate { get; set; }
    public decimal MarkPrice { get; set; }
    public decimal SpotPrice { get; set; }
    public decimal Premium { get; set; }
    public DateTime LastUpdated { get; set; }
    public DateTime ValidUntil { get; set; }
    public string TransactionHash { get; set; } = string.Empty;
    public string AccountAddress { get; set; } = string.Empty;
    public int Confirmations { get; set; }
}

