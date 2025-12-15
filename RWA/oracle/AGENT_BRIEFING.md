# RWA Oracle - Agent Briefing Document

**Date:** January 2025  
**Project:** Autonom-Style RWA Oracle Implementation  
**Total Tasks:** 11 Task Briefs

---

## 📋 Overview

This document provides agent briefing instructions for implementing an RWA Oracle system with corporate action adjustments, funding rates, and risk management. Each task brief contains complete specifications, requirements, and acceptance criteria.

---

## 🎯 Project Goals

Build an oracle system that enables:
1. **Accurate RWA pricing** with corporate action adjustments
2. **Funding rates for perpetual futures** on equities/RWAs
3. **Risk management** with deleveraging recommendations
4. **On-chain integration** with Solana perp DEXs

---

## 📚 Preparation for Agents

### Before Starting, Review:
1. **Main Implementation Plan:** `AUTONOM_STYLE_RWA_ORACLE_IMPLEMENTATION_PLAN.md`
   - Complete technical architecture
   - Code examples and patterns
   - Database schemas
   - Integration strategies

2. **Capabilities Document:** `END_PRODUCT_CAPABILITIES_AND_OASIS_ADVANTAGES.md`
   - Understand what you're building
   - Why OASIS provides advantages
   - Use cases and examples

3. **Your Specific Task Brief:** See task assignments below
   - Complete requirements
   - Implementation steps
   - Acceptance criteria
   - Test cases

---

## 🚀 Task Assignments

### Phase 1: Foundation & Data (Weeks 1-4)

#### 📌 Task 16: Corporate Action Data Source Integration
**Brief:** `16-corporate-action-data-source-integration.md`

**Agent Instructions:**
- **Priority:** ⭐ Critical
- **Estimated Time:** 1-2 weeks
- **Dependencies:** None (can start immediately)

**Key Responsibilities:**
1. Integrate at least 3 financial data APIs (Alpha Vantage, IEX Cloud, Polygon.io)
2. Create `CorporateAction` entity and database schema
3. Build ingestion service with deduplication logic
4. Create scheduled job for daily updates
5. Implement multi-source validation/consensus

**Critical Success Factors:**
- Must handle rate limiting correctly
- Multi-source consensus working
- Deduplication prevents duplicates
- Scheduled job runs reliably

**Files to Create:**
- `Domain/Entities/CorporateAction.cs`
- `Application/Contracts/ICorporateActionService.cs`
- `Infrastructure/ImplementationContract/CorporateActionService.cs`
- `Infrastructure/ExternalServices/CorporateActions/` (data source adapters)
- Database migration

---

#### 📌 Task 17: Corporate Action Price Adjustment Engine
**Brief:** `17-corporate-action-price-adjustment-engine.md`

**Agent Instructions:**
- **Priority:** ⭐ Critical
- **Estimated Time:** 1 week
- **Dependencies:** Task 16 (needs CorporateAction entity)

**Key Responsibilities:**
1. Build price adjustment calculation service
2. Implement adjustment algorithms (splits, reverse splits, mergers)
3. Create adjustment history tracking
4. Optimize with caching
5. Validate against known historical events (e.g., AAPL splits)

**Critical Success Factors:**
- Adjustments calculated correctly for all action types
- Performance: <100ms for single adjustment
- Validated against known corporate actions
- Caching reduces database queries

**Files to Create:**
- `Application/Contracts/IPriceAdjustmentService.cs`
- `Infrastructure/ImplementationContract/PriceAdjustmentService.cs`
- `Application/DTOs/PriceAdjustment/`

---

#### 📌 Task 18: Equity Price Feed Service
**Brief:** `18-equity-price-feed-service.md`

**Agent Instructions:**
- **Priority:** ⭐ Critical
- **Estimated Time:** 1-2 weeks
- **Dependencies:** Task 16, Task 17

**Key Responsibilities:**
1. Integrate at least 3 equity price data sources
2. Implement multi-source consensus calculation
3. Apply corporate action adjustments (use Task 17 service)
4. Calculate confidence scores
5. Integrate with existing OASIS oracle infrastructure

