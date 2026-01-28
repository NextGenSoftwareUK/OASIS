# ONET API

## Overview

The ONET API manages the OASIS Network (ONET) protocol. It provides OASISDNA configuration, network status, nodes, connect/disconnect, stats, start/stop, topology, and broadcast. Use it to operate and monitor the OASIS network layer.

**Base URL:** `http://api.oasisweb4.com/api/v1/onet`

**Authentication:** Depends on endpoint; check Swagger.

**Rate Limits:** Same as [WEB4 API](../../reference/rate-limits.md).

---

## Endpoints Summary

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `oasisdna` | Get OASISDNA configuration for ONET |
| PUT | `oasisdna` | Update OASISDNA for ONET |
| GET | `network/status` | Network status |
| GET | `network/nodes` | List network nodes |
| POST | `network/connect` | Connect to network |
| POST | `network/disconnect` | Disconnect from network |
| GET | `network/stats` | Network statistics |
| POST | `network/start` | Start network |
| POST | `network/stop` | Stop network |
| GET | `network/topology` | Network topology |
| POST | `network/broadcast` | Broadcast to network |

Request/response schemas and auth: see [Swagger](http://api.oasisweb4.com/swagger/index.html) under **ONET**.

---

## Related Documentation

- [ONODE API](onode-api.md) – OASIS Node operations
- [WEB4 Overview](../overview.md)

---

*Last Updated: January 24, 2026*
