using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Globalization;
using EOSNewYork.EOSCore.Response.API;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Entities.DTOs.GetAccount;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.GeoSpatialNFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.GeoSpatialNFT.Request;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Response;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Response;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using System.Threading;

namespace NextGenSoftware.OASIS.API.Providers.TelosOASIS
{
    public partial class TelosOASIS : OASISStorageProviderBase, IOASISBlockchainStorageProvider, IOASISSmartContractProvider, IOASISNFTProvider, IOASISNETProvider
    {
        private static Dictionary<Guid, GetAccountResponseDto> _avatarIdToTelosAccountLookup = new Dictionary<Guid, GetAccountResponseDto>();
        private AvatarManager _avatarManager = null;
        private KeyManager _keyManager = null;
        private readonly HttpClient _httpClient;
        private const string TELOS_API_BASE_URL = "https://api.telos.net";

        private EOSIOOASIS.EOSIOOASIS _eosioOASIS;
        public EOSIOOASIS.EOSIOOASIS EOSIOOASIS => _eosioOASIS;

        public TelosOASIS(string host, string eosAccountName, string eosChainId, string eosAccountPk)
        {
            this.ProviderName = "TelosOASIS";
            this.ProviderDescription = "Telos Provider";
            this.ProviderType = new EnumValue<ProviderType>(API.Core.Enums.ProviderType.TelosOASIS);
            this.ProviderCategory = new(Core.Enums.ProviderCategory.StorageAndNetwork);
            this.ProviderCategories.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Blockchain));
            this.ProviderCategories.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.NFT));
            this.ProviderCategories.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.SmartContract));
            this.ProviderCategories.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Storage));

            _eosioOASIS = new EOSIOOASIS.EOSIOOASIS(host, eosAccountName, eosChainId, eosAccountPk);
            _httpClient = new HttpClient();
            // Ensure HttpClient uses the configured Telos API base URL for relative requests
            _httpClient.BaseAddress = new Uri(TELOS_API_BASE_URL);
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

        private KeyManager KeyManager
        {
            get
            {
                if (_keyManager == null)
                    _keyManager = new KeyManager(this, OASISDNA);

                return _keyManager;
            }
        }
    }
}
