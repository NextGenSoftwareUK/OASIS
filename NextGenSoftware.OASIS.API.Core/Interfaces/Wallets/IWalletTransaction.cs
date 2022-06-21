
using NextGenSoftware.OASIS.API.Core.Enums;
using System;

namespace NextGenSoftware.OASIS.API.Core.Interfaces
{
    public interface IWalletTransaction
    {
        public decimal Amount { get; set; }
        public string FromWalletAddress { get; set; }
        public string ToWalletAddress { get; set; }
        public string Token { get; set; }
        public DateTime Date { get; set; }
        public ProviderType ProviderType { get; set; }
    }
}