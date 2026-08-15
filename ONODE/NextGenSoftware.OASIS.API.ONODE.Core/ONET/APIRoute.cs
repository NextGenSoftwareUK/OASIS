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
{
    public class APIRoute
    {
        public string NetworkType { get; set; } = string.Empty;
        public int Priority { get; set; }
        public string LoadBalancingStrategy { get; set; } = string.Empty;
        public int Timeout { get; set; }
        public int RetryCount { get; set; }
    }

    public class APIEndpoint
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Endpoint { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string Method { get; set; } = string.Empty;
        public int Timeout { get; set; }
        public int RetryCount { get; set; }
        public string NetworkType { get; set; } = string.Empty;
        public string BridgeId { get; set; } = string.Empty;
        public Dictionary<string, object> Configuration { get; set; } = new Dictionary<string, object>();
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }
    }

    public class APIGatewayStats
    {
        public int TotalBridges { get; set; }
        public int TotalEndpoints { get; set; }
        public int TotalRoutes { get; set; }
        public double CacheHitRate { get; set; }
        public string LoadBalancerStatus { get; set; } = string.Empty;
        public DateTime LastActivity { get; set; }
    }
}
