using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses
{
    public interface IWeb3NFTTransactionResponse : ITransactionResponse
    {
        IWeb3NFT Web3NFT { get; set; }
    }
}