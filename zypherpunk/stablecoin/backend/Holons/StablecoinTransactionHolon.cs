using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.API.Zypherpunk.Stablecoin.Holons
{
    /// <summary>
    /// Represents a stablecoin transaction
    /// Tracks all operations (mint, redeem, yield, liquidate, transfer)
    /// </summary>
    public class StablecoinTransactionHolon : IHolon
    {
        // IHolon interface properties
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public HolonType HolonType { get; set; } = HolonType.Transaction;
        public Dictionary<ProviderType, string> ProviderUniqueStorageKey { get; set; }
        public Dictionary<ProviderType, Dictionary<string, string>> ProviderMetaData { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime ModifiedDate { get; set; }
        public DateTime? DeletedDate { get; set; }
        public Guid? PreviousVersionId { get; set; }
        public Guid? VersionId { get; set; }
        public bool IsActive { get; set; } = true;
        
        // Transaction Identity
        public string TransactionId { get; set; }
        public TransactionType Type { get; set; }
        public string PositionId { get; set; }
        
        // Transaction Details
        public decimal Amount { get; set; }
        public string FromAddress { get; set; }
        public string ToAddress { get; set; }
        
        // Chain Transactions
        public string ZcashTxHash { get; set; } // If collateral operation
        public string AztecTxHash { get; set; } // If stablecoin operation
        public string BridgeTxHash { get; set; } // If bridge operation
        
        // Status
        public TransactionStatus Status { get; set; }
        public DateTime? CompletedAt { get; set; }
        
        // Privacy
        public bool IsPrivate { get; set; } = true;
        public string ViewingKey { get; set; } // For auditability (encrypted)
        
        // Metadata
        public Dictionary<string, object> MetaData { get; set; }
        
        public StablecoinTransactionHolon()
        {
            Id = Guid.NewGuid();
            TransactionId = Id.ToString();
            ProviderUniqueStorageKey = new Dictionary<ProviderType, string>();
            ProviderMetaData = new Dictionary<ProviderType, Dictionary<string, string>>();
            MetaData = new Dictionary<string, object>();
            CreatedDate = DateTime.UtcNow;
            ModifiedDate = DateTime.UtcNow;
            Status = TransactionStatus.Pending;
        }
    }
    
    public enum TransactionType
    {
        Mint,           // Mint stablecoin
        Redeem,         // Redeem stablecoin
        YieldGenerate,  // Generate yield
        YieldDistribute, // Distribute yield
        Liquidate,      // Liquidate position
        Transfer        // Transfer stablecoin
    }
    
    public enum TransactionStatus
    {
        Pending,
        Processing,
        Completed,
        Failed,
        Cancelled
    }
}

