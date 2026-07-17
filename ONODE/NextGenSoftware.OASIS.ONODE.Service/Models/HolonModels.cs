namespace NextGenSoftware.OASIS.ONODE.Service.Models;

public class AvatarNodeStateHolon
{
    public string HolonType { get; set; } = "AvatarNodeState";
    public string AvatarId { get; set; } = "";
    public string NodeId { get; set; } = "";
    public DateTime LastSeen { get; set; } = DateTime.UtcNow;
    public string? PublicEndpoint { get; set; }
    public string Version { get; set; } = "1.0.0";
    public List<HolonServiceState> Services { get; set; } = [];
    public HolonMetrics Metrics { get; set; } = new();
}

public class HolonServiceState
{
    public string Id { get; set; } = "";
    public string Status { get; set; } = "Stopped";
    public double UptimeSeconds { get; set; }
    public int Port { get; set; }
    public string WindowMode { get; set; } = "Visible";
    public int? Pid { get; set; }
    public List<HolonProviderState> Providers { get; set; } = [];
}

public class HolonProviderState
{
    public string ProviderType { get; set; } = "";
    public bool IsEnabled { get; set; }
    public int Priority { get; set; }
}

public class HolonMetrics
{
    public int PeersConnected { get; set; }
    public long BytesReadPerSec { get; set; }
    public long BytesWrittenPerSec { get; set; }
    public double RequestsPerSec { get; set; }
    public double AvgLatencyMs { get; set; }
}

public enum CommandType
{
    Start, Stop, Restart, GetLogs, UpdateConfig, GetConfig, GetMetrics,
    GetProviders, EnableProvider, DisableProvider, SetProviderPriority
}

public enum CommandStatus
{
    Pending, Executing, Done, Error
}

public class CommandHolon
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string HolonType { get; set; } = "ONODECommand";
    public string TargetNodeId { get; set; } = "";
    public string IssuedByAvatarId { get; set; } = "";
    public CommandType Command { get; set; }
    public string? Service { get; set; }
    public string? Payload { get; set; }
    public CommandStatus Status { get; set; } = CommandStatus.Pending;
    public string? Result { get; set; }
    public DateTime IssuedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
}
