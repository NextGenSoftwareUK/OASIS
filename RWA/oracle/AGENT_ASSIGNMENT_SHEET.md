# RWA Oracle - Agent Assignment Sheet

**Date:** January 2025  
**Use this sheet to track agent assignments**

---

## 📋 Assignment Template

Copy this template for each assignment:

```
**Agent:** [Agent Name/ID]
**Task:** [Task Number] - [Task Name]
**Status:** 🟡 Assigned
**Assigned Date:** [Date]
**Brief:** oracle/[task-file].md
**Dependencies:** [List dependent tasks]
**Estimated Completion:** [Date]
```

---

## 🎯 Task Assignments

### Phase 1: Foundation & Data

#### Task 22: Database Schema (DO FIRST - No Dependencies)
**Brief:** `oracle/22-database-schema-risk-management.md`
**Priority:** ⭐ Critical - Start Here!
**Agent:** ________________
**Status:** 🟡 Pending
**Notes:** Can be done immediately, needed by all other tasks

---

#### Task 16: Corporate Action Data Source Integration
**Brief:** `oracle/16-corporate-action-data-source-integration.md`
**Priority:** ⭐ Critical
**Agent:** ________________
**Status:** 🟡 Pending
**Dependencies:** None
**Notes:** Can start immediately

---

#### Task 17: Corporate Action Price Adjustment Engine
**Brief:** `oracle/17-corporate-action-price-adjustment-engine.md`
**Priority:** ⭐ Critical
**Agent:** ________________
**Status:** 🟡 Pending
**Dependencies:** Task 16
**Notes:** Wait for Task 16 completion

---

#### Task 18: Equity Price Feed Service
**Brief:** `oracle/18-equity-price-feed-service.md`
**Priority:** ⭐ Critical
**Agent:** ________________
**Status:** 🟡 Pending
**Dependencies:** Task 16, Task 17
**Notes:** Wait for Tasks 16 & 17 completion

---

### Phase 2: Funding Rates

#### Task 19: Funding Rate Calculation Service
**Brief:** `oracle/19-funding-rate-calculation-service.md`
**Priority:** ⭐ Critical
**Agent:** ________________
**Status:** 🟡 Pending
**Dependencies:** Task 18
**Notes:** Wait for Task 18 completion

---

#### Task 20: Funding Rate On-Chain Publishing (Solana)
**Brief:** `oracle/20-funding-rate-onchain-publishing-solana.md`
**Priority:** ⭐ Critical
**Agent:** ________________
**Status:** 🟡 Pending
**Dependencies:** Task 19
**Notes:** Requires Solana/Anchor knowledge

---

### Phase 3: Risk Management

#### Task 21: Risk Management Module
**Brief:** `oracle/21-risk-management-module.md`
**Priority:** ⭐ Critical
**Agent:** ________________
**Status:** 🟡 Pending
**Dependencies:** Task 16, Task 18, Task 19
**Notes:** Most complex task, wait for dependencies

---

### Phase 4: API Layer (Can be done in parallel)

#### Task 23: API Endpoints - Corporate Actions
**Brief:** `oracle/23-api-endpoints-corporate-actions.md`
**Priority:** ⭐ Critical
**Agent:** ________________
**Status:** 🟡 Pending
**Dependencies:** Task 16, Task 22
**Notes:** Can do in parallel with other API tasks

---

#### Task 24: API Endpoints - Equity Prices
**Brief:** `oracle/24-api-endpoints-equity-prices.md`
**Priority:** ⭐ Critical
**Agent:** ________________
**Status:** 🟡 Pending
**Dependencies:** Task 18, Task 22
**Notes:** Can do in parallel with other API tasks

---

#### Task 25: API Endpoints - Funding Rates
**Brief:** `oracle/25-api-endpoints-funding-rates.md`
**Priority:** ⭐ Critical
**Agent:** ________________
**Status:** 🟡 Pending
**Dependencies:** Task 19, Task 20, Task 22
**Notes:** Can do in parallel with other API tasks

---

#### Task 26: API Endpoints - Risk Management
**Brief:** `oracle/26-api-endpoints-risk-management.md`
**Priority:** ⭐ Critical
**Agent:** ________________
**Status:** 🟡 Pending
**Dependencies:** Task 21, Task 22
**Notes:** Can do in parallel with other API tasks

---

## 📊 Assignment Status Overview

| Task # | Task Name | Agent | Status | Dependencies Met? |
|--------|-----------|-------|--------|-------------------|
| 22 | Database Schema | _____ | 🟡 | ✅ Yes (None) |
| 16 | Corporate Actions Data | _____ | 🟡 | ✅ Yes (None) |
| 17 | Price Adjustment | _____ | 🟡 | ⏳ Waiting on 16 |
| 18 | Equity Price Feed | _____ | 🟡 | ⏳ Waiting on 16, 17 |
| 19 | Funding Rate Calc | _____ | 🟡 | ⏳ Waiting on 18 |
| 20 | On-Chain Publishing | _____ | 🟡 | ⏳ Waiting on 19 |
| 21 | Risk Management | _____ | 🟡 | ⏳ Waiting on 16, 18, 19 |
| 23 | API - Corporate Actions | _____ | 🟡 | ⏳ Waiting on 16, 22 |
| 24 | API - Equity Prices | _____ | 🟡 | ⏳ Waiting on 18, 22 |
| 25 | API - Funding Rates | _____ | 🟡 | ⏳ Waiting on 19, 20, 22 |
| 26 | API - Risk Management | _____ | 🟡 | ⏳ Waiting on 21, 22 |

---

## 🚀 Quick Start Instructions for Agents

1. **Read your task brief:** `oracle/[task-number]-[task-name].md`
2. **Review main docs:**
   - `oracle/AUTONOM_STYLE_RWA_ORACLE_IMPLEMENTATION_PLAN.md` (architecture)
   - `oracle/END_PRODUCT_CAPABILITIES_AND_OASIS_ADVANTAGES.md` (context)
   - `oracle/AGENT_BRIEFING.md` (general instructions)
3. **Check dependencies:** Ensure prerequisite tasks are complete
4. **Start implementation:** Follow task brief step-by-step
5. **Update status:** Mark as ✅ Complete when done

---

## ✅ Status Legend

- 🟡 **Pending** - Not yet started
- 🔵 **In Progress** - Currently working on it
- 🟢 **Review** - Completed, awaiting review
- ✅ **Complete** - Approved and merged
- ❌ **Blocked** - Blocked by dependency or issue

---

**Instructions:**
1. Fill in agent names as tasks are assigned
2. Update status as work progresses
3. Mark dependencies as met when prerequisite tasks complete
4. Use this sheet to coordinate parallel work

---

**Last Updated:** January 2025

