using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;
using Nethereum.Hex.HexTypes;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.Providers.Shared.Evm;

internal static class EvmTokenEscrow
{
    internal sealed class Options
    {
        internal string EscrowAddress, StorageAddress, Host;
        internal BigInteger ChainId;
        internal bool IsActivated;
        internal string TokenAbi;
        internal Func<Task<OASISResult<bool>>> Activate;
        internal Func<Guid,OASISResult<List<string>>> Keys;
        internal Func<Account,Web3> Client;
        internal Func<Web3,Account,Function,string,string,object[],Action<string>,Task<TransactionReceipt>> Submit;
        internal Func<string,Guid,IEnumerable<string>,ITransactionResponse> Response;
        internal Func<string,string,IEnumerable<string>,Exception,Exception> Reconcile;
    }
    private const string TokenEscrowRuntimeSha256 = "f99e28db6c6424f3a7b224f447f6eb1d1ea5e08407ddad0c80744ffca0879163";
    private const string EscrowAbi = """
    [
      {"type":"function","name":"deposits","inputs":[{"type":"bytes32"}],"outputs":[{"type":"address"},{"type":"address"},{"type":"uint256"},{"type":"bool"}],"stateMutability":"view"},
      {"type":"function","name":"lock","inputs":[{"type":"bytes32"},{"type":"address"},{"type":"uint256"}],"outputs":[],"stateMutability":"nonpayable"},
      {"type":"function","name":"unlock","inputs":[{"type":"bytes32"}],"outputs":[],"stateMutability":"nonpayable"}
    ]
    """;

