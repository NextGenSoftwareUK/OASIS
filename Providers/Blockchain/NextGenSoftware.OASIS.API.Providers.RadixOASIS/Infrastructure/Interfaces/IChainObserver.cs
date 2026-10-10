using NextGenSoftware.OASIS.API.Providers.RadixOASIS.Infrastructure.Entities.DTOs.Oracle;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.RadixOASIS.Infrastructure.Interfaces;

public interface IChainObserver
{
    string ChainName { get; }
    bool IsMonitoring { get; }
    event EventHandler<ChainEventData>? OnChainEvent;
    event EventHandler<OASISErrorEventArgs>? OnError;
    Task<OASISResult<ChainStateData>> GetChainStateAsync(CancellationToken token = default);
    Task<OASISResult<BlockData>> GetLatestBlockAsync(CancellationToken token = default);
    Task<OASISResult<TransactionData>> GetTransactionAsync(string transactionHash, CancellationToken token = default);
    Task<OASISResult<TransactionVerification>> VerifyTransactionAsync(string transactionHash, CancellationToken token = default);
    Task<OASISResult<PriceData>> GetPriceAsync(string symbol, CancellationToken token = default);
    Task<OASISResult<PriceData>> GetPriceFeedAsync(string tokenSymbol, string currency = "USD", CancellationToken token = default);
    Task<OASISResult<ChainHealthData>> GetChainHealthAsync(CancellationToken token = default);
    Task<OASISResult<bool>> StartMonitoringAsync(CancellationToken token = default);
    Task<OASISResult<bool>> StopMonitoringAsync(CancellationToken token = default);
}

