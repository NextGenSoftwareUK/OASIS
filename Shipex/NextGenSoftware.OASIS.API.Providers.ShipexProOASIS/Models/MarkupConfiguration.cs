using System;

namespace NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Models
{
    /// <summary>
    /// Markup configuration model for merchant/carrier markup settings.
    /// Used for markup calculation and management.
    /// </summary>
    public class MarkupConfiguration
    {
        public Guid MarkupId { get; set; }
        public Guid? MerchantId { get; set; } // null = global/default
        public string Carrier { get; set; } = string.Empty; // "UPS", "FedEx", "DHL"
        public MarkupType Type { get; set; } // Fixed or Percentage
        public decimal Value { get; set; } // amount or percentage
        public DateTime EffectiveFrom { get; set; }
        public DateTime? EffectiveTo { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>
    /// Markup type enumeration
    /// </summary>
    public enum MarkupType
    {
        Fixed,        // Add fixed amount
        Percentage    // Add percentage of base rate
    }
}




