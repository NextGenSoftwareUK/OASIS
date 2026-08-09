using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Managers;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Network
{    public class RoutingStats
    {
        public int TotalNodes { get; set; }
        public int ActiveNodes { get; set; }
        public int CachedPaths { get; set; }
        public double AverageLatency { get; set; }
        public double AverageReliability { get; set; }
        public string RoutingAlgorithm { get; set; } = string.Empty;
        public DateTime LastOptimization { get; set; }
    }

}