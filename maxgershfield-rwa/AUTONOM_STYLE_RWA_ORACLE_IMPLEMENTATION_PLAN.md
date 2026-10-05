# Autonom-Style RWA Oracle Implementation Plan

**Date:** January 2025  
**Purpose:** Roadmap for building an RWA oracle with corporate action adjustments, funding service, and risk management - inspired by Autonom (Solana Colosseum Hackathon winner)

---

## 📊 Executive Summary

**What Autonom Does:**
- Specialized oracle for Real World Assets (RWAs), particularly equities
- Accounts for corporate actions (splits, dividends, M&A) that affect share prices
- Provides dynamic pricing on-chain for perp DEXs
- Funding service for perpetual futures
- Risk module with deleveraging recommendations around risk windows

**Your Current Foundation:**
- Custom Oracle Service with multi-source data aggregation
- OASIS HyperDrive infrastructure for parallel queries
- Multi-provider architecture (60+ providers)
- Oracle feed builder with task-based pipeline
- RWA Exchange platform with fractional NFT minting
- Database layer with Entity Framework

**Gap Analysis:**
- ❌ No corporate action tracking/adjustments
- ❌ No equity-specific price feeds
- ❌ No funding rate calculations for perps
- ❌ No risk management/deleveraging recommendations
- ✅ Strong oracle infrastructure foundation
- ✅ Multi-source aggregation capability
- ✅ Flexible data pipeline system

---

## 🎯 Core Features to Build

### 1. Corporate Action Adjustment Engine ⭐ Critical

**Problem:** Equities undergo corporate actions (splits, dividends, M&A) that change share prices without reflecting true value changes. Traditional oracles don't account for these.

**Solution:** Build a corporate action adjustment layer that:
- Tracks all corporate actions for tracked equities
- Applies adjustments retroactively to price history
- Provides adjusted prices alongside raw prices
- Maintains adjustment history for auditability

**Implementation Components:**

#### A. Corporate Action Data Source Integration
```csharp
// New Entity: CorporateAction
public class CorporateAction
{
    public string Id { get; set; }
    public string Symbol { get; set; } // Stock ticker
    public CorporateActionType Type { get; set; } // Split, Dividend, Merger, etc.
    public DateTime ExDate { get; set; } // Ex-dividend/split date
    public DateTime RecordDate { get; set; }
    public DateTime EffectiveDate { get; set; }
    
    // Split-specific
    public decimal? SplitRatio { get; set; } // e.g., 2:1 split = 2.0
    
    // Dividend-specific
    public decimal? DividendAmount { get; set; }
    
    // Merger-specific
    public string? AcquiringSymbol { get; set; }
    public decimal? ExchangeRatio { get; set; } // Shares of acquiring per target share
    
    public DateTime CreatedAt { get; set; }
    public string DataSource { get; set; } // Where we got this data
}
```

**Data Sources to Integrate:**
1. **Financial Data APIs:**
   - Alpha Vantage Corporate Actions API
   - IEX Cloud Corporate Actions
   - Polygon.io Dividends & Splits
   - Yahoo Finance (scraping/API)
   - Financial Modeling Prep

2. **OASIS Oracle Task Pipeline:**
   - Create tasks to fetch corporate actions daily
   - Store in database with deduplication
   - Validate against multiple sources for consensus

#### B. Price Adjustment Calculation Engine
```csharp
public interface IPriceAdjustmentService
{
    Task<decimal> GetAdjustedPriceAsync(string symbol, decimal rawPrice, DateTime priceDate);
    Task<List<PriceAdjustment>> GetAdjustmentHistoryAsync(string symbol, DateTime fromDate, DateTime toDate);
    Task<decimal> ApplyCorporateActionAsync(decimal price, CorporateAction action);
}

// Adjustment types:
// 1. Stock Split: Price ÷ SplitRatio
//    Example: $100 stock with 2:1 split = $50 adjusted
// 2. Reverse Split: Price × SplitRatio
//    Example: $1 stock with 1:5 reverse split = $5 adjusted
// 3. Dividend: No price adjustment needed (separate tracking)
// 4. Merger: Price × ExchangeRatio (if acquiring company)
```

