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
        var units = checked(amount * 1000000m);
        if (amount <= 0 || units != decimal.Truncate(units) || units > long.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(amount), "Positive exact int64 SUN units are required (6 decimal places).");
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
        raw.Data = ByteString.CopyFromUtf8(memo ?? "");
        var requestedRawData = raw.ToByteArray();
        var signed = _transactions.GetTransactionSign(unsigned.Transaction, privateKey);
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
