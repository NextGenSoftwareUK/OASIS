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

// GenerateAssemblyInfo is off for this project, so the attribute is declared here rather than in the csproj.
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("NextGenSoftware.OASIS.API.Providers.TelosOASIS")]

namespace NextGenSoftware.OASIS.API.Providers.EOSIOOASIS
{
    public partial class EOSIOOASIS : OASISStorageProviderBase, IOASISStorageProvider, IOASISNETProvider, IOASISBlockchainStorageProvider, IOASISSmartContractProvider, IOASISNFTProvider, IOASISSuperStar, IDisposable
    {
        private static readonly Dictionary<Guid, GetAccountResponseDto> _avatarIdToEOSIOAccountLookup = new();
        private IEosClient _eosClient;
        private AvatarDetailEosProviderRepository _avatarDetailRepository;
        private AvatarEosProviderRepository _avatarRepository;
        private HolonEosProviderRepository _holonRepository;
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

        /// <summary>
        /// The provider type this instance stamps into <c>ProviderUniqueStorageKey</c> / <c>ProviderMetaData</c>
        /// on the records it maps. It is <see cref="Core.Enums.ProviderType.EOSIOOASIS"/> unless another Antelope
        /// provider (e.g. TelosOASIS) composes this implementation as its storage engine, in which case records
        /// belong to the composing provider.
        /// </summary>
        internal ProviderType StorageProviderType => _storageProviderType;
        private readonly ProviderType _storageProviderType;

        public EOSIOOASIS(string hostUri, string eosAccountName, string eosChainId, string eosAccountPk)
            : this(hostUri, eosAccountName, eosChainId, eosAccountPk, Core.Enums.ProviderType.EOSIOOASIS) { }

        /// <summary>Composes the EOSIO storage implementation on behalf of another Antelope provider.</summary>
        internal EOSIOOASIS(string hostUri, string eosAccountName, string eosChainId, string eosAccountPk, ProviderType storageProviderType)
        {
            if (string.IsNullOrEmpty(hostUri))
                throw new ArgumentNullException(nameof(hostUri));
            if (storageProviderType is Core.Enums.ProviderType.None or Core.Enums.ProviderType.All or Core.Enums.ProviderType.Default)
                throw new ArgumentOutOfRangeException(nameof(storageProviderType), "A concrete storage provider type is required.");
            _storageProviderType = storageProviderType;

            ProviderName = "EOSIOOASIS";
            ProviderDescription = "EOSIO Provider";
            ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.EOSIOOASIS);
            this.ProviderCategory = new(Core.Enums.ProviderCategory.StorageAndNetwork);
            this.ProviderCapabilities.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Blockchain));
            this.ProviderCapabilities.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.NFT));
            this.ProviderCapabilities.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.SmartContract));
            this.ProviderCapabilities.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Storage));

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

    }
}
