# Task 22: Database Schema - Risk Management & RWA Oracle

**Status:** 🟡 **Pending**  
**Priority:** ⭐ Critical  
**Estimated Time:** 3-5 days  
**Dependencies:** None (but needed by other tasks)

---

## 📋 Overview

Create all database entities, configurations, and migrations for the RWA Oracle system including corporate actions, equity prices, funding rates, and risk management tables.

---

## ✅ Objectives

1. Create all database entities for RWA Oracle
2. Create Entity Framework configurations
3. Create database migrations
4. Add indexes for performance
5. Set up relationships between entities

---

## 🎯 Requirements

### 1. **Corporate Actions Table**

```csharp
// Domain/Entities/CorporateAction.cs

public class CorporateAction
{
    public Guid Id { get; set; }
    public string Symbol { get; set; } // Stock ticker (e.g., "AAPL")
    public CorporateActionType Type { get; set; }
    public DateTime ExDate { get; set; }
    public DateTime RecordDate { get; set; }
    public DateTime EffectiveDate { get; set; }
    
    // Split-specific
    public decimal? SplitRatio { get; set; } // Precision: 18,8
    
    // Dividend-specific
    public decimal? DividendAmount { get; set; } // Precision: 18,8
    public string? DividendCurrency { get; set; }
    
    // Merger-specific
    public string? AcquiringSymbol { get; set; }
    public decimal? ExchangeRatio { get; set; } // Precision: 18,8
    
    // Metadata
    public string DataSource { get; set; }
    public string? ExternalId { get; set; }
    public bool IsVerified { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
```

**Indexes:**
- `IX_CorporateActions_Symbol_EffectiveDate` (Symbol, EffectiveDate)
- `IX_CorporateActions_Symbol_ExDate` (Symbol, ExDate)

### 2. **Equity Prices Table**

```csharp
// Domain/Entities/EquityPrice.cs

public class EquityPrice
{
    public Guid Id { get; set; }
    public string Symbol { get; set; }
    public decimal RawPrice { get; set; } // Precision: 18,8
    public decimal AdjustedPrice { get; set; } // Precision: 18,8
    public decimal Confidence { get; set; } // Precision: 5,4 (0-1 scale)
    public DateTime PriceDate { get; set; }
    public string Source { get; set; }
    public DateTime CreatedAt { get; set; }
    
    // JSON column for source breakdown
    public string? SourceBreakdownJson { get; set; } // Store List<SourcePrice> as JSON
}
```

**Indexes:**
- `IX_EquityPrices_Symbol_PriceDate` (Symbol, PriceDate DESC)

### 3. **Funding Rates Table**

```csharp
// Domain/Entities/FundingRate.cs

public class FundingRate
{
    public Guid Id { get; set; }
    public string Symbol { get; set; }
    public decimal Rate { get; set; } // Annualized percentage, Precision: 18,8
    public decimal HourlyRate { get; set; } // Precision: 18,12
    public decimal MarkPrice { get; set; } // Precision: 18,8
    public decimal SpotPrice { get; set; } // Precision: 18,8
    public decimal AdjustedSpotPrice { get; set; } // Precision: 18,8
    public decimal Premium { get; set; } // Precision: 18,8 (can be negative)
    public decimal PremiumPercentage { get; set; } // Precision: 18,8
    public decimal BaseRate { get; set; } // Precision: 18,8
    public decimal CorporateActionAdjustment { get; set; } // Precision: 18,8
    public decimal LiquidityAdjustment { get; set; } // Precision: 18,8
    public decimal VolatilityAdjustment { get; set; } // Precision: 18,8
    public DateTime CalculatedAt { get; set; }
    public DateTime ValidUntil { get; set; }
    public string? OnChainTransactionHash { get; set; }
}
```

**Indexes:**
- `IX_FundingRates_Symbol_CalculatedAt` (Symbol, CalculatedAt DESC)
- `IX_FundingRates_Symbol_ValidUntil` (Symbol, ValidUntil)

### 4. **Risk Windows Table**

```csharp
// Domain/Entities/RiskWindow.cs

public class RiskWindow
{
    public Guid Id { get; set; }
    public string Symbol { get; set; }
    public RiskLevel Level { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime CreatedAt { get; set; }
    
    // Navigation property
    public ICollection<RiskFactor> Factors { get; set; }
}
```

