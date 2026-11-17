using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.Common;
using Solnet.Rpc.Types;
using Solnet.Wallet;

namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS;

public partial class SolanaOASIS
{
    public async Task<OASISResult<object>> ReadRecentWalletActivityAsync(string walletAddress, CancellationToken cancellationToken)
    {
        const int pageSize = 50;
        var result = new OASISResult<object>();
        try
        {
            var address = new PublicKey(walletAddress);
            if (address.KeyBytes.Length != 32)
                throw new ArgumentException("Invalid Solana wallet address.");

            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            deadline.CancelAfter(TimeSpan.FromSeconds(10));
            var history = await _rpcClient.GetSignaturesForAddressAsync(address.Key, pageSize + 1, null, null, Commitment.Finalized)
                .WaitAsync(deadline.Token);
            if (!history.WasSuccessful || history.Result == null)
                throw new InvalidOperationException("Solana RPC did not return finalized wallet activity.");

            var signatures = new HashSet<string>(StringComparer.Ordinal);
            var recent = history.Result.Take(pageSize).Select(entry =>
            {
                if (entry == null || string.IsNullOrEmpty(entry.Signature) || !signatures.Add(entry.Signature))
                    throw new InvalidOperationException("Solana RPC returned an invalid transaction signature.");

                return new
                {
                    signature = entry.Signature,
                    slot = entry.Slot,
                    finalizedAt = entry.BlockTime.HasValue
                        ? (DateTimeOffset?)DateTimeOffset.FromUnixTimeSeconds(checked((long)entry.BlockTime.Value)) : null,
                    succeeded = entry.Error == null
                };
            }).ToArray();

            result.Result = new
            {
                recentActivity = recent,
                recentTransactionCount = recent.Length,
                hasMore = history.Result.Count > pageSize,
                lastActivity = recent.FirstOrDefault()?.finalizedAt
            };
            result.IsLoaded = true;
            result.LoadedCount = recent.Length;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            OASISErrorHandling.HandleError(ref result, "The Solana wallet activity read timed out.");
        }
        catch (Exception error) when (error is not OperationCanceledException)
        {
            OASISErrorHandling.HandleError(ref result, "Unable to read finalized Solana wallet activity.", error);
        }
        return result;
    }

    public async Task<OASISResult<List<object>>> ReadWalletAssetsAsync(string walletAddress, CancellationToken cancellationToken)
    {
        const string splToken = "TokenkegQfeZyiNwAJbNbGKPFXCWuBvf9Ss623VQ5DA";
        const string token2022 = "TokenzQdBNbLqP5VEhdkAS6EPFLC1PHnBqCXEpPxuEb";
        var result = new OASISResult<List<object>>();
        try
        {
            var address = new PublicKey(walletAddress);
            if (address.KeyBytes.Length != 32)
                throw new ArgumentException("Invalid Solana wallet address.");

            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            deadline.CancelAfter(TimeSpan.FromSeconds(10));
            var nativeTask = _rpcClient.GetBalanceAsync(address.Key, Commitment.Finalized);
            var splTask = _rpcClient.GetTokenAccountsByOwnerAsync(address.Key, null, splToken, Commitment.Finalized);
            var token2022Task = _rpcClient.GetTokenAccountsByOwnerAsync(address.Key, null, token2022, Commitment.Finalized);
            await Task.WhenAll(nativeTask, splTask, token2022Task).WaitAsync(deadline.Token);

            var native = await nativeTask;
            var spl = await splTask;
            var extensions = await token2022Task;
            if (!native.WasSuccessful || native.Result == null || !spl.WasSuccessful || spl.Result?.Value == null ||
                !extensions.WasSuccessful || extensions.Result?.Value == null)
                throw new InvalidOperationException("Solana RPC did not return all finalized wallet balances.");

            var accounts = new HashSet<string>(StringComparer.Ordinal);
            var totals = new Dictionary<string, (BigInteger Amount, int Decimals)>(StringComparer.Ordinal);
            foreach (var account in spl.Result.Value.Concat(extensions.Result.Value))
            {
                var info = account?.Account?.Data?.Parsed?.Info;
                if (string.IsNullOrEmpty(account?.PublicKey) || !accounts.Add(account.PublicKey) ||
                    info?.Owner != address.Key || string.IsNullOrEmpty(info.Mint) || info.TokenAmount == null ||
                    !ulong.TryParse(info.TokenAmount.Amount, NumberStyles.None, CultureInfo.InvariantCulture, out var raw))
                    throw new InvalidOperationException("Solana RPC returned invalid token account data.");

                int decimals = info.TokenAmount.Decimals;
                if (decimals < 0 || decimals > byte.MaxValue ||
                    (totals.TryGetValue(info.Mint, out var total) && total.Decimals != decimals))
                    throw new InvalidOperationException("Solana RPC returned inconsistent token precision.");
                totals[info.Mint] = (total.Amount + raw, decimals);
            }

            var balances = new List<object> { new { symbol = "SOL", amount = WalletAssetAmount(native.Result.Value, 9) } };
            foreach (var (mint, total) in totals.OrderBy(entry => entry.Key, StringComparer.Ordinal))
                if (total.Amount != 0)
                    balances.Add(new { mint, amount = WalletAssetAmount(total.Amount, total.Decimals) });

            result.Result = balances;
            result.IsLoaded = true;
            result.LoadedCount = balances.Count;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            OASISErrorHandling.HandleError(ref result, "The Solana wallet balance read timed out.");
        }
        catch (Exception error) when (error is not OperationCanceledException)
        {
            OASISErrorHandling.HandleError(ref result, "Unable to read finalized Solana wallet balances.", error);
        }
        return result;
    }

    private static string WalletAssetAmount(BigInteger raw, int decimals)
    {
        var digits = raw.ToString(CultureInfo.InvariantCulture);
        if (decimals == 0 || raw.IsZero) return digits;
        digits = digits.PadLeft(decimals + 1, '0');
        return digits.Insert(digits.Length - decimals, ".").TrimEnd('0').TrimEnd('.');
    }
}
