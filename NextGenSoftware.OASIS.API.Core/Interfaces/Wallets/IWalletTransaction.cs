
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.API.Core.Interfaces
{
    public interface IWalletTransaction
    {
        public decimal Amount { get; set; }
        public string FromWalletAddress { get; set; }
        public string ToWalletAddress { get; set; }
        public string Token { get; set; }
        public string MemoText { get; set; }
        public ProviderType ProviderType { get; set; }
    }
}