    internal static async Task<OASISResult<ITransactionResponse>> ExecuteAsync(Options options, Guid id, string tokenAddress,
        string requestedEscrow, Guid avatarId, string key, string sender, decimal amount, bool unlock)
    {
        var result = new OASISResult<ITransactionResponse>();
        var confirmed = new List<string>();
        string currentHash = null, stage = null;
        try
        {
            if (options.ChainId <= 0 || id == Guid.Empty || avatarId == Guid.Empty || (!unlock && amount <= 0)
                || !Nethereum.Util.AddressUtil.Current.IsValidEthereumAddressHexFormat(tokenAddress)
                || !Nethereum.Util.AddressUtil.Current.IsValidEthereumAddressHexFormat(options.EscrowAddress)
                || !string.Equals(requestedEscrow, options.EscrowAddress, StringComparison.OrdinalIgnoreCase)
                || string.Equals(options.EscrowAddress, options.StorageAddress, StringComparison.OrdinalIgnoreCase)
                || string.Equals(options.EscrowAddress, "0x" + new string('0', 40), StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Explicit deposit/owner identity, token, positive lock amount and matching configured separate escrow are required.");
            List<Account> candidates=null;
            Account account=null;
            if (unlock)
            {
                var keys = options.Keys(avatarId);
                if (keys.IsError) { OASISErrorHandling.HandleError(ref result, keys.Message, keys.Exception); result.ErrorCode = keys.ErrorCode; return result; }
                if (keys.Result == null || keys.Result.Count == 0 || keys.Result.Any(string.IsNullOrWhiteSpace))
                    throw new ArgumentException("Release requires authorized depositor signing wallets.");
                candidates=keys.Result.Select(value=>new Account(value,options.ChainId)).GroupBy(value=>value.Address,StringComparer.OrdinalIgnoreCase).Select(group=>group.First()).ToList();
            }
            else
            {
                if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Depositor signing key is required.");
                account=new Account(key,options.ChainId);
                if(!string.Equals(account.Address,sender,StringComparison.OrdinalIgnoreCase))
                    throw new ArgumentException("Lock signing key does not match the explicit sender.");
            }
            if (!options.IsActivated)
            {
                var active = await options.Activate();
                if (active.IsError) { OASISErrorHandling.HandleError(ref result, active.Message, active.Exception); result.ErrorCode = active.ErrorCode; return result; }
            }
            var web3 = unlock ? new Web3(options.Host):options.Client(account);
            var code = await web3.Eth.GetCode.SendRequestAsync(options.EscrowAddress);
            if (string.IsNullOrEmpty(code) || !code.StartsWith("0x", StringComparison.Ordinal)
                || !string.Equals(Convert.ToHexString(SHA256.HashData(Convert.FromHexString(code.Substring(2)))).ToLowerInvariant(), TokenEscrowRuntimeSha256, StringComparison.Ordinal))
                throw new ArgumentException("Configured escrow runtime differs from the reviewed pinned contract.");
            var nativeId = SHA256.HashData(Encoding.UTF8.GetBytes(id.ToString("N")));
            var escrow = web3.Eth.GetContract(EscrowAbi, options.EscrowAddress);
            var recordFunction = escrow.GetFunction("deposits");
            var record = await recordFunction.CallDeserializingToObjectAsync<EvmEscrowDeposit>(nativeId);
            var empty = string.Equals(record.Owner, "0x" + new string('0', 40), StringComparison.OrdinalIgnoreCase);
            if ((!unlock && !empty) || (unlock && (empty || record.Released
                || !string.Equals(record.Token, tokenAddress, StringComparison.OrdinalIgnoreCase))))
                throw new StorageLifecycleConflictException("Deposit identity is used, released, missing or not owned by this signer/token.");
            if(unlock)
            {
                account=candidates.SingleOrDefault(value=>string.Equals(value.Address,record.Owner,StringComparison.OrdinalIgnoreCase));
                if(account==null) throw new StorageLifecycleConflictException("No authorized avatar key owns the native escrow deposit.");
                // Bind signing only after pinned runtime and native deposit ownership were verified.
                web3=options.Client(account);
                escrow=web3.Eth.GetContract(EscrowAbi,options.EscrowAddress);
                recordFunction=escrow.GetFunction("deposits");
            }
            var token = web3.Eth.GetContract(options.TokenAbi, tokenAddress);
            var balance = token.GetFunction("balanceOf");
            var ownerBefore = await balance.CallAsync<BigInteger>(account.Address);
            var escrowBefore = await balance.CallAsync<BigInteger>(options.EscrowAddress);
            var supplyBefore = await token.GetFunction("totalSupply").CallAsync<BigInteger>();
            var units = unlock ? record.Amount : NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.ExactEvmTokenAmount.ToUnits(amount, await token.GetFunction("decimals").CallAsync<byte>());
            if (units <= 0 || (!unlock && ownerBefore < units) || (unlock && escrowBefore < units))
                throw new StorageLifecycleConflictException("Escrow token balance does not satisfy the explicit operation.");

            async Task<string> SubmitAsync(Function function, string operation, params object[] arguments)
            {
                await function.EstimateGasAsync(account.Address, null, null, arguments);
                currentHash = null; stage = operation;
                var receipt = await options.Submit(web3,account,function,operation == "approve" ? tokenAddress : options.EscrowAddress,operation,arguments,hash=>currentHash=hash);
                if (!ValidHash(currentHash)) throw new InvalidOperationException("Escrow submission returned no valid native hash.");
                if (receipt?.Status?.Value != BigInteger.One || !string.Equals(receipt.TransactionHash, currentHash, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Escrow stage has no matching successful receipt.");
                confirmed.Add(currentHash); var acknowledged = currentHash; currentHash = null; stage = null;
                return acknowledged;
            }
            if (!unlock)
            {
                var allowance = token.GetFunction("allowance");
                var approved = await allowance.CallAsync<BigInteger>(account.Address, options.EscrowAddress);
                if (approved < units)
                {
                    if (approved != 0) throw new ArgumentException("Clear a partial existing escrow allowance explicitly before locking.");
                    await SubmitAsync(token.GetFunction("approve"), "approve", options.EscrowAddress, units);
                    if (await allowance.CallAsync<BigInteger>(account.Address, options.EscrowAddress) != units)
                        throw new InvalidOperationException("Acknowledged approval does not establish the exact escrow allowance.");
                }
            }
            var transaction = unlock
                ? await SubmitAsync(escrow.GetFunction("unlock"), "unlock", nativeId)
                : await SubmitAsync(escrow.GetFunction("lock"), "lock", nativeId, tokenAddress, units);
            var final = await recordFunction.CallDeserializingToObjectAsync<EvmEscrowDeposit>(nativeId);
            if (!string.Equals(final.Owner, account.Address, StringComparison.OrdinalIgnoreCase)
                || !string.Equals(final.Token, tokenAddress, StringComparison.OrdinalIgnoreCase)
                || final.Amount != units || final.Released != unlock
                || await balance.CallAsync<BigInteger>(account.Address) != ownerBefore + (unlock ? units : -units)
                || await balance.CallAsync<BigInteger>(options.EscrowAddress) != escrowBefore + (unlock ? -units : units)
                || await token.GetFunction("totalSupply").CallAsync<BigInteger>() != supplyBefore)
                throw new InvalidOperationException("Escrow receipt does not establish exact deposit/custody effects.");
            result.Result = options.Response(transaction, id, confirmed);
            result.IsSaved = true; result.SavedCount = 1;
            result.Message = unlock ? "Owned deposit released." : "Explicit owned deposit locked.";
        }
        catch (Exception cause)
        {
            var native = stage != null || confirmed.Count != 0;
            OASISErrorHandling.HandleError(ref result, "EVM token escrow operation failed.", native
                ? options.Reconcile(stage != null ? currentHash ?? (cause as INativeMutationReconciliationRequired)?.OriginalTransactionId : confirmed.LastOrDefault(), stage, confirmed, cause) : cause);
            if (!native && cause is ArgumentException) result.ErrorCode = "HYPERDRIVE_PAYLOAD_INVALID";
            if (!native && cause is StorageLifecycleConflictException) result.ErrorCode = StorageLifecycleConflictException.ErrorCode;
        }
        return result;
    }

    private static bool ValidHash(string hash) => hash?.Length == 66 && hash.StartsWith("0x",StringComparison.Ordinal) && hash.Skip(2).All(Uri.IsHexDigit);
}

[FunctionOutput]
internal sealed class EvmEscrowDeposit : IFunctionOutputDTO
{
    public EvmEscrowDeposit() { }
    [Parameter("address", "owner", 1)] public string Owner { get; set; }
    [Parameter("address", "token", 2)] public string Token { get; set; }
    [Parameter("uint256", "amount", 3)] public BigInteger Amount { get; set; }
    [Parameter("bool", "released", 4)] public bool Released { get; set; }
}
