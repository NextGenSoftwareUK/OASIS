using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using NBitcoin;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.BitcoinOASIS;

public partial class BitcoinOASIS : IOASISIdBoundTokenEscrowProvider
{
    private readonly HolonManager _bitcoinEscrowReceipts;
    private HolonManager BitcoinEscrowReceipts => _bitcoinEscrowReceipts ?? HolonManager.Instance;

    private const string BitcoinEscrowRecordType = "OASIS.BitcoinEscrow.v1";

    private sealed record BitcoinEscrowReceipt(Guid LockId, Guid AssetId, Guid AvatarId,
        string Source, long Satoshis, string SignedTransaction);

    public OASISResult<ITransactionResponse> LockToken(ILockWeb3TokenRequest request) =>
        LockTokenAsync(request).GetAwaiter().GetResult();

    public async Task<OASISResult<ITransactionResponse>> LockTokenAsync(ILockWeb3TokenRequest request)
    {
        try
        {
            if (request == null)
                throw new ArgumentException("A Bitcoin escrow lock request is required.");

            var (authority, receiptActor, pool, poolSigner) = ReadBitcoinEscrowConfiguration(request.LockId,
                request.Web3TokenId, request.LockedByAvatarId, request.TokenAddress, request.EscrowContractAddress);
            await VerifyBitcoinEscrowNetworkAsync();
            var satoshis = request.Amount * 100_000_000m;
            if (satoshis <= 0 || satoshis > 2_100_000_000_000_000m || satoshis != decimal.Truncate(satoshis))
                throw new ArgumentException("The lock amount must be a positive exact number of satoshis.");

            var recordId = BitcoinEscrowRecordId(pool, request.LockId, "deposit");
            var previous = await ReadBitcoinEscrowReceiptAsync(recordId, receiptActor, authority, poolSigner);
            if (previous != null)
            {
                ValidateBitcoinEscrowReceipt(previous, request.LockId, request.Web3TokenId, request.LockedByAvatarId);
                if (previous.Satoshis != (long)satoshis ||
                    (!string.IsNullOrWhiteSpace(request.FromWalletAddress) && previous.Source != request.FromWalletAddress))
                    throw new InvalidOperationException("This lock ID is already bound to a different Bitcoin deposit.");
                return await PublishBitcoinEscrowReceiptAsync(previous);
            }

            var wallet = await WalletManager.GetAvatarDefaultWalletByIdAsync(request.LockedByAvatarId,
                ProviderType.Value, showPrivateKeys: true);
            if (wallet.IsError || string.IsNullOrWhiteSpace(wallet.Result?.WalletAddress))
                throw new InvalidOperationException("The locking avatar has no Bitcoin wallet.");

            var source = BitcoinAddress.Create(wallet.Result.WalletAddress, SigningNetwork);
            if (!string.IsNullOrWhiteSpace(request.FromWalletAddress) &&
                !source.Equals(BitcoinAddress.Create(request.FromWalletAddress, SigningNetwork)))
                throw new InvalidOperationException("Only the locking avatar's configured Bitcoin wallet can fund this deposit.");

            var privateKey = string.IsNullOrWhiteSpace(request.FromWalletPrivateKey)
                ? wallet.Result.PrivateKey : request.FromWalletPrivateKey;
            if (string.IsNullOrWhiteSpace(privateKey))
                throw new InvalidOperationException("The locking avatar has no Bitcoin signing key.");

            var signer = new BitcoinSecret(privateKey, SigningNetwork);
            if (!BitcoinEscrowSignerOwnsAddress(signer, source) || source.Equals(pool))
                throw new InvalidOperationException("The Bitcoin funding signer must own the avatar wallet and be distinct from the escrow pool.");

            using var escrowKey = DeriveBitcoinEscrowKey(poolSigner, recordId);
            var escrowAddress = escrowKey.PubKey.GetAddress(ScriptPubKeyType.Segwit, SigningNetwork);
            using var ord = CreateOrdClient();
            var tip = await ReadSharedOrdinalTipAsync(ord);
            var feeRate = await ReadBitcoinPaymentFeeRateAsync();
            var deposit = await CreateSignedCardinalPaymentAsync(ord, signer, source, feeRate,
                new TxOut(Money.Satoshis((long)satoshis), escrowAddress.ScriptPubKey));
            if (deposit == null || deposit.Outputs[0].IsDust())
                throw new InvalidOperationException("The avatar has insufficient confirmed cardinal BTC or the deposit would be dust.");
            if (await ReadSharedOrdinalTipAsync(ord) != tip)
                throw new InvalidOperationException("The indexed Bitcoin chain changed while preparing the deposit.");

            var receipt = new BitcoinEscrowReceipt(request.LockId, request.Web3TokenId, request.LockedByAvatarId,
                source.ToString(), (long)satoshis, deposit.ToHex());
            await ReserveBitcoinEscrowReceiptAsync(recordId, receipt, receiptActor, authority, poolSigner);
            return await PublishBitcoinEscrowReceiptAsync(receipt);
        }
        catch (Exception ex)
        {
            return BitcoinEscrowError(ex);
        }
    }

