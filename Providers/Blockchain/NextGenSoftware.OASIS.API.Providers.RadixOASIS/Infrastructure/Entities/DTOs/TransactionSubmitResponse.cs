namespace NextGenSoftware.OASIS.API.Providers.RadixOASIS.Infrastructure.Entities.DTOs;

/// <summary>
/// Response from submitting a transaction to Radix
/// </summary>
public class TransactionSubmitResponse
{
    public bool Duplicate { get; set; }
    public string? TransactionHash { get; set; }
}

