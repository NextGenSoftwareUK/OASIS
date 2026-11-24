using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using System.Threading;

namespace NextGenSoftware.OASIS.API.Providers.XRPLOASIS;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

/// <summary>
/// XRP Ledger provider for OASIS storage. Records are compressed, encrypted and held as envelopes on a Kubo node; the
/// ledger is the authority: each write or delete is a signed (Ed25519), validated AccountSet whose memo commits the key
/// and the envelope's CID and SHA-256. Avatar, avatar-detail and holon semantics come from KeyValueStorageProviderBase.
/// </summary>
public sealed class XRPLOASIS : KeyValueStorageProviderBase, IOASISStorageProvider, IOASISNETProvider, IOASISBlockchainStorageProvider
{
    /// <summary>Unconfigured instance: storage operations fail with an explicit configuration error.</summary>
    public XRPLOASIS() : this(new UnconfiguredXrplBackend()) { }

    /// <param name="rpcEndpoint">XRPL JSON-RPC endpoint (rippled or Clio).</param>
    /// <param name="privateKeyHex">32-byte Ed25519 private key (hex) of the funded storage account.</param>
    /// <param name="kuboApi">Kubo HTTP API that holds the encrypted record envelopes.</param>
    /// <param name="encryptionKey">32-byte record encryption key; envelopes are compressed then encrypted.</param>
    public XRPLOASIS(string rpcEndpoint, string privateKeyHex, string kuboApi, byte[] encryptionKey)
        : this(CreateBackend(rpcEndpoint, privateKeyHex, kuboApi, encryptionKey)) { }

    private XRPLOASIS(IKeyValueBackend backend) : base(backend)
    {
        ProviderName = "XRPLOASIS";
        ProviderDescription = "XRP Ledger provider for OASIS - ledger-committed, encrypted record storage";
        ProviderType = new EnumValue<ProviderType>(NextGenSoftware.OASIS.API.Core.Enums.ProviderType.XRPLOASIS);
        ProviderCategory = new EnumValue<ProviderCategory>(NextGenSoftware.OASIS.API.Core.Enums.ProviderCategory.StorageAndNetwork);
        ProviderCapabilities.Add(new EnumValue<ProviderCategory>(NextGenSoftware.OASIS.API.Core.Enums.ProviderCategory.Blockchain));
        ProviderCapabilities.Add(new EnumValue<ProviderCategory>(NextGenSoftware.OASIS.API.Core.Enums.ProviderCategory.Storage));
    }

    /// <summary>XRPL classic address of the storage account derived from the configured key.</summary>
    public string StorageAddress => (Backend as AddressedBackend)?.Address;

    private static IKeyValueBackend CreateBackend(string rpcEndpoint, string privateKeyHex, string kuboApi, byte[] encryptionKey)
    {
        if (encryptionKey == null || encryptionKey.Length != 32)
            throw new ArgumentException("XRPL storage requires an explicit 32-byte record encryption key.", nameof(encryptionKey));
        var ledger = new XrplLedgerKeyValueBackend(rpcEndpoint, privateKeyHex, kuboApi);
        return new AddressedBackend(new PrivateKeyValueBackend(ledger, encryptionKey, compress: true), ledger.Address);
    }

    public string GetProviderVersion() => "1.0.0";
    public Task<string> GetProviderVersionAsync() => Task.FromResult("1.0.0");

    /// <summary>Classic address of the storage account.</summary>
    public static string AddressForPrivateKey(string privateKeyHex) => new XrplEd25519Signer(privateKeyHex).Address;

    private sealed class AddressedBackend(PrivateKeyValueBackend inner, string address) : IKeyValueBackend, IDisposable
    {
        public string Address { get; } = address;
        public Task<string> GetAsync(string key, CancellationToken cancellationToken = default) => inner.GetAsync(key, cancellationToken);
        public Task PutAsync(string key, string value, CancellationToken cancellationToken = default) => inner.PutAsync(key, value, cancellationToken);
        public Task DeleteAsync(string key, CancellationToken cancellationToken = default) => inner.DeleteAsync(key, cancellationToken);
        public Task<IReadOnlyList<string>> ListKeysAsync(string prefix, CancellationToken cancellationToken = default) => inner.ListKeysAsync(prefix, cancellationToken);
        public Task VerifyAsync(CancellationToken cancellationToken = default) => inner.VerifyAsync(cancellationToken);
        public void Dispose() => inner.Dispose();
    }

    private sealed class UnconfiguredXrplBackend : IKeyValueBackend
    {
        private static Exception Missing() => new InvalidOperationException(
            "XRPL storage requires an explicit RPC endpoint, Ed25519 storage key, Kubo endpoint and record encryption key.");
        public Task<string> GetAsync(string key, CancellationToken cancellationToken = default) => Task.FromException<string>(Missing());
        public Task PutAsync(string key, string value, CancellationToken cancellationToken = default) => Task.FromException(Missing());
        public Task DeleteAsync(string key, CancellationToken cancellationToken = default) => Task.FromException(Missing());
        public Task<IReadOnlyList<string>> ListKeysAsync(string prefix, CancellationToken cancellationToken = default) => Task.FromException<IReadOnlyList<string>>(Missing());
        public Task VerifyAsync(CancellationToken cancellationToken = default) => Task.FromException(Missing());
    }

