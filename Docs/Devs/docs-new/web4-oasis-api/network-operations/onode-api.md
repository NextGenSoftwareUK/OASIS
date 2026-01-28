# ONODE API

## Overview

The ONODE API manages OASIS Node operations. It provides OASISDNA configuration, node status, info, start/stop/restart, metrics, logs, config, peers, and stats. Use it to operate and monitor individual OASIS nodes.

**Base URL:** `http://api.oasisweb4.com/api/v1/onode`

**Authentication:** Depends on endpoint; check Swagger.

**Rate Limits:** Same as [WEB4 API](../../reference/rate-limits.md).

---

## Endpoints Summary

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `oasisdna` | Get OASISDNA configuration for ONODE |
| PUT | `oasisdna` | Update OASISDNA for ONODE |
| GET | `status` | Node status |
| GET | `info` | Node info |
| POST | `start` | Start node |
| POST | `stop` | Stop node |
| POST | `restart` | Restart node |
| GET | `metrics` | Node metrics |
| GET | `logs` | Node logs |
| GET | `config` | Get config |
| PUT | `config` | Update config |
| GET | `peers` | List peers |
| GET | `stats` | Node statistics |

Request/response schemas and auth: see [Swagger](http://api.oasisweb4.com/swagger/index.html) under **ONODE**.

---

## Related Documentation

- [ONET API](onet-api.md) – OASIS Network protocol
- [WEB4 Overview](../overview.md)

---

*Last Updated: January 24, 2026*
