using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Request;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Requests;

namespace NextGenSoftware.OASIS.API.Core.Objects.NFT.Request
{
    public class Web3NFTWalletTransactionRequest : WalletTransactionRequest, IWeb3NFTWalletTransactionRequest
    {
        public int TokenId { get; set; }
        public string TokenAddress { get; set; }
        public bool WaitTillNFTSent { get; set; } = true;
        public int WaitForNFTToSendInSeconds { get; set; } = 60;
        public int AttemptToSendEveryXSeconds { get; set; } = 5;
    }
}