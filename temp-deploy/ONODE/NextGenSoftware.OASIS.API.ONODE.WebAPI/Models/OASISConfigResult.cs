
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Helpers;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Models
{
    public class OASISConfigResult<T>
    {
        public bool IsError { get; set; }
        public OASISHttpResponseMessage<T> Response { get; set; }
        public AutoReplicationMode AutoReplicationMode { get; set; } = AutoReplicationMode.UseGlobalDefaultInOASISDNA;
        public AutoFailOverMode AutoFailOverMode { get; set; } = AutoFailOverMode.UseGlobalDefaultInOASISDNA;
        public AutoLoadBalanceMode AutoLoadBalanceMode { get; set; } = AutoLoadBalanceMode.UseGlobalDefaultInOASISDNA;
        public bool PreviousAutoReplicationEnabled { get; set; }
        public bool PreviousAutoFailOverEnabled { get; set; }
        public bool PreviousAutoLoadBalanaceEnabled { get; set; }
        public string PreviousAutoReplicationList { get; set; }
        public string PreviousAutoFailOverList { get; set; }
        public string PreviousAutoLoadBalanaceList { get; set; }
    }
}