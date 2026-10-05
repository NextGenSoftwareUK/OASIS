using System;

namespace NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Models
{
    /// <summary>
    /// Merchant model - represents merchant configuration and credentials.
    /// Full implementation will be completed in Task 1.2 (Schema Design) and Task 1.3 (Repository).
    /// </summary>
    public class Merchant
    {
        public Guid MerchantId { get; set; }
        public Guid? AvatarId { get; set; } // OASIS Avatar ID - links merchant to avatar
        public string CompanyName { get; set; }
        public ContactInfo ContactInfo { get; set; }
        public string ApiKeyHash { get; set; } // Hashed API key
        public RateLimitTier RateLimitTier { get; set; }
        public bool IsActive { get; set; }
        public bool QuickBooksConnected { get; set; }
        public MerchantConfiguration Configuration { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class ContactInfo
    {
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Address { get; set; }
    }

    public enum RateLimitTier
    {
        Basic,
        Standard,
        Premium,
        Enterprise
    }

    public class MerchantConfiguration
    {
        // Additional merchant-specific configuration
        public bool AutoCreateInvoices { get; set; }
        public string DefaultCurrency { get; set; }
        public string TimeZone { get; set; }
    }
}