**Implementation Logic:**
```csharp
public async Task<decimal> GetAdjustedPriceAsync(string symbol, decimal rawPrice, DateTime priceDate)
{
    // Get all corporate actions before priceDate
    var actions = await _context.CorporateActions
        .Where(ca => ca.Symbol == symbol && ca.EffectiveDate <= priceDate)
        .OrderBy(ca => ca.EffectiveDate)
        .ToListAsync();
    
    decimal adjustedPrice = rawPrice;
    
    // Apply adjustments chronologically
    foreach (var action in actions)
    {
        adjustedPrice = ApplyCorporateAction(adjustedPrice, action);
    }
    
    return adjustedPrice;
}

private decimal ApplyCorporateAction(decimal price, CorporateAction action)
{
    return action.Type switch
    {
        CorporateActionType.StockSplit => price / action.SplitRatio.Value,
        CorporateActionType.ReverseSplit => price * action.SplitRatio.Value,
        CorporateActionType.Merger => price * action.ExchangeRatio.Value,
        _ => price // Dividends don't adjust price
    };
}
```

---

### 2. RWA Price Feed Service ⭐ Critical

**Problem:** Current oracle focuses on crypto assets. Need equity/RWA-specific feeds.

**Solution:** Extend oracle to support equities with:
- Real-time equity prices (from multiple exchanges)
- Corporate action-adjusted prices
- Confidence scoring based on data source reliability
- Account-based context (not just raw prices)

#### A. Equity Data Sources
```csharp
public interface IEquityPriceSource
{
    Task<EquityPrice> GetPriceAsync(string symbol);
    Task<List<EquityPrice>> GetPricesAsync(List<string> symbols);
    Task<EquityPriceHistory> GetPriceHistoryAsync(string symbol, DateTime from, DateTime to);
}

// Price sources:
// 1. Alpha Vantage - Free tier available
// 2. IEX Cloud - Good for US equities
// 3. Polygon.io - Real-time and historical
// 4. Yahoo Finance API/Scraping
// 5. Twelve Data
// 6. Finnhub
```

#### B. Adjusted Price Feed API
```csharp
// New Oracle Feed Type: EquityAdjustedPrice

[HttpGet("/api/oracle/rwa/equity/{symbol}/price")]
public async Task<IActionResult> GetAdjustedEquityPrice(string symbol)
{
    // 1. Fetch raw price from multiple sources
    var rawPrices = await _priceAggregator.GetPricesFromAllSourcesAsync(symbol);
    
    // 2. Calculate consensus price (weighted average)
    var consensusPrice = CalculateConsensusPrice(rawPrices);
    
    // 3. Apply corporate action adjustments
    var adjustedPrice = await _adjustmentService.GetAdjustedPriceAsync(
        symbol, 
        consensusPrice.Value, 
        DateTime.UtcNow
    );
    
    // 4. Calculate confidence score
    var confidence = CalculateConfidence(rawPrices, adjustedPrice);
    
    return Ok(new EquityPriceResponse
    {
        Symbol = symbol,
        RawPrice = consensusPrice.Value,
        AdjustedPrice = adjustedPrice,
        Confidence = confidence,
        Sources = rawPrices.Select(p => new SourceInfo
        {
            Name = p.Source,
            Price = p.Value,
            Timestamp = p.Timestamp
        }).ToList(),
        CorporateActionsApplied = await GetRecentActionsAsync(symbol),
        LastUpdated = DateTime.UtcNow
    });
}
```

#### C. Account-Based Context
Unlike raw price feeds, include:
- **Position context:** Are there open positions in this asset?
- **Leverage context:** What's the current leverage?
- **Risk context:** Are we in a risk window?
- **Corporate action alerts:** Upcoming ex-dates

---

### 3. Funding Service for Perpetual Futures ⭐ Critical