    public OASISResult<ITransactionResponse> UnlockToken(IUnlockWeb3TokenRequest request) =>
        UnlockTokenAsync(request).GetAwaiter().GetResult();

    public async Task<OASISResult<ITransactionResponse>> UnlockTokenAsync(IUnlockWeb3TokenRequest request)
    {
        try
        {
            if (request == null)
                throw new ArgumentException("A Bitcoin escrow unlock request is required.");

            var (authority, receiptActor, pool, poolSigner) = ReadBitcoinEscrowConfiguration(request.LockId,
                request.Web3TokenId, request.UnlockedByAvatarId, request.TokenAddress, request.EscrowContractAddress);
            await VerifyBitcoinEscrowNetworkAsync();
            var depositId = BitcoinEscrowRecordId(pool, request.LockId, "deposit");
            var releaseId = BitcoinEscrowRecordId(pool, request.LockId, "release");
            var deposit = await ReadBitcoinEscrowReceiptAsync(depositId, receiptActor, authority, poolSigner);
            if (deposit == null)
                return new OASISResult<ITransactionResponse> { IsError = true, ErrorCode = OASISResultErrorCodes.RecordNotFound,
                    Message = "No matching Bitcoin escrow deposit was found." };

            ValidateBitcoinEscrowReceipt(deposit, request.LockId, request.Web3TokenId, request.UnlockedByAvatarId);
            var release = await ReadBitcoinEscrowReceiptAsync(releaseId, receiptActor, authority, poolSigner);
            if (release != null)
            {
                ValidateBitcoinEscrowReceipt(release, request.LockId, request.Web3TokenId, request.UnlockedByAvatarId);
                return await PublishBitcoinEscrowReceiptAsync(release);
            }

            var transaction = Transaction.Parse(deposit.SignedTransaction, SigningNetwork);
            var confirmed = await GetTransactionStatusAsync(transaction.GetHash().ToString());
            if (confirmed.IsError || confirmed.Result != BridgeTransactionStatus.Completed)
                throw new InvalidOperationException("The exact Bitcoin escrow deposit must be confirmed before release.");

            using var escrowKey = DeriveBitcoinEscrowKey(poolSigner, depositId);
            var escrowSigner = escrowKey.GetWif(SigningNetwork);
            var escrowAddress = escrowSigner.GetAddress(ScriptPubKeyType.Segwit);
            using var ord = CreateOrdClient();
            var tip = await ReadSharedOrdinalTipAsync(ord);
            var lockedCoin = await ReadOrdinalCoinAsync(ord, new OutPoint(transaction.GetHash(), 0),
                escrowAddress, escrowSigner, wrappedSegwit: false, inscriptionId: null);
            if (lockedCoin == null || lockedCoin.Amount.Satoshi != deposit.Satoshis)
                throw new InvalidOperationException("The exact Bitcoin escrow output is unavailable, spent or differs from its deposit receipt.");

            var destination = BitcoinAddress.Create(deposit.Source, SigningNetwork);
            var feeRate = await ReadBitcoinPaymentFeeRateAsync();
            var payment = await CreateSignedCardinalPaymentAsync(ord, poolSigner, pool, feeRate, lockedCoin,
                escrowKey, new TxOut(lockedCoin.Amount, destination.ScriptPubKey));
            if (payment == null)
                throw new InvalidOperationException("The Bitcoin escrow pool lacks confirmed cardinal BTC for the release fee.");
            if (await ReadSharedOrdinalTipAsync(ord) != tip)
                throw new InvalidOperationException("The indexed Bitcoin chain changed while preparing the release.");

            release = deposit with { SignedTransaction = payment.ToHex() };
            await ReserveBitcoinEscrowReceiptAsync(releaseId, release, receiptActor, authority, poolSigner);
            return await PublishBitcoinEscrowReceiptAsync(release);
        }
        catch (Exception ex)
        {
            return BitcoinEscrowError(ex);
        }
    }

