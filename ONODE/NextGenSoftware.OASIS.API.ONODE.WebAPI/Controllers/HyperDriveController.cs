using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using NextGenSoftware.OASIS.API.Core.Configuration;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Helpers;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
{


    /// <summary>
    /// Model for recording requests
    /// </summary>
    public class RecordRequestModel
    {
        public ProviderType ProviderType { get; set; }
        public bool Success { get; set; }
        public double ResponseTimeMs { get; set; }
        public double Cost { get; set; }
    }

    /// <summary>
    /// Model for recording connections
    /// </summary>
    public class RecordConnectionModel
    {
        public ProviderType ProviderType { get; set; }
        public bool IsConnecting { get; set; }
    }

    /// <summary>
    /// HyperDrive status information
    /// </summary>
    public class HyperDriveStatus
    {
        public string Mode { get; set; }
        public string EffectiveConfigurationSource { get; set; }
        public DateTime EffectiveConfigurationAppliedUtc { get; set; }
        public bool IsEnabled { get; set; }
        public bool AutoFailoverEnabled { get; set; }
        public bool AutoReplicationEnabled { get; set; }
        public bool AutoLoadBalancingEnabled { get; set; }
        public LoadBalancingStrategy DefaultStrategy { get; set; }
        public List<ProviderType> EnabledProviders { get; set; }
        public List<ProviderType> LoadBalancingProviders { get; set; }
        public List<ProviderType> FailoverProviders { get; set; }
        public List<ProviderType> ReplicationProviders { get; set; }
        public int TotalProviders { get; set; }
        public int ActiveProviders { get; set; }
        public DateTime LastHealthCheck { get; set; }
        public ProviderSelectionDiagnostic LastProviderSelection { get; set; }
        public HyperDriveFailoverDiagnostic LastFailover { get; set; }
        public HyperDriveReplicationDiagnostic LastReplication { get; set; }
    }

    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public partial class HyperDriveController : OASISControllerBase
    {
        private readonly OASISHyperDriveConfigManager _configManager;
        private readonly ProviderManager _providerManager;
        private readonly PerformanceMonitor _performanceMonitor;
        private readonly AIOptimizationEngine _aiEngine;
        private readonly AdvancedAnalyticsEngine _analyticsEngine;
        private readonly PredictiveFailoverEngine _failoverEngine;

        public HyperDriveController()
        {
            _configManager = OASISHyperDriveConfigManager.Instance;
            _providerManager = ProviderManager.Instance;
            _performanceMonitor = _providerManager.PerformanceMonitor;
            _aiEngine = AIOptimizationEngine.Instance;
            _analyticsEngine = AdvancedAnalyticsEngine.Instance;
            _failoverEngine = PredictiveFailoverEngine.Instance;
        }
    }
}
