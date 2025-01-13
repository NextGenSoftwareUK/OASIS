using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using NextGenSoftware.OASIS.API.Bridging;
using Solnet.Rpc;

namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS;

public sealed class SolanaBridgeOptions
{
    /// <summary>
    /// OASIS technical account (Pool Account) public key.
    /// </summary>
    public required string PublicKey { get; init; }

    /// <summary>
    /// OASIS technical account (Pool Account) private key.
    /// </summary>
    public required string PrivateKey { get; init; }

    /// <summary>
    /// Solana RPC API Host Uri.
    /// </summary>
    public required string HostUri { get; init; }
}

public sealed class SolanaBridge(
    IOptions<SolanaBridgeOptions> options
) : IOASISBridge
{
    private readonly IRpcClient _rpcClient = ClientFactory.GetClient(options.Value.HostUri);

    public Task<(string PublicKey, string PrivateKey)> CreateAccountAsync(CancellationToken token = default)
    {
        throw new System.NotImplementedException();
    }

    public Task<string> DepositAsync(decimal amount, string accountAddress)
    {
        throw new System.NotImplementedException();
    }

    public async Task<decimal> GetAccountBalanceAsync(string accountAddress, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();

        var result = await _rpcClient.GetBalanceAsync(accountAddress);

        return 0;
    }

    public Task<BridgeTransactionStatus> GetTransactionStatus(string transactionHash, CancellationToken token = default)
    {
        throw new System.NotImplementedException();
    }

    public Task<string> WithdrawAsync(decimal amount, string accountAddress)
    {
        throw new System.NotImplementedException();
    }
}