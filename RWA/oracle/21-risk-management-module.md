# Task 21: Risk Management Module

**Status:** 🟡 **Pending**  
**Priority:** ⭐ Critical  
**Estimated Time:** 2-3 weeks  
**Dependencies:** Task 16, Task 18, Task 19

---

## 📋 Overview

Build a comprehensive risk management module that identifies risk windows, assesses position risk, and generates deleveraging recommendations around corporate actions and volatile periods. Also provides recommendations to return to baseline leverage after risk windows pass.

---

## ✅ Objectives

1. Identify risk windows (corporate actions, high volatility, low liquidity)
2. Assess risk levels for positions
3. Generate deleveraging recommendations
4. Generate return-to-baseline recommendations
5. Track risk recommendation acknowledgments
6. Provide risk assessment APIs

---

## 🎯 Requirements

### 1. **Risk Window Identification**

```csharp
public interface IRiskWindowService
{
    Task<RiskWindow> IdentifyRiskWindowAsync(string symbol, DateTime date);
    Task<List<RiskWindow>> GetActiveRiskWindowsAsync(List<string> symbols);
    Task<List<RiskWindow>> GetUpcomingRiskWindowsAsync(List<string> symbols, int daysAhead = 7);
    Task<RiskWindow?> GetRecentRiskWindowAsync(string symbol, int daysBack = 7);
}

public class RiskWindow
{
    public Guid Id { get; set; }
    public string Symbol { get; set; }
    public RiskLevel Level { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public List<RiskFactor> Factors { get; set; }
    public DateTime CreatedAt { get; set; }
}

public enum RiskLevel
{
    Low = 0,
    Medium = 1,
    High = 2,
    Critical = 3
}

public class RiskFactor
{
    public RiskFactorType Type { get; set; }
    public string Description { get; set; }
    public decimal Impact { get; set; } // 0-1 scale
    public DateTime EffectiveDate { get; set; }
    public string? Details { get; set; } // JSON or additional info
}

public enum RiskFactorType
{
    CorporateAction = 0,
    HighVolatility = 1,
    LowLiquidity = 2,
    LargePosition = 3,
    MarketEvent = 4,
    RegulatoryEvent = 5
}
```

### 2. **Risk Assessment**

```csharp
public interface IRiskAssessmentService
{
    Task<RiskAssessment> AssessRiskAsync(string symbol, Position? position = null);
    Task<List<RiskAssessment>> AssessBatchRiskAsync(List<string> symbols);
    Task<RiskAssessment> AssessPositionRiskAsync(string symbol, Position position);
}

public class RiskAssessment
{
    public string Symbol { get; set; }
    public RiskLevel Level { get; set; }
    public decimal CurrentLeverage { get; set; }
    public decimal RecommendedLeverage { get; set; }
    public decimal RiskScore { get; set; } // 0-100 scale
    public List<RiskFactor> Factors { get; set; }
    public RiskWindow? ActiveRiskWindow { get; set; }
    public List<RiskRecommendation> Recommendations { get; set; }
    public DateTime AssessedAt { get; set; }
}

public class Position
{
    public string Id { get; set; } // Position ID from perp DEX
    public string Symbol { get; set; }
    public decimal Size { get; set; } // Position size
    public decimal Leverage { get; set; }
    public decimal EntryPrice { get; set; }
    public decimal MarkPrice { get; set; }
    public decimal LiquidationPrice { get; set; }
    public string? UserAddress { get; set; } // Optional: for user-specific assessments
}
```

### 3. **Risk Recommendation Generation**

```csharp
public interface IRiskRecommendationService
{
    Task<List<RiskRecommendation>> GetRecommendationsAsync(string symbol);
    Task<List<RiskRecommendation>> GetRecommendationsForPositionAsync(string symbol, Position position);
    Task<List<RiskRecommendation>> GetReturnToBaselineRecommendationsAsync(string symbol);
    Task<bool> AcknowledgeRecommendationAsync(Guid recommendationId);
}

public class RiskRecommendation
{
    public Guid Id { get; set; }
    public string Symbol { get; set; }
    public string? PositionId { get; set; }
    public RiskAction Action { get; set; }
    public decimal CurrentLeverage { get; set; }
    public decimal TargetLeverage { get; set; }
    public decimal ReductionPercentage { get; set; } // For deleveraging
    public decimal IncreasePercentage { get; set; } // For return to baseline
    public string Reason { get; set; }
    public Priority Priority { get; set; }
    public DateTime RecommendedBy { get; set; }
    public DateTime? ValidUntil { get; set; }
    public bool Acknowledged { get; set; }
    public DateTime? AcknowledgedAt { get; set; }
    public List<RiskFactor> RelatedFactors { get; set; }
}

public enum RiskAction
{
    Deleverage = 0,
    GradualDeleverage = 1,
    ReturnToBaseline = 2,
    ClosePosition = 3 // For critical risk
}

public enum Priority
{
    Low = 0,
    Medium = 1,
    High = 2,
    Critical = 3
}
```