    private (ProviderType Authority, Guid ReceiptActor, BitcoinAddress Pool, BitcoinSecret Signer) ReadBitcoinEscrowConfiguration(
        Guid lockId, Guid assetId, Guid avatarId, string token, string requestedPool)
    {
        if (!_isActivated)
            throw new InvalidOperationException("Bitcoin provider is not activated.");
        if (lockId == Guid.Empty || assetId == Guid.Empty || avatarId == Guid.Empty)
            throw new ArgumentException("Bitcoin escrow requires an explicit lock ID, asset ID and avatar.");
        if (!string.Equals(token, "BTC", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Bitcoin escrow currently requires the BTC asset.");

        var pool = ReadOrdinalCustodyPool();
        if (!string.Equals(requestedPool, pool.ToString(), StringComparison.Ordinal))
            throw new ArgumentException("The request must explicitly select the configured Bitcoin escrow pool.");

        var configured = Environment.GetEnvironmentVariable("OASIS_BITCOIN_ESCROW_RECEIPT_PROVIDER");
        if (!Enum.TryParse(configured, out ProviderType authority) || !Enum.IsDefined(authority) ||
            authority is Core.Enums.ProviderType.Default or Core.Enums.ProviderType.BitcoinOASIS)
            throw new InvalidOperationException("Configure a concrete non-Bitcoin conditional storage authority for escrow receipts.");

        if (!Guid.TryParse(Environment.GetEnvironmentVariable("OASIS_BITCOIN_ESCROW_RECEIPT_ACTOR"), out var receiptActor) ||
            receiptActor == Guid.Empty || receiptActor == avatarId)
            throw new InvalidOperationException("Configure a dedicated Bitcoin escrow receipt service avatar.");

        return (authority, receiptActor, pool, new BitcoinSecret(_privateKey, SigningNetwork));
    }

    private async Task VerifyBitcoinEscrowNetworkAsync()
    {
        var genesis = (await _httpClient.GetStringAsync("block-height/0")).Trim();
        if (!uint256.TryParse(genesis, out var hash) || hash != SigningNetwork.GetGenesis().GetHash())
            throw new InvalidOperationException("Esplora is serving a different Bitcoin escrow network.");
    }

    private static bool BitcoinEscrowSignerOwnsAddress(BitcoinSecret signer, BitcoinAddress address) =>
        address.Equals(signer.GetAddress(ScriptPubKeyType.Segwit)) ||
        address.Equals(signer.GetAddress(ScriptPubKeyType.SegwitP2SH)) ||
        address.Equals(signer.GetAddress(ScriptPubKeyType.TaprootBIP86)) ||
        address.Equals(signer.GetAddress(ScriptPubKeyType.Legacy));

    private Guid BitcoinEscrowRecordId(BitcoinAddress pool, Guid lockId, string phase) =>
        new Guid(SHA256.HashData(Encoding.UTF8.GetBytes($"{BitcoinEscrowRecordType}:{SigningNetwork.GetGenesis().GetHash()}:{pool}:{lockId:D}:{phase}"))
            .AsSpan(0, 16));

    private static Key DeriveBitcoinEscrowKey(BitcoinSecret signer, Guid depositId) =>
        new Key(HMACSHA256.HashData(signer.PrivateKey.ToBytes(), Encoding.UTF8.GetBytes($"{BitcoinEscrowRecordType}:output:{depositId:D}")));

    private static byte[] AuthenticateBitcoinEscrowReceipt(BitcoinSecret signer, Guid recordId, string json) =>
        HMACSHA256.HashData(signer.PrivateKey.ToBytes(), Encoding.UTF8.GetBytes($"{BitcoinEscrowRecordType}:receipt:{recordId:D}:{json}"));

    private async Task<BitcoinEscrowReceipt> ReadBitcoinEscrowReceiptAsync(Guid id, Guid receiptActor,
        ProviderType authority, BitcoinSecret signer)
    {
        var loaded = await BitcoinEscrowReceipts.LoadHolonForWriteAsync(id, receiptActor, authority);
        if (loaded.ErrorCode == OASISResultErrorCodes.RecordNotFound)
            return null;
        if (loaded.IsError || loaded.Result?.Record == null)
            throw new InvalidOperationException("The Bitcoin escrow receipt authority could not be read.");

        var record = loaded.Result.Record;
        if (record.CreatedByAvatarId != receiptActor || record.CustomHolonType != BitcoinEscrowRecordType ||
            record.MetaData == null || !record.MetaData.TryGetValue("receipt", out var payload) ||
            !record.MetaData.TryGetValue("authentication", out var authentication))
            throw new InvalidOperationException("The Bitcoin escrow receipt does not belong to this avatar.");

        var json = payload.ToString();
        var actual = Convert.FromHexString(authentication.ToString());
        if (!CryptographicOperations.FixedTimeEquals(actual, AuthenticateBitcoinEscrowReceipt(signer, id, json)))
            throw new InvalidOperationException("The Bitcoin escrow receipt failed authentication.");

        return JsonSerializer.Deserialize<BitcoinEscrowReceipt>(json)
            ?? throw new InvalidOperationException("The Bitcoin escrow receipt could not be decoded.");
    }

    private static void ValidateBitcoinEscrowReceipt(BitcoinEscrowReceipt receipt, Guid lockId, Guid assetId, Guid avatarId)
    {
        if (receipt.LockId != lockId || receipt.AssetId != assetId || receipt.AvatarId != avatarId)
            throw new InvalidOperationException("The Bitcoin escrow receipt does not match the requested deposit.");
    }

    private async Task ReserveBitcoinEscrowReceiptAsync(Guid id, BitcoinEscrowReceipt receipt,
        Guid receiptActor, ProviderType authority, BitcoinSecret signer)
    {
        var json = JsonSerializer.Serialize(receipt);
        var holon = new Holon
        {
            Id = id, Name = BitcoinEscrowRecordType, HolonType = HolonType.Custom,
            CustomHolonType = BitcoinEscrowRecordType, IsPublic = false,
            MetaData = new Dictionary<string, object>
            {
                ["receipt"] = json,
                ["authentication"] = Convert.ToHexString(AuthenticateBitcoinEscrowReceipt(signer, id, json))
            }
        };
        var conditions = new Dictionary<Guid, Guid?> { [id] = null };
        var saved = await BitcoinEscrowReceipts.SaveHolonAsync(holon, receiptActor,
            new CosmicWriteOptions(id, conditions), saveChildren: false, recursive: false, providerType: authority);
        if (saved.IsError || !saved.IsSaved)
            throw new InvalidOperationException("The Bitcoin escrow receipt was not reserved on its authority. Reconcile this lock ID before trying again.");
    }

    private async Task<OASISResult<ITransactionResponse>> PublishBitcoinEscrowReceiptAsync(BitcoinEscrowReceipt receipt)
    {
        var transaction = Transaction.Parse(receipt.SignedTransaction, SigningNetwork);
        var id = transaction.GetHash();
        try
        {
            await ReadVerifiedTransactionAsync(id);
            return new OASISResult<ITransactionResponse>(new BitcoinTransactionResponse { TransactionResult = id.ToString() });
        }
        catch (BitcoinTransactionNotFoundException)
        {
            return await BroadcastSignedBitcoinAsync(transaction, null);
        }
    }

    private static OASISResult<ITransactionResponse> BitcoinEscrowError(Exception ex) =>
        new OASISResult<ITransactionResponse> { IsError = true, ErrorCount = 1,
            Message = ex is ArgumentException or InvalidOperationException
                ? ex.Message : $"Bitcoin escrow failed ({ex.GetType().Name})." };
}
