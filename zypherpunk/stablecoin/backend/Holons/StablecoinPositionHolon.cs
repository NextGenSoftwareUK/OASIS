using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.API.Zypherpunk.Stablecoin.Holons
{
    /// <summary>
    /// Represents a user's stablecoin position (collateral + debt)
    /// Stored as a Holon for automatic replication across providers
    /// </summary>
    public class StablecoinPositionHolon : IHolon
    {
        // IHolon interface properties
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public HolonType HolonType { get; set; } = HolonType.StablecoinPosition;
        public Dictionary<ProviderType, string> ProviderUniqueStorageKey { get; set; }
        public Dictionary<ProviderType, Dictionary<string, string>> ProviderMetaData { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime ModifiedDate { get; set; }
        public DateTime? DeletedDate { get; set; }
        public Guid? PreviousVersionId { get; set; }
        public Guid? VersionId { get; set; }
        public bool IsActive { get; set; } = true;
        
        // Position Identity
        public string PositionId { get; set; } // GUID as string for easy reference
        public string AvatarId { get; set; } // OASIS Avatar ID
        public string AztecAddress { get; set; } // User's Aztec address
        public string ZcashAddress { get; set; } // User's Zcash address
        
        // Collateral (ZEC locked on Zcash)
        public decimal CollateralAmount { get; set; } // ZEC amount locked
        public string CollateralTxHash { get; set; } // Zcash transaction hash
        public DateTime CollateralLockedAt { get; set; }
        public string CollateralViewingKey { get; set; } // For auditability (encrypted)
        
        // Debt (Stablecoin minted on Aztec)
        public decimal StablecoinDebt { get; set; } // Total stablecoin minted
        public decimal StablecoinBalance { get; set; } // Current stablecoin balance
        
        // Position Health
        public decimal CollateralRatio { get; set; } // Current collateral ratio (percentage)
        public PositionHealthStatus HealthStatus { get; set; }
        public DateTime LastHealthCheck { get; set; }
        
        // Yield
        public decimal YieldEarned { get; set; } // Total yield earned
        public decimal YieldAPY { get; set; } // Current APY
        public YieldStrategy YieldStrategy { get; set; }
        public DateTime LastYieldUpdate { get; set; }
        
        // Liquidation
        public bool IsLiquidated { get; set; }
        public DateTime? LiquidatedAt { get; set; }
        public string LiquidationTxHash { get; set; }
        
        // Metadata
        public Dictionary<string, object> MetaData { get; set; }
        
        public StablecoinPositionHolon()
        {
            Id = Guid.NewGuid();
            PositionId = Id.ToString();
            ProviderUniqueStorageKey = new Dictionary<ProviderType, string>();
            ProviderMetaData = new Dictionary<ProviderType, Dictionary<string, string>>();
            MetaData = new Dictionary<string, object>();
            CreatedDate = DateTime.UtcNow;
            ModifiedDate = DateTime.UtcNow;
            HealthStatus = PositionHealthStatus.Healthy;
        }
    }
    
    public enum PositionHealthStatus
    {
        Healthy,    // Above collateral ratio
        Warning,    // Between collateral ratio and liquidation threshold
        Critical    // Below liquidation threshold
    }
    
    public enum YieldStrategy
    {
        Lending,      // Lend to private lending pools
        Liquidity,    // Provide liquidity to private DEX
        Staking,      // Stake in private validators
        Custom        // Custom strategy
    }
}

