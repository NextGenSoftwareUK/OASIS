using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Core.Managers.Stablecoin.Services
{
    /// <summary>
    /// Service for minting/burning stablecoin (zUSD) on Aztec Network
    /// </summary>
    public interface IAztecStablecoinService
    {
        Task<OASISResult<string>> MintStablecoinAsync(string aztecAddress, decimal amount, CancellationToken cancellationToken = default);
        Task<OASISResult<string>> BurnStablecoinAsync(string aztecAddress, decimal amount, CancellationToken cancellationToken = default);
    }
}

