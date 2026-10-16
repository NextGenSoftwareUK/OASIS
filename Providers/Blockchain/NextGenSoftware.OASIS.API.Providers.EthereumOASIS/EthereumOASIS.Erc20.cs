using System;
using System.Numerics;
using System.Threading.Tasks;
using Nethereum.Contracts;
using Nethereum.Hex.HexTypes;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.EthereumOASIS;

public partial class EthereumOASIS
{
    private const string TokenAbi = """
    [
      {"type":"function","name":"decimals","inputs":[],"outputs":[{"type":"uint8"}],"stateMutability":"view"},
      {"type":"function","name":"balanceOf","inputs":[{"type":"address"}],"outputs":[{"type":"uint256"}],"stateMutability":"view"},
      {"type":"function","name":"totalSupply","inputs":[],"outputs":[{"type":"uint256"}],"stateMutability":"view"},
      {"type":"function","name":"transfer","inputs":[{"type":"address"},{"type":"uint256"}],"outputs":[{"type":"bool"}],"stateMutability":"nonpayable"},
      {"type":"function","name":"burn","inputs":[{"type":"uint256"}],"outputs":[],"stateMutability":"nonpayable"},
      {"type":"function","name":"mint","inputs":[{"type":"address"},{"type":"uint256"}],"outputs":[],"stateMutability":"nonpayable"},
      {"type":"function","name":"allowance","inputs":[{"type":"address"},{"type":"address"}],"outputs":[{"type":"uint256"}],"stateMutability":"view"},
      {"type":"function","name":"approve","inputs":[{"type":"address"},{"type":"uint256"}],"outputs":[{"type":"bool"}],"stateMutability":"nonpayable"}
    ]
    """;

    private static BigInteger ExactTokenUnits(decimal amount, byte decimals)
        =>NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.ExactEvmTokenAmount.ToUnits(amount,decimals);

    private enum TokenMutation { Transfer, Burn, Mint }

    private Task<OASISResult<ITransactionResponse>> ExecuteErc20MutationAsync(string key, string token, string recipient, decimal amount, bool burn)
        => ExecuteErc20MutationCoreAsync(key, token, recipient, amount, burn ? TokenMutation.Burn : TokenMutation.Transfer);

    private async Task<OASISResult<ITransactionResponse>> ExecuteErc20MutationCoreAsync(string key, string token, string recipient, decimal amount, TokenMutation operation)
    {
        var result = new OASISResult<ITransactionResponse>();
        string hash = null;
        var dispatched = false;
        try
        {
            var burn = operation == TokenMutation.Burn;
            var mint = operation == TokenMutation.Mint;
            if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount));
            if (!Nethereum.Util.AddressUtil.Current.IsValidEthereumAddressHexFormat(token)
                || (!burn && (!Nethereum.Util.AddressUtil.Current.IsValidEthereumAddressHexFormat(recipient)
                    || string.Equals(recipient, "0x" + new string('0', 40), StringComparison.OrdinalIgnoreCase))))
                throw new ArgumentException("Valid token and nonzero transfer recipient addresses are required.");
            if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Token owner's signing key is required.");
            var account = new Account(key, ChainId);
            if (!IsProviderActivated)
            {
                var active = await ActivateProviderAsync();
                if (active.IsError) { OASISErrorHandling.HandleError(ref result, active.Message, active.Exception); result.ErrorCode = active.ErrorCode; return result; }
            }
            var web3 = CreateWeb3WithAccount(account, HostURI);
            var code = await web3.Eth.GetCode.SendRequestAsync(token);
            if (string.IsNullOrEmpty(code) || code == "0x" || code == "0x0") throw new ArgumentException("Token address has no contract bytecode.");
            var contract = web3.Eth.GetContract(TokenAbi, token);
            var units = ExactTokenUnits(amount, await contract.GetFunction("decimals").CallAsync<byte>());
            var balance = contract.GetFunction("balanceOf");
            var before = await balance.CallAsync<BigInteger>(account.Address);
            if (!mint && before < units) throw new InvalidOperationException("Insufficient token balance.");
            var supply = await contract.GetFunction("totalSupply").CallAsync<BigInteger>();
            var targetBefore = burn ? BigInteger.Zero : await balance.CallAsync<BigInteger>(recipient);
            var function = contract.GetFunction(mint ? "mint" : burn ? "burn" : "transfer");
            var arguments = burn ? new object[] { units } : new object[] { recipient, units };
            if (mint) await function.EstimateGasAsync(account.Address, null, null, arguments);
            dispatched = true;
            hash = await function.SendTransactionAsync(account.Address, new HexBigInteger(600000), null, arguments);
            if (!IsStorageTransactionIdentity(hash)) throw new InvalidOperationException("Token submission returned no valid native hash.");
            var receipt = await PollTokenReceiptAsync(web3, hash);
            if (receipt == null || !string.Equals(receipt.TransactionHash, hash, StringComparison.OrdinalIgnoreCase)
                || receipt.Status == null || (receipt.Status.Value != 0 && receipt.Status.Value != 1))
                throw new InvalidOperationException("Token receipt has no authoritative matching outcome.");
            result.Result = new NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses.TransactionResponse { TransactionResult = hash };
            if (receipt.Status.Value == 0)
            {
                result.ErrorCode = "ETHEREUM_TOKEN_REVERTED";
                OASISErrorHandling.HandleError(ref result, "Token transaction reverted; native hash retained.", new InvalidOperationException("EVM token execution reverted."));
                return result;
            }
            var after = await balance.CallAsync<BigInteger>(account.Address);
            var supplyAfter = await contract.GetFunction("totalSupply").CallAsync<BigInteger>();
            var self = !burn && string.Equals(account.Address, recipient, StringComparison.OrdinalIgnoreCase);
            var expectedOwner = mint ? before + (self ? units : BigInteger.Zero) : before - (self ? BigInteger.Zero : units);
            var expectedSupply = mint ? supply + units : supply - (burn ? units : BigInteger.Zero);
            var expectedTarget = targetBefore + (!mint && self ? BigInteger.Zero : units);
            if (after != expectedOwner || supplyAfter != expectedSupply
                || (!burn && await balance.CallAsync<BigInteger>(recipient) != expectedTarget))
                throw new InvalidOperationException("Acknowledged token transaction does not establish the requested exact balance/supply effect.");
            result.IsSaved = true;
            result.SavedCount = 1;
            result.Message = mint ? "Exact token mint confirmed." : burn ? "Exact token burn confirmed." : "Exact token transfer confirmed.";
        }
        catch (Exception cause)
        {
            result.Result = null;
            OASISErrorHandling.HandleError(ref result, "Ethereum token mutation failed.", dispatched ? new EthereumTokenMutationReconciliationException(hash, cause) : cause);
            if (!dispatched && cause is ArgumentException) result.ErrorCode = "HYPERDRIVE_PAYLOAD_INVALID";
        }
        return result;
    }

    protected virtual Task<TransactionReceipt> PollTokenReceiptAsync(Web3 web3, string hash)
        => web3.TransactionManager.TransactionReceiptService.PollForReceiptAsync(hash);
}

public sealed class EthereumTokenMutationReconciliationException : Exception, INativeMutationReconciliationRequired
{
    public string OriginalTransactionId { get; }
    public EthereumTokenMutationReconciliationException(string hash, Exception cause)
        : base("Ethereum token outcome requires reconciliation before replay.", cause) => OriginalTransactionId = hash;
}
