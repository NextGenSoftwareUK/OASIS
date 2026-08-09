using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Nethereum.JsonRpc.Client;
using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Avatar;
using System.Text.Json;
using System.Linq;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using System.Net.Http;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Utilities;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using NextGenSoftware.Utilities.ExtentionMethods;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Objects;
using Nethereum.Hex.HexTypes;
using Nethereum.Hex.HexConvertors.Extensions;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using Nethereum.Contracts;
using Nethereum.ABI.FunctionEncoding.Attributes;
using System.IO;
using System.Reflection;
using System.Text;
using Nethereum.RPC.Accounts;
// using Nethereum.StandardTokenEIP20; // Commented out - type doesn't exist

namespace NextGenSoftware.OASIS.API.Providers.EthereumOASIS
{
    public partial class EthereumOASIS : OASISStorageProviderBase, IOASISDBStorageProvider, IOASISNETProvider, IOASISSuperStar, IOASISBlockchainStorageProvider, IOASISNFTProvider
    {
        public Web3 Web3Client;
        private NextGenSoftwareOASISService _nextGenSoftwareOasisService;
        private Account _oasisAccount;
        private KeyManager _keyManager;
        private WalletManager _walletManager;
        private string _contractAddress;
        private string _network;
        private string _abi;
        private HttpClient _httpClient;
        private string _apiBaseUrl;

        private KeyManager KeyManager
        {
            get
            {
                if (_keyManager == null)
                    _keyManager = new KeyManager(this);
                 //_keyManager = new KeyManager(ProviderManager.GetStorageProvider(Core.Enums.ProviderType.EthereumOASIS));

                return _keyManager;
            }
        }

        private WalletManager WalletManager
        {
            get
            {
                if (_walletManager == null)
                    _walletManager = new WalletManager(this);
                    //_walletManager = new WalletManager(ProviderManager.GetStorageProvider(Core.Enums.ProviderType.EthereumOASIS));

                return _walletManager;
            }
        }

        public string HostURI { get; set; }
        public string ChainPrivateKey { get; set; }
        public BigInteger ChainId { get; set; }
        public string ContractAddress { get; set; }


        public EthereumOASIS(string hostUri, string chainPrivateKey, BigInteger chainId, string contractAddress)
        {
            this.ProviderName = "EthereumOASIS";
            this.ProviderDescription = "Ethereum Provider";
            this.ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.EthereumOASIS);
            this.ProviderCategory = new(Core.Enums.ProviderCategory.StorageAndNetwork);
            this.ProviderCategories.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Blockchain));
            this.ProviderCategories.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.EVMBlockchain));
            this.ProviderCategories.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.NFT));
            this.ProviderCategories.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.SmartContract));
            this.ProviderCategories.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Storage));

            this.HostURI = hostUri;
            this.ChainPrivateKey = chainPrivateKey;
            this.ChainId = chainId;
            this.ContractAddress = contractAddress;
        }

        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            OASISResult<bool> result = new OASISResult<bool>();

            try
            {
                if (!string.IsNullOrEmpty(HostURI) && !string.IsNullOrEmpty(ChainPrivateKey) && ChainId > 0)
                {
                    _oasisAccount = new Account(ChainPrivateKey, ChainId);
                    Web3Client = CreateWeb3WithAccount(_oasisAccount, HostURI);

                    _nextGenSoftwareOasisService = new NextGenSoftwareOASISService(Web3Client, ContractAddress);
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error occured in ActivateProviderAsync in EthereumOASIS Provider. Reason: {ex}");
            }

            if (!result.IsError)
                IsProviderActivated = true;

            return result;

            //if (result.IsError)
            //    return result;

            //return await base.ActivateProviderAsync();
        }

        /// <summary>
        /// Create Web3 using only the 2-parameter (IAccount, string) constructor to avoid MissingMethodException
        /// when Nethereum 4.4+ changed the 4-param overload from Common.Logging.ILog to ILogger.
        /// </summary>
        private static Web3 CreateWeb3WithAccount(IAccount account, string url)
        {
            var ctor = typeof(Web3).GetConstructor(BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(IAccount), typeof(string) }, null);
            if (ctor == null)
                throw new InvalidOperationException("Nethereum.Web3.Web3 (IAccount, string) constructor not found. Check Nethereum.Web3 package version.");
            return (Web3)ctor.Invoke(new object[] { account, url ?? "" });
        }

        public override OASISResult<bool> ActivateProvider()
        {
            return ActivateProviderAsync().Result;
        }

        public override async Task<OASISResult<bool>> DeActivateProviderAsync()
        {
            _oasisAccount = null;
            Web3Client = null;
            _nextGenSoftwareOasisService = null;

            _keyManager = null;
            _walletManager = null;

            IsProviderActivated = false;
            return new OASISResult<bool>(true);

            // return await base.DeActivateProviderAsync();
        }

        public override OASISResult<bool> DeActivateProvider()
        {
            return DeActivateProviderAsync().Result;
        }
    }
}
