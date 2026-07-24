using NextGenSoftware.OASIS.API.Providers.RadixOASIS.Infrastructure.Entities.Enums;

namespace NextGenSoftware.OASIS.API.Providers.RadixOASIS.Infrastructure.Services.Radix;

/// <summary>
/// Interface for Radix blockchain service operations
/// </summary>
public interface IRadixService : IOASISBridge
{
    /// <summary>
    /// Gets the Radix address from a public key
    /// </summary>
    OASISResult<string> GetAddress(PublicKey publicKey, RadixAddressType addressType, 
        RadixNetworkType networkType, CancellationToken token = default);

    /// <summary>
    /// Gets the current chain state
    /// </summary>
    Task<OASISResult<Infrastructure.Entities.DTOs.Oracle.RadixChainState>> GetChainStateAsync(CancellationToken token = default);

    /// <summary>
    /// Gets the latest epoch
    /// </summary>
    Task<OASISResult<ulong>> GetLatestEpochAsync(CancellationToken token = default);

    /// <summary>
    /// Gets transaction details
    /// </summary>
    Task<OASISResult<Infrastructure.Entities.DTOs.Oracle.RadixTransactionDetails>> GetTransactionDetailsAsync(string transactionHash, CancellationToken token = default);

    /// <summary>
    /// Verifies a transaction
    /// </summary>
    Task<OASISResult<bool>> VerifyTransactionAsync(string transactionHash, CancellationToken token = default);

    /// <summary>
    /// Gets XRD price
    /// </summary>
    Task<OASISResult<Infrastructure.Entities.DTOs.Oracle.RadixPriceFeed>> GetXrdPriceAsync(string currency = "USD", CancellationToken token = default);
}

