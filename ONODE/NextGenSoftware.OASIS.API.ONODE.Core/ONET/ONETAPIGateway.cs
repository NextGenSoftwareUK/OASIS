using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Http;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Network
{    /// <summary>
    /// ONET API Gateway - unified API bridging Web2 and Web3.
    /// </summary>
    public partial class ONETAPIGateway : OASISManager
    {
        private readonly Dictionary<string, APIBridge> _apiBridges = new Dictionary<string, APIBridge>();
        private Dictionary<string, APIRoute> _apiRoutes = new Dictionary<string, APIRoute>();
        private readonly Dictionary<string, APIEndpoint> _endpoints = new Dictionary<string, APIEndpoint>();
        private readonly APIRouter _router;
        private readonly APILoadBalancer _loadBalancer;
        private readonly APICache _cache;
        private readonly Dictionary<string, APIRoute> _routes = new Dictionary<string, APIRoute>();
        private int _requestCount = 0;

        /// <summary>
        /// Real per-endpoint rate limiting, enforced in CallUnifiedAPIAsync. Previously
        /// InitializeRateLimitingAsync/InitializeRateLimitingPoliciesAsync/InitializeRateLimitingAlgorithmsAsync
        /// (and their near-duplicates further down this file) only ever logged labels like "Initializing
        /// token bucket algorithm" - no limiter object existed anywhere, so nothing was ever actually rate
        /// limited no matter how the gateway was configured.
        /// </summary>
        private readonly RateLimiter _rateLimiter = new RateLimiter();

        /// <summary>Requests allowed per endpoint per MaxRequestsPerWindow (default 100/minute). Configurable per deployment.</summary>
        public int MaxRequestsPerWindow { get; set; } = 100;
        public TimeSpan RateLimitWindow { get; set; } = TimeSpan.FromMinutes(1);

        public ONETAPIGateway(IOASISStorageProvider storageProvider, OASISDNA oasisdna = null) : base(storageProvider, oasisdna)
        {
            _router = new APIRouter();
            _loadBalancer = new APILoadBalancer();
            _cache = new APICache();
        }

    }
}