**Critical Success Factors:**
- Multi-source consensus working correctly
- Outlier detection removes bad prices
- Confidence scores accurate (0-1 scale)
- Performance: <500ms for single symbol

**Files to Create:**
- `Application/Contracts/IEquityPriceService.cs`
- `Infrastructure/ImplementationContract/EquityPriceService.cs`
- `Infrastructure/ExternalServices/EquityPrices/` (data source adapters)
- `Domain/Entities/EquityPrice.cs`

---

#### 📌 Task 22: Database Schema - Risk Management & RWA Oracle
**Brief:** `22-database-schema-risk-management.md`

**Agent Instructions:**
- **Priority:** ⭐ Critical
- **Estimated Time:** 3-5 days
- **Dependencies:** None (should be done first or early)

**Key Responsibilities:**
1. Create all database entities (CorporateAction, EquityPrice, FundingRate, RiskWindow, RiskFactor, RiskRecommendation)
2. Create Entity Framework configurations with proper precision
3. Create database migration
4. Add indexes for performance
5. Set up relationships

**Critical Success Factors:**
- All decimal fields have correct precision
- All indexes created
- Migration can be applied and rolled back
- Relationships configured correctly

**Files to Create:**
- `Domain/Entities/` (all entities)
- `Domain/Enums/` (all enums)
- `Infrastructure/DataAccess/EntityConfigurations/` (all configs)
- `Infrastructure/DataAccess/Migrations/[timestamp]_AddRwaOracleTables.cs`

---

### Phase 2: Funding Rates (Weeks 5-7)

#### 📌 Task 19: Funding Rate Calculation Service
**Brief:** `19-funding-rate-calculation-service.md`

**Agent Instructions:**
- **Priority:** ⭐ Critical
- **Estimated Time:** 1-2 weeks
- **Dependencies:** Task 18 (needs equity price service)

**Key Responsibilities:**
1. Build funding rate calculation engine
2. Implement base rate from premium to spot
3. Add corporate action adjustments
4. Add liquidity adjustments
5. Add volatility adjustments
6. Create supporting services (ILiquidityService, IVolatilityService)

**Critical Success Factors:**
- All adjustments combined correctly
- Rate caps applied (max 100%, min -100%)
- Hourly rate calculated correctly
- Performance: <1 second per symbol

**Files to Create:**
- `Application/Contracts/IFundingRateService.cs`
- `Application/Contracts/ILiquidityService.cs`
- `Application/Contracts/IVolatilityService.cs`
- `Infrastructure/ImplementationContract/FundingRateService.cs`
- `Infrastructure/ImplementationContract/LiquidityService.cs`
- `Infrastructure/ImplementationContract/VolatilityService.cs`
- `Domain/Entities/FundingRate.cs`

---

#### 📌 Task 20: Funding Rate On-Chain Publishing (Solana)
**Brief:** `20-funding-rate-onchain-publishing-solana.md`

**Agent Instructions:**
- **Priority:** ⭐ Critical
- **Estimated Time:** 1-2 weeks
- **Dependencies:** Task 19 (needs funding rate service)

**Key Responsibilities:**
1. Create Solana program (Anchor) for storing funding rates
2. Implement PDA-based storage for each symbol
3. Build service to publish rates on-chain
4. Handle transaction signing and confirmation
5. Test on devnet before mainnet

**Critical Success Factors:**
- Program compiles and deploys
- PDAs created correctly
- Rates can be read from on-chain
- Transactions confirmed reliably
- Performance: <5 seconds per symbol

**Files to Create:**
- `programs/rwa-oracle/src/lib.rs` (Anchor program)
- `Infrastructure/Blockchain/Solana/IOnChainFundingPublisher.cs`
- `Infrastructure/Blockchain/Solana/OnChainFundingPublisher.cs`

---

### Phase 3: Risk Management (Weeks 8-10)

#### 📌 Task 21: Risk Management Module
**Brief:** `21-risk-management-module.md`

