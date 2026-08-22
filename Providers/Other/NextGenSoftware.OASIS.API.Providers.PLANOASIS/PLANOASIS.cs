using NextGenSoftware.OASIS.API.Core;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Avatar;
using NextGenSoftware.OASIS.API.Core.Objects.Avatar;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using NextGenSoftware.Utilities.ExtentionMethods;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Holons;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Linq;
using System.IO;

namespace NextGenSoftware.OASIS.API.Providers.BlockStackOASIS
{
    public partial class PLANOASIS : OASISStorageProviderBase, IOASISStorageProvider, IOASISNETProvider, IOASISSuperStar
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiBaseUrl;
        private readonly string _apiKey;
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

        public PLANOASIS(string apiBaseUrl = "https://api.plan-systems.org/v1", string apiKey = "")
        {
            this.ProviderName = "PLANOASIS";
            this.ProviderDescription = "PLAN Provider";
            this.ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.PLANOASIS);
            this.ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);
            
            _apiBaseUrl = apiBaseUrl;
            _apiKey = apiKey;
            _httpClient = new HttpClient();
            
            if (!string.IsNullOrEmpty(_apiKey))
            {
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
            }
        }

    }
}