### 4. **Risk Window Identification Logic**

#### Corporate Action Window:
```
If corporate action within 7 days:
  StartDate = ExDate - 3 days
  EndDate = EffectiveDate + 3 days
  RiskLevel = High
  Factor: CorporateAction with Impact = 0.8

If corporate action within 3 days:
  RiskLevel = Critical
  Factor: CorporateAction with Impact = 1.0
```

#### Volatility Window:
```
If 30-day volatility > 40%:
  RiskLevel = High
  Factor: HighVolatility with Impact = min(0.9, volatility / 50%)

If 30-day volatility > 60%:
  RiskLevel = Critical
```

#### Liquidity Window:
```
If liquidity score < 0.3:
  RiskLevel = High
  Factor: LowLiquidity with Impact = 1.0 - liquidity score
```

#### Combined Risk Window:
```
If multiple factors:
  RiskLevel = max(all factor risk levels)
  StartDate = min(all factor start dates)
  EndDate = max(all factor end dates)
```

### 5. **Leverage Recommendation Logic**

```csharp
private decimal GetRecommendedLeverage(RiskLevel riskLevel, decimal baseLeverage = 5.0m)
{
    return riskLevel switch
    {
        RiskLevel.Low => baseLeverage, // 5x
        RiskLevel.Medium => baseLeverage * 0.7m, // 3.5x
        RiskLevel.High => baseLeverage * 0.5m, // 2.5x
        RiskLevel.Critical => baseLeverage * 0.3m, // 1.5x
        _ => baseLeverage
    };
}
```

### 6. **Deleveraging Recommendation Logic**

```
If CurrentLeverage > RecommendedLeverage * 1.1: // 10% buffer
  Action = Deleverage (if within 3 days of risk window)
  Action = GradualDeleverage (if 4-7 days before risk window)
  ReductionPercentage = ((Current - Recommended) / Current) * 100
  Priority = High (if Critical risk) or Medium (if High risk)
```

### 7. **Return to Baseline Logic**

```
If past risk window and CurrentLeverage < NormalLeverage * 0.9:
  Action = ReturnToBaseline
  TargetLeverage = NormalLeverage (Low risk level)
  IncreasePercentage = ((Normal - Current) / Current) * 100
  Priority = Low (gradual return)
```

---

## 📁 Files to Create

```
Application/Contracts/IRiskWindowService.cs
Application/Contracts/IRiskAssessmentService.cs
Application/Contracts/IRiskRecommendationService.cs
Application/DTOs/RiskManagement/
  ├── RiskWindow.cs
  ├── RiskFactor.cs
  ├── RiskAssessment.cs
  ├── RiskRecommendation.cs
  ├── Position.cs
Infrastructure/ImplementationContract/RiskWindowService.cs
Infrastructure/ImplementationContract/RiskAssessmentService.cs
Infrastructure/ImplementationContract/RiskRecommendationService.cs
Domain/Entities/
  ├── RiskWindow.cs
  ├── RiskFactor.cs
  ├── RiskRecommendation.cs
Domain/Enums/
  ├── RiskLevel.cs
  ├── RiskFactorType.cs
  ├── RiskAction.cs
  ├── Priority.cs
Infrastructure/DataAccess/EntityConfigurations/
  ├── RiskWindowConfig.cs
  ├── RiskFactorConfig.cs
  ├── RiskRecommendationConfig.cs
Infrastructure/DataAccess/Migrations/[timestamp]_AddRiskManagement.cs
```

---

## 🔧 Implementation Steps

1. **Create Domain Models**
   - RiskWindow entity
   - RiskFactor entity
   - RiskRecommendation entity
   - Enums (RiskLevel, RiskFactorType, RiskAction, Priority)

2. **Create Database Schema**
   - Entity configurations
   - Migrations
   - Indexes (Symbol + StartDate, Symbol + EndDate)

3. **Implement Risk Window Service**
   - Corporate action window identification
   - Volatility window identification
   - Liquidity window identification
   - Combined window logic