**Agent Instructions:**
- **Priority:** ⭐ Critical
- **Estimated Time:** 2-3 weeks
- **Dependencies:** Task 16, Task 18, Task 19

**Key Responsibilities:**
1. Build risk window identification (corporate actions, volatility, liquidity)
2. Implement risk assessment engine
3. Create deleveraging recommendation logic
4. Create return-to-baseline recommendation logic
5. Build scheduled jobs for assessments

**Critical Success Factors:**
- Risk windows identified correctly
- Leverage recommendations calculated correctly
- Recommendations generated when needed
- Recommendations can be acknowledged

**Files to Create:**
- `Application/Contracts/IRiskWindowService.cs`
- `Application/Contracts/IRiskAssessmentService.cs`
- `Application/Contracts/IRiskRecommendationService.cs`
- `Infrastructure/ImplementationContract/RiskWindowService.cs`
- `Infrastructure/ImplementationContract/RiskAssessmentService.cs`
- `Infrastructure/ImplementationContract/RiskRecommendationService.cs`

---

### Phase 4: API Layer (Weeks 11-12)

#### 📌 Task 23: API Endpoints - Corporate Actions
**Brief:** `23-api-endpoints-corporate-actions.md`

**Agent Instructions:**
- **Priority:** ⭐ Critical
- **Estimated Time:** 3-5 days
- **Dependencies:** Task 16, Task 22

**Key Responsibilities:**
1. Create REST API endpoints for corporate actions
2. Implement filtering and pagination
3. Add input validation
4. Create response DTOs
5. Add authorization (admin for POST)

**Endpoints to Create:**
- `GET /api/oracle/rwa/corporate-actions/{symbol}`
- `GET /api/oracle/rwa/corporate-actions/{symbol}/upcoming`
- `GET /api/oracle/rwa/corporate-actions/{id}`
- `POST /api/oracle/rwa/corporate-actions` (admin)

**Files to Create:**
- `API/Controllers/RwaOracle/CorporateActionsController.cs`
- `Application/DTOs/CorporateAction/` (all DTOs)

---

#### 📌 Task 24: API Endpoints - Equity Prices
**Brief:** `24-api-endpoints-equity-prices.md`

**Agent Instructions:**
- **Priority:** ⭐ Critical
- **Estimated Time:** 3-5 days
- **Dependencies:** Task 18, Task 22

**Key Responsibilities:**
1. Create REST API endpoints for equity prices
2. Support raw and adjusted prices
3. Implement historical price queries
4. Add batch price queries
5. Add caching headers

**Endpoints to Create:**
- `GET /api/oracle/rwa/equity/{symbol}/price`
- `GET /api/oracle/rwa/equity/{symbol}/price/history`
- `GET /api/oracle/rwa/equity/prices/batch`
- `GET /api/oracle/rwa/equity/{symbol}/price/at-date`

**Files to Create:**
- `API/Controllers/RwaOracle/EquityPricesController.cs`
- `Application/DTOs/EquityPrice/` (all DTOs)

---

#### 📌 Task 25: API Endpoints - Funding Rates
**Brief:** `25-api-endpoints-funding-rates.md`

**Agent Instructions:**
- **Priority:** ⭐ Critical
- **Estimated Time:** 3-5 days
- **Dependencies:** Task 19, Task 20, Task 22

**Key Responsibilities:**
1. Create REST API endpoints for funding rates
2. Include funding rate factors in responses
3. Include on-chain status
4. Implement historical rate queries
5. Add batch queries

**Endpoints to Create:**
- `GET /api/oracle/rwa/funding/{symbol}/rate`
- `GET /api/oracle/rwa/funding/{symbol}/rate/history`
- `GET /api/oracle/rwa/funding/rates/batch`
- `POST /api/oracle/rwa/funding/{symbol}/publish-onchain` (admin)

**Files to Create:**
- `API/Controllers/RwaOracle/FundingRatesController.cs`
- `Application/DTOs/FundingRate/` (all DTOs)

---

#### 📌 Task 26: API Endpoints - Risk Management
**Brief:** `26-api-endpoints-risk-management.md`

