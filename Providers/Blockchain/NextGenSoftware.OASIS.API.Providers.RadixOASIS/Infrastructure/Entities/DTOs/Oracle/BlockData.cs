namespace NextGenSoftware.OASIS.API.Providers.RadixOASIS.Infrastructure.Entities.DTOs.Oracle;

public class BlockData
{
    public ulong BlockHeight { get; set; }
    public ulong Epoch { get; set; }
    public DateTime Timestamp { get; set; }
    public int TransactionCount { get; set; }
}