**Indexes:**
- `IX_RiskWindows_Symbol_StartDate` (Symbol, StartDate)
- `IX_RiskWindows_Symbol_EndDate` (Symbol, EndDate)
- `IX_RiskWindows_StartDate_EndDate` (StartDate, EndDate)

### 5. **Risk Factors Table**

```csharp
// Domain/Entities/RiskFactor.cs

public class RiskFactor
{
    public Guid Id { get; set; }
    public Guid RiskWindowId { get; set; }
    public RiskFactorType Type { get; set; }
    public string Description { get; set; } // Max 500 chars
    public decimal Impact { get; set; } // Precision: 5,4 (0-1 scale)
    public DateTime EffectiveDate { get; set; }
    public string? Details { get; set; } // JSON, nullable
    
    // Navigation property
    public RiskWindow RiskWindow { get; set; }
}
```

**Indexes:**
- `IX_RiskFactors_RiskWindowId` (RiskWindowId)
- `IX_RiskFactors_Type_EffectiveDate` (Type, EffectiveDate)

### 6. **Risk Recommendations Table**

```csharp
// Domain/Entities/RiskRecommendation.cs

public class RiskRecommendation
{
    public Guid Id { get; set; }
    public string Symbol { get; set; }
    public string? PositionId { get; set; } // Nullable, from perp DEX
    public RiskAction Action { get; set; }
    public decimal CurrentLeverage { get; set; } // Precision: 18,8
    public decimal TargetLeverage { get; set; } // Precision: 18,8
    public decimal? ReductionPercentage { get; set; } // Precision: 5,2 (nullable)
    public decimal? IncreasePercentage { get; set; } // Precision: 5,2 (nullable)
    public string Reason { get; set; } // Max 1000 chars
    public Priority Priority { get; set; }
    public DateTime RecommendedBy { get; set; }
    public DateTime? ValidUntil { get; set; } // Nullable
    public bool Acknowledged { get; set; }
    public DateTime? AcknowledgedAt { get; set; } // Nullable
    public string? AcknowledgedBy { get; set; } // User/avatar ID, nullable
}
```

**Indexes:**
- `IX_RiskRecommendations_Symbol_RecommendedBy` (Symbol, RecommendedBy DESC)
- `IX_RiskRecommendations_Symbol_Acknowledged` (Symbol, Acknowledged)
- `IX_RiskRecommendations_PositionId` (PositionId) // Where PositionId IS NOT NULL
- `IX_RiskRecommendations_ValidUntil` (ValidUntil) // Where ValidUntil IS NOT NULL

---

## 📁 Files to Create

```
Domain/Entities/
  ├── CorporateAction.cs
  ├── EquityPrice.cs
  ├── FundingRate.cs
  ├── RiskWindow.cs
  ├── RiskFactor.cs
  └── RiskRecommendation.cs

Domain/Enums/
  ├── CorporateActionType.cs
  ├── RiskLevel.cs
  ├── RiskFactorType.cs
  ├── RiskAction.cs
  └── Priority.cs

Infrastructure/DataAccess/EntityConfigurations/
  ├── CorporateActionConfig.cs
  ├── EquityPriceConfig.cs
  ├── FundingRateConfig.cs
  ├── RiskWindowConfig.cs
  ├── RiskFactorConfig.cs
  └── RiskRecommendationConfig.cs

Infrastructure/DataAccess/Migrations/
  └── [timestamp]_AddRwaOracleTables.cs

Infrastructure/DataAccess/DataContext.cs (add DbSets)
```

---

## 🔧 Implementation Steps

1. **Create Enums**
   - CorporateActionType
   - RiskLevel
   - RiskFactorType
   - RiskAction
   - Priority

2. **Create Entities**
   - CorporateAction
   - EquityPrice
   - FundingRate
   - RiskWindow
   - RiskFactor
   - RiskRecommendation

3. **Create Entity Configurations**
   - Set precision for all decimal fields
   - Configure string max lengths
   - Set up relationships (RiskWindow -> RiskFactor)
   - Configure indexes