**Problem:** Perp DEXs need funding rates. Traditional crypto funding is based on premium/discount to spot. RWAs need different logic.

**Solution:** Build funding rate calculation service that:
- Calculates funding rates based on premium to adjusted spot price
- Considers corporate action windows (higher volatility expected)
- Adjusts for low liquidity periods
- Updates on-chain for perp DEXs to consume

#### A. Funding Rate Calculation
```csharp
public interface IFundingRateService
{
    Task<FundingRate> CalculateFundingRateAsync(string symbol, decimal markPrice);
    Task<List<FundingRate>> GetFundingRateHistoryAsync(string symbol, int hours);
    Task<decimal> GetCurrentFundingRateAsync(string symbol);
}

public class FundingRate
{
    public string Symbol { get; set; }
    public decimal Rate { get; set; } // Annualized percentage (e.g., 0.1 = 10%)
    public decimal HourlyRate { get; set; } // Rate per hour
    public decimal MarkPrice { get; set; }
    public decimal SpotPrice { get; set; }
    public decimal Premium { get; set; } // Mark - Spot
    public decimal PremiumPercentage { get; set; } // (Mark - Spot) / Spot
    public DateTime CalculatedAt { get; set; }
    public FundingRateFactors Factors { get; set; }
}

public class FundingRateFactors
{
    public decimal BaseRate { get; set; } // Standard funding rate
    public decimal CorporateActionAdjustment { get; set; } // Boost during CA windows
    public decimal LiquidityAdjustment { get; set; } // Adjustment for low liquidity
    public decimal VolatilityAdjustment { get; set; } // Adjustment based on volatility
}
```

#### B. Funding Rate Calculation Logic
```csharp
public async Task<FundingRate> CalculateFundingRateAsync(string symbol, decimal markPrice)
{
    // 1. Get adjusted spot price
    var spotPriceResponse = await _equityPriceService.GetAdjustedPriceAsync(symbol);
    var spotPrice = spotPriceResponse.AdjustedPrice;
    
    // 2. Calculate premium/discount
    var premium = markPrice - spotPrice;
    var premiumPercentage = (premium / spotPrice) * 100;
    
    // 3. Base funding rate (e.g., premium * 0.1 = 10% annualized)
    var baseRate = premiumPercentage * 0.1m; // Adjust multiplier as needed
    
    // 4. Check for upcoming corporate actions (within 7 days)
    var upcomingActions = await _corporateActionService.GetUpcomingActionsAsync(symbol, days: 7);
    var corporateActionAdjustment = upcomingActions.Any() ? 0.5m : 0m; // +0.5% if CA upcoming
    
    // 5. Liquidity adjustment (lower liquidity = higher funding to compensate)
    var liquidityScore = await _liquidityService.GetLiquidityScoreAsync(symbol);
    var liquidityAdjustment = (1.0m - liquidityScore) * 0.3m; // Up to +0.3% for low liquidity
    
    // 6. Volatility adjustment
    var volatility = await _volatilityService.GetVolatilityAsync(symbol, days: 30);
    var volatilityAdjustment = Math.Max(0, (volatility - 0.2m) * 0.2m); // +0.2% per 1% vol above 20%
    
    // 7. Calculate final funding rate
    var finalRate = baseRate + corporateActionAdjustment + liquidityAdjustment + volatilityAdjustment;
    var hourlyRate = finalRate / (365m * 24m); // Convert annualized to hourly
    
    return new FundingRate
    {
        Symbol = symbol,
        Rate = finalRate,
        HourlyRate = hourlyRate,
        MarkPrice = markPrice,
        SpotPrice = spotPrice,
        Premium = premium,
        PremiumPercentage = premiumPercentage,
        CalculatedAt = DateTime.UtcNow,
        Factors = new FundingRateFactors
        {
            BaseRate = baseRate,
            CorporateActionAdjustment = corporateActionAdjustment,
            LiquidityAdjustment = liquidityAdjustment,
            VolatilityAdjustment = volatilityAdjustment
        }
    };
}
```

