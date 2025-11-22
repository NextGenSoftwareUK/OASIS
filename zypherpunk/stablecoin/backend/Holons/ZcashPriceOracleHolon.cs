using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.API.Zypherpunk.Stablecoin.Holons
{
    /// <summary>
    /// Represents the ZEC price oracle data
    /// Aggregates prices from multiple sources
    /// </summary>
    public class ZcashPriceOracleHolon : IHolon
    {
        // IHolon interface properties
        public Guid Id { get; set; }
        public string Name { get; set; } = "Zcash Price Oracle";
        public string Description { get; set; }
        public HolonType HolonType { get; set; } = HolonType.Oracle;
        public Dictionary<ProviderType, string> ProviderUniqueStorageKey { get; set; }
        public Dictionary<ProviderType, Dictionary<string, string>> ProviderMetaData { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime ModifiedDate { get; set; }
        public DateTime? DeletedDate { get; set; }
        public Guid? PreviousVersionId { get; set; }
        public Guid? VersionId { get; set; }
        public bool IsActive { get; set; } = true;
        
        // Oracle Identity
        public string OracleId { get; set; }
        
        // Price Data
        public decimal CurrentPrice { get; set; }
        public decimal PreviousPrice { get; set; }
        public decimal PriceChange24h { get; set; }
        public decimal PriceChangePercent24h { get; set; }
        
        // Price History
        public List<PricePoint> PriceHistory { get; set; }
        
        // Oracle Sources
        public List<OracleSource> Sources { get; set; }
        
        // Verification
        public string LastUpdateProof { get; set; } // Merkle proof or signature
        public DateTime LastUpdateTime { get; set; }
        public string LastUpdatedBy { get; set; } // Oracle operator address
        
        // Metadata
        public Dictionary<string, object> MetaData { get; set; }
        
        public ZcashPriceOracleHolon()
        {
            Id = Guid.NewGuid();
            OracleId = Id.ToString();
            ProviderUniqueStorageKey = new Dictionary<ProviderType, string>();
            ProviderMetaData = new Dictionary<ProviderType, Dictionary<string, string>>();
            PriceHistory = new List<PricePoint>();
            Sources = new List<OracleSource>();
            MetaData = new Dictionary<string, object>();
            CreatedDate = DateTime.UtcNow;
            ModifiedDate = DateTime.UtcNow;
        }
    }
    
    public class PricePoint
    {
        public decimal Price { get; set; }
        public DateTime Timestamp { get; set; }
        public string Source { get; set; }
        public string Proof { get; set; }
    }
    
    public class OracleSource
    {
        public string Name { get; set; }
        public string Endpoint { get; set; }
        public decimal Weight { get; set; } // Weight in aggregation
        public bool IsActive { get; set; }
        public DateTime LastSuccessfulUpdate { get; set; }
    }
}

