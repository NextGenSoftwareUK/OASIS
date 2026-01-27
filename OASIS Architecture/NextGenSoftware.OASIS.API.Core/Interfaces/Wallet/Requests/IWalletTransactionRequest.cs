namespace NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;

/// <summary>
/// Interface for wallet transaction requests
/// </summary>
public interface IWalletTransactionRequest
{
    string FromWalletAddress { get; set; }
    string ToWalletAddress { get; set; }
    decimal Amount { get; set; }
    string MemoText { get; set; }
}

