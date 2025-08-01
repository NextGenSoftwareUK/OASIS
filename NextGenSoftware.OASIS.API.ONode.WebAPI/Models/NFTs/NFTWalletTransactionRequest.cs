using NextGenSoftware.OASIS.API.Core.Enums;
using System;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.NFT
{
    public class NFTWalletTransactionRequest
    {
        public string MintWalletAddress { get; set; }
        public string FromWalletAddress { get; set; }
        public string ToWalletAddress { get; set; }
        //public string FromToken { get; set; }
        //public string ToToken { get; set; }
        public string FromProviderType { get; set; }
        public string ToProviderType { get; set; }
        public decimal Amount { get; set; }
        public string MemoText { get; set; }
    }
}