4. **Implement Risk Assessment Service**
   - Risk level calculation
   - Leverage recommendation
   - Risk score calculation
   - Position risk assessment

5. **Implement Risk Recommendation Service**
   - Deleveraging recommendation logic
   - Gradual deleveraging logic
   - Return to baseline logic
   - Recommendation prioritization

6. **Create Scheduled Jobs**
   - Daily risk window identification
   - Hourly risk assessment for tracked symbols
   - Recommendation generation
   - Cleanup old recommendations

7. **Add Position Integration**
   - Interface to fetch positions from perp DEXs
   - Position risk assessment
   - Position-specific recommendations

8. **Testing**
   - Unit tests for risk window identification
   - Unit tests for risk assessment
   - Unit tests for recommendation logic
   - Integration tests with real data

---

## ✅ Acceptance Criteria

- [ ] Risk windows identified correctly for corporate actions
- [ ] Risk windows identified correctly for volatility
- [ ] Risk windows identified correctly for liquidity
- [ ] Combined risk windows calculated correctly
- [ ] Risk levels assigned correctly (Low/Medium/High/Critical)
- [ ] Leverage recommendations calculated correctly
- [ ] Deleveraging recommendations generated when needed
- [ ] Return to baseline recommendations generated after risk windows
- [ ] Recommendations can be acknowledged
- [ ] Scheduled jobs run correctly
- [ ] Database schema created and migrated
- [ ] Unit tests with >85% coverage
- [ ] Integration tests pass

---

## 📊 Test Cases

### Test Case 1: Corporate Action Risk Window

**Input:**
- Symbol: AAPL
- Corporate action: 4-for-1 split on 2024-08-31
- Current date: 2024-08-28 (3 days before)

**Expected:**
- RiskWindow created
- StartDate: 2024-08-28 (ExDate - 3)
- EndDate: 2024-09-03 (EffectiveDate + 3)
- RiskLevel: Critical (within 3 days)
- Factor: CorporateAction with Impact = 1.0

### Test Case 2: High Volatility Risk Window

**Input:**
- Symbol: VOLATILE_STOCK
- 30-day volatility: 45%

**Expected:**
- RiskWindow created
- RiskLevel: High
- Factor: HighVolatility with Impact = 0.9

### Test Case 3: Deleveraging Recommendation

**Input:**
- Symbol: AAPL
- Current Leverage: 8x
- Recommended Leverage: 2.5x (High risk)
- Risk window starts in 2 days

**Expected:**
- Recommendation: Deleverage
- Target Leverage: 2.5x
- Reduction Percentage: 68.75%
- Priority: High
- Reason: "Risk window identified: Corporate action effective 2024-08-31"

### Test Case 4: Gradual Deleveraging

**Input:**
- Current Leverage: 8x
- Recommended Leverage: 2.5x
- Risk window starts in 5 days

**Expected:**
- Recommendation: GradualDeleverage
- Target Leverage: 2.5x * 0.8 = 2.0x (80% of recommended)
- Reduction Percentage: 75%
- Priority: Medium

### Test Case 5: Return to Baseline

**Input:**
- Symbol: AAPL
- Risk window ended 2 days ago
- Current Leverage: 2x (reduced during risk window)
- Normal Leverage: 5x

**Expected:**
- Recommendation: ReturnToBaseline
- Target Leverage: 5x
- Increase Percentage: 150%
- Priority: Low

### Test Case 6: Multiple Risk Factors

**Input:**
- Corporate action in 4 days
- Volatility: 50%
- Low liquidity: 0.25

**Expected:**
- Combined risk window
- RiskLevel: Critical (highest of all factors)
- Multiple factors in RiskWindow.Factors

---

## 🔗 Related Tasks

- **Task 16:** Corporate Action Data Source Integration (depends on)
- **Task 18:** Equity Price Feed Service (depends on - for volatility/liquidity)
- **Task 19:** Funding Rate Calculation Service (depends on - uses same volatility/liquidity services)
- **Task 24:** API Endpoints - Risk Management (exposes this module)

---

## 📚 References

- Risk Management Best Practices
- Corporate Action Impact on Derivatives
- Leverage and Margin Requirements

---

## 💡 Notes

- Base leverage (5x) should be configurable
- Consider different base leverage for different asset classes
- Risk windows should overlap slightly to avoid gaps
- Recommendations should expire after risk window ends
- Consider user preferences (some users may want to keep higher leverage)
- Add alerting for critical risk levels

---

**Last Updated:** January 2025  
**Assigned To:** [Agent Name]
