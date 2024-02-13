using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallets.Response;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Infrastructure.Repository
{
    public interface IEosTransferRepository
    {
        Task<OASISResult<ITransactionRespone>> TransferEosToken(string fromAccountName, string toAccountName, decimal amount);
        Task<OASISResult<ITransactionRespone>> TransferEosNft(string fromAccountName, string toAccountName, decimal amount, string nftSymbol);
    }
}