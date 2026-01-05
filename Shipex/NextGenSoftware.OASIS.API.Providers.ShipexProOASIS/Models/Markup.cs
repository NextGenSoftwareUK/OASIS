using System;

namespace NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Models
{
    /// <summary>
    /// Markup model - represents markup configuration per merchant/carrier.
    /// Full implementation will be completed in Task 1.2 (Schema Design) and Task 1.3 (Repository).
    /// </summary>
    public class Markup
    {
        public Guid MarkupId { get; set; }
        public Guid? MerchantId { get; set; } // null means global/default
        public string Carrier { get; set; }
        public MarkupType MarkupType { get; set; }
        public decimal MarkupValue { get; set; }
        public DateTime EffectiveFrom { get; set; }
        public DateTime? EffectiveTo { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public enum MarkupType
    {
        Fixed,
        Percentage
    }
}

