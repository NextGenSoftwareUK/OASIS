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
    public partial class ActivityPubOASIS : OASISStorageProviderBase, IOASISStorageProvider, IOASISNETProvider, IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly string _instanceUrl;
        private readonly string _accessToken;
        private bool _isActivated;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the ActivityPubOASIS provider
        /// </summary>
        /// <param name="instanceUrl">URL of the ActivityPub instance (e.g., https://mastodon.social)</param>
        /// <param name="accessToken">OAuth access token for API authentication</param>
        public ActivityPubOASIS(string instanceUrl = "https://mastodon.social", string accessToken = "")
            : this(instanceUrl, accessToken, new HttpClientHandler()) { }

        /// <summary>The provider owns the supplied transport and disposes it only at final disposal.</summary>
        public ActivityPubOASIS(string instanceUrl, string accessToken, HttpMessageHandler handler)
        {
            this.ProviderName = "ActivityPubOASIS";
            this.ProviderDescription = "ActivityPub Provider - Federated social network protocol";
            this.ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.ActivityPubOASIS);
            this.ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);

            ArgumentNullException.ThrowIfNull(instanceUrl);
            if(!Uri.TryCreate(instanceUrl,UriKind.Absolute,out var root)
                ||(root.Scheme!=Uri.UriSchemeHttps&&!(root.Scheme==Uri.UriSchemeHttp&&root.IsLoopback))
                ||!string.IsNullOrEmpty(root.UserInfo)||!string.IsNullOrEmpty(root.Query)||!string.IsNullOrEmpty(root.Fragment)
                ||root.AbsolutePath!="/")
                throw new ArgumentException("An HTTPS instance origin (or loopback HTTP) without path, credentials, query or fragment is required.",nameof(instanceUrl));
            _instanceUrl = root.GetLeftPart(UriPartial.Authority);
            _accessToken = accessToken;
            // Instance identity and authentication are bound to this configured origin.
            if(handler is HttpClientHandler httpHandler)httpHandler.AllowAutoRedirect=false;
            if(handler is SocketsHttpHandler socketsHandler)socketsHandler.AllowAutoRedirect=false;
            _httpClient = new HttpClient(handler ?? throw new ArgumentNullException(nameof(handler)))
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
