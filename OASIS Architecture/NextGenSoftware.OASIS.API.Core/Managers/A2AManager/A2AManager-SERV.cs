using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces.Agent;
// TODO: ONETUnifiedArchitecture is in ONODE.Core which creates a circular dependency with API.Core
// SERV methods are implemented as extension methods in ONODE.Core
// See: ONODE.Core/Managers/A2AManager/A2AManagerExtensions-SERV.cs
// using NextGenSoftware.OASIS.API.ONODE.Core.Network;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.Logging;

namespace NextGenSoftware.OASIS.API.Core.Managers
{
    /// <summary>
    /// A2A Protocol integration with SERV infrastructure (ONET Unified Architecture)
    /// NOTE: SERV functionality is implemented as extension methods in ONODE.Core
    /// See: ONODE.Core/Managers/A2AManager/A2AManagerExtensions-SERV.cs
    /// ONODE.Core is required for SERV functionality - import the namespace to use these methods
    /// </summary>
    public partial class A2AManager
    {
        // SERV methods are implemented as extension methods in ONODE.Core
        // Import namespace: using NextGenSoftware.OASIS.API.ONODE.Core.Managers;
        // Methods: RegisterAgentAsServiceAsync, DiscoverAgentsViaSERVAsync
    }
}
