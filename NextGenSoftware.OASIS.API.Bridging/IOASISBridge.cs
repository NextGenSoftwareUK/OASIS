using System.Threading;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Bridging;

public enum BridgeTransactionStatus
{
    Succeed,
    Failed,
    InProgress
}

public interface IOASISBridge
{
    Task<decimal> GetAccountBalanceAsync(string accountAddress, CancellationToken token = default);
    Task<(string PublicKey, string PrivateKey)> CreateAccountAsync(CancellationToken token = default);
    Task<string> WithdrawAsync(decimal amount, string accountAddress); // Debit
    Task<string> DepositAsync(decimal amount, string accountAddress); // Credit
    Task<BridgeTransactionStatus> GetTransactionStatus(string transactionHash, CancellationToken token = default);
}