**Agent Instructions:**
- **Priority:** ⭐ Critical
- **Estimated Time:** 3-5 days
- **Dependencies:** Task 21, Task 22

**Key Responsibilities:**
1. Create REST API endpoints for risk management
2. Implement filtering and pagination
3. Add recommendation acknowledgment endpoint
4. Create response DTOs
5. Add authentication requirement

**Endpoints to Create:**
- `GET /api/oracle/rwa/risk/{symbol}/assessment`
- `GET /api/oracle/rwa/risk/{symbol}/window`
- `GET /api/oracle/rwa/risk/{symbol}/recommendations`
- `GET /api/oracle/rwa/risk/recommendations/return-to-baseline`
- `POST /api/oracle/rwa/risk/recommendation/{id}/acknowledge`
- `GET /api/oracle/rwa/risk/windows/active`

**Files to Create:**
- `API/Controllers/RwaOracle/RiskManagementController.cs`
- `Application/DTOs/RiskManagement/` (all DTOs)

---

## 📋 Common Requirements for All Agents

### Code Standards
- Follow existing codebase patterns and conventions
- Use Entity Framework for database access
- Use AutoMapper for entity-to-DTO mapping
- Follow OASIS API response format: `{ result, isError, message }`
- Use async/await for all async operations
- Add proper error handling and logging

### Testing Requirements
- Unit tests with >80% coverage (critical services >85%)
- Integration tests for API endpoints
- Test with real data where possible
- Validate against known examples (e.g., AAPL corporate actions)

### Documentation
- XML comments on public methods
- Update README if adding new features
- Document any configuration needed

### Integration Points
- Register services in `API/Infrastructure/DI/CustomServiceRegister.cs`
- Use existing OASIS infrastructure (HyperDrive, providers)
- Follow existing authentication/authorization patterns

---

## 🔗 Important References

### Project Location
- **Root:** `/Volumes/Storage/OASIS_CLEAN/RWA/`
- **Backend:** `/Volumes/Storage/OASIS_CLEAN/RWA/backend/`
- **Task Briefs:** `/Volumes/Storage/OASIS_CLEAN/RWA/oracle/`

### Key Files to Reference
- **Implementation Plan:** `oracle/AUTONOM_STYLE_RWA_ORACLE_IMPLEMENTATION_PLAN.md`
- **Database Context:** `backend/src/api/Infrastructure/DataAccess/DataContext.cs`
- **Service Registration:** `backend/src/api/API/Infrastructure/DI/CustomServiceRegister.cs`
- **OASIS API Client:** Check existing API integration patterns in codebase

### External Dependencies
- **Entity Framework Core:** For database access
- **AutoMapper:** For DTO mapping
- **Solana/Anchor:** For Task 20 (on-chain publishing)
- **Financial APIs:** Alpha Vantage, IEX Cloud, Polygon.io (get API keys)

---

## ✅ Completion Checklist for Agents

Before marking task complete, ensure:
- [ ] All acceptance criteria met
- [ ] Unit tests written and passing (>80% coverage)
- [ ] Integration tests written and passing
- [ ] Code reviewed for quality
- [ ] Services registered in DI container
- [ ] Database migrations applied successfully
- [ ] API endpoints tested (if applicable)
- [ ] Documentation updated
- [ ] Task brief marked as complete

---

## 📞 Support & Questions

If you encounter issues:
1. Review the main implementation plan for architecture guidance
2. Check existing codebase for similar patterns
3. Reference the task brief for detailed requirements
4. Test with known examples (AAPL splits, etc.)

---

## 🎯 Success Metrics

**Overall Project Success:**
- Corporate actions tracked for tracked symbols
- Prices adjusted correctly for corporate actions
- Funding rates calculated and published on-chain
- Risk recommendations generated correctly
- All API endpoints working
- Performance targets met (<500ms for price queries, <1s for funding rates)

**Good luck! 🚀**

---

**Last Updated:** January 2025  
**Status:** Ready for Agent Assignment ✅

