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
{    public class APIRoute
    {
        public string NetworkType { get; set; } = string.Empty;
        public int Priority { get; set; }
        public string LoadBalancingStrategy { get; set; } = string.Empty;
        public int Timeout { get; set; }
        public int RetryCount { get; set; }
    }

}