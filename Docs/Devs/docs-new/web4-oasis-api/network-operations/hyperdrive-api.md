# HyperDrive API

## Overview

The HyperDrive API manages the OASIS auto-failover, auto-replication, and auto-load-balancing system. It provides configuration, metrics, status, AI recommendations, replication and failover rules, subscription/quota, cost controls, and provider management. Use it to inspect and tune how OASIS routes requests across providers for high availability and performance.

**Base URL:** `/api/hyperdrive`

**Authentication:** Varies by endpoint; many configuration and admin endpoints require auth. Check Swagger for `[Authorize]`.

**Rate Limits:** Same as [WEB4 API](../../reference/rate-limits.md).

**Key Features:**
- ✅ **Config** – Get/put configuration and mode (Legacy / OASISHyperDrive2); validate; reset
- ✅ **Metrics** – Per-provider and global metrics; record request/connection; geographic/cost overrides; reset
- ✅ **Status** – Current HyperDrive status (enabled providers, failover/replication/load-balance state)
- ✅ **AI & analytics** – Recommendations; predictive analytics; report; dashboard; cost/performance optimization; record performance/failure
- ✅ **Replication** – Triggers, provider rules, data-type rules, schedule, cost optimization; replication rules GET/PUT
- ✅ **Failover** – Triggers, provider rules, escalation rules; failover rules GET/PUT; predictions; record failure; preventive
- ✅ **Subscription** – Usage alerts, quota notifications, config
- ✅ **Costs** – Current, history, projections; limits
- ✅ **Recommendations** – Smart, security
- ✅ **Intelligent mode** – Get/put, enable/disable
- ✅ **Quota** – Usage, limits, status
- ✅ **Providers** – Free, low-cost provider lists

---

## Quick Start

### Get current configuration

```http
GET http://api.oasisweb4.com/api/hyperdrive/config
```

### Get HyperDrive status

```http
GET http://api.oasisweb4.com/api/hyperdrive/status
```

### Get best provider (for load balancing)

```http
GET http://api.oasisweb4.com/api/hyperdrive/best-provider
```

---

## Endpoints Summary

| Area | Method | Endpoint |
|------|--------|----------|
| **Config** | GET | `config` |
| | PUT | `config` |
| | GET | `mode` |
| | PUT | `mode` |
| | POST | `config/validate` |
| | POST | `config/reset` |
| **Metrics** | GET | `metrics` |
| | GET | `metrics/{providerType}` |
| | POST | `record-request` |
| | POST | `record-connection` |
| | PUT | `geographic/{providerType}` |
| | PUT | `cost/{providerType}` |
| | POST | `metrics/{providerType}/reset` |
| | POST | `metrics/reset-all` |
| **Status** | GET | `status` |
| **AI / Analytics** | GET | `ai/recommendations` |
| | GET | `analytics/predictive/{providerType}` |
| | GET | `analytics/report` |
| | GET | `dashboard` |
| | GET | `failover/predictions` |
| | POST | `analytics/record` |
| | POST | `ai/record-performance` |
| | POST | `failover/record-failure` |
| | GET | `analytics/cost-optimization` |
| | GET | `analytics/performance-optimization` |
| | POST | `failover/preventive` |
| **Replication** | POST | `replication/triggers` |
| | PUT | `replication/triggers/{id}` |
| | DELETE | `replication/triggers/{id}` |
| | GET | `replication/provider-rules`, `data-type-rules`, `schedule-rules`, `cost-optimization` |
| | PUT | Same as above for update |
| | GET | `replication/rules` |
| | PUT | `replication/rules` |
| **Failover** | POST | `failover/triggers` |
| | PUT | `failover/triggers/{id}` |
| | DELETE | `failover/triggers/{id}` |
| | GET | `failover/provider-rules`, `escalation-rules` |
| | PUT | Same |
| | GET | `failover/rules` |
| | PUT | `failover/rules` |
| **Subscription** | GET | `subscription/usage-alerts`, `quota-notifications`, `config` |
| | POST | `subscription/usage-alerts`, `quota-notifications` |
| | PUT | `subscription/usage-alerts/{id}`, `quota-notifications/{id}`, `config` |
| | DELETE | `subscription/usage-alerts/{id}`, `quota-notifications/{id}` |
| **Costs** | GET | `costs/current`, `costs/history`, `costs/projections` |
| | PUT | `costs/limits` |
| **Recommendations** | GET | `recommendations/smart`, `recommendations/security` |
| **Intelligent mode** | GET | `intelligent-mode` |
| | PUT | `intelligent-mode` |
| | POST | `intelligent-mode/enable`, `intelligent-mode/disable` |
| **Quota** | GET | `quota/usage`, `quota/limits`, `quota/status` |
| **Data permissions** | GET | `data-permissions` |
| | PUT | `data-permissions` |
| **Providers** | GET | `providers/free`, `providers/low-cost` |

Request/response schemas and auth requirements are in [Swagger](http://api.oasisweb4.com/swagger/index.html) under **HyperDrive**.

---

## Response Format

Standard OASIS wrapper: `{ "result": { ... }, "isError": false, "message": "..." }`. Always check **isError**.

---

## Related Documentation

- [WEB4 OASIS API Overview](../overview.md) – Provider selection and HyperDrive behaviour
- [Rate Limits](../../reference/rate-limits.md)

---

*Last Updated: January 24, 2026*
