using NextGenSoftware.OASIS.API.Providers.RadixOASIS.Infrastructure.Entities;
using NextGenSoftware.OASIS.API.Providers.RadixOASIS.Infrastructure.Entities.DTOs.Oracle;
using NextGenSoftware.OASIS.API.Providers.RadixOASIS.Infrastructure.Interfaces;
using NextGenSoftware.OASIS.API.Providers.RadixOASIS.Infrastructure.Services.Radix;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.RadixOASIS.Infrastructure.Oracle;

/// <summary>
/// Radix chain observer for oracle operations.
/// Implements first-party oracle pattern - Radix can run their own oracle node with no middleware.
/// Inspired by API3's Airnode approach.
/// </summary>
public class RadixChainObserver : IChainObserver
{
    private readonly IRadixService _radixService;
    private readonly RadixOASISConfig _config;
    private bool _isMonitoring;
    private CancellationTokenSource? _monitoringCancellationTokenSource;

    public string ChainName => "Radix";
    public bool IsMonitoring => _isMonitoring;

    public event EventHandler<ChainEventData>? OnChainEvent;
    public event EventHandler<OASISErrorEventArgs>? OnError;

    public RadixChainObserver(IRadixService radixService, RadixOASISConfig config)
    {
        _radixService = radixService ?? throw new ArgumentNullException(nameof(radixService));
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    /// <summary>
    /// Gets the current chain state
    /// </summary>
    public async Task<OASISResult<ChainStateData>> GetChainStateAsync(CancellationToken token = default)
    {
        var result = new OASISResult<ChainStateData>();
        
        try
        {
            var chainStateResult = await _radixService.GetChainStateAsync(token);
            
            if (chainStateResult.IsError || chainStateResult.Result == null)
            {
                result.IsError = true;
                result.Message = chainStateResult.Message ?? "Failed to get chain state";
                return result;
            }

            var radixState = chainStateResult.Result;
            
            result.Result = new ChainStateData
            {
                ChainName = ChainName,
                BlockHeight = radixState.CurrentEpoch, // Radix uses epochs instead of block heights
                Epoch = radixState.CurrentEpoch,
                LastBlockTime = radixState.LastBlockTime,
                IsHealthy = radixState.IsHealthy,
                NetworkId = radixState.NetworkId
            };
            
            result.IsError = false;
            return result;
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError<ChainStateData>(ref result,
                $"Error getting chain state: {ex.Message}", ex);
            return result;
        }
    }

    /// <summary>
    /// Gets the latest block (epoch) information
    /// </summary>
    public async Task<OASISResult<BlockData>> GetLatestBlockAsync(CancellationToken token = default)
    {
        var result = new OASISResult<BlockData>();
        
        try
        {
            var epochResult = await _radixService.GetLatestEpochAsync(token);
            
            if (epochResult.IsError)
            {
                result.IsError = true;
                result.Message = epochResult.Message ?? "Failed to get latest epoch";
                return result;
            }

            result.Result = new BlockData
            {
                BlockHeight = epochResult.Result,
                Epoch = epochResult.Result,
                Timestamp = DateTime.UtcNow,
                TransactionCount = 0 // Would need to query transactions for this epoch
            };
            
            result.IsError = false;
            return result;
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError<BlockData>(ref result,
                $"Error getting latest block: {ex.Message}", ex);
            return result;
        }
    }

    /// <summary>
    /// Gets transaction details by hash
    /// </summary>
    public async Task<OASISResult<TransactionData>> GetTransactionAsync(string transactionHash, CancellationToken token = default)
    {
        var result = new OASISResult<TransactionData>();
        
        try
        {
            var txDetailsResult = await _radixService.GetTransactionDetailsAsync(transactionHash, token);
            
            if (txDetailsResult.IsError || txDetailsResult.Result == null)
            {
                result.IsError = true;
                result.Message = txDetailsResult.Message ?? "Failed to get transaction";
                return result;
            }

            var radixTx = txDetailsResult.Result;
            
            result.Result = new TransactionData
            {
                TransactionHash = radixTx.TransactionHash,
                FromAddress = radixTx.FromAddress,
                ToAddress = radixTx.ToAddress,
                Amount = radixTx.Amount,
                TokenSymbol = radixTx.TokenSymbol,
                Timestamp = radixTx.Timestamp,
                Status = radixTx.Status,
                BlockHeight = radixTx.Epoch,
                Epoch = radixTx.Epoch
            };
            
            result.IsError = false;
            return result;
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError<TransactionData>(ref result, $"Error getting transaction: {ex.Message}", ex);
            return result;
        }
    }

    /// <summary>
    /// Verifies a transaction's validity
    /// </summary>
    public async Task<OASISResult<TransactionVerification>> VerifyTransactionAsync(string transactionHash, CancellationToken token = default)
    {
        var result = new OASISResult<TransactionVerification>();
        
        try
        {
            var verifyResult = await _radixService.VerifyTransactionAsync(transactionHash, token);
            
            if (verifyResult.IsError)
            {
                result.IsError = true;
                result.Message = verifyResult.Message ?? "Failed to verify transaction";
                return result;
            }

            var txResult = await _radixService.GetTransactionDetailsAsync(transactionHash, token);
            
            result.Result = new TransactionVerification
            {
                TransactionHash = transactionHash,
                IsValid = verifyResult.Result,
                IsConfirmed = verifyResult.Result && txResult.Result?.Status == "Completed",
                Confirmations = verifyResult.Result ? 1 : 0, // Radix finality is instant
                Status = txResult.Result?.Status ?? "Unknown",
                VerifiedAt = DateTime.UtcNow,
                Confidence = verifyResult.Result ? 100 : 0
            };
            
            result.IsError = false;
            return result;
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError<TransactionVerification>(ref result,
                $"Error verifying transaction: {ex.Message}", ex);
            return result;
        }
    }

    /// <summary>
    /// Gets price data for a token symbol
    /// </summary>
    public async Task<OASISResult<PriceData>> GetPriceAsync(string symbol, CancellationToken token = default)
    {
        // Call GetPriceFeedAsync with default currency
        return await GetPriceFeedAsync(symbol, "USD", token);
    }

    /// <summary>
    /// Gets price feed data for XRD
    /// </summary>
    public async Task<OASISResult<PriceData>> GetPriceFeedAsync(string tokenSymbol, string currency = "USD", CancellationToken token = default)
    {
        var result = new OASISResult<PriceData>();
        
        try
        {
            if (tokenSymbol.ToUpper() != "XRD")
            {
                result.IsError = true;
                result.Message = $"Price feed for {tokenSymbol} not supported. Only XRD is supported.";
                return result;
            }

            var priceResult = await _radixService.GetXrdPriceAsync(currency, token);
            
            if (priceResult.IsError || priceResult.Result == null)
            {
                result.IsError = true;
                result.Message = priceResult.Message ?? "Failed to get price feed";
                return result;
            }

            var radixPrice = priceResult.Result;
            
            result.Result = new PriceData
            {
                TokenSymbol = radixPrice.TokenSymbol,
                Currency = radixPrice.Currency,
                Price = radixPrice.Price,
                Timestamp = radixPrice.Timestamp,
                Change24h = radixPrice.Change24h,
                Volume24h = radixPrice.Volume24h,
                Source = radixPrice.Source
            };
            
            result.IsError = false;
            return result;
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError<PriceData>(ref result,
                $"Error getting price feed: {ex.Message}", ex);
            return result;
        }
    }

    /// <summary>
    /// Starts monitoring the chain for events
    /// </summary>
    public async Task<OASISResult<bool>> StartMonitoringAsync(CancellationToken token = default)
    {
        var result = new OASISResult<bool>();
        
        try
        {
            if (_isMonitoring)
            {
                result.Result = true;
                result.Message = "Already monitoring";
                return result;
            }

            _monitoringCancellationTokenSource = new CancellationTokenSource();
            _isMonitoring = true;

            // Start background monitoring task
            _ = Task.Run(async () => await MonitorChainAsync(_monitoringCancellationTokenSource.Token), token);

            result.Result = true;
            result.IsError = false;
            return result;
        }
        catch (Exception ex)
        {
            _isMonitoring = false;
            OASISErrorHandling.HandleError<bool>(ref result,
                $"Error starting monitoring: {ex.Message}", ex);
            return result;
        }
    }

    /// <summary>
    /// Stops monitoring the chain
    /// </summary>
    public async Task<OASISResult<bool>> StopMonitoringAsync(CancellationToken token = default)
    {
        var result = new OASISResult<bool>();
        
        try
        {
            if (!_isMonitoring)
            {
                result.Result = true;
                return result;
            }

            _monitoringCancellationTokenSource?.Cancel();
            _monitoringCancellationTokenSource?.Dispose();
            _monitoringCancellationTokenSource = null;
            _isMonitoring = false;

            result.Result = true;
            result.IsError = false;
            return result;
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError<bool>(ref result,
                $"Error stopping monitoring: {ex.Message}", ex);
            return result;
        }
    }

    /// <summary>
    /// Gets chain health metrics
    /// </summary>
    public async Task<OASISResult<ChainHealthData>> GetChainHealthAsync(CancellationToken token = default)
    {
        var result = new OASISResult<ChainHealthData>();
        
        try
        {
            var chainStateResult = await GetChainStateAsync(token);
            
            if (chainStateResult.IsError || chainStateResult.Result == null)
            {
                result.IsError = true;
                result.Message = "Failed to get chain health";
                return result;
            }

            result.Result = new ChainHealthData
            {
                ChainName = ChainName,
                IsHealthy = chainStateResult.Result.IsHealthy,
                Uptime = chainStateResult.Result.IsHealthy ? 100 : 0,
                AverageResponseTime = TimeSpan.FromMilliseconds(100), // Would track actual response times
                ErrorCount = 0, // Would track errors
                LastHealthCheck = DateTime.UtcNow
            };
            
            result.IsError = false;
            return result;
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError<ChainHealthData>(ref result,
                $"Error getting chain health: {ex.Message}", ex);
            return result;
        }
    }

    /// <summary>
    /// Background task to monitor chain events
    /// </summary>
    private async Task MonitorChainAsync(CancellationToken cancellationToken)
    {
        ulong lastEpoch = 0;
        
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var epochResult = await _radixService.GetLatestEpochAsync(cancellationToken);
                
                if (!epochResult.IsError && epochResult.Result > lastEpoch)
                {
                    lastEpoch = epochResult.Result;
                    
                    // Fire event for new epoch (block)
                    OnChainEvent?.Invoke(this, new ChainEventData
                    {
                        EventType = "NewEpoch",
                        ChainName = ChainName,
                        Timestamp = DateTime.UtcNow,
                        EventData = new { Epoch = lastEpoch }
                    });
                }

                // Poll every 5 seconds
                await Task.Delay(5000, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                OnError?.Invoke(this, new OASISErrorEventArgs
                {
                    Reason = "Error in chain monitoring",
                    Exception = ex
                });
                
                // Wait before retrying
                await Task.Delay(5000, cancellationToken);
            }
        }
    }
}


