using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Agent;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Request;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.Logging;
// TODO: NFTManager is in ONODE.Core which creates a circular dependency with API.Core
// This file should be moved to ONODE.Core or use dependency injection
// using NextGenSoftware.OASIS.API.ONODE.Core.Managers;

namespace NextGenSoftware.OASIS.API.Core.Managers
{
    /// <summary>
    /// A2A Protocol integration with NFTManager
    /// Handles agent reputation NFTs, service completion certificates, and achievement badges
    /// </summary>
    public partial class A2AManager
    {
        // NOTE: NFT methods are implemented as extension methods in ONODE.Core
        // See: ONODE.Core/Managers/A2AManager/A2AManagerExtensions-NFT.cs
        // Instance methods removed to allow extension methods to be used
        // ONODE.Core is required for NFT functionality
    }
}

