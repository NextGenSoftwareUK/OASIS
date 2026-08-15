using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Objects.Avatar;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
// using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Request; // Removed - use Requests (plural) instead
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Response;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.MoralisOASIS
{
    public partial class MoralisOASIS : OASISStorageProviderBase, IOASISStorageProvider, IOASISNETProvider, IOASISNFTProvider
    {
        private readonly string _apiKey;
        private readonly string _baseUrl;
        private readonly HttpClient _httpClient;
        private readonly string _contractAddress;
        private readonly string _chain; // Blockchain chain (e.g., "eth", "polygon", "bsc")

        public MoralisOASIS(string apiKey, string baseUrl = "https://deep-index.moralis.io/api/v2", string contractAddress = "", string chain = "eth")
        {
            _apiKey = apiKey;
            _baseUrl = baseUrl;
            _contractAddress = contractAddress;
            _chain = chain;
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("X-API-Key", _apiKey);

            this.ProviderName = "MoralisOASIS";
            this.ProviderDescription = "Moralis Web3 API Provider";
            this.ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.MoralisOASIS);
            this.ProviderCategory = new(Core.Enums.ProviderCategory.StorageAndNetwork);
            this.ProviderCategories.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Blockchain));
            this.ProviderCategories.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.NFT));
            this.ProviderCategories.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.SmartContract));
            this.ProviderCategories.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Storage));
        }
    }

    #region Moralis Response Models

    public class MoralisApiResult
    {
        public string result { get; set; }
        public string error { get; set; }
    }

    #endregion
}
}
