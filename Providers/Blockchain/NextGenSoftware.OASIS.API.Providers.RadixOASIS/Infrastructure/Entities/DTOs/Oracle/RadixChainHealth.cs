namespace NextGenSoftware.OASIS.API.Providers.RadixOASIS.Infrastructure.Entities.DTOs.Oracle;

/// <summary>
/// Radix chain health metrics for oracle operations
/// </summary>
public class RadixChainHealth
{
    public string ChainName { get; set; } = "Radix";
    public bool IsHealthy { get; set; }
    public decimal Uptime { get; set; } // 0-100%
    public TimeSpan AverageResponseTime { get; set; }
    public int ErrorCount { get; set; }
    public DateTime LastHealthCheck { get; set; }
    public ulong CurrentEpoch { get; set; }
    public int ActiveValidatorCount { get; set; }
    public decimal? NetworkThroughput { get; set; } // Transactions per second
}


