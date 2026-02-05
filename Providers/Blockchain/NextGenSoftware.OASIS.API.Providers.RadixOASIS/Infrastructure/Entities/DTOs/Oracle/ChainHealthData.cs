namespace NextGenSoftware.OASIS.API.Providers.RadixOASIS.Infrastructure.Entities.DTOs.Oracle;

public class ChainHealthData
{
    public string ChainName { get; set; } = string.Empty;
    public bool IsHealthy { get; set; }
    public decimal Uptime { get; set; }
    public decimal UptimePercentage { get; set; }
    public int ActiveValidatorCount { get; set; }
    public DateTime LastChecked { get; set; }
    public DateTime LastHealthCheck { get; set; }
    public string Status { get; set; } = string.Empty;
    public TimeSpan? AverageResponseTime { get; set; }
    public int? ErrorCount { get; set; }
}

