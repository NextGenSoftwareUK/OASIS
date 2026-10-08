using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Linq;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Objects.Avatar;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using Aptos;
using NextGenSoftware.OASIS.API.Core.Objects;
using static NextGenSoftware.Utilities.KeyHelper;

namespace NextGenSoftware.OASIS.API.Providers.AptosOASIS
{
    public class AptosTransactionResponse : ITransactionResponse
    {
        public string TransactionHash { get; set; }
        public string Version { get; set; }
        public string Message { get; set; }
        public bool Success { get; set; }
        public string TransactionResult { get; set; }
    }
    public partial class AptosOASIS : OASISStorageProviderBase, IOASISStorageProvider, IOASISNETProvider, IOASISBlockchainStorageProvider, IOASISSmartContractProvider, IOASISNFTProvider, IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly string _rpcEndpoint;
        private readonly string _network;
        private readonly string _privateKey;
        private readonly string _contractAddress;
        private readonly AptosClient _aptosClient;
        private readonly Ed25519Account _account;
        private bool _isActivated;

        private WalletManager _walletManager;

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

        public AptosOASIS(string rpcEndpoint = "https://api.mainnet.aptoslabs.com/v1", string network = "mainnet", string privateKey = null, string contractAddress = "0x1", WalletManager walletManager = null)
        {
            _rpcEndpoint = rpcEndpoint;
            _network = network;
            _privateKey = privateKey;
            _contractAddress = contractAddress;
            _walletManager = walletManager;
            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri(_rpcEndpoint);
            var normalizedEndpoint = _rpcEndpoint.TrimEnd('/');
            var networkConfig = new NetworkConfig(
                _network,
                normalizedEndpoint,
                $"{normalizedEndpoint}/graphql",
                chainId: -1);
            _aptosClient = new AptosClient(new AptosConfig(networkConfig));
            if (!string.IsNullOrWhiteSpace(_privateKey))
                _account = new Ed25519Account(new Ed25519PrivateKey(_privateKey, strict: false));

            this.ProviderName = "AptosOASIS";
            this.ProviderDescription = "Aptos blockchain provider for OASIS";
            this.ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.AptosOASIS);
            this.ProviderCategory = new(Core.Enums.ProviderCategory.StorageAndNetwork);
            this.ProviderCapabilities.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Blockchain));
            this.ProviderCapabilities.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.NFT));
            this.ProviderCapabilities.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.SmartContract));
            this.ProviderCapabilities.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Storage));

        }


        // Provider metadata set in constructor

    }
}
