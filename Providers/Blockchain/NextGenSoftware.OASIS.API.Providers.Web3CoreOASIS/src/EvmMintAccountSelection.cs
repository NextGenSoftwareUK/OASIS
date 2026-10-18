using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Nethereum.Web3.Accounts;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.Providers.Shared.Evm;

// One maintained selection policy; also source-linked by EthereumOASIS.
internal static class EvmMintAccountSelection
{
    internal static async Task<OASISResult<Account>> SelectAsync(List<string> keys, BigInteger chainId,
        Func<Task<OASISResult<string>>> readDefault)
    {
        var result = new OASISResult<Account>();
        try
        {
            if (keys == null || keys.Count == 0 || keys.Any(string.IsNullOrWhiteSpace))
                throw new InvalidOperationException("Mint requires authorized EVM signing wallets.");
            var accounts = keys.Select(key => new Account(key, chainId))
                .GroupBy(account => account.Address, StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First()).ToList();
            if (accounts.Count == 1)
                result.Result = accounts[0];
            else
            {
                var wallet = await readDefault();
                if (wallet.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, wallet.Message, wallet.Exception);
                    result.ErrorCode = wallet.ErrorCode;
                    return result;
                }
                if (!Nethereum.Util.AddressUtil.Current.IsValidEthereumAddressHexFormat(wallet.Result))
                    throw new InvalidOperationException("Multiple EVM wallets require a valid configured default wallet address.");
                result.Result = accounts.SingleOrDefault(account => string.Equals(account.Address, wallet.Result, StringComparison.OrdinalIgnoreCase))
                    ?? throw new UnauthorizedAccessException("The default mint wallet has no authorized signing key.");
            }
        }
        catch (Exception cause)
        {
            OASISErrorHandling.HandleError(ref result, "EVM mint wallet selection failed.", cause);
            if (cause is ArgumentException) result.ErrorCode = "HYPERDRIVE_PAYLOAD_INVALID";
        }
        return result;
    }
}