4. **Add DbSets to DataContext**
   ```csharp
   public DbSet<CorporateAction> CorporateActions { get; set; }
   public DbSet<EquityPrice> EquityPrices { get; set; }
   public DbSet<FundingRate> FundingRates { get; set; }
   public DbSet<RiskWindow> RiskWindows { get; set; }
   public DbSet<RiskFactor> RiskFactors { get; set; }
   public DbSet<RiskRecommendation> RiskRecommendations { get; set; }
   ```

5. **Create Migration**
   - Use Entity Framework migrations
   - Review generated SQL
   - Ensure all indexes are created
   - Ensure all constraints are correct

6. **Test Migration**
   - Apply migration to test database
   - Verify all tables created
   - Verify all indexes created
   - Test insert/update/delete operations

---

## ✅ Acceptance Criteria

- [ ] All entities created with correct properties
- [ ] All enums created
- [ ] All Entity Framework configurations created
- [ ] All decimal fields have proper precision (18,8 or appropriate)
- [ ] All string fields have max length constraints
- [ ] All relationships configured correctly
- [ ] All indexes created
- [ ] Migration created successfully
- [ ] Migration can be applied to database
- [ ] Migration can be rolled back
- [ ] All tables queryable after migration

---

## 📊 Entity Configuration Examples

### CorporateAction Configuration:

```csharp
public class CorporateActionConfig : IEntityTypeConfiguration<CorporateAction>
{
    public void Configure(EntityTypeBuilder<CorporateAction> builder)
    {
        builder.ToTable("CorporateActions");
        
        builder.HasKey(ca => ca.Id);
        
        builder.Property(ca => ca.Symbol)
            .IsRequired()
            .HasMaxLength(20);
        
        builder.Property(ca => ca.SplitRatio)
            .HasPrecision(18, 8);
        
        builder.Property(ca => ca.DividendAmount)
            .HasPrecision(18, 8);
        
        builder.Property(ca => ca.ExchangeRatio)
            .HasPrecision(18, 8);
        
        builder.HasIndex(ca => new { ca.Symbol, ca.EffectiveDate })
            .HasDatabaseName("IX_CorporateActions_Symbol_EffectiveDate");
        
        builder.HasIndex(ca => new { ca.Symbol, ca.ExDate })
            .HasDatabaseName("IX_CorporateActions_Symbol_ExDate");
    }
}
```

### RiskWindow Configuration:

```csharp
public class RiskWindowConfig : IEntityTypeConfiguration<RiskWindow>
{
    public void Configure(EntityTypeBuilder<RiskWindow> builder)
    {
        builder.ToTable("RiskWindows");
        
        builder.HasKey(rw => rw.Id);
        
        builder.Property(rw => rw.Symbol)
            .IsRequired()
            .HasMaxLength(20);
        
        builder.HasMany(rw => rw.Factors)
            .WithOne(rf => rf.RiskWindow)
            .HasForeignKey(rf => rf.RiskWindowId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasIndex(rw => new { rw.Symbol, rw.StartDate })
            .HasDatabaseName("IX_RiskWindows_Symbol_StartDate");
        
        builder.HasIndex(rw => new { rw.Symbol, rw.EndDate })
            .HasDatabaseName("IX_RiskWindows_Symbol_EndDate");
    }
}
```

---

## 🔗 Related Tasks

- **Task 16:** Corporate Action Data Source Integration (uses CorporateAction entity)
- **Task 17:** Corporate Action Price Adjustment Engine (uses CorporateAction entity)
- **Task 18:** Equity Price Feed Service (uses EquityPrice entity)
- **Task 19:** Funding Rate Calculation Service (uses FundingRate entity)
- **Task 21:** Risk Management Module (uses RiskWindow, RiskFactor, RiskRecommendation entities)

---

## 📚 References

- Entity Framework Core Documentation
- Entity Framework Migrations
- SQL Server Index Best Practices

---

## 💡 Notes

- Use decimal(18,8) for most prices and ratios
- Use decimal(5,4) for confidence scores and impact (0-1 range)
- Use decimal(18,12) for hourly rates (very small values)
- Consider JSON columns for flexible data (SourceBreakdown, Details)
- All timestamps should be UTC
- Consider adding CreatedAt/UpdatedAt audit fields to all tables
- Add soft delete capability if needed (IsDeleted flag)

---

**Last Updated:** January 2025  
**Assigned To:** [Agent Name]
