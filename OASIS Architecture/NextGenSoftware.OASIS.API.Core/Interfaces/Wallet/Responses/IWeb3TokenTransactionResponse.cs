
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Response
{
    public interface IWeb3TokenTransactionResponse : ITransactionResponse
    {
        IWeb3Token Web3Token { get; set; }
    }
}