#### C. On-Chain Publishing
For Solana perp DEXs, publish funding rates on-chain:
```csharp
public interface IOnChainFundingPublisher
{
    Task<string> PublishFundingRateAsync(string symbol, FundingRate rate);
    Task<List<FundingRate>> GetOnChainFundingRatesAsync(List<string> symbols);
}

// Publish to Solana:
// 1. Create PDA (Program Derived Address) for each symbol
// 2. Store funding rate + timestamp
// 3. Update every hour (or per funding period)
// 4. Perp DEXs read from on-chain data
```

---

### 4. Risk Management Module ⭐ Critical

**Problem:** Perp DEXs need to manage risk around corporate actions and volatile periods. Need automated deleveraging recommendations.

**Solution:** Build risk module that:
- Monitors positions and leverage
- Identifies risk windows (corporate actions, high volatility)
- Generates deleveraging recommendations
- Calculates suggested target leverage

#### A. Risk Assessment Engine
```csharp
public interface IRiskManagementService
{
    Task<RiskAssessment> AssessRiskAsync(string symbol, Position position);
    Task<List<RiskRecommendation>> GetRecommendationsAsync(string symbol);
    Task<RiskWindow> IdentifyRiskWindowAsync(string symbol, DateTime date);
}

public class RiskAssessment
{
    public string Symbol { get; set; }
    public RiskLevel Level { get; set; } // Low, Medium, High, Critical
    public decimal CurrentLeverage { get; set; }
    public decimal RecommendedLeverage { get; set; }
    public List<RiskFactor> Factors { get; set; }
    public List<RiskRecommendation> Recommendations { get; set; }
}

public class RiskFactor
{
    public RiskFactorType Type { get; set; } // CorporateAction, Volatility, Liquidity, etc.
    public string Description { get; set; }
    public decimal Impact { get; set; } // 0-1 scale
    public DateTime EffectiveDate { get; set; }
}

public enum RiskFactorType
{
    CorporateAction,
    HighVolatility,
    LowLiquidity,
    LargePosition,
    MarketEvent
}
```

#### B. Risk Window Identification
```csharp
public async Task<RiskWindow> IdentifyRiskWindowAsync(string symbol, DateTime date)
{
    var riskWindow = new RiskWindow
    {
        Symbol = symbol,
        StartDate = date,
        EndDate = date,
        RiskLevel = RiskLevel.Low,
        Factors = new List<RiskFactor>()
    };
    
    // 1. Check for corporate actions within window
    var corporateActions = await _corporateActionService.GetActionsInWindowAsync(
        symbol, 
        date.AddDays(-3), 
        date.AddDays(3)
    );
    
    if (corporateActions.Any())
    {
        riskWindow.RiskLevel = RiskLevel.High;
        riskWindow.StartDate = corporateActions.Min(ca => ca.ExDate).AddDays(-3);
        riskWindow.EndDate = corporateActions.Max(ca => ca.EffectiveDate).AddDays(3);
        
        foreach (var action in corporateActions)
        {
            riskWindow.Factors.Add(new RiskFactor
            {
                Type = RiskFactorType.CorporateAction,
                Description = $"{action.Type} effective {action.EffectiveDate:yyyy-MM-dd}",
                Impact = GetCorporateActionImpact(action.Type),
                EffectiveDate = action.EffectiveDate
            });
        }
    }
    
    // 2. Check volatility
    var volatility = await _volatilityService.GetVolatilityAsync(symbol, days: 30);
    if (volatility > 0.4m) // 40% volatility
    {
        riskWindow.RiskLevel = RiskLevel.High;
        riskWindow.Factors.Add(new RiskFactor
        {
            Type = RiskFactorType.HighVolatility,
            Description = $"High volatility: {volatility:P}",
            Impact = Math.Min(0.8m, volatility / 0.5m), // Cap at 0.8
            EffectiveDate = date
        });
    }
    
    // 3. Check liquidity
    var liquidity = await _liquidityService.GetLiquidityScoreAsync(symbol);
    if (liquidity < 0.3m) // Low liquidity
    {
        riskWindow.RiskLevel = RiskLevel.High;
        riskWindow.Factors.Add(new RiskFactor
        {
            Type = RiskFactorType.LowLiquidity,
            Description = $"Low liquidity: {liquidity:P}",
            Impact = 1.0m - liquidity,
            EffectiveDate = date
        });
    }
    
    return riskWindow;
}
```

