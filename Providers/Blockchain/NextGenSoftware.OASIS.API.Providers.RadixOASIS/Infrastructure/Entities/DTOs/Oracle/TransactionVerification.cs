namespace NextGenSoftware.OASIS.API.Providers.RadixOASIS.Infrastructure.Entities.DTOs.Oracle;

public class TransactionVerification
{
    public string TransactionHash { get; set; } = string.Empty;
    public bool IsValid { get; set; }
    public bool IsVerified { get; set; }
    public bool IsConfirmed { get; set; }
    public int Confirmations { get; set; }
    public DateTime VerifiedAt { get; set; }
    public string? Status { get; set; }
    public int? Confidence { get; set; }
}

