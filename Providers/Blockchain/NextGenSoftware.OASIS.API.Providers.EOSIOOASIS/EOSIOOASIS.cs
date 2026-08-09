using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EOSNewYork.EOSCore;
using Newtonsoft.Json;
using NextGenSoftware.Logging;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Avatar;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Utilities;
using NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Entities.DTOs.CurrencyBalance;
using NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Entities.DTOs.GetAccount;
using NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Entities.Models;
using NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Infrastructure.EOSClient;
using NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Infrastructure.Persistence;
using NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Infrastructure.Repository;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using NextGenSoftware.Utilities.ExtentionMethods;
using NextGenSoftware.OASIS.API.Core.Objects;
using Nethereum.Signer;
using Nethereum.Hex.HexConvertors.Extensions;
using System.IO;
using System.Text.Json;

namespace NextGenSoftware.OASIS.API.Providers.EOSIOOASIS
{
    public partial class EOSIOOASIS : OASISStorageProviderBase, IOASISStorageProvider, IOASISNETProvider, IOASISBlockchainStorageProvider, IOASISSmartContractProvider, IOASISNFTProvider, IOASISSuperStar, IDisposable
    {
        private static readonly Dictionary<Guid, GetAccountResponseDto> _avatarIdToEOSIOAccountLookup = new();
        private IEosClient _eosClient;
        private IEosProviderRepository<AvatarDetailDto> _avatarDetailRepository;
        private IEosProviderRepository<AvatarDto> _avatarRepository;
        private IEosProviderRepository<HolonDto> _holonRepository;
        private IEosTransferRepository _transferRepository;
        private AvatarManager _avatarManager;
        private KeyManager _keyManager;
        private WalletManager _walletManager;
        private string _contractAddress;

        public ChainAPI ChainAPI => new ChainAPI();

        public string HostURI { get; set; }
        public string EOSAccountName { get; set; }
        public string EOSChainId { get; set; }
        public string EOSAccountPk { get; set; }

