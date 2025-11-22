using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Helpers;

namespace NextGenSoftware.OASIS.API.Providers.ZcashOASIS.Infrastructure.Services.Zcash
{
    public interface IZcashBridgeService
    {
        Task<string> LockZECForBridgeAsync(decimal amount, string destinationChain, string destinationAddress, string viewingKey = null);
    }
}

