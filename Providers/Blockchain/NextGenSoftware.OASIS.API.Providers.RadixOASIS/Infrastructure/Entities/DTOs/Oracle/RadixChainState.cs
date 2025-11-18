namespace NextGenSoftware.OASIS.API.Providers.RadixOASIS.Infrastructure.Entities.DTOs.Oracle;

/// <summary>
/// Radix chain state data for oracle operations
/// </summary>
public class RadixChainState
{
    public string ChainName { get; set; } = "Radix";
    public ulong CurrentEpoch { get; set; }
    public ulong? RoundInEpoch { get; set; }
    public DateTime LastBlockTime { get; set; }
    public bool IsHealthy { get; set; }
    public string NetworkId { get; set; } = string.Empty;
    public string NetworkName { get; set; } = string.Empty; // "MainNet" or "StokeNet"
    public decimal? AverageTransactionFee { get; set; }
    public int ActiveValidatorCount { get; set; }
}

