using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.ActivityPubOASIS
{
    /// <summary>
    /// ActivityPub Provider for OASIS
    /// Implements the ActivityPub protocol for federated social networks (Mastodon, Pleroma, etc.)
    /// </summary>
    public partial class ActivityPubOASIS : OASISStorageProviderBase, IOASISStorageProvider, IOASISNETProvider
    {
        private readonly HttpClient _httpClient;
        private readonly string _instanceUrl;
        private readonly string _accessToken;
        private bool _isActivated;

        /// <summary>
        /// Initializes a new instance of the ActivityPubOASIS provider
        /// </summary>
        /// <param name="instanceUrl">URL of the ActivityPub instance (e.g., https://mastodon.social)</param>
        /// <param name="accessToken">OAuth access token for API authentication</param>
        public ActivityPubOASIS(string instanceUrl = "https://mastodon.social", string accessToken = "")
        {
            this.ProviderName = "ActivityPubOASIS";
            this.ProviderDescription = "ActivityPub Provider - Federated social network protocol";
            this.ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.ActivityPubOASIS);
            this.ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);

            _instanceUrl = instanceUrl ?? throw new ArgumentNullException(nameof(instanceUrl));
            _accessToken = accessToken;
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(_instanceUrl)
            };

            if (!string.IsNullOrEmpty(_accessToken))
            {
                _httpClient.DefaultRequestHeaders.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _accessToken);
            }
        }

    }
}