#### C. Deleveraging Recommendations
```csharp
public async Task<List<RiskRecommendation>> GetRecommendationsAsync(string symbol)
{
    var recommendations = new List<RiskRecommendation>();
    
    // Get current positions for symbol
    var positions = await _positionService.GetPositionsAsync(symbol);
    var riskWindow = await IdentifyRiskWindowAsync(symbol, DateTime.UtcNow);
    
    foreach (var position in positions)
    {
        var assessment = await AssessRiskAsync(symbol, position);
        
        // If leverage too high for risk level, recommend deleveraging
        if (assessment.CurrentLeverage > assessment.RecommendedLeverage * 1.1m) // 10% buffer
        {
            recommendations.Add(new RiskRecommendation
            {
                Symbol = symbol,
                PositionId = position.Id,
                Action = RiskAction.Deleverage,
                CurrentLeverage = assessment.CurrentLeverage,
                TargetLeverage = assessment.RecommendedLeverage,
                ReductionPercentage = ((assessment.CurrentLeverage - assessment.RecommendedLeverage) / assessment.CurrentLeverage) * 100,
                Reason = "Risk window identified: " + string.Join(", ", riskWindow.Factors.Select(f => f.Description)),
                Priority = riskWindow.RiskLevel == RiskLevel.Critical ? Priority.High : Priority.Medium,
                RecommendedBy = DateTime.UtcNow,
                ValidUntil = riskWindow.EndDate
            });
        }
        
        // If approaching risk window, recommend gradual deleveraging
        var upcomingRiskWindow = await IdentifyRiskWindowAsync(symbol, DateTime.UtcNow.AddDays(7));
        if (upcomingRiskWindow.RiskLevel >= RiskLevel.Medium && riskWindow.RiskLevel == RiskLevel.Low)
        {
            recommendations.Add(new RiskRecommendation
            {
                Symbol = symbol,
                PositionId = position.Id,
                Action = RiskAction.GradualDeleverage,
                CurrentLeverage = assessment.CurrentLeverage,
                TargetLeverage = assessment.RecommendedLeverage * 0.8m, // Reduce to 80% of recommended
                ReductionPercentage = 20,
                Reason = "Upcoming risk window in 7 days",
                Priority = Priority.Medium,
                RecommendedBy = DateTime.UtcNow,
                ValidUntil = upcomingRiskWindow.StartDate
            });
        }
    }
    
    return recommendations;
}

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

#### D. Return to Baseline Recommendations
After risk window passes, recommend returning to normal leverage:
```csharp
public async Task<List<RiskRecommendation>> GetReturnToBaselineRecommendationsAsync(string symbol)
{
    var recommendations = new List<RiskRecommendation>();
    
    // Check if we're past a risk window
    var recentRiskWindow = await _riskWindowService.GetRecentRiskWindowAsync(symbol, days: 7);
    if (recentRiskWindow != null && DateTime.UtcNow > recentRiskWindow.EndDate)
    {
        var positions = await _positionService.GetPositionsAsync(symbol);
        var normalLeverage = GetRecommendedLeverage(RiskLevel.Low);
        
        foreach (var position in positions)
        {
            if (position.Leverage < normalLeverage * 0.9m) // If below 90% of normal
            {
                recommendations.Add(new RiskRecommendation
                {
                    Symbol = symbol,
                    PositionId = position.Id,
                    Action = RiskAction.ReturnToBaseline,
                    CurrentLeverage = position.Leverage,
                    TargetLeverage = normalLeverage,
                    IncreasePercentage = ((normalLeverage - position.Leverage) / position.Leverage) * 100,
                    Reason = "Risk window has passed, returning to baseline leverage",
                    Priority = Priority.Low,
                    RecommendedBy = DateTime.UtcNow
                });
            }
        }
    }
    
    return recommendations;
}
```

---

## 🗄️ Database Schema Extensions

### New Tables Needed

```sql
-- Corporate Actions
CREATE TABLE CorporateActions (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    Symbol NVARCHAR(20) NOT NULL,
    Type INT NOT NULL, -- 0=Split, 1=ReverseSplit, 2=Dividend, 3=Merger, etc.
    ExDate DATETIME2 NOT NULL,
    RecordDate DATETIME2 NOT NULL,
    EffectiveDate DATETIME2 NOT NULL,
    SplitRatio DECIMAL(18,8) NULL,
    DividendAmount DECIMAL(18,8) NULL,
    AcquiringSymbol NVARCHAR(20) NULL,
    ExchangeRatio DECIMAL(18,8) NULL,
    DataSource NVARCHAR(100) NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    INDEX IX_CorporateActions_Symbol_EffectiveDate (Symbol, EffectiveDate)
);

