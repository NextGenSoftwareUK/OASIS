using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.GeoSpatialNFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.GeoSpatialNFT.Request;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using NextGenSoftware.Utilities.ExtentionMethods;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using Nethereum.Signer;
using Nethereum.Hex.HexConvertors.Extensions;
using System.IO;
using static NextGenSoftware.Utilities.KeyHelper;

namespace NextGenSoftware.OASIS.API.Providers.TRONOASIS
{
    public partial class TRONOASIS : NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.KeyValueStorageProviderBase, IOASISNETProvider, IOASISBlockchainStorageProvider, IOASISIdBoundTokenEscrowProvider, IOASISSmartContractProvider, IOASISNFTProvider, IOASISSuperStar
    {
        private readonly HttpClient _httpClient;
        private const string TRON_API_BASE_URL = "https://api.trongrid.io";
        private readonly string _contractAddress;
        private readonly string _escrowContractAddress;
        private readonly TronIdBoundEscrowConfiguration _idBoundEscrow;
        private readonly string _bridgePoolWalletAddress;
        private WalletManager _walletManager;
        private TronNativeTransferClient _nativeTransfers;
        private Func<string, Task<string>> _nativeSigningKeyResolver;
        private TronSdkRuntime _ownedRuntime;
        private readonly string _fullNodeGrpcEndpoint, _solidityNodeGrpcEndpoint, _genesisBlockId;
        private readonly string _signingKeyEnvironmentVariable, _apiKeyEnvironmentVariable;
        private readonly long _contractFeeLimitSun;
        private readonly SemaphoreSlim _runtimeLifecycle = new SemaphoreSlim(1, 1);

        public WalletManager WalletManager
        {
            get
            {
                if (_walletManager == null)
                    _walletManager = WalletManager.Instance;
                return _walletManager;
            }
            set => _walletManager = value;
        }

