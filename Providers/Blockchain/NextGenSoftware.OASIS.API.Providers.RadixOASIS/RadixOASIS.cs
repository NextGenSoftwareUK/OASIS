using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using System.Collections.Generic;
using System.Linq;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Providers.RadixOASIS.Infrastructure.Entities;
using NextGenSoftware.OASIS.API.Providers.RadixOASIS.Infrastructure.Services.Radix;
using NextGenSoftware.OASIS.API.Providers.RadixOASIS.Infrastructure.Oracle;
using NextGenSoftware.OASIS.API.Providers.RadixOASIS.Infrastructure.Helpers;
using NextGenSoftware.OASIS.API.Providers.RadixOASIS.Infrastructure.Entities.DTOs;
using NextGenSoftware.OASIS.API.Providers.RadixOASIS.Extensions;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.RadixOASIS;

/// <summary>
/// OASIS Provider for Radix DLT. Core storage (avatars, avatar details, holons) comes from KeyValueStorageProviderBase
/// over one <c>OasisKeyValueStore</c> Scrypto component: records are compressed and encrypted (ledger state is public),
/// written by notarised manifests acknowledged only on CommittedSuccess, and read from committed state through the
/// node's Engine State API; activation proves that store. Bridge, token and first-party oracle operations use
/// <see cref="RadixBridgeService"/>, <see cref="OracleNode"/> and <see cref="ChainObserver"/>, which need no storage.
/// </summary>
public partial class RadixOASIS : KeyValueStorageProviderBase, IOASISStorageProvider,
    IOASISBlockchainStorageProvider, IOASISSmartContractProvider, IOASISNETProvider, IDisposable
{
    private readonly IRadixService _radixService;
    private readonly RadixOASISConfig _config;
    private readonly HttpClient _httpClient;
    private readonly RadixLedgerClient? _ledger;
    private readonly RadixOracleNode _oracleNode;
    private readonly RadixChainObserver _chainObserver;

    /// <summary>
    /// Gets the Radix bridge service for cross-chain operations
    /// </summary>
    public IRadixService? RadixBridgeService => _radixService;

    /// <summary>
    /// Gets the Radix first-party oracle node - allows Radix to run their own oracle with no middleware
    /// </summary>
    public RadixOracleNode? OracleNode => _oracleNode;

    /// <summary>
    /// Gets the Radix chain observer for oracle operations
    /// </summary>
    public RadixChainObserver? ChainObserver => _chainObserver;

    /// <summary>The account that signs and pays for storage transactions, when storage is configured.</summary>
    public string? StorageAccountAddress => _ledger?.AccountAddress;

    /// <summary>Bridge-only instance: storage operations fail with an explicit configuration error.</summary>
    public RadixOASIS(string hostUri, byte networkId, string accountAddress, string privateKey)
        : this(new RadixOASISConfig
        {
            HostUri = hostUri,
            NetworkId = networkId,
            AccountAddress = accountAddress,
            PrivateKey = privateKey
        })
    {
    }

    /// <summary>
    /// Storage is enabled when <see cref="RadixOASISConfig.CoreApiUri"/>, <see cref="RadixOASISConfig.EngineStateApiUri"/>,
    /// <see cref="RadixOASISConfig.NetworkName"/>, <see cref="RadixOASISConfig.KeyValueStoreComponentAddress"/>,
    /// <see cref="RadixOASISConfig.PrivateKey"/> and <see cref="RadixOASISConfig.StorageEncryptionKey"/> are all set.
    /// </summary>
    public RadixOASIS(RadixOASISConfig config)
        : this(config ?? throw new ArgumentNullException(nameof(config)), CreateLedger(config))
    {
    }

    private RadixOASIS(RadixOASISConfig config, RadixLedgerClient? ledger)
        : base(CreateBackend(config, ledger))
    {
        this.ProviderName = nameof(RadixOASIS);
        this.ProviderDescription = "Radix DLT Blockchain Provider with Bridge Support";
        this.ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.RadixOASIS);
        this.ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);
        this.ProviderCapabilities.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Blockchain));
        this.ProviderCapabilities.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.NFT));
        this.ProviderCapabilities.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.SmartContract));
        this.ProviderCapabilities.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Storage));

        _config = config;
        _ledger = ledger;
        _httpClient = new HttpClient();
        _radixService = new RadixService(_config, _httpClient);
        // Bridge and oracle need no storage: they are usable whether or not storage is configured or activated.
        _chainObserver = new RadixChainObserver(_radixService, _config);
        _oracleNode = new RadixOracleNode(_radixService, _config);
    }

    private static bool StorageConfigured(RadixOASISConfig config)
        => !string.IsNullOrWhiteSpace(config.CoreApiUri) || !string.IsNullOrWhiteSpace(config.EngineStateApiUri)
            || !string.IsNullOrWhiteSpace(config.KeyValueStoreComponentAddress) || !string.IsNullOrWhiteSpace(config.StorageEncryptionKey);

    private static RadixLedgerClient? CreateLedger(RadixOASISConfig config)
    {
        if (!StorageConfigured(config)) return null;
        if (string.IsNullOrWhiteSpace(config.CoreApiUri) || string.IsNullOrWhiteSpace(config.EngineStateApiUri)
            || string.IsNullOrWhiteSpace(config.NetworkName) || string.IsNullOrWhiteSpace(config.KeyValueStoreComponentAddress)
            || string.IsNullOrWhiteSpace(config.StorageEncryptionKey) || string.IsNullOrWhiteSpace(config.PrivateKey))
            throw new ArgumentException("Radix storage needs CoreApiUri, EngineStateApiUri, NetworkName, KeyValueStoreComponentAddress, PrivateKey and StorageEncryptionKey together.", nameof(config));
        return new RadixLedgerClient(new Uri(config.CoreApiUri), new Uri(config.EngineStateApiUri), config.NetworkId, config.NetworkName, config.PrivateKey);
    }

    private static IKeyValueBackend CreateBackend(RadixOASISConfig config, RadixLedgerClient? ledger)
    {
        if (ledger == null) return new UnconfiguredRadixBackend();
        var key = Convert.FromHexString(config.StorageEncryptionKey!);
        if (key.Length != 32)
            throw new ArgumentException("Radix storage requires a 32-byte record encryption key.", nameof(config));
        return new PrivateKeyValueBackend(new RadixKeyValueBackend(ledger, config.KeyValueStoreComponentAddress!), key, compress: true);
    }

    public void Dispose()
    {
        (Backend as IDisposable)?.Dispose();
        _ledger?.Dispose();
        _httpClient.Dispose();
        IsProviderActivated = false;
    }

    private sealed class UnconfiguredRadixBackend : IKeyValueBackend
    {
        private static Exception Missing() => new InvalidOperationException(
            "Radix storage requires CoreApiUri, EngineStateApiUri, NetworkName, KeyValueStoreComponentAddress, PrivateKey and StorageEncryptionKey.");
        public Task<string> GetAsync(string key, CancellationToken cancellationToken = default) => Task.FromException<string>(Missing());
        public Task PutAsync(string key, string value, CancellationToken cancellationToken = default) => Task.FromException(Missing());
        public Task DeleteAsync(string key, CancellationToken cancellationToken = default) => Task.FromException(Missing());
        public Task<IReadOnlyList<string>> ListKeysAsync(string prefix, CancellationToken cancellationToken = default) => Task.FromException<IReadOnlyList<string>>(Missing());
        public Task VerifyAsync(CancellationToken cancellationToken = default) => Task.FromException(Missing());
    }
}