        public EOSIOOASIS(string hostUri, string eosAccountName, string eosChainId, string eosAccountPk)
        {
            if (string.IsNullOrEmpty(hostUri))
                throw new ArgumentNullException(nameof(hostUri));

            ProviderName = "EOSIOOASIS";
            ProviderDescription = "EOSIO Provider";
            ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.EOSIOOASIS);
            this.ProviderCategory = new(Core.Enums.ProviderCategory.StorageAndNetwork);
            this.ProviderCategories.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Blockchain));
            this.ProviderCategories.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.NFT));
            this.ProviderCategories.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.SmartContract));
            this.ProviderCategories.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Storage));

            HostURI = hostUri;
            EOSAccountName = eosAccountName;
            EOSChainId = eosChainId;
            EOSAccountPk = eosAccountPk;

            //_eosClient = new EosClient(new Uri(hostUri));
            //_holonRepository = new HolonEosProviderRepository(_eosClient, eosAccountName, hostUri, eosChainId, eosAccountPk);
            //_avatarDetailRepository = new AvatarDetailEosProviderRepository(_eosClient, eosAccountName,hostUri, eosChainId, eosAccountPk);
            //_avatarRepository = new AvatarEosProviderRepository(_eosClient, eosAccountName, hostUri, eosChainId, eosAccountPk);
            //_transferRepository = new EosTransferRepository(eosAccountName, hostUri, eosChainId, eosAccountPk);
        }

        private AvatarManager AvatarManager
        {
            get
            {
                if (_avatarManager == null)
                    _avatarManager = new AvatarManager(this);

                return _avatarManager;
            }
        }

        private WalletManager WalletManager
        {
            get
            {
                if (_walletManager == null)
                    _walletManager = new WalletManager(this, AvatarManager.OASISDNA);
                //_walletManager = new WalletManager(ProviderManager.GetStorageProvider(Core.Enums.ProviderType.MongoDBOASIS),AvatarManager.OASISDNA);

                return _walletManager;
            }
        }

        private KeyManager KeyManager
        {
            get
            {
                if (_keyManager == null)
                    _keyManager = new KeyManager(this);

                return _keyManager;
            }
        }

        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            OASISResult<bool> result = new OASISResult<bool>();
            string errorMessage = "Error occured in ActivateProviderAsync method in EOSIOOASIS Provider. Reason:";

            try
            {
                _eosClient = new EosClient(new Uri(HostURI));
                _holonRepository = new HolonEosProviderRepository(_eosClient, EOSAccountName, HostURI, EOSChainId, EOSAccountPk);
                _avatarDetailRepository = new AvatarDetailEosProviderRepository(_eosClient, EOSAccountName, HostURI, EOSChainId, EOSAccountPk);
                _avatarRepository = new AvatarEosProviderRepository(_eosClient, EOSAccountName, HostURI, EOSChainId, EOSAccountPk);
                _transferRepository = new EosTransferRepository(EOSAccountName, HostURI, EOSChainId, EOSAccountPk);

                // Get server state. Just need to receive correct response, otherwise exception would be thrown.
                var nodeInfo = await _eosClient.GetNodeInfo();

                // Response was received, but payload was incorrect.
                if (nodeInfo == null || !nodeInfo.IsNodeInfoCorrect())
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} NodeInfo Received Incorrect.");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} {ex}");
            }

            if (!result.IsError)
                IsProviderActivated = true;

            //if (result.IsError)
            //    return result;

            //return await base.ActivateProviderAsync();
            return result;
        }

        public override OASISResult<bool> ActivateProvider()
        {
            OASISResult<bool> result = new OASISResult<bool>();
            string errorMessage = "Error occured in ActivateProvider method in EOSIOOASIS Provider. Reason:";

            try
            {
                _eosClient = new EosClient(new Uri(HostURI));
                _holonRepository = new HolonEosProviderRepository(_eosClient, EOSAccountName, HostURI, EOSChainId, EOSAccountPk);
                _avatarDetailRepository = new AvatarDetailEosProviderRepository(_eosClient, EOSAccountName, HostURI, EOSChainId, EOSAccountPk);
                _avatarRepository = new AvatarEosProviderRepository(_eosClient, EOSAccountName, HostURI, EOSChainId, EOSAccountPk);
                _transferRepository = new EosTransferRepository(EOSAccountName, HostURI, EOSChainId, EOSAccountPk);

                // Get server state. Just need to receive correct response, otherwise exception would be thrown.
                var nodeInfo = _eosClient.GetNodeInfo().Result;

                // Response was received, but payload was incorrect.
                if (nodeInfo == null || !nodeInfo.IsNodeInfoCorrect())
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} NodeInfo Received Incorrect.");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} {ex}");
            }

            if (!result.IsError)
                IsProviderActivated = true;

            return result;

            //if (result.IsError)
            //    return result;

            //return base.ActivateProvider();
        }

        public override async Task<OASISResult<bool>> DeActivateProviderAsync()
        {
            _eosClient.Dispose();
            _eosClient = null;
            _holonRepository = null;
            _avatarDetailRepository = null;
            _avatarRepository = null;
            _transferRepository = null;

            _avatarManager = null;
            _keyManager = null;
            _walletManager = null;

            IsProviderActivated = false;
            return new OASISResult<bool>(true);

            //return await base.DeActivateProviderAsync();
        }

        public override OASISResult<bool> DeActivateProvider()
        {
            _eosClient.Dispose();
            _eosClient = null;
            _holonRepository = null;
            _avatarDetailRepository = null;
            _avatarRepository = null;
            _transferRepository = null;

            _avatarManager = null;
            _keyManager = null;
            _walletManager = null;

            IsProviderActivated = false;
            return new OASISResult<bool>(true);

            //return base.DeActivateProvider();
        }
    }
}
