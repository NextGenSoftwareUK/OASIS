namespace NextGenSoftware.OASIS.API.Providers.RadixOASIS.Infrastructure.Entities.DTOs.Oracle;

/// <summary>
/// Detailed Radix transaction data for oracle operations
/// </summary>
public class RadixTransactionDetails
{
    public string IntentHash { get; set; } = string.Empty;
    public string TransactionHash { get; set; } = string.Empty;
    public string FromAddress { get; set; } = string.Empty;
    public string ToAddress { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string TokenSymbol { get; set; } = "XRD";
    public string ResourceAddress { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string Status { get; set; } = string.Empty;
    public ulong Epoch { get; set; }
    public decimal Fee { get; set; }
    public string Manifest { get; set; } = string.Empty;
}

