# WEB10 MCP Tool Reference

The WEB10 Source Layer exposes **1 typed MCP tool** — the root identity and ontological foundation of the entire OASIS stack. WEB10 = WEB0: the beginning and the end, queryable as one real endpoint.

All tools return a JSON-serialised `OASISResult<T>` envelope. On success `isError` is `false` and data is in `result`. On failure `isError` is `true` and `message` describes the problem.

Install: `npm install -g @oasisomniverse/mcp-server` or `dotnet tool install -g NextGenSoftware.OASIS.MCP.Server`

---

## Source tool

#### `web10_get_source`
Returns the foundational OASIS runtime/version identity (the Alpha) together with WEB9's live unified status across WEB4–WEB8 (the Omega). WEB10 = WEB0 as one real, queryable endpoint.

**Parameters:** None.

**Returns:** `SourceStatus` object containing:
| Field | Description |
|---|---|
| `oasisVersion` | Runtime version of the OASIS stack |
| `buildDate` | Build timestamp |
| `stackIdentity` | Root identity string — the Source holon anchor |
| `unifiedStatus` | Full `UnifiedStatus` from WEB9 (all layers WEB4–WEB8) |
| `timestamp` | UTC timestamp of the query |

**Example use:**
```
Use the web10_get_source tool to get the current OASIS stack version and full health status.
```

---

## See also

- [WEB10 REST API Reference](WEB10_REST_API_Reference.md) *(in development — MCP tool is the primary access method now)*
- [Full MCP Tool Reference (all layers)](../WEB6/WEB6_MCP_Tool_Reference.md)
- [WEB9 MCP Tool Reference](../WEB9/WEB9_MCP_Tool_Reference.md)
- [MCP Server README](../../../../WEB6/NextGenSoftware.OASIS.MCP.Server/README.md)
