using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Core.Managers.Bridge.Services;

/// <summary>
/// Service for fetching real-time exchange rates between cryptocurrencies
/// </summary>
public interface IExchangeRateService
{
    /// <summary>
    /// Gets the exchange rate from one token to another
    /// </summary>
    /// <param name="fromToken">Source token symbol (e.g., "SOL", "XRD")</param>
    /// <param name="toToken">Destination token symbol</param>
    /// <param name="token">Cancellation token</param>
    /// <returns>Exchange rate (how much of toToken you get for 1 fromToken)</returns>
    Task<OASISResult<decimal>> GetExchangeRateAsync(
        string fromToken, 
        string toToken, 
        CancellationToken token = default);
}