-- Equity Prices (with adjustments)
CREATE TABLE EquityPrices (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    Symbol NVARCHAR(20) NOT NULL,
    RawPrice DECIMAL(18,8) NOT NULL,
    AdjustedPrice DECIMAL(18,8) NOT NULL,
    Confidence DECIMAL(5,4) NOT NULL, -- 0-1 scale
    PriceDate DATETIME2 NOT NULL,
    Source NVARCHAR(100) NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    INDEX IX_EquityPrices_Symbol_PriceDate (Symbol, PriceDate DESC)
);

-- Funding Rates
CREATE TABLE FundingRates (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    Symbol NVARCHAR(20) NOT NULL,
    Rate DECIMAL(18,8) NOT NULL, -- Annualized percentage
    HourlyRate DECIMAL(18,12) NOT NULL,
    MarkPrice DECIMAL(18,8) NOT NULL,
    SpotPrice DECIMAL(18,8) NOT NULL,
    Premium DECIMAL(18,8) NOT NULL,
    PremiumPercentage DECIMAL(18,8) NOT NULL,
    CalculatedAt DATETIME2 NOT NULL,
    OnChainTransactionHash NVARCHAR(100) NULL, -- Solana tx hash
    INDEX IX_FundingRates_Symbol_CalculatedAt (Symbol, CalculatedAt DESC)
);

-- Risk Windows
CREATE TABLE RiskWindows (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    Symbol NVARCHAR(20) NOT NULL,
    RiskLevel INT NOT NULL,
    StartDate DATETIME2 NOT NULL,
    EndDate DATETIME2 NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    INDEX IX_RiskWindows_Symbol_StartDate (Symbol, StartDate)
);

-- Risk Factors
CREATE TABLE RiskFactors (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    RiskWindowId UNIQUEIDENTIFIER NOT NULL,
    Type INT NOT NULL,
    Description NVARCHAR(500) NOT NULL,
    Impact DECIMAL(5,4) NOT NULL, -- 0-1 scale
    EffectiveDate DATETIME2 NOT NULL,
    FOREIGN KEY (RiskWindowId) REFERENCES RiskWindows(Id)
);

