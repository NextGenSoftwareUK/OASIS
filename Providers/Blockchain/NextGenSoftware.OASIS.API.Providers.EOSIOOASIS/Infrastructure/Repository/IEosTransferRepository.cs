using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Infrastructure.Repository
{
    public interface IEosTransferRepository
    {
        Task<OASISResult<ITransactionResponse>> TransferEosToken(string fromAccountName, string toAccountName, decimal amount);
        Task<OASISResult<ITransactionResponse>> TransferEosNft(string fromAccountName, string toAccountName, decimal amount, string nftSymbol);
    }
}