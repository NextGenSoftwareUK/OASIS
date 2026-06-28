# NextGenSoftware.OASIS.MCP.Server

A native C# MCP (Model Context Protocol) server covering the entire OASIS stack, WEB4 through WEB10. Built on the
official `ModelContextProtocol` C# SDK, communicating over stdio.

## Coverage

- **WEB4** (OASIS API / ONODE) and **WEB5** (STAR API / STARNET) — covered via `web4_request` / `web5_request`,
  generic HTTP passthrough tools that can call any endpoint of those two large, pre-existing, already-complete REST
  APIs (see their own Swagger docs for the full endpoint list). This avoids hand-duplicating hundreds of existing
  endpoints as bespoke tools while still giving a client genuine, complete access to every one of them.
- **WEB6** (AI abstraction / FAHRN / Holonic BRAID / Holonic memory / Orchestrator bridge), **WEB7** (Symbiotic
  layer), **WEB8** (Inter-Galactic mesh), **WEB9** (Singularity status aggregation) and **WEB10** (Source) — covered
  by specific, typed tools that call directly into each layer's `.Core` project **in-process** (no HTTP round-trip),
  one tool per public manager method. This is the full surface of each of those Core APIs.

## Running

```bash
dotnet run --project MCP/NextGenSoftware.OASIS.MCP.Server
```

The server boots the OASIS engine (loads `OASIS_DNA.json`, activates the default storage provider) before serving
any tool calls, then speaks MCP over stdio.

### Environment variables

| Variable | Purpose | Default |
|---|---|---|
| `WEB4_API_BASE_URL` | Base URL `web4_request` forwards to | `https://api.web4.oasisomniverse.one` |
| `WEB5_API_BASE_URL` | Base URL `web5_request` forwards to | `https://api.starnet.oasisomniverse.one` |
| `WEB4_API_BASE_URL` / `WEB5_API_BASE_URL` / `WEB6_API_BASE_URL` / `WEB7_API_BASE_URL` / `WEB8_API_BASE_URL` | Also read by WEB9/WEB10's status aggregation tools | see `SingularityAggregationManager` |

Point these at `http://localhost:<port>` for any layer you're running locally instead of the production
`*.oasisomniverse.one` deployments.

## Adding to an MCP client (e.g. Claude Desktop / Cursor)

```json
{
  "mcpServers": {
    "oasis-web4-to-web10": {
      "command": "dotnet",
      "args": ["run", "--project", "C:/Source/OASIS2/MCP/NextGenSoftware.OASIS.MCP.Server"]
    }
  }
}
```
