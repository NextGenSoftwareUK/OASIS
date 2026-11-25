using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Starknet;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Utilities;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using System.Text.Json;

namespace NextGenSoftware.OASIS.API.Providers.StarknetOASIS;

public sealed partial class StarknetOASIS : NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.KeyValueStorageProviderBase,
    IOASISStorageProvider,
    IOASISBlockchainStorageProvider,
    IOASISNETProvider,
    IOASISSmartContractProvider
{
    /// <summary>OpenZeppelin account (openzeppelin_account AccountABI, constructor(public_key: felt252)), declared on Sepolia and mainnet.
    /// The bridge re-verifies the class ABI before every deployment.</summary>
    public const string DefaultAccountClassHash = "0x540d7f5ec7ecf317e68d48564934cb99259781b1ee3cedbbc37ec5337f8e688";
    public const string SepoliaChainId = "0x534e5f5345504f4c4941";
    public const string MainnetChainId = "0x534e5f4d41494e";

    private readonly HttpClient _httpClient;
    private readonly string _network;
    private readonly string _chainId;
    private readonly string _rpcEndpoint;
    private readonly string _accountClassHash;
    private readonly IStarknetRpcClient _rpcClient;
    private readonly string _contractAddress;
    internal IStarknetSdkBridge Sdk { get; set; } = new StarknetSdkBridge();
    private bool _isActivated;

    /// <summary>Maps a configured network name to its Starknet chain id; unknown or retired networks (e.g. Goerli) are rejected.</summary>
    public static string ChainIdFor(string network) => (network ?? string.Empty).Trim().ToLowerInvariant() switch
    {
        "sepolia" or "sn_sepolia" => SepoliaChainId,
        "mainnet" or "sn_main" => MainnetChainId,
        _ => throw new ArgumentException($"Unsupported Starknet network '{network}'. Use 'sepolia' or 'mainnet'.", nameof(network))
    };

    public StarknetOASIS(string network = null, string apiBaseUrl = null, string contractAddress = null, string accountClassHash = null)
        : this(new UnconfiguredStarknetBackend(), network, apiBaseUrl, contractAddress, accountClassHash, null) { }

    /// <summary>
    /// Ledger-committed storage: records are compressed and encrypted into Kubo envelopes, and the OasisKv contract holds each
    /// owner's key -> (CID, SHA-256) commitments, written by the storage account's own SUCCEEDED transactions.
    /// </summary>
    /// <param name="chainId">Chain id of the RPC (e.g. a devnet's), verified by the bridge before every call.</param>
    /// <param name="rpcUrl">Starknet JSON-RPC endpoint.</param>
    /// <param name="contractAddress">Deployed OasisKv contract (contracts/src/lib.cairo).</param>
    /// <param name="accountClassHash">OpenZeppelin account class of the storage account.</param>
    /// <param name="privateKey">Stark private key of the deployed storage account.</param>
    /// <param name="kuboApi">Kubo HTTP API holding the encrypted envelopes.</param>
    /// <param name="encryptionKey">32-byte record encryption key.</param>
    public StarknetOASIS(string chainId, string rpcUrl, string contractAddress, string accountClassHash, string privateKey, string kuboApi, byte[] encryptionKey)
        : this(CreateStorageBackend(chainId, rpcUrl, contractAddress, accountClassHash, privateKey, kuboApi, encryptionKey),
            null, rpcUrl, contractAddress, accountClassHash, chainId) { }

    private static NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.IKeyValueBackend CreateStorageBackend(
        string chainId, string rpcUrl, string contractAddress, string accountClassHash, string privateKey, string kuboApi, byte[] encryptionKey)
    {
        if (encryptionKey == null || encryptionKey.Length != 32)
            throw new ArgumentException("Starknet storage requires an explicit 32-byte record encryption key.", nameof(encryptionKey));
        var ledger = new StarknetLedgerKeyValueBackend(new StarknetSdkBridge(), rpcUrl, chainId, contractAddress, accountClassHash, privateKey, kuboApi);
        return new NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.PrivateKeyValueBackend(ledger, encryptionKey, compress: true);
    }

    private sealed class UnconfiguredStarknetBackend : NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.IKeyValueBackend
    {
        private static Exception Missing() => new InvalidOperationException(
            "Starknet storage requires an explicit RPC, OasisKv contract, deployed storage account key, Kubo endpoint and record encryption key.");
        public Task<string> GetAsync(string key, CancellationToken cancellationToken = default) => Task.FromException<string>(Missing());
        public Task PutAsync(string key, string value, CancellationToken cancellationToken = default) => Task.FromException(Missing());
        public Task DeleteAsync(string key, CancellationToken cancellationToken = default) => Task.FromException(Missing());
        public Task<IReadOnlyList<string>> ListKeysAsync(string prefix, CancellationToken cancellationToken = default) => Task.FromException<IReadOnlyList<string>>(Missing());
        public Task VerifyAsync(CancellationToken cancellationToken = default) => Task.FromException(Missing());
    }

    private StarknetOASIS(NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.IKeyValueBackend backend, string network, string apiBaseUrl,
        string contractAddress, string accountClassHash, string chainId)
        : base(backend)
    {
        ProviderName = nameof(StarknetOASIS);
        ProviderDescription = "Starknet privacy provider for cross-chain swaps";
        ProviderType = new EnumValue<Core.Enums.ProviderType>(Core.Enums.ProviderType.StarknetOASIS);
        this.ProviderCategory = new(Core.Enums.ProviderCategory.StorageAndNetwork);
        this.ProviderCapabilities.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Blockchain));
        this.ProviderCapabilities.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.NFT));
        this.ProviderCapabilities.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.SmartContract));
        this.ProviderCapabilities.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Storage));

        _network = chainId != null ? "custom" : network ?? Environment.GetEnvironmentVariable("STARKNET_NETWORK") ?? "sepolia";
        _chainId = chainId ?? ChainIdFor(_network);
        _rpcEndpoint = apiBaseUrl ?? Environment.GetEnvironmentVariable("STARKNET_API_BASE_URL")
            ?? (_chainId == MainnetChainId ? "https://starknet-rpc.publicnode.com" : "https://starknet-sepolia-rpc.publicnode.com");
        _accountClassHash = accountClassHash ?? Environment.GetEnvironmentVariable("STARKNET_ACCOUNT_CLASS_HASH") ?? DefaultAccountClassHash;
        _contractAddress = contractAddress ?? Environment.GetEnvironmentVariable("STARKNET_OASIS_CONTRACT_ADDRESS");
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(_rpcEndpoint)
        };
        _rpcClient = new StarknetRpcClient(_httpClient, _rpcEndpoint);
    }
}
