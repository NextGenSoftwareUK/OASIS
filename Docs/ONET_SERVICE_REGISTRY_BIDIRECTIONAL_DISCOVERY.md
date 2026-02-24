# ONET Service Registry & Bidirectional Discovery with OpenSERV

**Last Updated:** January 2026  
**Status:** ✅ Complete

---

## Overview

This document explains the **ONET Service Registry** (OASIS's internal service discovery infrastructure) and the **bidirectional discovery** integration with **OpenSERV Platform** (external AI agent platform).

---

## Important Terminology Clarification

### ONET Service Registry (OASIS Infrastructure)
- **What it is:** OASIS's internal service discovery and routing infrastructure
- **Part of:** ONET Unified Architecture
- **Purpose:** Register, discover, and route A2A agents within OASIS ecosystem
- **Location:** `UnifiedAgentServiceManager` in OASIS Core

### OpenSERV Platform (External Partner)
- **What it is:** External AI agent platform (openserv.ai)
- **Relationship:** OASIS's investors/partners
- **Purpose:** AI agent infrastructure and marketplace
- **Integration:** Bidirectional discovery bridge

### Key Distinction
- **ONET Service Registry** = OASIS's own infrastructure (internal)
- **OpenSERV Platform** = External partner platform (external)

**Previous Confusion:** The term "SERV" was used to refer to OASIS's service registry, which was confusing because OpenSERV is the partner platform. This has been renamed to "ONET Service Registry" for clarity.

---

## Bidirectional Discovery

### What is Bidirectional Discovery?

Bidirectional discovery enables agents to be discoverable on both platforms:

1. **OASIS → OpenSERV:** OASIS A2A agents can be registered with OpenSERV platform
2. **OpenSERV → OASIS:** OpenSERV agents can be registered with OASIS and discovered via ONET Service Registry

### Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    OASIS Ecosystem                          │
│                                                             │
│  ┌──────────────────┐         ┌──────────────────┐        │
│  │ ONET Service     │         │ OpenSERV         │        │
│  │ Registry         │◄───────►│ Platform         │        │
│  │ (Internal)       │ Bridge  │ (External)       │        │
│  └──────────────────┘         └──────────────────┘        │
│         │                              │                    │
│         │                              │                    │
│         ▼                              ▼                    │
│  ┌──────────────────┐         ┌──────────────────┐        │
│  │ OASIS A2A Agents │         │ OpenSERV Agents   │        │
│  └──────────────────┘         └──────────────────┘        │
└─────────────────────────────────────────────────────────────┘
```

---

## Implementation Details

### 1. OASIS → OpenSERV Registration

**Method:** `RegisterOasisAgentWithOpenServAsync()`

**What it does:**
- Registers an OASIS A2A agent with OpenSERV platform
- Makes the agent discoverable on OpenSERV
- Stores OpenSERV registration info in agent metadata

**API Endpoint:**
```http
POST /api/a2a/openserv/register-oasis-agent
Authorization: Bearer {token}
Content-Type: application/json

{
  "openServApiKey": "sk-...",
  "oasisAgentEndpoint": "https://api.oasisweb4.com/api/a2a/agent-card/{agentId}"
}
```

**Code Example:**
```csharp
var result = await A2AManager.Instance.RegisterOasisAgentWithOpenServAsync(
    agentId: myAgentId,
    openServApiKey: "sk-...",
    oasisAgentEndpoint: "https://api.oasisweb4.com/api/a2a/agent-card/{agentId}"
);
```

### 2. OpenSERV → OASIS Registration

**Method:** `RegisterOpenServAgentAsync()`

**What it does:**
- Registers an OpenSERV agent as an A2A agent in OASIS
- Creates avatar and registers capabilities
- Registers with ONET Service Registry
- Makes agent discoverable via ONET Service Registry

**API Endpoint:**
```http
POST /api/a2a/openserv/register
Content-Type: application/json

{
  "openServAgentId": "agent-123",
  "openServEndpoint": "https://api.openserv.ai/agents/agent-123",
  "capabilities": ["data-analysis", "nlp"],
  "apiKey": "sk-..."
}
```

### 3. Discovery from OpenSERV Platform

**Method:** `DiscoverAgentsFromOpenServAsync()`

**What it does:**
- Queries OpenSERV platform directly for agents
- Returns both OpenSERV native agents and OASIS agents registered with OpenSERV
- Converts to A2A Agent Cards

**API Endpoint:**
```http
GET /api/a2a/openserv/discover?openServApiKey=sk-...&capability=data-analysis
```

**Code Example:**
```csharp
var result = await A2AManager.Instance.DiscoverAgentsFromOpenServAsync(
    openServApiKey: "sk-...",
    capability: "data-analysis"
);
```

### 4. Unified Discovery (ONET Service Registry + OpenSERV)

**Method:** `DiscoverAgentsViaONETServiceRegistryAsync()`

**What it does:**
- Queries ONET Service Registry (OASIS's internal registry)
- Optionally includes agents from OpenSERV platform
- Returns unified list of agents from both sources

**API Endpoint:**
```http
GET /api/a2a/agents/discover-onet?service=data-analysis&includeOpenServ=true&openServApiKey=sk-...
```

**Code Example:**
```csharp
var result = await A2AManager.Instance.DiscoverAgentsViaONETServiceRegistryAsync(
    serviceName: "data-analysis",
    includeOpenServAgents: true,
    openServApiKey: "sk-..."
);
```

---

## Migration from "SERV" to "ONET Service Registry"

### Deprecated Methods

The following methods are marked as `[Obsolete]` for backward compatibility:

- `DiscoverAgentsViaSERVAsync()` → Use `DiscoverAgentsViaONETServiceRegistryAsync()` instead

### Deprecated Endpoints

The following endpoints are maintained for backward compatibility but redirect to new endpoints:

- `GET /api/a2a/agents/discover-serv` → Redirects to `GET /api/a2a/agents/discover-onet`

### Updated Terminology

| Old Term | New Term |
|----------|----------|
| SERV | ONET Service Registry |
| SERV infrastructure | ONET Service Registry |
| SERV discovery | ONET Service Registry discovery |
| SERV registration | ONET Service Registry registration |

---

## Benefits of Bidirectional Discovery

### For OASIS
- ✅ OASIS agents discoverable on OpenSERV platform
- ✅ Access to OpenSERV's user base
- ✅ Network effects from cross-platform discovery
- ✅ Enhanced ecosystem value

### For OpenSERV
- ✅ OpenSERV agents discoverable in OASIS ecosystem
- ✅ Access to OASIS's ONET Service Registry
- ✅ Integration with A2A Protocol
- ✅ Cross-platform agent marketplace

### For Developers
- ✅ Single API for discovering agents from both platforms
- ✅ Unified agent representation (Agent Cards)
- ✅ Seamless cross-platform agent communication
- ✅ Simplified agent registration process

---

## API Endpoints Summary

### ONET Service Registry Discovery
- `GET /api/a2a/agents/discover-onet` - Discover agents via ONET Service Registry (with optional OpenSERV integration)

### OpenSERV Platform Discovery
- `GET /api/a2a/openserv/discover` - Discover agents directly from OpenSERV platform

### Registration
- `POST /api/a2a/openserv/register` - Register OpenSERV agent with OASIS
- `POST /api/a2a/openserv/register-oasis-agent` - Register OASIS agent with OpenSERV platform

### Legacy (Deprecated)
- `GET /api/a2a/agents/discover-serv` - Legacy endpoint (redirects to discover-onet)

---

## Code Examples

### Register OASIS Agent with OpenSERV

```csharp
// Register your OASIS agent with OpenSERV platform
var result = await A2AManager.Instance.RegisterOasisAgentWithOpenServAsync(
    agentId: myAgentId,
    openServApiKey: "sk-your-openserv-api-key",
    oasisAgentEndpoint: null // Will use default OASIS API endpoint
);

if (!result.IsError)
{
    Console.WriteLine($"Agent registered with OpenSERV as: {result.Result}");
}
```

### Discover Agents from Both Platforms

```csharp
// Discover agents from ONET Service Registry + OpenSERV platform
var result = await A2AManager.Instance.DiscoverAgentsViaONETServiceRegistryAsync(
    serviceName: "data-analysis",
    includeOpenServAgents: true,
    openServApiKey: "sk-your-openserv-api-key"
);

if (!result.IsError && result.Result != null)
{
    foreach (var agentCard in result.Result)
    {
        Console.WriteLine($"Found agent: {agentCard.Name} ({agentCard.AgentId})");
    }
}
```

### Discover Only from OpenSERV Platform

```csharp
// Discover agents directly from OpenSERV platform
var result = await A2AManager.Instance.DiscoverAgentsFromOpenServAsync(
    openServApiKey: "sk-your-openserv-api-key",
    capability: "data-analysis"
);

if (!result.IsError && result.Result != null)
{
    foreach (var agentCard in result.Result)
    {
        Console.WriteLine($"Found OpenSERV agent: {agentCard.Name}");
    }
}
```

---

## Related Documentation

- **ONET Service Registry Documentation:** `/Docs/SERV_AGENT_INFRASTRUCTURE.md` (updated with new terminology)
- **OpenSERV Partnership Benefits:** `/Docs/OPENSERV_PARTNERSHIP_BENEFITS.md`
- **A2A Protocol Documentation:** `/A2A/README.md`

---

**Status:** ✅ Implementation Complete  
**Last Updated:** January 2026
