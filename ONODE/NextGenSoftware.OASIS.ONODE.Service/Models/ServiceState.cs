namespace NextGenSoftware.OASIS.ONODE.Service.Models;

public class ServiceState
{
    public string Id { get; set; } = "";
    public ServiceStatus Status { get; set; } = ServiceStatus.Stopped;
    public int? Pid { get; set; }
    public int Port { get; set; }
    public WindowMode WindowMode { get; set; }
    public DateTime? StartedAt { get; set; }
    public int RestartCount { get; set; }
    public string? LastError { get; set; }
    public bool Installed { get; set; }

    public double UptimeSeconds => StartedAt.HasValue && Status == ServiceStatus.Running
        ? (DateTime.UtcNow - StartedAt.Value).TotalSeconds : 0;
}

public class ServiceMetrics
{
    public string ServiceId { get; set; } = "";
    public int PeersConnected { get; set; }
    public long BytesReadPerSec { get; set; }
    public long BytesWrittenPerSec { get; set; }
    public double RequestsPerSec { get; set; }
    public double AvgLatencyMs { get; set; }
    public DateTime CollectedAt { get; set; } = DateTime.UtcNow;
}

public class SupervisorStatus
{
    public string NodeId { get; set; } = "";
    public string Version { get; set; } = "1.0.0";
    public DateTime StartedAt { get; set; }
    public List<ServiceState> Services { get; set; } = [];
    public AggregateMetrics Metrics { get; set; } = new();
}

public class AggregateMetrics
{
    public int TotalPeers { get; set; }
    public long TotalBytesReadPerSec { get; set; }
    public long TotalBytesWrittenPerSec { get; set; }
    public double TotalRequestsPerSec { get; set; }
}
