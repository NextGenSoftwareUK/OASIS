using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Core.Managers.Stablecoin.Services
{
    /// <summary>
    /// Oracle service for fetching ZEC price in USD
    /// </summary>
    public interface IZecPriceOracle
    {
        Task<OASISResult<decimal>> GetZecPriceAsync(CancellationToken cancellationToken = default);
    }
}

