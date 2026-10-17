using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Response;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Response;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using System.Text.Json.Serialization;
using static NextGenSoftware.Utilities.KeyHelper;

namespace NextGenSoftware.OASIS.API.Providers.SuiOASIS
{
    public partial class SuiOASIS : NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.KeyValueStorageProviderBase, IOASISNETProvider, IOASISBlockchainStorageProvider, IOASISIdBoundTokenEscrowProvider, IOASISSmartContractProvider, IOASISNFTProvider, IDisposable
    {
        private readonly string _privateKey;
        private readonly string _contractAddress;
        private WalletManager _walletManager;
        public WalletManager WalletManager
        {
            get => _walletManager ??= new WalletManager(this, OASISDNA);
            set => _walletManager = value;
        }

        /// <summary>
        /// Initializes a new instance of the SuiOASIS provider
        /// </summary>
        /// <param name="rpcEndpoint">Sui RPC endpoint URL</param>
        /// <param name="network">Sui network (mainnet, testnet, devnet)</param>
        /// <param name="privateKey">Private key for signing transactions</param>
        public SuiOASIS(string rpcEndpoint = "https://fullnode.mainnet.sui.io:443", string network = "mainnet", string chainId = "", string contractAddress = "", string privateKey = "", string storageObjectId = "", string currencyObjectId = "", Func<byte[]> storageEncryptionKeyResolver = null, SuiTokenEscrowConfiguration tokenEscrow = null)
            : this(new SuiSdkClient(rpcEndpoint, network, chainId, privateKey, contractAddress, storageObjectId, currencyObjectId),privateKey,contractAddress,storageEncryptionKeyResolver)
        { tokenEscrow?.Validate(); _tokenEscrow = tokenEscrow; }

        /// <summary>Explicit immutable per-transaction gas ceiling; avoids overloading existing null-RPC constructor calls.</summary>
        public static SuiOASIS CreateWithNativeFeeLimits(SuiNativeFeeLimits nativeFeeLimits,string rpcEndpoint = "https://fullnode.mainnet.sui.io:443", string network = "mainnet", string chainId = "", string contractAddress = "", string privateKey = "", string storageObjectId = "", string currencyObjectId = "", Func<byte[]> storageEncryptionKeyResolver = null, SuiTokenEscrowConfiguration tokenEscrow = null)
            =>new(nativeFeeLimits,rpcEndpoint,network,chainId,contractAddress,privateKey,storageObjectId,currencyObjectId,storageEncryptionKeyResolver,tokenEscrow);

        private SuiOASIS(SuiNativeFeeLimits nativeFeeLimits,string rpcEndpoint,string network,string chainId,string contractAddress,string privateKey,string storageObjectId,string currencyObjectId,Func<byte[]> storageEncryptionKeyResolver,SuiTokenEscrowConfiguration tokenEscrow)
            :this(new SuiSdkClient(rpcEndpoint,network,chainId,privateKey,contractAddress,storageObjectId,currencyObjectId,
                (nativeFeeLimits??throw new ArgumentNullException(nameof(nativeFeeLimits))).MaximumGasBudgetMist),privateKey,contractAddress,storageEncryptionKeyResolver)
        { tokenEscrow?.Validate();_tokenEscrow=tokenEscrow; }

        private readonly SuiSdkClient _sdkClient;
        private readonly NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.PersistedPrivateKeyValueBackend _privateStorage;
        private SuiOASIS(SuiSdkClient client,string privateKey,string contractAddress,Func<byte[]> keyResolver)
            : this(client,privateKey,contractAddress,new NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.PersistedPrivateKeyValueBackend(
                new SuiStorageBackend(client),keyResolver ?? (() => NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.PersistedPrivateKeyValueBackend.ResolveEnvironmentKey("OASIS_SUI_STORAGE_ENCRYPTION_KEY"))))
        { }
        private SuiOASIS(SuiSdkClient client,string privateKey,string contractAddress,NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.PersistedPrivateKeyValueBackend storage)
            : base(storage)
        {
            _privateStorage=storage;
            _sdkClient=client;
            this.ProviderName = "SuiOASIS";
            this.ProviderDescription = "Sui Provider - High-performance blockchain platform";
            this.ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.SuiOASIS);
            this.ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);

            _privateKey = privateKey;
            _contractAddress = contractAddress;

            this.ProviderCapabilities.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Blockchain));
            this.ProviderCapabilities.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.NFT));
            this.ProviderCapabilities.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.SmartContract));
            this.ProviderCapabilities.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Storage));
        }

        public void Dispose()
        {
            _privateStorage.Dispose();
            _sdkClient.Dispose();
            IsProviderActivated = false;
        }
    }
}