    #region Wallet, token and geo members (not part of XRPL storage)

    private static OASISResult<T> NotSupported<T>(string method)
    {
        var r = new OASISResult<T>();
        OASISErrorHandling.HandleError(ref r, $"{method} is not implemented by XRPLOASIS.");
        return r;
    }
    private static Task<OASISResult<T>> NotSupportedAsync<T>(string method) => Task.FromResult(NotSupported<T>(method));

    public OASISResult<IEnumerable<IAvatar>> GetAvatarsNearMe(long geoLat, long geoLong, int radiusInMeters) => NotSupported<IEnumerable<IAvatar>>(nameof(GetAvatarsNearMe));
    public OASISResult<IEnumerable<IHolon>> GetHolonsNearMe(long geoLat, long geoLong, int radiusInMeters, HolonType Type) => NotSupported<IEnumerable<IHolon>>(nameof(GetHolonsNearMe));
    public OASISResult<ITransactionResponse> SendToken(ISendWeb3TokenRequest request) => NotSupported<ITransactionResponse>(nameof(SendToken));
    public Task<OASISResult<ITransactionResponse>> SendTokenAsync(ISendWeb3TokenRequest request) => NotSupportedAsync<ITransactionResponse>(nameof(SendTokenAsync));
    public OASISResult<ITransactionResponse> MintToken(IMintWeb3TokenRequest request) => NotSupported<ITransactionResponse>(nameof(MintToken));
    public Task<OASISResult<ITransactionResponse>> MintTokenAsync(IMintWeb3TokenRequest request) => NotSupportedAsync<ITransactionResponse>(nameof(MintTokenAsync));
    public OASISResult<ITransactionResponse> BurnToken(IBurnWeb3TokenRequest request) => NotSupported<ITransactionResponse>(nameof(BurnToken));
    public Task<OASISResult<ITransactionResponse>> BurnTokenAsync(IBurnWeb3TokenRequest request) => NotSupportedAsync<ITransactionResponse>(nameof(BurnTokenAsync));
    public OASISResult<ITransactionResponse> LockToken(ILockWeb3TokenRequest request) => NotSupported<ITransactionResponse>(nameof(LockToken));
    public Task<OASISResult<ITransactionResponse>> LockTokenAsync(ILockWeb3TokenRequest request) => NotSupportedAsync<ITransactionResponse>(nameof(LockTokenAsync));
    public OASISResult<ITransactionResponse> UnlockToken(IUnlockWeb3TokenRequest request) => NotSupported<ITransactionResponse>(nameof(UnlockToken));
    public Task<OASISResult<ITransactionResponse>> UnlockTokenAsync(IUnlockWeb3TokenRequest request) => NotSupportedAsync<ITransactionResponse>(nameof(UnlockTokenAsync));
    public OASISResult<double> GetBalance(IGetWeb3WalletBalanceRequest request) => NotSupported<double>(nameof(GetBalance));
    public Task<OASISResult<double>> GetBalanceAsync(IGetWeb3WalletBalanceRequest request) => NotSupportedAsync<double>(nameof(GetBalanceAsync));
    public OASISResult<IList<IWalletTransaction>> GetTransactions(IGetWeb3TransactionsRequest request) => NotSupported<IList<IWalletTransaction>>(nameof(GetTransactions));
    public Task<OASISResult<IList<IWalletTransaction>>> GetTransactionsAsync(IGetWeb3TransactionsRequest request) => NotSupportedAsync<IList<IWalletTransaction>>(nameof(GetTransactionsAsync));
    public OASISResult<IKeyPairAndWallet> GenerateKeyPair() => NotSupported<IKeyPairAndWallet>(nameof(GenerateKeyPair));
    public Task<OASISResult<IKeyPairAndWallet>> GenerateKeyPairAsync() => NotSupportedAsync<IKeyPairAndWallet>(nameof(GenerateKeyPairAsync));
    public Task<OASISResult<(string PublicKey, string PrivateKey, string SeedPhrase)>> CreateAccountAsync(CancellationToken token = default) => NotSupportedAsync<(string, string, string)>(nameof(CreateAccountAsync));
    public Task<OASISResult<(string PublicKey, string PrivateKey)>> RestoreAccountAsync(string seedPhrase, CancellationToken token = default) => NotSupportedAsync<(string, string)>(nameof(RestoreAccountAsync));
    public Task<OASISResult<BridgeTransactionResponse>> WithdrawAsync(decimal amount, string senderAccountAddress, string senderPrivateKey) => NotSupportedAsync<BridgeTransactionResponse>(nameof(WithdrawAsync));
    public Task<OASISResult<BridgeTransactionResponse>> DepositAsync(decimal amount, string receiverAccountAddress) => NotSupportedAsync<BridgeTransactionResponse>(nameof(DepositAsync));
    public Task<OASISResult<BridgeTransactionStatus>> GetTransactionStatusAsync(string transactionHash, CancellationToken token = default) => NotSupportedAsync<BridgeTransactionStatus>(nameof(GetTransactionStatusAsync));

    #endregion
}
