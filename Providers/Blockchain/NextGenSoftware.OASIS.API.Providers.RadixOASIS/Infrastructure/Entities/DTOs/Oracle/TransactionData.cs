namespace NextGenSoftware.OASIS.API.Providers.RadixOASIS.Infrastructure.Entities.DTOs.Oracle;

public class TransactionData
{
    public string TransactionHash { get; set; } = string.Empty;
    public string FromAddress { get; set; } = string.Empty;
    public string ToAddress { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime Timestamp { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? TokenSymbol { get; set; }
    public ulong? BlockHeight { get; set; }
    public ulong? Epoch { get; set; }
}

