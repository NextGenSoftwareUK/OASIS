namespace NextGenSoftware.OASIS.API.Providers.RadixOASIS.Infrastructure.Entities.DTOs.Oracle;

/// <summary>
/// Radix block data for oracle operations
/// </summary>
public class RadixBlock
{
    public string BlockHash { get; set; } = string.Empty;
    public ulong Epoch { get; set; }
    public ulong? RoundInEpoch { get; set; }
    public DateTime Timestamp { get; set; }
    public int TransactionCount { get; set; }
    public string PreviousBlockHash { get; set; } = string.Empty;
    public string ValidatorAddress { get; set; } = string.Empty;
}


