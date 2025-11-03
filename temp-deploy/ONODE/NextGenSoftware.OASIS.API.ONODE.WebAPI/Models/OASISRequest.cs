
namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Models
{
    public class OASISRequest
    {
        public string ProviderType { get; set; }
        public bool SetGlobally { get; set; }
        //public Core.Enums.AutoFailOverMode AutoFailOverMode { get; set; } = Core.Enums.AutoFailOverMode.NotSet;
        //public Core.Enums.AutoReplicationMode AutoReplicationMode { get; set; } = Core.Enums.AutoReplicationMode.NotSet;
        //public Core.Enums.AutoLoadBalanceMode AutoLoadBalanceMode { get; set; } = Core.Enums.AutoLoadBalanceMode.NotSet;
        //public bool? AutoFailOverEnabled { get; set; } 
        //public bool? AutoReplicationEnabled { get; set; }
        //public bool? AutoLoadBalanceEnabled { get; set; }
        public string AutoFailOverMode { get; set; } = "default";
        public string AutoReplicationMode { get; set; } = "default";
        public string AutoLoadBalanceMode { get; set; } = "default";
        public string AutoFailOverProviders { get; set; }
        public string AutoReplicationProviders { get; set; }
        public string AutoLoadBalanceProviders { get; set; }
        public bool WaitForAutoReplicationResult { get; set; }
        public bool ShowDetailedSettings { get; set; } = true;
    }
}