        public TRONOASIS(string rpcEndpoint = "https://api.trongrid.io", string network = "mainnet", string chainId = "0x2b6653dc", string contractAddress = "", WalletManager walletManager = null,
            TronNativeTransferClient nativeTransfers = null, Func<string, Task<string>> nativeSigningKeyResolver = null,
            string fullNodeGrpcEndpoint = "", string solidityNodeGrpcEndpoint = "", string genesisBlockId = "",
            string signingKeyEnvironmentVariable = "OASIS_TRON_PRIVATE_KEY", string apiKeyEnvironmentVariable = "OASIS_TRON_API_KEY",
            string escrowContractAddress = "", string bridgePoolWalletAddress = "", TronPrivateStorageBackend storageBackend = null,
            string storageContractAddress = "", string storageOwnerAddress = "",
            string storageSigningKeyEnvironmentVariable = "OASIS_TRON_STORAGE_PRIVATE_KEY",
            string storageEncryptionKeyEnvironmentVariable = "OASIS_TRON_STORAGE_ENCRYPTION_KEY", long contractFeeLimitSun = 100000000,
            TronIdBoundEscrowConfiguration idBoundEscrow = null)
            : base(new TronProviderStorageBackend(storageBackend))
        {
            ((TronProviderStorageBackend)Backend).Lifecycle = _runtimeLifecycle;
            ((TronProviderStorageBackend)Backend).IsActive = () => IsProviderActivated;
            if (storageBackend != null && (!string.IsNullOrWhiteSpace(storageContractAddress) || !string.IsNullOrWhiteSpace(storageOwnerAddress)))
                throw new ArgumentException("Choose either injected storage or configured storage, not both.");
            if (storageBackend == null && (!string.IsNullOrWhiteSpace(storageContractAddress) || !string.IsNullOrWhiteSpace(storageOwnerAddress)))
            {
                if (string.IsNullOrWhiteSpace(storageContractAddress) || string.IsNullOrWhiteSpace(storageOwnerAddress)
                    || string.IsNullOrWhiteSpace(storageSigningKeyEnvironmentVariable) || string.IsNullOrWhiteSpace(storageEncryptionKeyEnvironmentVariable))
                    throw new ArgumentException("Storage contract, owner and credential environment-variable names are required together.");
                ((TronProviderStorageBackend)Backend).Factory = () => new TronPrivateStorageBackend(_nativeTransfers,
                    storageOwnerAddress, storageContractAddress,
                    () => Environment.GetEnvironmentVariable(storageSigningKeyEnvironmentVariable),
                    () =>
                    {
                        var encoded = Environment.GetEnvironmentVariable(storageEncryptionKeyEnvironmentVariable);
                        if (string.IsNullOrWhiteSpace(encoded)) throw new InvalidOperationException("TRON storage encryption credential is missing.");
                        try { return Convert.FromBase64String(encoded); }
                        catch (FormatException) { throw new InvalidOperationException("TRON storage encryption credential must be base64 for a persistent 32-byte key."); }
                    });
            }
            _nativeTransfers = nativeTransfers;
            _nativeSigningKeyResolver = nativeSigningKeyResolver;
            _fullNodeGrpcEndpoint = fullNodeGrpcEndpoint;
            _solidityNodeGrpcEndpoint = solidityNodeGrpcEndpoint;
            _genesisBlockId = genesisBlockId;
            _signingKeyEnvironmentVariable = signingKeyEnvironmentVariable;
            if (contractFeeLimitSun <= 0) throw new ArgumentOutOfRangeException(nameof(contractFeeLimitSun));
            _contractFeeLimitSun = contractFeeLimitSun;
            _apiKeyEnvironmentVariable = apiKeyEnvironmentVariable;
            _walletManager = walletManager;
            _contractAddress = contractAddress ?? "";
            _escrowContractAddress = escrowContractAddress ?? "";
            idBoundEscrow?.Validate();
            if (idBoundEscrow != null && (idBoundEscrow.ContractAddress == _escrowContractAddress
                || idBoundEscrow.ContractAddress == _contractAddress || idBoundEscrow.ContractAddress == storageContractAddress))
                throw new ArgumentException("Id-bound custody requires a separate explicitly configured contract.");
            _idBoundEscrow = idBoundEscrow;
            _bridgePoolWalletAddress = bridgePoolWalletAddress ?? "";
            this.ProviderName = "TRONOASIS";
            this.ProviderDescription = "TRON Provider";
            this.ProviderType = new EnumValue<ProviderType>(NextGenSoftware.OASIS.API.Core.Enums.ProviderType.TRONOASIS);
            this.ProviderCategory = new(Core.Enums.ProviderCategory.StorageAndNetwork);
            this.ProviderCapabilities.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Blockchain));
            this.ProviderCapabilities.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.EVMBlockchain));
            this.ProviderCapabilities.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.NFT));
            this.ProviderCapabilities.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.SmartContract));
            this.ProviderCapabilities.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Storage));

            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri(rpcEndpoint);
        }


    }

    public class TRONClient
    {
        private readonly string _apiUrl;

        public TRONClient(string apiUrl = "https://api.trongrid.io")
        {
            _apiUrl = apiUrl;
        }

        public async Task<TRONAccountInfo> GetAccountInfoAsync(string accountAddress)
        {
            try
            {
                using (var httpClient = new System.Net.Http.HttpClient())
                {
                    var response = await httpClient.GetAsync($"{_apiUrl}/v1/accounts/{accountAddress}");
                    if (response.IsSuccessStatusCode)
                    {
                        var content = await response.Content.ReadAsStringAsync();
                        var accountData = System.Text.Json.JsonSerializer.Deserialize<JsonElement>(content);
                        
                        return new TRONAccountInfo
                        {
                            Address = accountAddress,
                            Balance = accountData.TryGetProperty("balance", out var balance) ? balance.GetInt64() : 0,
                            Energy = accountData.TryGetProperty("energy", out var energy) ? energy.GetInt64() : 0,
                            Bandwidth = accountData.TryGetProperty("bandwidth", out var bandwidth) ? bandwidth.GetInt64() : 0
                        };
                    }
                }
            }
            catch (Exception)
            {
                // Return null if query fails
            }
            return null;
        }

        public async Task<TRONAccountInfo> GetAccountInfoByEmailAsync(string email)
        {
            // TRON doesn't have email-based account lookup, so return null
            // This would need to be implemented via a mapping service
            return null;
        }
    }

    public class TRONAccountInfo
    {
        public string Address { get; set; }
        public long? Balance { get; set; }
        public long? Energy { get; set; }
        public long? Bandwidth { get; set; }
    }
}
