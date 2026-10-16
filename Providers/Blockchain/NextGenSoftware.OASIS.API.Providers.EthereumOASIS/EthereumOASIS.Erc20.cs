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
    private const string TokenAbi = "[{\"type\":\"function\",\"name\":\"decimals\",\"inputs\":[],\"outputs\":[{\"type\":\"uint8\"}],\"stateMutability\":\"view\"},{\"type\":\"function\",\"name\":\"balanceOf\",\"inputs\":[{\"type\":\"address\"}],\"outputs\":[{\"type\":\"uint256\"}],\"stateMutability\":\"view\"},{\"type\":\"function\",\"name\":\"totalSupply\",\"inputs\":[],\"outputs\":[{\"type\":\"uint256\"}],\"stateMutability\":\"view\"},{\"type\":\"function\",\"name\":\"transfer\",\"inputs\":[{\"type\":\"address\"},{\"type\":\"uint256\"}],\"outputs\":[{\"type\":\"bool\"}],\"stateMutability\":\"nonpayable\"},{\"type\":\"function\",\"name\":\"burn\",\"inputs\":[{\"type\":\"uint256\"}],\"outputs\":[],\"stateMutability\":\"nonpayable\"}]";

    private static BigInteger ExactTokenUnits(decimal amount, byte decimals)
    {
        if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount), "Token amount must be positive.");
        var bits = decimal.GetBits(amount);
        var coefficient = new BigInteger((uint)bits[0]) + (new BigInteger((uint)bits[1]) << 32) + (new BigInteger((uint)bits[2]) << 64);
        var scale = (bits[3] >> 16) & 255;
        BigInteger units;
        if (decimals >= scale) units = coefficient * BigInteger.Pow(10, decimals - scale);
        else
        {
            units = BigInteger.DivRem(coefficient, BigInteger.Pow(10, scale - decimals), out var remainder);
            if (remainder != 0) throw new ArgumentException("Token amount exceeds the contract's decimal precision.", nameof(amount));
        }
        if (units <= 0 || units >= (BigInteger.One << 256)) throw new ArgumentOutOfRangeException(nameof(amount), "Token amount exceeds uint256.");
        return units;
    }

    private async Task<OASISResult<ITransactionResponse>> ExecuteErc20MutationAsync(string key, string token, string recipient, decimal amount, bool burn)
    {
        var result = new OASISResult<ITransactionResponse>();
        string hash = null;
        var dispatched = false;
        try
        {
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
            if (before < units) throw new InvalidOperationException("Insufficient token balance.");
            var supply = await contract.GetFunction("totalSupply").CallAsync<BigInteger>();
            var targetBefore = burn ? BigInteger.Zero : await balance.CallAsync<BigInteger>(recipient);
            var function = contract.GetFunction(burn ? "burn" : "transfer");
            var arguments = burn ? new object[] { units } : new object[] { recipient, units };
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
            if (after != before - (self ? BigInteger.Zero : units) || supplyAfter != supply - (burn ? units : BigInteger.Zero)
                || (!burn && await balance.CallAsync<BigInteger>(recipient) != targetBefore + (self ? BigInteger.Zero : units)))
                throw new InvalidOperationException("Acknowledged token transaction does not establish the requested exact balance/supply effect.");
            result.IsSaved = true;
            result.SavedCount = 1;
            result.Message = burn ? "Exact token burn confirmed." : "Exact token transfer confirmed.";
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
