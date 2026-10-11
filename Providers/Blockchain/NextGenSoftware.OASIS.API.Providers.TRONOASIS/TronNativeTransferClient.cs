using System;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf;
using TronNet;
using TronNet.Protocol;

namespace NextGenSoftware.OASIS.API.Providers.TRONOASIS;

/// <summary>Uses real protobuf node operations, local SDK signing and solidified receipts.</summary>
public sealed class TronNativeTransferClient
{
    private readonly IWalletClient _wallet;
    private readonly ITransactionClient _transactions;
    private readonly string _genesisBlockId;

    public TronNativeTransferClient(IWalletClient wallet, ITransactionClient transactions, string genesisBlockId)
    {
        _wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
        _transactions = transactions ?? throw new ArgumentNullException(nameof(transactions));
        if (string.IsNullOrWhiteSpace(genesisBlockId) || genesisBlockId.Length != 64
            || !System.Text.RegularExpressions.Regex.IsMatch(genesisBlockId, "\\A[0-9a-fA-F]{64}\\z"))
            throw new ArgumentException("The expected 32-byte genesis block ID is required.", nameof(genesisBlockId));
        _genesisBlockId = genesisBlockId;
    }

    public async Task VerifyNetworkAsync(CancellationToken cancellationToken = default)
    {
        var genesis = await _wallet.GetProtocol().GetBlockByNum2Async(new NumberMessage { Num = 0 }, _wallet.GetHeaders(),
            DateTime.UtcNow.AddSeconds(15), cancellationToken);
        if (!string.Equals(Convert.ToHexString(genesis.Blockid.ToByteArray()), _genesisBlockId, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("TRON genesis block does not match the configured network.");
        var solidGenesis = await _wallet.GetSolidityProtocol().GetBlockByNum2Async(new NumberMessage { Num = 0 }, _wallet.GetHeaders(),
            DateTime.UtcNow.AddSeconds(15), cancellationToken);
        if (!string.Equals(Convert.ToHexString(solidGenesis.Blockid.ToByteArray()), _genesisBlockId, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("TRON solidity node genesis block does not match the configured network.");
    }

    public async Task<string> SendAsync(string from, string to, decimal amount, string privateKey,
        string memo, CancellationToken cancellationToken = default)
    {
        var units = NativeUnits(amount);
        if (string.IsNullOrWhiteSpace(privateKey)) throw new ArgumentException("A real sender signing key is required.");
        // Shasta and Nile also use 0x41; the SDK's old TestNet value uses legacy 0xa0.
        var signer = new TronECKey(privateKey, TronNetwork.MainNet);
        var source = _wallet.ParseAddress(from);
        var recipient = _wallet.ParseAddress(to);
        if (source.Length != 21 || recipient.Length != 21 || source[0] != 0x41 || recipient[0] != 0x41)
            throw new ArgumentException("TRON sender and recipient must be 21-byte addresses with the current 0x41 prefix.");
        if (!_wallet.ParseAddress(signer.GetPublicAddress()).Equals(source))
            throw new ArgumentException("Signing key does not own the sender address.");
        var node = _wallet.GetProtocol();
        var headers = _wallet.GetHeaders();
        await VerifyNetworkAsync(cancellationToken);

        var unsigned = await node.CreateTransaction2Async(new TransferContract
        {
            OwnerAddress = source, ToAddress = recipient, Amount = (long)units
        }, headers, DateTime.UtcNow.AddSeconds(15), cancellationToken);
        if (unsigned.Result == null || !unsigned.Result.Result || unsigned.Transaction?.RawData == null)
            throw new InvalidOperationException("TRON node rejected transaction construction: " + unsigned.Result?.Message.ToStringUtf8());
        var raw = unsigned.Transaction.RawData;
        if (raw.Contract.Count != 1 || raw.Contract[0].Type != Transaction.Types.Contract.Types.ContractType.TransferContract)
            throw new InvalidOperationException("TRON node returned an unexpected transaction contract.");
        var transfer = raw.Contract[0].Parameter.Unpack<TransferContract>();
        if (!transfer.OwnerAddress.Equals(source) || !transfer.ToAddress.Equals(recipient) || transfer.Amount != (long)units)
            throw new InvalidOperationException("TRON node changed the requested sender, recipient or amount.");
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        if (raw.Timestamp < now - 60000 || raw.Timestamp > now + 60000 || raw.Expiration <= now)
            throw new InvalidOperationException("TRON node returned stale or invalid transaction timestamps.");
        return await SignBroadcastConfirmAsync(unsigned.Transaction, privateKey, memo, cancellationToken);
    }

    internal static long NativeUnits(decimal amount)
    {
        if (amount <= 0 || amount > long.MaxValue / 1000000m)
            throw new ArgumentOutOfRangeException(nameof(amount), "Positive exact int64 SUN units are required (6 decimal places).");
        var units = amount * 1000000m;
        if (units != decimal.Truncate(units))
            throw new ArgumentOutOfRangeException(nameof(amount), "Positive exact int64 SUN units are required (6 decimal places).");
        return (long)units;
    }

    public Task<string> BurnTokenAsync(string tokenAddress, decimal amount, string privateKey,
        CancellationToken cancellationToken = default, string publicKey = null)
        => ExecuteTrc20Async(tokenAddress, amount, privateKey, cancellationToken, publicKey);

    public Task<string> SendTokenAsync(string from, string to, string tokenAddress, decimal amount,
        string privateKey, string memo = null, string publicKey = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(from) || string.IsNullOrWhiteSpace(to))
            throw new ArgumentException("TRC20 sender and recipient addresses are required.");
        return ExecuteTrc20Async(tokenAddress, amount, privateKey, cancellationToken, publicKey, from, to, memo);
    }

    public Task<string> MintTokenAsync(string minter, string tokenAddress, decimal amount, string privateKey,
        string memo = null, string publicKey = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(minter)) throw new ArgumentException("A TRC20 minter address is required.");
        return ExecuteTrc20Async(tokenAddress, amount, privateKey, cancellationToken, publicKey, minter, minter, memo, mint: true);
    }

    private async Task<string> ExecuteTrc20Async(string tokenAddress, decimal amount, string privateKey,
        CancellationToken cancellationToken, string publicKey, string from = null, string to = null, string memo = null, bool mint = false)
    {
        if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount));
        if (string.IsNullOrWhiteSpace(privateKey)) throw new ArgumentException("A real token holder signing key is required.");
        if (!System.Text.RegularExpressions.Regex.IsMatch(privateKey, "\\A[0-9a-fA-F]{64}\\z"))
            throw new ArgumentException("TRON signing keys require 32-byte hexadecimal encoding.");
        if (!string.IsNullOrWhiteSpace(publicKey))
        {
            var actual = Convert.ToHexString(new Nethereum.Signer.EthECKey(privateKey).GetPubKey());
            if (!string.Equals(actual, publicKey.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? publicKey[2..] : publicKey,
                StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Signing key does not match the supplied public key.");
        }
        var signer = new TronECKey(privateKey, TronNetwork.MainNet);
        var source = _wallet.ParseAddress(signer.GetPublicAddress());
        ByteString recipient = null;
        if (from != null)
        {
            if (!_wallet.ParseAddress(from).Equals(source))
                throw new ArgumentException("Signing key does not own the requested TRC20 sender address.");
            recipient = _wallet.ParseAddress(to);
            if (recipient.Length != 21 || recipient[0] != 0x41)
                throw new ArgumentException("A current TRON recipient address is required.");
        }
        var contract = _wallet.ParseAddress(tokenAddress);
        if (contract.Length != 21 || contract[0] != 0x41)
            throw new ArgumentException("A current TRON contract address is required.");
        await VerifyNetworkAsync(cancellationToken);
        var node = _wallet.GetProtocol();
        var headers = _wallet.GetHeaders();
        var decimals = await node.TriggerConstantContractAsync(new TriggerSmartContract
        {
            OwnerAddress = source, ContractAddress = contract,
            Data = ByteString.CopyFrom(Convert.FromHexString("313ce567")) // decimals()
        }, headers, DateTime.UtcNow.AddSeconds(15), cancellationToken);
        if (decimals.Result == null || !decimals.Result.Result || decimals.ConstantResult.Count != 1
            || decimals.ConstantResult[0].Length != 32)
            throw new InvalidOperationException("TRC20 decimals query did not return one ABI uint256.");
        var precision = new System.Numerics.BigInteger(decimals.ConstantResult[0].ToByteArray(), isUnsigned: true, isBigEndian: true);
        if (precision > 255) throw new InvalidOperationException("TRC20 decimals exceed uint8.");
        var units = TokenUnits(amount, (int)precision);
        var data = EncodeTrc20Call(recipient, units, mint);
        var requested = new TriggerSmartContract { OwnerAddress = source, ContractAddress = contract, Data = ByteString.CopyFrom(data) };
        var unsigned = await node.TriggerContractAsync(requested, headers, DateTime.UtcNow.AddSeconds(15), cancellationToken);
        if (unsigned.Result == null || !unsigned.Result.Result || unsigned.Transaction?.RawData == null)
            throw new InvalidOperationException("TRON node rejected TRC20 transaction construction.");
        var raw = unsigned.Transaction.RawData;
        if (raw.Contract.Count != 1 || raw.Contract[0].Type != Transaction.Types.Contract.Types.ContractType.TriggerSmartContract
            || !raw.Contract[0].Parameter.Unpack<TriggerSmartContract>().Equals(requested))
            throw new InvalidOperationException("TRON node changed the requested TRC20 contract or parameters.");
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        if (raw.Timestamp < now - 60000 || raw.Timestamp > now + 60000 || raw.Expiration <= now)
            throw new InvalidOperationException("TRON node returned stale TRC20 transaction timestamps.");
        raw.FeeLimit = 100000000; // Maximum transaction fee, not an asset amount.
        return await SignBroadcastConfirmAsync(unsigned.Transaction, privateKey, memo ?? "", cancellationToken,
            requireContractSuccess: true, requireTransferReturn: recipient != null, allowVoidContractReturn: mint);
    }

    internal static byte[] EncodeTrc20Call(ByteString recipient, System.Numerics.BigInteger units, bool mint = false)
    {
        if (mint && recipient == null) throw new ArgumentException("Mint requires an explicit recipient.");
        if (units <= 0 || units >= (System.Numerics.BigInteger.One << 256))
            throw new ArgumentOutOfRangeException(nameof(units));
        if (recipient != null && (recipient.Length != 21 || recipient[0] != 0x41))
            throw new ArgumentException("A current TRON recipient address is required.");
        var abiUnits = units.ToByteArray(isUnsigned: true, isBigEndian: true);
        var data = new byte[recipient == null ? 36 : 68];
        Convert.FromHexString(recipient == null ? "42966c68" : mint ? "40c10f19" : "a9059cbb").CopyTo(data, 0);
        if (recipient != null)
            recipient.ToByteArray().AsSpan(1).CopyTo(data.AsSpan(16, 20));
        abiUnits.CopyTo(data, data.Length - abiUnits.Length);
        return data;
    }

    internal static System.Numerics.BigInteger TokenUnits(decimal amount, int decimals)
    {
        if (amount <= 0 || decimals < 0 || decimals > 255) throw new ArgumentOutOfRangeException(nameof(amount));
        var bits = decimal.GetBits(amount);
        var units = new System.Numerics.BigInteger((uint)bits[0])
            + (new System.Numerics.BigInteger((uint)bits[1]) << 32) + (new System.Numerics.BigInteger((uint)bits[2]) << 64);
        var scale = (bits[3] >> 16) & 255;
        if (scale <= decimals) units *= System.Numerics.BigInteger.Pow(10, decimals - scale);
        else {
            units = System.Numerics.BigInteger.DivRem(units, System.Numerics.BigInteger.Pow(10, scale - decimals), out var remainder);
            if (remainder != 0) throw new ArgumentException("Token amount exceeds ledger decimal precision.", nameof(amount));
        }
        if (units >= (System.Numerics.BigInteger.One << 256)) throw new ArgumentOutOfRangeException(nameof(amount));
        return units;
    }

    internal static void ValidateTrc20TransferReturn(TransactionInfo receipt)
    {
        if (receipt.ContractResult.Count != 1 || receipt.ContractResult[0].Length != 32)
            throw new InvalidOperationException("TRC20 transfer did not return one ABI boolean.");
        var value = new System.Numerics.BigInteger(receipt.ContractResult[0].ToByteArray(), isUnsigned: true, isBigEndian: true);
        if (value != System.Numerics.BigInteger.One)
            throw new InvalidOperationException("TRC20 transfer did not return true; contract execution success alone does not prove transfer success.");
    }

    private async Task<string> SignBroadcastConfirmAsync(Transaction transaction, string privateKey, string memo,
        CancellationToken cancellationToken, bool requireContractSuccess = false, bool requireTransferReturn = false,
        bool allowVoidContractReturn = false)
    {
        var raw = transaction.RawData;
        var headers = _wallet.GetHeaders();
        raw.Data = ByteString.CopyFromUtf8(memo ?? "");
        var requestedRawData = raw.ToByteArray();
        var signed = _transactions.GetTransactionSign(transaction, privateKey);
        if (signed?.RawData == null || signed.Signature.Count != 1
            || signed.Signature[0].Length != 65
            || !CryptographicOperations.FixedTimeEquals(requestedRawData, signed.RawData.ToByteArray()))
            throw new InvalidOperationException("TRON signer returned an unsigned or altered transaction.");
        var digest = SHA256.HashData(signed.RawData.ToByteArray());
        var transactionId = Convert.ToHexString(digest).ToLowerInvariant();
        // Broadcast exactly once; receipt polling never constructs or submits a replacement.
        Return broadcast;
        try
        {
            broadcast = await _transactions.BroadcastTransactionAsync(signed).WaitAsync(TimeSpan.FromSeconds(15), cancellationToken);
        }
        catch (Exception error)
        {
            throw new InvalidOperationException($"TRON broadcast outcome for {transactionId} is unknown. Track this original transaction before any retry.", error);
        }
        if (broadcast == null || !broadcast.Result)
            throw new InvalidOperationException($"TRON rejected broadcast {transactionId}: {broadcast?.Code} {broadcast?.Message.ToStringUtf8()}");

        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(TimeSpan.FromMinutes(3));
        try
        {
            var solidity = _wallet.GetSolidityProtocol();
            var id = new BytesMessage { Value = ByteString.CopyFrom(digest) };
            while (true)
            {
                var receipt = await solidity.GetTransactionInfoByIdAsync(id, headers,
                    DateTime.UtcNow.AddSeconds(15), deadline.Token);
                if (receipt.Id.Length != 0)
                {
                    if (!receipt.Id.Equals(id.Value) || receipt.BlockNumber <= 0)
                        throw new InvalidOperationException("TRON returned an inconsistent solidified receipt.");
                    if ((int)receipt.Result != 0)
                        throw new InvalidOperationException($"TRON transfer {transactionId} failed in the solidified chain.");
                    if (requireContractSuccess && (receipt.Receipt == null
                        || (int)receipt.Receipt.Result != 1)) // SDK contractResult SUCCESS=1; DEFAULT=0 is not success.
                        throw new InvalidOperationException($"TRON contract execution {transactionId} did not report SUCCESS.");
                    if (requireTransferReturn && (!allowVoidContractReturn || receipt.ContractResult.Count != 0))
                        ValidateTrc20TransferReturn(receipt);
                    return transactionId;
                }
                await Task.Delay(TimeSpan.FromSeconds(2), deadline.Token);
            }
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException($"TRON broadcast {transactionId} remains unconfirmed. Track this original transaction; do not submit a replacement blindly.");
        }
        catch (Exception error)
        {
            throw new InvalidOperationException($"TRON confirmation failed for broadcast {transactionId}. Track this original transaction before any retry.", error);
        }
    }
}
