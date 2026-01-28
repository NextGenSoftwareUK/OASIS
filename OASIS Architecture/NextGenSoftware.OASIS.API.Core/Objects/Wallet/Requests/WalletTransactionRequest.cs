using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;

namespace NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;

/// <summary>
/// Wallet transaction request implementation
/// </summary>
public class WalletTransactionRequest : IWalletTransactionRequest
{
    public string FromWalletAddress { get; set; } = string.Empty;
    public string ToWalletAddress { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string MemoText { get; set; } = string.Empty;
}



