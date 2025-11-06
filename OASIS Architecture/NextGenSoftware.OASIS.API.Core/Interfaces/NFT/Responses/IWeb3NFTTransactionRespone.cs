using NextGenSoftware.OASIS.API.Core.Interfaces.Wallets.Response;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Response
{
    public interface IWeb3NFTTransactionRespone : ITransactionRespone
    {
        IWeb3NFT Web3NFT { get; set; }
    }
}