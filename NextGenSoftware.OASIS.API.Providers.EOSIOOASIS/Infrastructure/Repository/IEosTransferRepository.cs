using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Helpers;

namespace NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Infrastructure.Repository
{
    public interface IEosTransferRepository
    {
        Task<OASISResult<string>> TransferEosToken(string fromAccountName, string toAccountName, decimal amount);
        Task<OASISResult<bool>> TransferEosNft(string fromAccountName, string toAccountName, decimal amount, string nftSymbol);
    }
}