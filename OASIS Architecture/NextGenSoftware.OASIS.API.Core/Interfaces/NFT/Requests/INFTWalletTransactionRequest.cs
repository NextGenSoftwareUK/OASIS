using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallets.Requests;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Request
{
    public interface INFTWalletTransactionRequest : IWalletTransactionRequest
    {
        //public string MintWalletAddress { get; set; }
        public int TokenId { get; set; }
        public string TokenAddress { get; set; }
        //public string FromWalletAddress { get; set; }
        //public string ToWalletAddress { get; set; }
        ////public string FromToken { get; set; }
        ////public string ToToken { get; set; }
        ////public ProviderType FromProviderType { get; set; }
        ////public ProviderType ToProviderType { get; set; }
        ////public decimal Amount { get; set; }
        //public string MemoText { get; set; }

        public bool WaitTillNFTSent { get; set; }
        public int WaitForNFTToSendInSeconds { get; set; }
        public int AttemptToSendEveryXSeconds { get; set; }
    }
}