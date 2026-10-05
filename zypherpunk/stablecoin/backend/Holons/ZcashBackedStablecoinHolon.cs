using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.API.Zypherpunk.Stablecoin.Holons
{
    /// <summary>
    /// Represents the stablecoin system state
    /// Stored as a Holon for automatic replication and versioning
    /// </summary>
    public class ZcashBackedStablecoinHolon : IHolon
    {
        // IHolon interface properties
        public Guid Id { get; set; }
        public string Name { get; set; } = "Zcash-Backed Stablecoin";
        public string Description { get; set; }
        public HolonType HolonType { get; set; } = HolonType.StablecoinSystem;
        public Dictionary<ProviderType, string> ProviderUniqueStorageKey { get; set; }
        public Dictionary<ProviderType, Dictionary<string, string>> ProviderMetaData { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime ModifiedDate { get; set; }
        public DateTime? DeletedDate { get; set; }
        public Guid? PreviousVersionId { get; set; }
        public Guid? VersionId { get; set; }
        public bool IsActive { get; set; } = true;
        
        // Stablecoin Identity
        public string StablecoinName { get; set; } = "zUSD"; // or "zUSDC"
        public string Symbol { get; set; } = "zUSD";
        public int Decimals { get; set; } = 18;
        
        // System State
        public decimal TotalSupply { get; set; } // Total stablecoin minted
        public decimal TotalCollateral { get; set; } // Total ZEC locked
        public decimal TotalDebt { get; set; } // Total stablecoin debt
        
        // Risk Parameters
        public decimal CollateralRatio { get; set; } = 150m; // 150% (1.5x)
        public decimal LiquidationThreshold { get; set; } = 120m; // 120% (1.2x)
        public decimal LiquidationBonus { get; set; } = 5m; // 5% bonus for liquidators
        public decimal MaxCollateralRatio { get; set; } = 200m; // 200% max
        public decimal MinCollateralRatio { get; set; } = 130m; // 130% min
        
        // Oracle Configuration
        public string OracleProvider { get; set; } = "CustomZcashOracle";
        public decimal CurrentZECPrice { get; set; }
        public DateTime LastPriceUpdate { get; set; }
        public int PriceUpdateInterval { get; set; } = 60; // seconds
        
        // Yield Configuration
        public YieldStrategy ActiveYieldStrategy { get; set; }
        public decimal CurrentAPY { get; set; }
        public decimal TotalYieldGenerated { get; set; }
        public DateTime LastYieldDistribution { get; set; }
        
        // Aztec Contract
        public string AztecContractAddress { get; set; }
        public string AztecContractHash { get; set; }
        
        // Metadata
        public Dictionary<string, object> MetaData { get; set; }
        
        public ZcashBackedStablecoinHolon()
        {
            Id = Guid.NewGuid();
            ProviderUniqueStorageKey = new Dictionary<ProviderType, string>();
            ProviderMetaData = new Dictionary<ProviderType, Dictionary<string, string>>();
            MetaData = new Dictionary<string, object>();
            CreatedDate = DateTime.UtcNow;
            ModifiedDate = DateTime.UtcNow;
        }
    }
}

