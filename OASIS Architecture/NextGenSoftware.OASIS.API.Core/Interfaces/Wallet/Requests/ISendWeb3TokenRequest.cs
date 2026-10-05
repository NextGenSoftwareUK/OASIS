using System;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests
{
    public interface ISendWeb3TokenRequest
    {
        public Guid Web3TokenId { get; set; }
        public string FromTokenAddress { get; set; }
        public string FromWalletAddress { get; set; }
        public string ToWalletAddress { get; set; }
        //public string FromToken { get; set; }
        //public string ToToken { get; set; }

        public decimal Amount { get; set; }
        public string MemoText { get; set; }
    }
}