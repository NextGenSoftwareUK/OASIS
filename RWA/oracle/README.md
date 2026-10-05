# RWA Oracle - Task Briefs & Documentation

**Date:** January 2025  
**Purpose:** All documentation and task briefs for the Autonom-style RWA Oracle implementation

---

## 📋 Overview

This folder contains all task briefs, implementation plans, and documentation for building an RWA Oracle system with corporate action adjustments, funding rates, and risk management - inspired by Autonom (Solana Colosseum Hackathon winner).

---

## 📚 Main Documents

### **Implementation Plan**
- **`AUTONOM_STYLE_RWA_ORACLE_IMPLEMENTATION_PLAN.md`**
  - Complete technical implementation plan
  - Architecture, code examples, database schemas
  - API specifications, integration strategies
  - 10-week phased implementation timeline

### **Capabilities & Advantages**
- **`END_PRODUCT_CAPABILITIES_AND_OASIS_ADVANTAGES.md`**
  - What the end product enables
  - OASIS unique advantages
  - Competitive analysis
  - Use case examples

### **Task Summary**
- **`AUTONOM_RWA_ORACLE_TASKS_SUMMARY.md`**
  - Complete list of all tasks
  - Dependency graph
  - Recommended execution order
  - Task assignment recommendations

---

## 🎯 Task Briefs

### Phase 1: Foundation & Data (Weeks 1-4)

#### Task 16: Corporate Action Data Source Integration
- **File:** `16-corporate-action-data-source-integration.md`
- **Time:** 1-2 weeks
- **Dependencies:** None
- **Description:** Integrate financial data APIs to fetch and store corporate actions

#### Task 17: Corporate Action Price Adjustment Engine
- **File:** `17-corporate-action-price-adjustment-engine.md`
- **Time:** 1 week
- **Dependencies:** Task 16
- **Description:** Build engine to apply corporate action adjustments to prices

#### Task 18: Equity Price Feed Service
- **File:** `18-equity-price-feed-service.md`
- **Time:** 1-2 weeks
- **Dependencies:** Task 16, Task 17
- **Description:** Build equity price aggregation with multi-source consensus

#### Task 22: Database Schema - Risk Management & RWA Oracle
- **File:** `22-database-schema-risk-management.md`
- **Time:** 3-5 days
- **Dependencies:** None (but needed by all tasks)
- **Description:** Create all database entities, configurations, and migrations

---

### Phase 2: Funding Rates (Weeks 5-7)

#### Task 19: Funding Rate Calculation Service
- **File:** `19-funding-rate-calculation-service.md`
- **Time:** 1-2 weeks
- **Dependencies:** Task 18
- **Description:** Build funding rate calculation engine for perpetual futures

#### Task 20: Funding Rate On-Chain Publishing (Solana)
- **File:** `20-funding-rate-onchain-publishing-solana.md`
- **Time:** 1-2 weeks
- **Dependencies:** Task 19
- **Description:** Create Solana program and service to publish funding rates on-chain

---

### Phase 3: Risk Management (Weeks 8-10)

#### Task 21: Risk Management Module
- **File:** `21-risk-management-module.md`
- **Time:** 2-3 weeks
- **Dependencies:** Task 16, Task 18, Task 19
- **Description:** Build risk window identification and recommendations

---

### Phase 4: API Layer (Weeks 11-12)

#### Task 23: API Endpoints - Corporate Actions
- **File:** `23-api-endpoints-corporate-actions.md`
- **Time:** 3-5 days
- **Dependencies:** Task 16, Task 22
- **Description:** Create REST API endpoints for corporate actions

#### Task 24: API Endpoints - Equity Prices
- **File:** `24-api-endpoints-equity-prices.md`
- **Time:** 3-5 days
- **Dependencies:** Task 18, Task 22
- **Description:** Create REST API endpoints for equity prices

#### Task 25: API Endpoints - Funding Rates
- **File:** `25-api-endpoints-funding-rates.md`
- **Time:** 3-5 days
- **Dependencies:** Task 19, Task 20, Task 22
- **Description:** Create REST API endpoints for funding rates

#### Task 26: API Endpoints - Risk Management
- **File:** `26-api-endpoints-risk-management.md`
- **Time:** 3-5 days
- **Dependencies:** Task 21, Task 22
- **Description:** Create REST API endpoints for risk management

---

## 🚀 Quick Start

1. **Read the Implementation Plan** - Start with `AUTONOM_STYLE_RWA_ORACLE_IMPLEMENTATION_PLAN.md`
2. **Review Capabilities** - Understand what you're building: `END_PRODUCT_CAPABILITIES_AND_OASIS_ADVANTAGES.md`
3. **Check Task Summary** - See all tasks and dependencies: `AUTONOM_RWA_ORACLE_TASKS_SUMMARY.md`
4. **Assign Tasks** - Use individual task briefs (16-26) for specific assignments

---

## 📊 Task Dependencies

```
Task 22 (Database Schema) ──┐
                             ├─> All other tasks
Task 16 (Corporate Actions) ─┼─> Task 17 (Price Adjustment)
                             │
                             ├─> Task 18 (Equity Prices)
                             │       │
                             │       └─> Task 19 (Funding Rates)
                             │               │
                             │               └─> Task 20 (On-Chain Publishing)
                             │
                             └─> Task 21 (Risk Management)
                                     │
                                     ├─> Task 18 (Equity Prices)
                                     └─> Task 19 (Funding Rates)

API Tasks:
- Task 23 depends on: Task 16, Task 22
- Task 24 depends on: Task 18, Task 22
- Task 25 depends on: Task 19, Task 20, Task 22
- Task 26 depends on: Task 21, Task 22
```

---

## ✅ Status

All task briefs are **ready for assignment**. Each brief includes:
- ✅ Detailed requirements
- ✅ Implementation steps
- ✅ Acceptance criteria
- ✅ Test cases
- ✅ Dependencies

---

**Last Updated:** January 2025  
**Total Tasks:** 11 task briefs + 3 documentation files

