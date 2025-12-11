using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Core.Managers.Stablecoin.Services
{
    /// <summary>
    /// Service for locking/unlocking ZEC collateral on Zcash
    /// </summary>
    public interface IZcashCollateralService
    {
        Task<OASISResult<string>> LockCollateralAsync(string zcashAddress, decimal amount, CancellationToken cancellationToken = default);
        Task<OASISResult<string>> UnlockCollateralAsync(string zcashAddress, decimal amount, CancellationToken cancellationToken = default);
    }
}

