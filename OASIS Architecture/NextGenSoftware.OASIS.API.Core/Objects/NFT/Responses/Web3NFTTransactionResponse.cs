using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;

namespace NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response
{
    public class Web3NFTTransactionResponse : TransactionResponse, IWeb3NFTTransactionResponse
    {
        public IWeb3NFT Web3NFT { get; set; }
        public string SendNFTTransactionResult { get; set; }
    }
}