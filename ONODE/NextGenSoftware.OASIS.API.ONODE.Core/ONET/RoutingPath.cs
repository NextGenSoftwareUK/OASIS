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
{    public class RoutingPath
    {
        public List<string> Nodes { get; set; } = new List<string>();
        public DateTime CalculatedAt { get; set; }
        public bool IsValid { get; set; }
        public int Priority { get; set; }
        public double TotalCost { get; set; }
        public double Efficiency { get; set; }
        public double Stability { get; set; }
    }

}