using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.Wallets.Requests
{
    public interface IWalletTransactionRequest
    {
        public string FromWalletAddress { get; set; }
        public string ToWalletAddress { get; set; }
        public string FromToken { get; set; }
        public string ToToken { get; set; }
        public ProviderType FromProviderType { get; set; }
        public ProviderType ToProviderType { get; set; }
        public decimal Amount { get; set; }
        public string MemoText { get; set; }
    }
}