-- Risk Recommendations
CREATE TABLE RiskRecommendations (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    Symbol NVARCHAR(20) NOT NULL,
    PositionId NVARCHAR(100) NULL, -- Reference to perp position
    Action INT NOT NULL, -- 0=Deleverage, 1=GradualDeleverage, 2=ReturnToBaseline
    CurrentLeverage DECIMAL(18,8) NOT NULL,
    TargetLeverage DECIMAL(18,8) NOT NULL,
    ReductionPercentage DECIMAL(5,2) NULL,
    IncreasePercentage DECIMAL(5,2) NULL,
    Reason NVARCHAR(1000) NOT NULL,
    Priority INT NOT NULL, -- 0=Low, 1=Medium, 2=High
    RecommendedBy DATETIME2 NOT NULL,
    ValidUntil DATETIME2 NULL,
    Acknowledged BIT NOT NULL DEFAULT 0,
    AcknowledgedAt DATETIME2 NULL,
    INDEX IX_RiskRecommendations_Symbol_RecommendedBy (Symbol, RecommendedBy DESC)
);
```

---

## 🔌 API Endpoints

### Corporate Actions
```
GET    /api/oracle/rwa/corporate-actions/{symbol}
GET    /api/oracle/rwa/corporate-actions/{symbol}/upcoming
POST   /api/oracle/rwa/corporate-actions (admin - add manually)
```

### Equity Prices (Adjusted)
```
GET    /api/oracle/rwa/equity/{symbol}/price
GET    /api/oracle/rwa/equity/{symbol}/price/history?from={date}&to={date}
GET    /api/oracle/rwa/equity/prices/batch (multiple symbols)
```

### Funding Rates
```
GET    /api/oracle/rwa/funding/{symbol}/rate
GET    /api/oracle/rwa/funding/{symbol}/rate/history?hours=24
GET    /api/oracle/rwa/funding/rates/batch
POST   /api/oracle/rwa/funding/{symbol}/publish-onchain (publish to Solana)
```

### Risk Management
```
GET    /api/oracle/rwa/risk/{symbol}/assessment
GET    /api/oracle/rwa/risk/{symbol}/window
GET    /api/oracle/rwa/risk/{symbol}/recommendations
GET    /api/oracle/rwa/risk/recommendations/return-to-baseline
POST   /api/oracle/rwa/risk/recommendation/{id}/acknowledge
```

---

## 📅 Implementation Phases

### Phase 1: Corporate Action Tracking (Weeks 1-2)
1. ✅ Create database schema
2. ✅ Integrate corporate action data sources (Alpha Vantage, IEX Cloud, Polygon)
3. ✅ Build corporate action ingestion service
4. ✅ Create price adjustment calculation engine
5. ✅ Test with historical data (validate adjustments)

### Phase 2: Adjusted Equity Price Feeds (Weeks 3-4)
1. ✅ Integrate equity price data sources
2. ✅ Build multi-source price aggregation
3. ✅ Implement adjusted price calculation
4. ✅ Create API endpoints
5. ✅ Add to existing oracle feed builder UI

### Phase 3: Funding Service (Weeks 5-6)
1. ✅ Design funding rate calculation logic
2. ✅ Implement funding rate service
3. ✅ Build on-chain publishing (Solana)
4. ✅ Create funding rate history tracking
5. ✅ Add funding rate feeds to oracle

### Phase 4: Risk Management Module (Weeks 7-8)
1. ✅ Build risk window identification
2. ✅ Implement risk assessment engine
3. ✅ Create deleveraging recommendation logic
4. ✅ Build return-to-baseline recommendations
5. ✅ Create risk management API endpoints
6. ✅ Add risk dashboard to frontend

### Phase 5: Integration & Testing (Weeks 9-10)
1. ✅ End-to-end testing
2. ✅ Integration with perp DEXs
3. ✅ Performance optimization
4. ✅ Documentation
5. ✅ Frontend UI polish

---

## 🔗 Integration with Existing Systems

### Leverage OASIS Oracle Infrastructure
- Use existing **Oracle Feed Builder** to create equity price feeds
- Extend **Task Pipeline** system to support corporate action ingestion
- Use **HyperDrive** for parallel data fetching from multiple sources
- Leverage **Consensus Mechanisms** for multi-source price validation

### Extend RWA Exchange Platform
- Connect corporate actions to fractional NFT positions
- Use adjusted prices for asset valuation
- Integrate risk recommendations with position management

### Solana Integration
- Publish funding rates as PDAs (Program Derived Addresses)
- Create on-chain oracle program for perp DEXs to read
- Use Anchor framework for Solana program development

---

## 📊 Data Sources & APIs

### Corporate Actions
- **Alpha Vantage:** `GET /query?function=SPLIT&symbol={symbol}&apikey={key}`
- **IEX Cloud:** `GET /stable/stock/{symbol}/splits`
- **Polygon.io:** `GET /v2/reference/splits/{ticker}`
- **Financial Modeling Prep:** `GET /api/v3/historical-price-full/stock_split/{symbol}`

### Equity Prices
- **Alpha Vantage:** `GET /query?function=GLOBAL_QUOTE&symbol={symbol}`
- **IEX Cloud:** `GET /stable/stock/{symbol}/quote`
- **Polygon.io:** `GET /v2/aggs/ticker/{ticker}/prev`
- **Yahoo Finance:** Scraping or unofficial API
- **Twelve Data:** `GET /price?symbol={symbol}&apikey={key}`

### Market Data (for risk calculations)
- **Volatility:** Calculate from historical prices (30-day rolling)
- **Liquidity:** Use volume data from price APIs
- **Positions:** Integration with perp DEX smart contracts

---

## 🎨 Frontend Components

### Corporate Actions Dashboard
- Calendar view of upcoming corporate actions
- Historical corporate actions table
- Impact visualization (price adjustments)

### Adjusted Price Feed Display
- Current price (raw vs adjusted)
- Corporate actions applied indicator
- Confidence score visualization
- Source breakdown

### Funding Rate Monitor
- Current funding rates table
- Funding rate history chart
- On-chain status indicator
- Funding rate factors breakdown

### Risk Management Dashboard
- Risk level indicators per symbol
- Active risk windows calendar
- Deleveraging recommendations list
- Position leverage vs recommended leverage
- Return-to-baseline recommendations

---

## 🔐 Security & Reliability

### Data Validation
- Multi-source consensus for corporate actions
- Outlier detection for price feeds
- Confidence scoring based on source reliability
- Historical validation (compare adjustments to known splits)

### Rate Limiting
- Respect API rate limits from data providers
- Implement caching to reduce API calls
- Use webhooks where available (e.g., Polygon)

### Error Handling
- Graceful degradation if one data source fails
- Fallback to historical data if real-time unavailable
- Alert system for data quality issues

---

## 📈 Success Metrics

1. **Data Accuracy:**
   - Corporate action detection rate: >95%
   - Price adjustment accuracy: validated against known events
   - Funding rate correlation with market premiums

2. **Performance:**
   - Price feed latency: <500ms
   - Funding rate calculation: <1 second
   - Risk assessment: <2 seconds

3. **Adoption:**
   - Number of equity symbols tracked: Start with S&P 500
   - Number of perp DEXs using funding rates
   - Number of positions using risk recommendations

---

## 🚀 Next Steps

1. **Immediate (Week 1):**
   - Set up database schema
   - Choose and integrate first corporate action data source
   - Build basic price adjustment calculator
   - Test with historical data

2. **Short Term (Month 1):**
   - Complete Phase 1 & 2
   - Integrate with existing oracle infrastructure
   - Build API endpoints
   - Create basic frontend components

3. **Medium Term (Months 2-3):**
   - Complete Phase 3 & 4
   - Solana on-chain integration
   - Full risk management module
   - Frontend polish

4. **Long Term:**
   - Expand to more asset types (commodities, bonds)
   - Machine learning for volatility prediction
   - Automated position management
   - Integration with more perp DEXs

---

## 📚 References

- **Autonom:** Solana Colosseum Hackathon winner (2024)
- **Chainlink:** Oracle architecture patterns
- **Pyth Network:** Price feed aggregation
- **Financial data APIs:** Alpha Vantage, IEX Cloud, Polygon.io documentation
- **Solana:** PDA accounts, Anchor framework

---

**Last Updated:** January 2025  
**Status:** Implementation Plan Complete ✅  
**Ready for Development:** Yes

