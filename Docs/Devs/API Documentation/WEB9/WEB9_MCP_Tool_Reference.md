# WEB9 MCP Tool Reference

The WEB9 Singularity Layer exposes **1 typed MCP tool** — a single endpoint that probes the entire OASIS stack in parallel and surfaces a unified health and status report. "The network observing itself."

All tools return a JSON-serialised `OASISResult<T>` envelope. On success `isError` is `false` and data is in `result`. On failure `isError` is `true` and `message` describes the problem.

Install: `npm install -g @oasisomniverse/mcp-server` or `dotnet tool install -g NextGenSoftware.OASIS.MCP.Server`

---

## Singularity Aggregation tool

#### `web9_get_unified_status`
Probes WEB4–WEB8 in parallel and returns one unified status report. This is the Singularity Layer made literal: real cross-service health aggregation and live metric collection in a single call.

**Parameters:** None.

**Returns:** `UnifiedStatus` object containing:
| Field | Description |
|---|---|
| `web4Status` | ONODE/OASIS API health (latency, error rate, avatar count) |
| `web5Status` | STAR ODK/STARNET health (holons, missions, quests online) |
| `web6Status` | AI API health (providers live, FAHRN agents registered, memory holons) |
| `web7Status` | Symbiosis layer health (active sessions, spaces) |
| `web8Status` | Galactic mesh health (nodes online, liveness window, routing status) |
| `overallHealth` | Composite wellness score 0–100 |
| `timestamp` | UTC timestamp of the aggregation |

---

## See also

- [WEB9 REST API Reference](WEB9_REST_API_Reference.md) *(in development — MCP tool is the primary access method now)*
- [Full MCP Tool Reference (all layers)](../WEB6/WEB6_MCP_Tool_Reference.md)
- [MCP Server README](../../../../WEB6/NextGenSoftware.OASIS.MCP.Server/README.md)
