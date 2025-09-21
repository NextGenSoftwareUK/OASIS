using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Request;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Requests;

namespace NextGenSoftware.OASIS.API.Core.Objects.NFT.Request
{
    public class NFTWalletTransactionRequest : WalletTransactionRequest, INFTWalletTransactionRequest
    {
        //public string MintWalletAddress { get; set; }
        public int TokenId { get; set; }
        public string TokenAddress { get; set; }
        //public string FromWalletAddress { get; set; }
        //public string ToWalletAddress { get; set; }
        ////public string FromToken { get; set; }
        ////public string ToToken { get; set; }
        //// ProviderType FromProviderType { get; set; }
        ////public ProviderType ToProviderType { get; set; }
        ////public decimal Amount { get; set; }
        //public string MemoText { get; set; }

        public bool WaitTillNFTSent { get; set; } = true;
        public int WaitForNFTToSendInSeconds { get; set; } = 60;
        public int AttemptToSendEveryXSeconds { get; set; } = 5;
    }
}