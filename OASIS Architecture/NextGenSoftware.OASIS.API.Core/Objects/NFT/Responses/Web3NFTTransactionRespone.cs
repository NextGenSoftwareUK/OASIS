using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Response;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Responses;

namespace NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response
{
    public class Web3NFTTransactionRespone : TransactionRespone, IWeb3NFTTransactionRespone
    {
        public IWeb3NFT Web3NFT { get; set; }
        public string SendNFTTransactionResult { get; set; }
    }
}