using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Objects.Avatar;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using System.Text.Json.Serialization;

namespace NextGenSoftware.OASIS.API.Providers.AWSOASIS
{
    /// <summary>
    /// AWS Provider for OASIS
    /// Implements Amazon Web Services integration for cloud storage and services
    /// </summary>
    public partial class AWSOASIS : OASISStorageProviderBase, IOASISStorageProvider, IOASISNETProvider
    {
        private readonly HttpClient _httpClient;
        private readonly string _region;
        private readonly string _accessKey;
        private readonly string _secretKey;
        private bool _isActivated;

        /// <summary>
        /// Initializes a new instance of the AWSOASIS provider
        /// </summary>
        /// <param name="region">AWS region (e.g., us-east-1, eu-west-1)</param>
        /// <param name="accessKey">AWS access key</param>
        /// <param name="secretKey">AWS secret key</param>
        public AWSOASIS(string region = "us-east-1", string accessKey = "", string secretKey = "")
        {
            this.ProviderName = "AWSOASIS";
            this.ProviderDescription = "AWS Provider - Amazon Web Services cloud integration";
            this.ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.AWSOASIS);
            this.ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);

            _region = region ?? throw new ArgumentNullException(nameof(region));
            _accessKey = accessKey;
            _secretKey = secretKey;
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri($"https://{_region}.amazonaws.com")
            };
        }
    }
}
