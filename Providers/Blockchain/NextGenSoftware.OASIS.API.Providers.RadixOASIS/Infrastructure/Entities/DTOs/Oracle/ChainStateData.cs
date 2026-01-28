namespace NextGenSoftware.OASIS.API.Providers.RadixOASIS.Infrastructure.Entities.DTOs.Oracle;

public class ChainStateData
{
    public string ChainName { get; set; } = string.Empty;
    public ulong BlockHeight { get; set; }
    public ulong Epoch { get; set; }
    public DateTime LastBlockTime { get; set; }
    public bool IsHealthy { get; set; }
    public string NetworkId { get; set; } = string.Empty;
}




