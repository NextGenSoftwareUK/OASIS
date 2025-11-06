
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallets.Requests;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Request
{
    public interface IWeb3NFTWalletTransactionRequest : IWalletTransactionRequest
    {
        public int TokenId { get; set; }
        public string TokenAddress { get; set; }
        public bool WaitTillNFTSent { get; set; }
        public int WaitForNFTToSendInSeconds { get; set; }
        public int AttemptToSendEveryXSeconds { get; set; }
    }
}