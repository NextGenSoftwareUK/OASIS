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
{    public class APIGatewayStats
    {
        public int TotalBridges { get; set; }
        public int TotalEndpoints { get; set; }
        public int TotalRoutes { get; set; }
        public double CacheHitRate { get; set; }
        public string LoadBalancerStatus { get; set; } = string.Empty;
        public DateTime LastActivity { get; set; }
    }

}