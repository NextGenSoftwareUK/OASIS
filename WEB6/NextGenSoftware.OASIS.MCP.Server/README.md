# OASIS MCP Server

The OASIS [Model Context Protocol](https://modelcontextprotocol.io) Server — 60+ typed tools covering the full OASIS stack (WEB4 through WEB10) delivered directly into Cursor, VS Code, Claude Desktop, and any other MCP-compatible IDE.

Built on the official [`ModelContextProtocol`](https://github.com/modelcontextprotocol/csharp-sdk) C# SDK, communicating over stdio. Part of the [OASIS Omniverse](https://oasisomniverse.one) ecosystem.

---

## Install

### Option A — npm (no .NET SDK required, recommended for most devs)

```bash
npm install -g @oasisomniverse/mcp-server
```

Downloads the correct pre-built native binary for your platform (Windows/Mac/Linux x64 & arm64) automatically.

### Option B — NuGet dotnet tool (requires .NET 8+ SDK)

```bash
dotnet tool install -g NextGenSoftware.OASIS.MCP.Server
```

### Option C — Build from source (requires .NET 10 SDK)

```bash
git clone https://github.com/NextGenSoftwareUK/OASIS.git
cd OASIS/WEB6/NextGenSoftware.OASIS.MCP.Server
dotnet build -c Release
dotnet run
```

---

## Configure your IDE

After installing via npm or NuGet, the `oasis-mcp` command is on your PATH. Add it to your IDE's MCP config:

**Cursor** (`~/.cursor/mcp.json`) / **VS Code** (`.vscode/mcp.json`) / **Claude Desktop** (`claude_desktop_config.json`):

```json
{
  "mcpServers": {
    "oasis": {
      "command": "oasis-mcp",
      "env": {
        "OASIS_API_URL": "https://api.oasisplatform.world",
        "OASIS_MCP_LICENSE_KEY": "your-license-key"
      }
    }
  }
}
```

If building from source, point directly at the project:

```json
{
  "mcpServers": {
    "oasis": {
      "command": "dotnet",
      "args": ["run", "--project", "/absolute/path/to/OASIS/WEB6/NextGenSoftware.OASIS.MCP.Server"]
    }
  }
}
```

---

## Tool coverage

| Layer | How covered |
|---|---|
| **WEB4** — OASIS API / ONODE (identity, karma, NFTs, wallets, smart contracts) | `web4_request` generic HTTP passthrough — full access to every WEB4 REST endpoint without duplicating hundreds of routes |
| **WEB5** — STAR API / STARNET (holon graph, OAPPs, quests) | `web5_request` generic HTTP passthrough — same pattern as WEB4 |
| **WEB6** — AI abstraction layer (FAHRN, Holonic BRAID, semantic cache, orchestration) | Specific typed tools — one per public manager method, called **in-process** (no HTTP round-trip) |
| **WEB7** — Symbiotic layer | Specific typed tools, in-process |
| **WEB8** — Inter-Galactic mesh | Specific typed tools, in-process |
| **WEB9** — Singularity status aggregation | Specific typed tools, in-process |
| **WEB10** — Source / Omniversal layer | Specific typed tools, in-process |

---

## Environment variables

| Variable | Purpose | Default |
|---|---|---|
| `OASIS_API_URL` | WEB4 API base URL | `https://api.oasisplatform.world` |
| `OASIS_MCP_LICENSE_KEY` | License key (required for write operations) | — |
| `WEB4_API_BASE_URL` | Override WEB4 base URL (e.g. for local dev) | `https://api.web4.oasisomniverse.one` |
| `WEB5_API_BASE_URL` | Override WEB5 base URL | `https://api.starnet.oasisomniverse.one` |
| `WEB6_API_BASE_URL` | Override WEB6 base URL | `https://api.web6.oasisomniverse.one` |
| `OPENAI_API_KEY` | OpenAI key (used by WEB6 AI tools) | — |
| `ANTHROPIC_API_KEY` | Anthropic key | — |
| `SERV_API_KEY` | OpenSERV key (one key, all models) | — |

Point the `*_BASE_URL` vars at `http://localhost:<port>` for any layer you're running locally instead of the live `*.oasisomniverse.one` deployments.

---

## Links

- [oasisomniverse.one](https://oasisomniverse.one) — main site
- [oasisweb4.com/products/mcp](https://oasisweb4.com/products/mcp) — product page & pricing
- [OASIS GitHub](https://github.com/NextGenSoftwareUK/OASIS) — full source
- [WEB6 README](../README.md) — WEB6 AI layer overview
