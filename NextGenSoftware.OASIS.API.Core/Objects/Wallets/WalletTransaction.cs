
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using System;

namespace NextGenSoftware.OASIS.API.Core.Objects
{
    public class WalletTransaction : IWalletTransaction
    {
        public decimal Amount { get; set; }
        public string FromWalletAddress { get; set; }
        public string ToWalletAddress { get; set; }
        public string Token { get; set; }
        public DateTime Date { get; set; }
        public ProviderType ProviderType { get; set; }
    }
}