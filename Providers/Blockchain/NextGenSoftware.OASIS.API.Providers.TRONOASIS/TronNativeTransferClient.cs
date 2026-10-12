using System;
using System.Linq;
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
    private readonly long _contractFeeLimitSun;
    internal string ExpectedGenesisBlockId => _genesisBlockId;

    public TronNativeTransferClient(IWalletClient wallet, ITransactionClient transactions, string genesisBlockId, long contractFeeLimitSun = 100000000)
    {
        _wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
        _transactions = transactions ?? throw new ArgumentNullException(nameof(transactions));
        if (string.IsNullOrWhiteSpace(genesisBlockId) || genesisBlockId.Length != 64
            || !System.Text.RegularExpressions.Regex.IsMatch(genesisBlockId, "\\A[0-9a-fA-F]{64}\\z"))
            throw new ArgumentException("The expected 32-byte genesis block ID is required.", nameof(genesisBlockId));
        _genesisBlockId = genesisBlockId;
        if (contractFeeLimitSun <= 0) throw new ArgumentOutOfRangeException(nameof(contractFeeLimitSun));
        _contractFeeLimitSun = contractFeeLimitSun;
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

    public async Task<decimal> GetNativeBalanceAsync(string address, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(address)) throw new ArgumentException("A TRON wallet address is required.");
        var requested = _wallet.ParseAddress(address);
        if (requested.Length != 21 || requested[0] != 0x41)
            throw new ArgumentException("A current 21-byte TRON wallet address is required.");
        await VerifyNetworkAsync(cancellationToken);
        var account = await _wallet.GetSolidityProtocol().GetAccountAsync(new Account { Address = requested },
            _wallet.GetHeaders(), DateTime.UtcNow.AddSeconds(15), cancellationToken);
        return NativeBalance(account, requested);
    }

    public async Task<Core.Managers.Bridge.Enums.BridgeTransactionStatus> GetTransactionStatusAsync(string hash, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(hash) || !System.Text.RegularExpressions.Regex.IsMatch(hash, "\\A[0-9a-fA-F]{64}\\z"))
            throw new ArgumentException("A 32-byte hexadecimal TRON transaction ID is required.");
        var id = new BytesMessage { Value = ByteString.CopyFrom(Convert.FromHexString(hash)) };
        await VerifyNetworkAsync(cancellationToken);
        var solidity = _wallet.GetSolidityProtocol();
        var receipt = await solidity.GetTransactionInfoByIdAsync(id, _wallet.GetHeaders(), DateTime.UtcNow.AddSeconds(15), cancellationToken);
        if (receipt.Id.Length == 0)
        {
            var pending = await _wallet.GetProtocol().GetTransactionByIdAsync(id, _wallet.GetHeaders(), DateTime.UtcNow.AddSeconds(15), cancellationToken);
            if (!ValidateTransactionIdentity(pending, id.Value))
                return Core.Managers.Bridge.Enums.BridgeTransactionStatus.NotFound;
            return Core.Managers.Bridge.Enums.BridgeTransactionStatus.Pending;
        }
        var confirmed = await solidity.GetTransactionByIdAsync(id, _wallet.GetHeaders(), DateTime.UtcNow.AddSeconds(15), cancellationToken);
        if (!ValidateTransactionIdentity(confirmed, id.Value))
            throw new InvalidOperationException("Solidified transaction identity is missing or inconsistent.");
        var contract = confirmed.RawData.Contract.Any(x => x.Type == Transaction.Types.Contract.Types.ContractType.TriggerSmartContract
            || x.Type == Transaction.Types.Contract.Types.ContractType.CreateSmartContract);
        return MapConfirmedTransactionStatus(receipt, id.Value, contract);
    }

    public async Task<System.Text.Json.JsonElement> GetNativeFeeReceiptAsync(string hash, CancellationToken cancellationToken = default)
    {
        if (!System.Text.RegularExpressions.Regex.IsMatch(hash ?? "", "\\A[0-9a-fA-F]{64}\\z"))
            throw new ArgumentException("Exact TRON transaction ID required.");
        var id = ByteString.CopyFrom(Convert.FromHexString(hash));
        await VerifyNetworkAsync(cancellationToken);
        var node = _wallet.GetSolidityProtocol();
        var request = new BytesMessage { Value = id };
        var receipt = await node.GetTransactionInfoByIdAsync(request, _wallet.GetHeaders(), DateTime.UtcNow.AddSeconds(15), cancellationToken);
        var transaction = await node.GetTransactionByIdAsync(request, _wallet.GetHeaders(), DateTime.UtcNow.AddSeconds(15), cancellationToken);
        return MapNativeFeeReceipt(transaction, receipt, id, _genesisBlockId);
    }

    internal static System.Text.Json.JsonElement MapNativeFeeReceipt(Transaction transaction, TransactionInfo receipt, ByteString id, string genesis)
    {
        ValidateSolidifiedReceiptIdentity(receipt, id);
        if (!ValidateTransactionIdentity(transaction, id) || receipt.BlockTimeStamp <= 0 || transaction.RawData.Contract.Count != 1)
            throw new InvalidOperationException("TRON fee receipt lacks exact solidified transaction or block time.");
        var contract = transaction.RawData.Contract[0];
        var owner = contract.Type switch {
            Transaction.Types.Contract.Types.ContractType.TransferContract => contract.Parameter.Unpack<TransferContract>().OwnerAddress,
            Transaction.Types.Contract.Types.ContractType.AccountCreateContract => contract.Parameter.Unpack<AccountCreateContract>().OwnerAddress,
            Transaction.Types.Contract.Types.ContractType.AccountUpdateContract => contract.Parameter.Unpack<AccountUpdateContract>().OwnerAddress,
            Transaction.Types.Contract.Types.ContractType.TriggerSmartContract => contract.Parameter.Unpack<TriggerSmartContract>().OwnerAddress,
            Transaction.Types.Contract.Types.ContractType.CreateSmartContract => contract.Parameter.Unpack<CreateSmartContract>().OwnerAddress,
            _ => throw new InvalidOperationException("Fee payer mapping is not established for this TRON contract type.")
        };
        if (owner.Length != 21 || owner[0] != 0x41) throw new InvalidOperationException("TRON fee payer address is invalid.");
        var energyFee = receipt.Receipt?.EnergyFee ?? 0;
        var netFee = receipt.Receipt?.NetFee ?? 0;
        var energyUsage = receipt.Receipt?.EnergyUsageTotal ?? 0;
        var bandwidthUsage = receipt.Receipt?.NetUsage ?? 0;
        if (receipt.Fee < 0 || energyFee < 0 || netFee < 0 || energyUsage < 0 || bandwidthUsage < 0
            || checked(energyFee + netFee) > receipt.Fee)
            throw new InvalidOperationException("TRON fee components do not establish an exact nonnegative total.");
        var vm = contract.Type == Transaction.Types.Contract.Types.ContractType.TriggerSmartContract || contract.Type == Transaction.Types.Contract.Types.ContractType.CreateSmartContract;
        var status = MapConfirmedTransactionStatus(receipt, id, vm);
        string Units(long value) => value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        return System.Text.Json.JsonSerializer.SerializeToElement(new {
            transactionHash = Convert.ToHexString(id.Span).ToLowerInvariant(), chainGenesisId = genesis.ToLowerInvariant(),
            payer = NBitcoin.DataEncoders.Encoders.Base58Check.EncodeData(owner.ToByteArray()),
            currency = "TRX", unit = "SUN", netUnits = Units(receipt.Fee), success = status == Core.Managers.Bridge.Enums.BridgeTransactionStatus.Completed,
            components = new { energyFee = Units(energyFee), bandwidthFee = Units(netFee), otherFee = Units(receipt.Fee - energyFee - netFee) },
            resources = new { energyUsage = Units(energyUsage), bandwidthUsage = Units(bandwidthUsage) },
            blockNumber = receipt.BlockNumber, timestampMs = receipt.BlockTimeStamp, basis = "solidified-receipt-total"
        });
    }

    internal static Core.Managers.Bridge.Enums.BridgeTransactionStatus MapConfirmedTransactionStatus(
        TransactionInfo receipt, ByteString expectedId, bool contract)
    {
        ValidateSolidifiedReceiptIdentity(receipt, expectedId);
        if (!Enum.IsDefined(receipt.Result)) throw new InvalidOperationException("Unknown native execution result.");
        if ((int)receipt.Result != 0) return Core.Managers.Bridge.Enums.BridgeTransactionStatus.Failed;
        if (contract)
        {
            if (receipt.Receipt == null || !Enum.IsDefined(receipt.Receipt.Result) || (int)receipt.Receipt.Result == 0)
                throw new InvalidOperationException("Contract receipt does not establish execution status.");
            if ((int)receipt.Receipt.Result != 1) return Core.Managers.Bridge.Enums.BridgeTransactionStatus.Failed;
        }
        return Core.Managers.Bridge.Enums.BridgeTransactionStatus.Completed;
    }

    public async Task<System.Collections.Generic.IReadOnlyList<string>> DiscoverNftCollectionsAsync(
        string ownerAddress, System.Collections.Generic.IEnumerable<string> transactionIds,
        CancellationToken cancellationToken = default)
        => (await DiscoverOrderedNftTransfersAsync(ownerAddress, transactionIds, cancellationToken))
            .Select(x => x.Contract).Distinct(StringComparer.Ordinal).OrderBy(x => x, StringComparer.Ordinal).ToArray();

    private async Task<System.Collections.Generic.IReadOnlyList<NativeNftTransfer>> DiscoverOrderedNftTransfersAsync(
        string ownerAddress, System.Collections.Generic.IEnumerable<string> transactionIds, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var owner = _wallet.ParseAddress(ownerAddress);
        var ordered = new System.Collections.Generic.List<(long Block, int Transaction, NativeNftTransfer Transfer)>();
        var blocks = new System.Collections.Generic.Dictionary<long, BlockExtention>();
        await VerifyNetworkAsync(cancellationToken);
        foreach (var hash in transactionIds.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (!System.Text.RegularExpressions.Regex.IsMatch(hash ?? "", "\\A[0-9a-fA-F]{64}\\z"))
                throw new ArgumentException("Indexed history must supply valid transaction IDs.");
            var id = new BytesMessage { Value = ByteString.CopyFrom(Convert.FromHexString(hash)) };
            var solidity = _wallet.GetSolidityProtocol();
            var receipt = await solidity.GetTransactionInfoByIdAsync(id, _wallet.GetHeaders(), DateTime.UtcNow.AddSeconds(15), cancellationToken);
            var transaction = await solidity.GetTransactionByIdAsync(id, _wallet.GetHeaders(), DateTime.UtcNow.AddSeconds(15), cancellationToken);
            if (!ValidateTransactionIdentity(transaction, id.Value))
                throw new InvalidOperationException("Indexed transaction is unavailable from the solidified SDK node.");
            ValidateSolidifiedReceipt(receipt, id.Value, true, false, false);
            var transfers = NftTransfersFromReceipt(receipt, owner);
            if (transfers.Count == 0) continue;
            if (!blocks.TryGetValue(receipt.BlockNumber, out var block))
            {
                block = await solidity.GetBlockByNum2Async(new NumberMessage { Num = receipt.BlockNumber },
                    _wallet.GetHeaders(), DateTime.UtcNow.AddSeconds(15), cancellationToken);
                if (block.BlockHeader?.RawData?.Number != receipt.BlockNumber)
                    throw new InvalidOperationException("Solidified receipt block is unavailable or mismatched.");
                blocks.Add(receipt.BlockNumber, block);
            }
            var position = -1;
            for (var index = 0; index < block.Transactions.Count; index++)
                if (block.Transactions[index].Txid.Equals(id.Value))
                {
                    if (!ValidateTransactionIdentity(block.Transactions[index].Transaction, id.Value))
                        throw new InvalidOperationException("Receipt block transaction identity is missing.");
                    position = index; break;
                }
            if (position < 0) throw new InvalidOperationException("Indexed transaction is absent from its solidified receipt block.");
            foreach (var transfer in transfers) ordered.Add((receipt.BlockNumber, position, transfer));
        }
        return ordered.OrderBy(x => x.Block).ThenBy(x => x.Transaction).ThenBy(x => x.Transfer.LogIndex)
            .Select(x => x.Transfer).ToArray();
    }

    public async Task<System.Collections.Generic.IReadOnlyList<string>> GetIndexedOwnedNftAddressesAsync(string ownerAddress,
        System.Collections.Generic.IEnumerable<string> transactionIds, CancellationToken cancellationToken = default)
    {
        var transfers = await DiscoverOrderedNftTransfersAsync(ownerAddress, transactionIds, cancellationToken);
        var owner = _wallet.ParseAddress(ownerAddress);
        var canonicalOwner = NBitcoin.DataEncoders.Encoders.Base58Check.EncodeData(owner.ToByteArray());
        var finalTransfers = FinalNftTransfers(transfers);
        var addresses = new System.Collections.Generic.List<string>();
        foreach (var collection in finalTransfers.GroupBy(x => x.Contract))
        {
            var candidates = collection.Where(x => x.To == canonicalOwner).ToArray();
            var balanceCall = EncodeAddressCall("balanceOf(address)", owner);
            async Task<System.Numerics.BigInteger> Balance() => (System.Numerics.BigInteger)TronStorageBackend.Decode(
                await QueryContractAsync(ownerAddress, collection.Key, balanceCall, cancellationToken), "uint256")[0].Result;
            var balance = await Balance();
            if (balance != candidates.Length)
                throw new InvalidOperationException("Indexed NFT identities do not cover the current collection balance; no partial inventory is returned.");
            foreach (var candidate in candidates)
            {
                var call = EncodeNftBurn(candidate.TokenId); Convert.FromHexString("6352211e").CopyTo(call, 0);
                var actualOwner = (System.Numerics.BigInteger)TronStorageBackend.Decode(
                    await QueryContractAsync(ownerAddress, collection.Key, call, cancellationToken), "uint256")[0].Result;
                if (actualOwner != new System.Numerics.BigInteger(owner.Span[1..], true, true))
                    throw new InvalidOperationException("Indexed NFT identity is no longer owned by the requested wallet.");
                addresses.Add(candidate.Contract + "/" + candidate.TokenId.ToString(System.Globalization.CultureInfo.InvariantCulture));
            }
            if (await Balance() != balance) throw new InvalidOperationException("NFT collection balance changed while reading inventory.");
        }
        return addresses;
    }

    internal static System.Collections.Generic.IReadOnlyList<string> NftCollectionsFromReceipt(TransactionInfo receipt, ByteString owner)
        => NftTransfersFromReceipt(receipt, owner).Select(transfer => transfer.Contract).Distinct(StringComparer.Ordinal).OrderBy(x => x, StringComparer.Ordinal).ToArray();

    internal sealed record NativeNftTransfer(string Contract, System.Numerics.BigInteger TokenId, string From, string To, int LogIndex);

    internal static NativeNftTransfer[] FinalNftTransfers(System.Collections.Generic.IReadOnlyList<NativeNftTransfer> orderedTransfers)
        => orderedTransfers.GroupBy(x => (x.Contract, x.TokenId)).Select(x => x.Last()).ToArray();

    internal static System.Collections.Generic.IReadOnlyList<NativeNftTransfer> NftTransfersFromReceipt(TransactionInfo receipt, ByteString owner)
    {
        ArgumentNullException.ThrowIfNull(receipt);
        if (owner == null || owner.Length != 21 || owner[0] != 0x41) throw new ArgumentException("A TRON owner address is required.");
        var transferTopic = Convert.FromHexString(new Nethereum.Util.Sha3Keccack().CalculateHash("Transfer(address,address,uint256)"));
        var transfers = new System.Collections.Generic.List<NativeNftTransfer>();
        string Address(ReadOnlySpan<byte> evmAddress)
        {
            var address = new byte[21]; address[0] = 0x41; evmAddress.CopyTo(address.AsSpan(1));
            return NBitcoin.DataEncoders.Encoders.Base58Check.EncodeData(address);
        }
        for (var index = 0; index < receipt.Log.Count; index++)
        {
            var log = receipt.Log[index];
            // TRC20 Transfer has three topics and a data amount. Never classify it as an NFT.
            if (log.Topics.Count != 4 || !log.Topics[0].Span.SequenceEqual(transferTopic)) continue;
            if (log.Address.Length != 20 || log.Data.Length != 0 || log.Topics.Any(topic => topic.Length != 32)
                || log.Topics[1].Span[..12].IndexOfAnyExcept((byte)0) >= 0
                || log.Topics[2].Span[..12].IndexOfAnyExcept((byte)0) >= 0)
                throw new InvalidOperationException("Malformed TRC721 Transfer log in solidified receipt.");
            if (!log.Topics[1].Span[12..].SequenceEqual(owner.Span[1..])
                && !log.Topics[2].Span[12..].SequenceEqual(owner.Span[1..])) continue;
            transfers.Add(new NativeNftTransfer(Address(log.Address.Span),
                new System.Numerics.BigInteger(log.Topics[3].Span, true, true),
                Address(log.Topics[1].Span[12..]), Address(log.Topics[2].Span[12..]), index));
        }
        return transfers;
    }

    internal static bool ValidateTransactionIdentity(Transaction transaction, ByteString expectedId)
    {
        if (expectedId == null || expectedId.Length != 32)
            throw new ArgumentException("A 32-byte transaction ID is required.");
        if (transaction == null || transaction.RawData == null || transaction.RawData.Contract.Count == 0)
            return false;
        if (!expectedId.Equals(ByteString.CopyFrom(SHA256.HashData(transaction.RawData.ToByteArray()))))
            throw new InvalidOperationException("TRON returned a transaction with a different ID.");
        return true;
    }

    public async Task<System.Collections.Generic.IReadOnlyList<Core.Interfaces.Wallet.Responses.IWalletTransaction>> ReadWalletTransactionsAsync(
        string ownerAddress, System.Collections.Generic.IEnumerable<string> transactionIds, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var owner = _wallet.ParseAddress(ownerAddress);
        await VerifyNetworkAsync(cancellationToken);
        var items = new System.Collections.Generic.List<Core.Interfaces.Wallet.Responses.IWalletTransaction>();
        foreach (var hash in transactionIds.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (!System.Text.RegularExpressions.Regex.IsMatch(hash ?? "", "\\A[0-9a-fA-F]{64}\\z")) throw new ArgumentException("Valid native transaction IDs are required.");
            var id = new BytesMessage { Value = ByteString.CopyFrom(Convert.FromHexString(hash)) };
            var transaction = await _wallet.GetSolidityProtocol().GetTransactionByIdAsync(id, _wallet.GetHeaders(), DateTime.UtcNow.AddSeconds(15), cancellationToken);
            if (!ValidateTransactionIdentity(transaction, id.Value)) throw new InvalidOperationException("Indexed transaction is not available from the solidified node.");
            var receipt = await _wallet.GetSolidityProtocol().GetTransactionInfoByIdAsync(id, _wallet.GetHeaders(), DateTime.UtcNow.AddSeconds(15), cancellationToken);
            ValidateSolidifiedReceiptIdentity(receipt, id.Value);
            items.Add(MapWalletTransaction(transaction, receipt, owner));
        }
        return items;
    }

    internal static Core.Interfaces.Wallet.Responses.IWalletTransaction MapWalletTransaction(Transaction transaction, TransactionInfo receipt, ByteString wallet)
    {
        var contract = transaction.RawData.Contract.Single();
        ByteString from, to; long amount;
        switch (contract.Type)
        {
            case Transaction.Types.Contract.Types.ContractType.AccountCreateContract:
                var accountCreation = contract.Parameter.Unpack<AccountCreateContract>();
                from = accountCreation.OwnerAddress; to = accountCreation.AccountAddress; amount = 0; break;
            case Transaction.Types.Contract.Types.ContractType.TransferContract:
                var transfer = contract.Parameter.Unpack<TransferContract>(); from = transfer.OwnerAddress; to = transfer.ToAddress; amount = transfer.Amount; break;
            case Transaction.Types.Contract.Types.ContractType.AccountUpdateContract:
                var update = contract.Parameter.Unpack<AccountUpdateContract>(); from = update.OwnerAddress; to = update.OwnerAddress; amount = 0; break;
            case Transaction.Types.Contract.Types.ContractType.TriggerSmartContract:
                var call = contract.Parameter.Unpack<TriggerSmartContract>(); from = call.OwnerAddress; to = call.ContractAddress; amount = call.CallValue; break;
            case Transaction.Types.Contract.Types.ContractType.CreateSmartContract:
                var create = contract.Parameter.Unpack<CreateSmartContract>(); from = create.OwnerAddress; to = receipt.ContractAddress; amount = create.NewContract.CallValue; break;
            default: throw new InvalidOperationException("Native wallet mapping for " + contract.Type + " is not implemented; no fabricated transaction is returned.");
        }
        string Address(ByteString value) => value.Length == 21 && value[0] == 0x41
            ? NBitcoin.DataEncoders.Encoders.Base58Check.EncodeData(value.ToByteArray()) : throw new InvalidOperationException("Invalid native transaction address.");
        if (amount < 0) throw new InvalidOperationException("Negative native transaction amount.");
        if (!from.Equals(wallet) && !to.Equals(wallet))
            throw new InvalidOperationException("Indexed native transaction does not involve the requested wallet.");
        var contractExecution = contract.Type == Transaction.Types.Contract.Types.ContractType.TriggerSmartContract
            || contract.Type == Transaction.Types.Contract.Types.ContractType.CreateSmartContract;
        if (!Enum.IsDefined(receipt.Result) || (contractExecution && receipt.Receipt != null && !Enum.IsDefined(receipt.Receipt.Result)))
            throw new InvalidOperationException("Unknown native receipt execution result.");
        // Preserve DEFAULT/missing VM results as unknown, never infer successful execution from them.
        var execution = (int)receipt.Result != 0 ? receipt.Result.ToString()
            : contractExecution ? receipt.Receipt?.Result.ToString() ?? "Unknown" : "Success";
        return new Core.Interfaces.Wallet.Response.WalletTransaction
        {
            NativeTransactionId = Convert.ToHexString(receipt.Id.Span).ToLowerInvariant(),
            RawAmount = amount.ToString(System.Globalization.CultureInfo.InvariantCulture), AmountUnit = "SUN", NativeOperation = contract.Type.ToString(),
            NativeExecutionResult = execution, NativeError = receipt.ResMessage.IsEmpty ? null : receipt.ResMessage.ToStringUtf8(),
            FromWalletAddress = Address(from),
            ToWalletAddress = contract.Type == Transaction.Types.Contract.Types.ContractType.CreateSmartContract
                && to.IsEmpty && execution != "Success" ? null : Address(to),
            Amount = (double)(amount / 1000000m),
            CreatedDate = DateTimeOffset.FromUnixTimeMilliseconds(transaction.RawData.Timestamp).UtcDateTime,
            TransactionType = from.Equals(wallet) ? Core.Enums.TransactionType.Debit : Core.Enums.TransactionType.Credit,
            Description = "TRON " + contract.Type + " (" + execution + "; amount is requested value): " + Convert.ToHexString(receipt.Id.Span).ToLowerInvariant()
        };
    }

    internal static decimal NativeBalance(Account account, ByteString requested)
    {
        if (account == null || requested == null || requested.Length != 21 || requested[0] != 0x41
            || !account.Address.Equals(requested) || account.Balance < 0)
            throw new InvalidOperationException("TRON returned a missing, inconsistent or invalid solidified account.");
        return account.Balance / 1000000m;
    }

    public async Task<string> CreateLedgerAccountAsync(string ownerAddress, string newAddress, string privateKey,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!System.Text.RegularExpressions.Regex.IsMatch(privateKey ?? "", "\\A[0-9a-fA-F]{64}\\z"))
            throw new ArgumentException("Account creation requires an explicit 32-byte owner signing key.");
        var owner = _wallet.ParseAddress(ownerAddress); var created = _wallet.ParseAddress(newAddress);
        if (owner.Length != 21 || created.Length != 21 || owner[0] != 0x41 || created[0] != 0x41 || owner.Equals(created))
            throw new ArgumentException("Account creation requires distinct current TRON addresses.");
        if (!_wallet.ParseAddress(new TronECKey(privateKey,TronNetwork.MainNet).GetPublicAddress()).Equals(owner))
            throw new ArgumentException("Account creation signing key does not own the creator.");
        await VerifyNetworkAsync(cancellationToken);
        var node = _wallet.GetProtocol();
        var unsigned = await node.CreateAccount2Async(new AccountCreateContract { OwnerAddress=owner, AccountAddress=created },
            _wallet.GetHeaders(),DateTime.UtcNow.AddSeconds(15),cancellationToken);
        if (unsigned.Result == null || !unsigned.Result.Result || unsigned.Transaction?.RawData == null)
            throw new InvalidOperationException("TRON rejected account creation construction.");
        var contracts = unsigned.Transaction.RawData.Contract;
        if (contracts.Count != 1 || contracts[0].Type != Transaction.Types.Contract.Types.ContractType.AccountCreateContract)
            throw new InvalidOperationException("TRON node changed account creation contract type.");
        var requested = contracts[0].Parameter.Unpack<AccountCreateContract>();
        if (!requested.OwnerAddress.Equals(owner) || !requested.AccountAddress.Equals(created) || (int)requested.Type != 0)
            throw new InvalidOperationException("TRON node changed account creation participants or type.");
        var hash = await SignBroadcastConfirmAsync(unsigned.Transaction,privateKey,"",cancellationToken);
        try
        {
            var account = await _wallet.GetSolidityProtocol().GetAccountAsync(new Account { Address=created },
                _wallet.GetHeaders(),DateTime.UtcNow.AddSeconds(15),cancellationToken);
            if (account == null || !account.Address.Equals(created))
                throw new InvalidOperationException("Created account identity was not established by solidified readback.");
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("TRON account creation " + hash + " committed but readback failed; reconcile before retrying.",ex);
        }
        return hash;
    }

    public async Task<string> SendAsync(string from, string to, decimal amount, string privateKey,
        string memo, CancellationToken cancellationToken = default, string publicKey = null)
    {
        var units = NativeUnits(amount);
        if (string.IsNullOrWhiteSpace(privateKey)) throw new ArgumentException("A real sender signing key is required.");
        if (!string.IsNullOrWhiteSpace(publicKey))
        {
            var actual = Convert.ToHexString(new Nethereum.Signer.EthECKey(privateKey).GetPubKey());
            if (!string.Equals(actual, publicKey.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? publicKey[2..] : publicKey,
                StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("The supplied public key does not match the native signing key.");
        }
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
        return await ExecuteContractAsync(source, contract, data, privateKey, memo, cancellationToken,
            requireTransferReturn: recipient != null, allowVoidContractReturn: mint);
    }

    private async Task<string> ExecuteContractAsync(ByteString source, ByteString contract, byte[] data,
        string privateKey, string memo, CancellationToken cancellationToken,
        bool requireTransferReturn = false, bool allowVoidContractReturn = false,
        System.Numerics.BigInteger? expectedContractAmount = null)
    {
        var node = _wallet.GetProtocol();
        var headers = _wallet.GetHeaders();
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
        raw.FeeLimit = _contractFeeLimitSun; // Explicit caller-approved ceiling, not an asset amount.
        return await SignBroadcastConfirmAsync(unsigned.Transaction, privateKey, memo ?? "", cancellationToken,
            requireContractSuccess: true, requireTransferReturn: requireTransferReturn, allowVoidContractReturn: allowVoidContractReturn,
            expectedContractAmount: expectedContractAmount);
    }

    /// <summary>Queries explicitly ABI-encoded data through the verified full node.</summary>
    public async Task<byte[]> QueryContractAsync(string callerAddress, string contractAddress, byte[] abiData,
        CancellationToken cancellationToken = default)
    {
        ValidateAbiCall(abiData);
        var data = ByteString.CopyFrom(abiData);
        var caller = _wallet.ParseAddress(callerAddress);
        var contract = _wallet.ParseAddress(contractAddress);
        EncodeAddressCall("validate(address,address)", caller, contract);
        await VerifyNetworkAsync(cancellationToken);
        var response = await _wallet.GetProtocol().TriggerConstantContractAsync(new TriggerSmartContract
        {
            OwnerAddress = caller, ContractAddress = contract, Data = data
        }, _wallet.GetHeaders(), DateTime.UtcNow.AddSeconds(15), cancellationToken);
        if (response.Result == null || !response.Result.Result || response.ConstantResult.Count != 1)
            throw new InvalidOperationException("TRON constant query failed or returned an ambiguous ABI result.");
        if (response.Transaction != null && response.Transaction.Ret.Any(ret =>
            (int)ret.ContractRet != 0 && (int)ret.ContractRet != 1)) // Constant calls may omit contractRet; committed receipts still require SUCCESS.
            throw new InvalidOperationException("TRON constant contract execution failed.");
        return response.ConstantResult[0].ToByteArray();
    }

    /// <summary>Signs one explicit ABI call and requires a solidified successful contract receipt.</summary>
    public async Task<string> ExecuteSignedContractAsync(string ownerAddress, string contractAddress, byte[] abiData,
        string privateKey, CancellationToken cancellationToken = default)
    {
        ValidateAbiCall(abiData);
        var data = (byte[])abiData.Clone();
        if (string.IsNullOrWhiteSpace(privateKey)
            || !System.Text.RegularExpressions.Regex.IsMatch(privateKey, "\\A[0-9a-fA-F]{64}\\z"))
            throw new ArgumentException("TRON signing keys require 32-byte hexadecimal encoding.");
        var owner = _wallet.ParseAddress(ownerAddress);
        var contract = _wallet.ParseAddress(contractAddress);
        EncodeAddressCall("validate(address,address)", owner, contract);
        if (!owner.Equals(_wallet.ParseAddress(new TronECKey(privateKey, TronNetwork.MainNet).GetPublicAddress())))
            throw new ArgumentException("Contract signing key does not own the requested caller.");
        await VerifyNetworkAsync(cancellationToken);
        return await ExecuteContractAsync(owner, contract, data, privateKey, "", cancellationToken);
    }

    private static void ValidateAbiCall(byte[] abiData)
    {
        if (abiData == null || abiData.Length < 4 || (abiData.Length - 4) % 32 != 0 || abiData.Length > 1024 * 1024)
            throw new ArgumentException("A bounded canonical ABI selector and encoded argument words are required.");
    }

    /// <summary>Deploys caller-supplied creation bytecode with no ABI registry or implicit constructor arguments.</summary>
    public async Task<(string TransactionId, string ContractAddress)> DeployBytecodeAsync(string ownerAddress,
        byte[] creationBytecode, string privateKey, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (creationBytecode == null || creationBytecode.Length == 0 || creationBytecode.Length > 1024 * 1024)
            throw new ArgumentException("Bounded creation bytecode is required.");
        if (!System.Text.RegularExpressions.Regex.IsMatch(privateKey ?? "", "\\A[0-9a-fA-F]{64}\\z"))
            throw new ArgumentException("TRON signing keys require 32-byte hexadecimal encoding.");
        var owner = _wallet.ParseAddress(ownerAddress);
        if (!owner.Equals(_wallet.ParseAddress(new TronECKey(privateKey, TronNetwork.MainNet).GetPublicAddress())))
            throw new ArgumentException("Deployment signer does not own the requested address.");
        var requested = new CreateSmartContract
        {
            OwnerAddress = owner,
            NewContract = new SmartContract
            {
                OriginAddress = owner, Bytecode = ByteString.CopyFrom(creationBytecode), Name = "OASIS",
                ConsumeUserResourcePercent = 100, OriginEnergyLimit = 10000000
            }
        };
        await VerifyNetworkAsync(cancellationToken);
        var unsigned = await _wallet.GetProtocol().DeployContractAsync(requested, _wallet.GetHeaders(), DateTime.UtcNow.AddSeconds(15), cancellationToken);
        if (unsigned.Result?.Result != true || unsigned.Transaction?.RawData == null)
            throw new InvalidOperationException("TRON node rejected contract deployment construction.");
        var raw = unsigned.Transaction.RawData;
        if (raw.Contract.Count != 1 || raw.Contract[0].Type != Transaction.Types.Contract.Types.ContractType.CreateSmartContract
            || !raw.Contract[0].Parameter.Unpack<CreateSmartContract>().Equals(requested))
            throw new InvalidOperationException("TRON node changed the requested deployment.");
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        if (raw.Timestamp < now - 60000 || raw.Timestamp > now + 60000 || raw.Expiration <= now)
            throw new InvalidOperationException("TRON deployment timestamps are invalid.");
        raw.FeeLimit = _contractFeeLimitSun;
        var hash = await SignBroadcastConfirmAsync(unsigned.Transaction, privateKey, "", cancellationToken, requireContractSuccess: true);
        var receipt = await _wallet.GetSolidityProtocol().GetTransactionInfoByIdAsync(
            new BytesMessage { Value = ByteString.CopyFrom(Convert.FromHexString(hash)) }, _wallet.GetHeaders(), DateTime.UtcNow.AddSeconds(15), cancellationToken);
        ValidateSolidifiedReceipt(receipt, ByteString.CopyFrom(Convert.FromHexString(hash)), true, false, false);
        if (receipt.ContractAddress.Length != 21 || receipt.ContractAddress[0] != 0x41)
            throw new InvalidOperationException("Deployment receipt lacks a native TRON contract address.");
        return (hash, NBitcoin.DataEncoders.Encoders.Base58Check.EncodeData(receipt.ContractAddress.ToByteArray()));
    }

    public async Task<string> UnlockEscrowAsync(string owner, string escrowAddress, string tokenAddress,
        string privateKey, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(privateKey)
            || !System.Text.RegularExpressions.Regex.IsMatch(privateKey, "\\A[0-9a-fA-F]{64}\\z"))
            throw new ArgumentException("TRON signing keys require 32-byte hexadecimal encoding.");
        var source = _wallet.ParseAddress(owner);
        if (!source.Equals(_wallet.ParseAddress(new TronECKey(privateKey, TronNetwork.MainNet).GetPublicAddress())))
            throw new ArgumentException("Escrow signing key does not own the requested depositor.");
        var escrow = _wallet.ParseAddress(escrowAddress);
        var token = _wallet.ParseAddress(tokenAddress);
        var query = EncodeAddressCall("locked(address,address)", token, source);
        var call = EncodeAddressCall("unlock(address)", token);
        if (escrow.Length != 21 || escrow[0] != 0x41)
            throw new ArgumentException("A current TRON escrow contract is required.");
        await VerifyNetworkAsync(cancellationToken);
        var deposit = await _wallet.GetProtocol().TriggerConstantContractAsync(new TriggerSmartContract
        {
            OwnerAddress = source, ContractAddress = escrow, Data = ByteString.CopyFrom(query)
        }, _wallet.GetHeaders(), DateTime.UtcNow.AddSeconds(15), cancellationToken);
        if (deposit.Result == null || !deposit.Result.Result || deposit.ConstantResult.Count != 1
            || deposit.ConstantResult[0].Length != 32)
            throw new InvalidOperationException("Escrow deposit query did not return one ABI uint256.");
        var amount = new System.Numerics.BigInteger(deposit.ConstantResult[0].ToByteArray(), isUnsigned: true, isBigEndian: true);
        if (amount <= 0) throw new InvalidOperationException("The requested signer has no owned escrow deposit.");
        return await ExecuteContractAsync(source, escrow, call, privateKey, "", cancellationToken,
            expectedContractAmount: amount);
    }

    public Task<string> SendNftAsync(string from, string to, string tokenAddress,
        System.Numerics.BigInteger tokenId, string privateKey, string memo = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(to))
            throw new ArgumentException("NFT transfers require an explicit recipient.", nameof(to));
        return ExecuteNftAsync(from, to, tokenAddress, tokenId, privateKey, memo, cancellationToken);
    }

    public Task<string> BurnNftAsync(string tokenAddress, System.Numerics.BigInteger tokenId,
        string privateKey, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(privateKey)
            || !System.Text.RegularExpressions.Regex.IsMatch(privateKey, "\\A[0-9a-fA-F]{64}\\z"))
            throw new ArgumentException("TRON signing keys require 32-byte hexadecimal encoding.");
        var owner = new TronECKey(privateKey, TronNetwork.MainNet).GetPublicAddress();
        return ExecuteNftAsync(owner, null, tokenAddress, tokenId, privateKey, null, cancellationToken);
    }

    /// <summary>Calls the explicitly selected mint(address,uint256,string) collection extension.</summary>
    public async Task<string> MintNftAsync(string owner, string recipient, string contractAddress,
        System.Numerics.BigInteger tokenId, string metadataUri, string privateKey, string memo,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(privateKey)
            || !System.Text.RegularExpressions.Regex.IsMatch(privateKey, "\\A[0-9a-fA-F]{64}\\z"))
            throw new ArgumentException("TRON signing keys require 32-byte hexadecimal encoding.");
        var source = _wallet.ParseAddress(owner);
        if (!source.Equals(_wallet.ParseAddress(new TronECKey(privateKey, TronNetwork.MainNet).GetPublicAddress())))
            throw new ArgumentException("NFT signing key does not own the requested minter.");
        var contract = _wallet.ParseAddress(contractAddress);
        EncodeAddressCall("ownerOf(uint256)", contract);
        var data = EncodeNftMint(_wallet.ParseAddress(recipient), tokenId, metadataUri);
        await VerifyNetworkAsync(cancellationToken);
        return await ExecuteContractAsync(source, contract, data, privateKey, memo, cancellationToken);
    }

    internal static byte[] EncodeNftMint(ByteString recipient, System.Numerics.BigInteger tokenId, string metadataUri)
    {
        if (tokenId < 0 || tokenId >= (System.Numerics.BigInteger.One << 256))
            throw new ArgumentOutOfRangeException(nameof(tokenId));
        if (string.IsNullOrWhiteSpace(metadataUri)) throw new ArgumentException("Metadata URI is required.");
        var uri = new System.Text.UTF8Encoding(false, true).GetBytes(metadataUri);
        if (uri.Length > 8192) throw new ArgumentException("Metadata URI exceeds the 8192-byte limit.");
        var address = EncodeAddressCall("mint(address,uint256,string)", recipient);
        if (recipient.ToByteArray().AsSpan(1).IndexOfAnyExcept((byte)0) < 0)
            throw new ArgumentException("NFT mint recipient must not be the zero address.");
        var data = new byte[132 + ((uri.Length + 31) / 32) * 32];
        address.CopyTo(data, 0);
        var id = tokenId.ToByteArray(isUnsigned: true, isBigEndian: true);
        id.CopyTo(data, 68 - id.Length);
        data[99] = 96; // Dynamic string offset from the start of ABI arguments.
        var length = new System.Numerics.BigInteger(uri.Length).ToByteArray(isUnsigned: true, isBigEndian: true);
        length.CopyTo(data, 132 - length.Length);
        uri.CopyTo(data, 132);
        return data;
    }

    private async Task<string> ExecuteNftAsync(string from, string to, string tokenAddress,
        System.Numerics.BigInteger tokenId, string privateKey, string memo, CancellationToken cancellationToken)
    {
        if (tokenId < 0 || tokenId >= (System.Numerics.BigInteger.One << 256))
            throw new ArgumentOutOfRangeException(nameof(tokenId));
        if (string.IsNullOrWhiteSpace(privateKey)
            || !System.Text.RegularExpressions.Regex.IsMatch(privateKey, "\\A[0-9a-fA-F]{64}\\z"))
            throw new ArgumentException("TRON signing keys require 32-byte hexadecimal encoding.");
        var source = _wallet.ParseAddress(from);
        if (!source.Equals(_wallet.ParseAddress(new TronECKey(privateKey, TronNetwork.MainNet).GetPublicAddress())))
            throw new ArgumentException("NFT signing key does not own the requested sender.");
        var recipient = to == null ? null : _wallet.ParseAddress(to);
        var contract = _wallet.ParseAddress(tokenAddress);
        EncodeAddressCall("ownerOf(uint256)", contract); // Validate contract address before node access.
        var data = recipient == null ? EncodeNftBurn(tokenId) : EncodeNftTransfer(source, recipient, tokenId);
        await VerifyNetworkAsync(cancellationToken);
        var query = new byte[36];
        Convert.FromHexString("6352211e").CopyTo(query, 0);
        var encodedId = tokenId.ToByteArray(isUnsigned: true, isBigEndian: true);
        encodedId.CopyTo(query, query.Length - encodedId.Length);
        var owner = await ReadContractUintAsync(source, contract, query, cancellationToken);
        var expected = new System.Numerics.BigInteger(source.ToByteArray().AsSpan(1), isUnsigned: true, isBigEndian: true);
        if (owner != expected) throw new InvalidOperationException("The requested sender does not own the on-chain NFT token ID.");
        return await ExecuteContractAsync(source, contract, data, privateKey, memo, cancellationToken);
    }

    internal static byte[] EncodeNftBurn(System.Numerics.BigInteger tokenId)
    {
        if (tokenId < 0 || tokenId >= (System.Numerics.BigInteger.One << 256))
            throw new ArgumentOutOfRangeException(nameof(tokenId));
        var data = new byte[36];
        Convert.FromHexString("42966c68").CopyTo(data, 0);
        var id = tokenId.ToByteArray(isUnsigned: true, isBigEndian: true);
        id.CopyTo(data, data.Length - id.Length);
        return data;
    }

    public async Task<string> GetNftMetadataUriAsync(string contractAddress, System.Numerics.BigInteger tokenId,
        CancellationToken cancellationToken = default)
    {
        var ownerCall = EncodeNftBurn(tokenId);
        Convert.FromHexString("6352211e").CopyTo(ownerCall, 0); // ownerOf(uint256)
        var ownerBytes = await QueryContractAsync(contractAddress, contractAddress, ownerCall, cancellationToken);
        var owner = (System.Numerics.BigInteger)TronStorageBackend.Decode(ownerBytes, "uint256")[0].Result;
        if (owner <= 0 || owner >= (System.Numerics.BigInteger.One << 160))
            throw new InvalidOperationException("NFT ownerOf did not return a nonzero ABI address.");
        var uriCall = EncodeNftBurn(tokenId);
        Convert.FromHexString("c87b56dd").CopyTo(uriCall, 0); // tokenURI(uint256)
        var uriBytes = await QueryContractAsync(contractAddress, contractAddress, uriCall, cancellationToken);
        return (string)TronStorageBackend.Decode(uriBytes, "string")[0].Result;
    }

    public async Task<System.Collections.Generic.IReadOnlyList<System.Numerics.BigInteger>> GetNftIdsForOwnerAsync(
        string contractAddress, string ownerAddress, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var owner = _wallet.ParseAddress(ownerAddress);
        var balanceCall = EncodeAddressCall("balanceOf(address)", owner);
        async Task<System.Numerics.BigInteger> Balance() => (System.Numerics.BigInteger)TronStorageBackend.Decode(
            await QueryContractAsync(ownerAddress, contractAddress, balanceCall, cancellationToken), "uint256")[0].Result;
        var count = await Balance();
        if (count > 10000) throw new InvalidOperationException("Collection inventory exceeds the explicit 10000-item query budget; no truncated inventory is returned.");
        var ids = new System.Collections.Generic.List<System.Numerics.BigInteger>();
        var unique = new System.Collections.Generic.HashSet<System.Numerics.BigInteger>();
        for (var index = System.Numerics.BigInteger.Zero; index < count; index++)
        {
            var output = await QueryContractAsync(ownerAddress, contractAddress, EncodeNftOwnerIndex(owner, index), cancellationToken);
            var id = (System.Numerics.BigInteger)TronStorageBackend.Decode(output, "uint256")[0].Result;
            if (!unique.Add(id)) throw new InvalidOperationException("NFT enumeration returned duplicate native token IDs.");
            var ownershipCall = EncodeNftBurn(id);
            Convert.FromHexString("6352211e").CopyTo(ownershipCall, 0);
            var actualOwner = (System.Numerics.BigInteger)TronStorageBackend.Decode(
                await QueryContractAsync(ownerAddress, contractAddress, ownershipCall, cancellationToken), "uint256")[0].Result;
            var expectedOwner = new System.Numerics.BigInteger(owner.ToByteArray().AsSpan(1), true, true);
            if (actualOwner != expectedOwner) throw new InvalidOperationException("NFT enumeration returned a token not owned by the requested address.");
            ids.Add(id);
        }
        if (await Balance() != count) throw new InvalidOperationException("NFT ownership changed during enumeration.");
        return ids;
    }

    internal static byte[] EncodeNftOwnerIndex(ByteString owner, System.Numerics.BigInteger index)
    {
        if (index < 0 || index >= (System.Numerics.BigInteger.One << 256)) throw new ArgumentOutOfRangeException(nameof(index));
        var data = new byte[68];
        EncodeAddressCall("tokenOfOwnerByIndex(address,uint256)", owner).CopyTo(data, 0);
        var encoded = index.ToByteArray(true, true);
        encoded.CopyTo(data, 68 - encoded.Length);
        return data;
    }

    internal static byte[] EncodeNftEscrow(string signature, ByteString contract, System.Numerics.BigInteger tokenId)
    {
        if (signature != "lockNFT(address,uint256)" && signature != "unlockNFT(address,uint256)")
            throw new ArgumentException("An explicit NFT escrow operation is required.");
        if (tokenId < 0 || tokenId >= (System.Numerics.BigInteger.One << 256))
            throw new ArgumentOutOfRangeException(nameof(tokenId));
        var data = new byte[68];
        EncodeAddressCall(signature, contract).CopyTo(data, 0);
        var id = tokenId.ToByteArray(true, true);
        id.CopyTo(data, data.Length - id.Length);
        return data;
    }

    public Task<string> ExecuteNftEscrowAsync(string owner, string escrow, string nftAddress,
        string privateKey, bool unlock, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var asset = ParseNftAddress(nftAddress);
        var call = EncodeNftEscrow(unlock ? "unlockNFT(address,uint256)" : "lockNFT(address,uint256)",
            _wallet.ParseAddress(asset.Contract), asset.TokenId);
        return ExecuteSignedContractAsync(owner, escrow, call, privateKey, cancellationToken);
    }

    internal static (string Contract, System.Numerics.BigInteger TokenId) ParseNftAddress(string address)
    {
        var parts = address?.Split('/');
        if (parts == null || parts.Length != 2 || string.IsNullOrWhiteSpace(parts[0])
            || !System.Text.RegularExpressions.Regex.IsMatch(parts[1], "\\A[0-9]+\\z")
            || !System.Numerics.BigInteger.TryParse(parts[1], out var id) || id >= (System.Numerics.BigInteger.One << 256))
            throw new ArgumentException("TRC721 native NFT address must be contract/unsigned-decimal-token-id.");
        return (parts[0], id);
    }

    internal static byte[] EncodeNftTransfer(ByteString from, ByteString to, System.Numerics.BigInteger tokenId)
    {
        if (tokenId < 0 || tokenId >= (System.Numerics.BigInteger.One << 256))
            throw new ArgumentOutOfRangeException(nameof(tokenId));
        var addresses = EncodeAddressCall("safeTransferFrom(address,address,uint256)", from, to);
        var data = new byte[100];
        addresses.CopyTo(data, 0);
        var id = tokenId.ToByteArray(isUnsigned: true, isBigEndian: true);
        id.CopyTo(data, data.Length - id.Length);
        return data;
    }

    public async Task<string> LockEscrowAsync(string owner, string escrowAddress, string tokenAddress,
        string privateKey, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(privateKey)
            || !System.Text.RegularExpressions.Regex.IsMatch(privateKey, "\\A[0-9a-fA-F]{64}\\z"))
            throw new ArgumentException("TRON signing keys require 32-byte hexadecimal encoding.");
        var source = _wallet.ParseAddress(owner);
        if (!source.Equals(_wallet.ParseAddress(new TronECKey(privateKey, TronNetwork.MainNet).GetPublicAddress())))
            throw new ArgumentException("Escrow signing key does not own the requested depositor.");
        var escrow = _wallet.ParseAddress(escrowAddress);
        var token = _wallet.ParseAddress(tokenAddress);
        var call = EncodeAddressCall("lockAll(address)", token);
        EncodeAddressCall("allowance(address,address)", source, escrow); // Validate all addresses before node access.
        await VerifyNetworkAsync(cancellationToken);
        var amount = await ReadContractUintAsync(source, token, EncodeAddressCall("balanceOf(address)", source), cancellationToken);
        if (amount <= 0) throw new InvalidOperationException("The requested signer has no spendable tokens to escrow.");
        var allowance = await ReadContractUintAsync(source, token,
            EncodeAddressCall("allowance(address,address)", source, escrow), cancellationToken);
        if (allowance != 0 && allowance != amount)
            throw new InvalidOperationException("Revoke the existing escrow allowance explicitly before locking a different amount.");
        string approvalId = null;
        if (allowance == 0)
            approvalId = await ApproveTokenAsync(owner, tokenAddress, escrowAddress, amount, privateKey, cancellationToken);
        try
        {
            return await ExecuteContractAsync(source, escrow, call, privateKey, "", cancellationToken,
                expectedContractAmount: amount);
        }
        catch (Exception error) when (approvalId != null)
        {
            throw new InvalidOperationException($"Escrow deposit failed after confirmed approval {approvalId}: {error.Message} Reconcile the original transactions and remaining allowance before retrying.", error);
        }
    }

    private async Task<System.Numerics.BigInteger> ReadContractUintAsync(ByteString source, ByteString contract,
        byte[] data, CancellationToken cancellationToken)
    {
        var response = await _wallet.GetProtocol().TriggerConstantContractAsync(new TriggerSmartContract
        {
            OwnerAddress = source, ContractAddress = contract, Data = ByteString.CopyFrom(data)
        }, _wallet.GetHeaders(), DateTime.UtcNow.AddSeconds(15), cancellationToken);
        if (response.Result == null || !response.Result.Result || response.ConstantResult.Count != 1
            || response.ConstantResult[0].Length != 32)
            throw new InvalidOperationException("Contract query did not return one ABI uint256.");
        return new System.Numerics.BigInteger(response.ConstantResult[0].ToByteArray(), isUnsigned: true, isBigEndian: true);
    }

    internal static byte[] EncodeAddressCall(string signature, params ByteString[] addresses)
    {
        var selector = new Nethereum.Util.Sha3Keccack().CalculateHash(signature)[..8];
        var data = new byte[4 + 32 * addresses.Length];
        Convert.FromHexString(selector).CopyTo(data, 0);
        for (var i = 0; i < addresses.Length; i++)
        {
            var address = addresses[i];
            if (address == null || address.Length != 21 || address[0] != 0x41)
                throw new ArgumentException("A current 21-byte TRON contract argument address is required.");
            address.ToByteArray().AsSpan(1).CopyTo(data.AsSpan(16 + 32 * i, 20));
        }
        return data;
    }

    internal static void ValidateContractAmount(TransactionInfo receipt, System.Numerics.BigInteger expected)
    {
        if (expected <= 0 || receipt.ContractResult.Count != 1 || receipt.ContractResult[0].Length != 32
            || new System.Numerics.BigInteger(receipt.ContractResult[0].ToByteArray(), isUnsigned: true, isBigEndian: true) != expected)
            throw new InvalidOperationException("Escrow confirmation did not return the expected deposited amount.");
    }

    public async Task<string> ApproveTokenAsync(string owner, string tokenAddress, string spender,
        System.Numerics.BigInteger rawUnits, string privateKey, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(privateKey)
            || !System.Text.RegularExpressions.Regex.IsMatch(privateKey, "\\A[0-9a-fA-F]{64}\\z"))
            throw new ArgumentException("TRON signing keys require 32-byte hexadecimal encoding.");
        var signer = new TronECKey(privateKey, TronNetwork.MainNet);
        var source = _wallet.ParseAddress(owner);
        if (!source.Equals(_wallet.ParseAddress(signer.GetPublicAddress())))
            throw new ArgumentException("Approval signing key does not own the requested token holder.");
        var contract = _wallet.ParseAddress(tokenAddress);
        if (contract.Length != 21 || contract[0] != 0x41)
            throw new ArgumentException("A current TRON token contract is required.");
        var data = EncodeApproval(_wallet.ParseAddress(spender), rawUnits);
        await VerifyNetworkAsync(cancellationToken);
        return await ExecuteContractAsync(source, contract, data, privateKey, "", cancellationToken,
            requireTransferReturn: true);
    }

    internal static byte[] EncodeApproval(ByteString spender, System.Numerics.BigInteger rawUnits)
    {
        if (rawUnits < 0 || rawUnits >= (System.Numerics.BigInteger.One << 256))
            throw new ArgumentOutOfRangeException(nameof(rawUnits));
        if (spender == null || spender.Length != 21 || spender[0] != 0x41)
            throw new ArgumentException("A current TRON spender address is required.");
        var data = new byte[68];
        Convert.FromHexString("095ea7b3").CopyTo(data, 0);
        spender.ToByteArray().AsSpan(1).CopyTo(data.AsSpan(16, 20));
        var units = rawUnits.ToByteArray(isUnsigned: true, isBigEndian: true);
        units.CopyTo(data, data.Length - units.Length);
        return data;
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
        bool allowVoidContractReturn = false, System.Numerics.BigInteger? expectedContractAmount = null)
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
                    ValidateSolidifiedReceipt(receipt, id.Value, requireContractSuccess, requireTransferReturn, allowVoidContractReturn);
                    if (expectedContractAmount.HasValue) ValidateContractAmount(receipt, expectedContractAmount.Value);
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
            throw new InvalidOperationException($"TRON confirmation failed for broadcast {transactionId}: {error.Message} Track this original transaction before any retry.", error);
        }
    }

    internal static void ValidateSolidifiedReceipt(TransactionInfo receipt, ByteString expectedId,
        bool requireContractSuccess, bool requireTransferReturn, bool allowVoidContractReturn)
    {
        ValidateSolidifiedReceiptIdentity(receipt, expectedId);
        if ((int)receipt.Result != 0)
            throw new InvalidOperationException("TRON transaction failed in the solidified chain.");
        if (requireContractSuccess && (receipt.Receipt == null || (int)receipt.Receipt.Result != 1))
            throw new InvalidOperationException("TRON contract execution did not report SUCCESS.");
        if (requireTransferReturn && (!allowVoidContractReturn || receipt.ContractResult.Count != 0))
            ValidateTrc20TransferReturn(receipt);
    }

    internal static void ValidateSolidifiedReceiptIdentity(TransactionInfo receipt, ByteString expectedId)
    {
        if (receipt == null || expectedId == null || expectedId.Length != 32
            || !receipt.Id.Equals(expectedId) || receipt.BlockNumber <= 0)
            throw new InvalidOperationException("TRON returned an inconsistent solidified receipt.");
    }
}
