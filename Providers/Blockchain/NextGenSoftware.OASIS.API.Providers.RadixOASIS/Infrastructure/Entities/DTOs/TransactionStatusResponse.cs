namespace NextGenSoftware.OASIS.API.Providers.RadixOASIS.Infrastructure.Entities.DTOs;

/// <summary>
/// Response for transaction status query
/// </summary>
public class TransactionStatusResponse
{
    public string IntentStatus { get; set; } = string.Empty;
    public string? KnownPayloads { get; set; }
}

