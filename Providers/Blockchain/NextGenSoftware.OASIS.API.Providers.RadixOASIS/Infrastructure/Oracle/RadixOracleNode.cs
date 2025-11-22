using NextGenSoftware.OASIS.API.Providers.RadixOASIS.Infrastructure.Entities;
using NextGenSoftware.OASIS.API.Providers.RadixOASIS.Infrastructure.Services.Radix;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.RadixOASIS.Infrastructure.Oracle;

/// <summary>
/// Radix First-Party Oracle Node - Standalone oracle node that Radix can run themselves.
/// Inspired by API3's Airnode - "API providers can run it themselves with no middleware."
/// 
/// This is a first-party oracle: Radix runs their own node, signs data with their own keys,
/// and provides oracle services directly without any third-party middleware.
/// </summary>
public class RadixOracleNode
{
    private readonly RadixChainObserver _chainObserver;
    private readonly IRadixService _radixService;
    private readonly RadixOASISConfig _config;
    private bool _isRunning;

    /// <summary>
    /// Gets whether the oracle node is currently running
    /// </summary>
    public bool IsRunning => _isRunning;

    /// <summary>
    /// Gets the chain observer for this oracle node
    /// </summary>
    public RadixChainObserver ChainObserver => _chainObserver;

    public RadixOracleNode(IRadixService radixService, RadixOASISConfig config)
    {
        _radixService = radixService ?? throw new ArgumentNullException(nameof(radixService));
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _chainObserver = new RadixChainObserver(radixService, config);
    }

    /// <summary>
    /// Starts the oracle node - begins monitoring and serving oracle data
    /// </summary>
    public async Task<OASISResult<bool>> StartAsync(CancellationToken token = default)
    {
        var result = new OASISResult<bool>();
        
        try
        {
            if (_isRunning)
            {
                result.Result = true;
                result.Message = "Oracle node is already running";
                return result;
            }

            // Start chain monitoring
            var monitorResult = await _chainObserver.StartMonitoringAsync(token);
            
            if (monitorResult.IsError)
            {
                result.IsError = true;
                result.Message = $"Failed to start monitoring: {monitorResult.Message}";
                return result;
            }

            _isRunning = true;
            result.Result = true;
            result.Message = "Radix Oracle Node started successfully - running as first-party oracle with no middleware";
            
            return result;
        }
        catch (Exception ex)
        {
            _isRunning = false;
            return OASISErrorHandling.HandleError<bool>(ref result,
                $"Error starting oracle node: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Stops the oracle node
    /// </summary>
    public async Task<OASISResult<bool>> StopAsync(CancellationToken token = default)
    {
        var result = new OASISResult<bool>();
        
        try
        {
            if (!_isRunning)
            {
                result.Result = true;
                return result;
            }

            // Stop chain monitoring
            var stopResult = await _chainObserver.StopMonitoringAsync(token);
            
            if (stopResult.IsError)
            {
                result.IsError = true;
                result.Message = $"Failed to stop monitoring: {stopResult.Message}";
                return result;
            }

            _isRunning = false;
            result.Result = true;
            result.Message = "Radix Oracle Node stopped";
            
            return result;
        }
        catch (Exception ex)
        {
            return OASISErrorHandling.HandleError<bool>(ref result,
                $"Error stopping oracle node: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Gets oracle data - chain state, prices, transactions, etc.
    /// This is the main API that other systems call to get oracle data from Radix.
    /// </summary>
    public async Task<OASISResult<OracleDataResponse>> GetOracleDataAsync(
        OracleDataRequest request,
        CancellationToken token = default)
    {
        var result = new OASISResult<OracleDataResponse>();
        
        try
        {
            if (!_isRunning)
            {
                result.IsError = true;
                result.Message = "Oracle node is not running. Call StartAsync() first.";
                return result;
            }

            var response = new OracleDataResponse
            {
                ChainName = "Radix",
                Timestamp = DateTime.UtcNow,
                SignedBy = _config.AccountAddress, // First-party: signed by Radix's own address
                Data = new Dictionary<string, object>()
            };

            // Handle different data types
            switch (request.DataType.ToLower())
            {
                case "chainstate":
                case "chain-state":
                    var chainState = await _chainObserver.GetChainStateAsync(token);
                    if (!chainState.IsError && chainState.Result != null)
                    {
                        response.Data["chainState"] = chainState.Result;
                    }
                    break;

                case "price":
                case "pricefeed":
                    var price = await _chainObserver.GetPriceFeedAsync(request.TokenSymbol ?? "XRD", request.Currency ?? "USD", token);
                    if (!price.IsError && price.Result != null)
                    {
                        response.Data["price"] = price.Result;
                    }
                    break;

                case "transaction":
                    if (!string.IsNullOrEmpty(request.TransactionHash))
                    {
                        var tx = await _chainObserver.GetTransactionAsync(request.TransactionHash, token);
                        if (!tx.IsError && tx.Result != null)
                        {
                            response.Data["transaction"] = tx.Result;
                        }
                    }
                    break;

                case "verification":
                    if (!string.IsNullOrEmpty(request.TransactionHash))
                    {
                        var verification = await _chainObserver.VerifyTransactionAsync(request.TransactionHash, token);
                        if (!verification.IsError && verification.Result != null)
                        {
                            response.Data["verification"] = verification.Result;
                        }
                    }
                    break;

                case "health":
                    var health = await _chainObserver.GetChainHealthAsync(token);
                    if (!health.IsError && health.Result != null)
                    {
                        response.Data["health"] = health.Result;
                    }
                    break;

                default:
                    result.IsError = true;
                    result.Message = $"Unknown data type: {request.DataType}";
                    return result;
            }

            // TODO: Sign the response with Radix's private key (first-party oracle signature)
            // response.Signature = SignData(response, _config.PrivateKey);

            result.Result = response;
            result.IsError = false;
            
            return result;
        }
        catch (Exception ex)
        {
            return OASISErrorHandling.HandleError<OracleDataResponse>(ref result,
                $"Error getting oracle data: {ex.Message}", ex);
        }
    }
}

/// <summary>
/// Request for oracle data
/// </summary>
public class OracleDataRequest
{
    public string DataType { get; set; } = string.Empty; // "chainstate", "price", "transaction", "verification", "health"
    public string? TokenSymbol { get; set; } // For price feeds
    public string? Currency { get; set; } = "USD"; // For price feeds
    public string? TransactionHash { get; set; } // For transaction/verification requests
}

/// <summary>
/// Response from oracle node
/// </summary>
public class OracleDataResponse
{
    public string ChainName { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string SignedBy { get; set; } = string.Empty; // First-party: signed by provider's own address
    public string? Signature { get; set; } // Cryptographic signature proving authenticity
    public Dictionary<string, object> Data { get; set; } = new();
}

