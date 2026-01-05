using System;

namespace NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Models;

/// <summary>
/// Represents an encrypted secret stored in OASIS STAR ledger
/// </summary>
public class SecretRecord
{
    public Guid Id { get; set; }
    public string Key { get; set; } // e.g., "iship:api-key:merchant-123" or "iship:api-key:global"
    public string EncryptedValue { get; set; }
    public string SecretType { get; set; } // "api-key", "oauth-token", "webhook-secret"
    public Guid? MerchantId { get; set; } // Optional merchant association
    public DateTime CreatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; } // For tokens
    public bool IsActive { get; set; }
    public string Description { get; set; } // Optional description
    
    public SecretRecord()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
        IsActive = true;
    }
}

