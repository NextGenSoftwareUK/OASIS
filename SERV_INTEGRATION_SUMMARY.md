# SERV Integration Summary

## Overview

SERV (Service Registry) is part of the **ONET Unified Architecture** - OASIS's decentralized service discovery and routing infrastructure. The SERV integration enables A2A (Agent-to-Agent) Protocol agents to be registered, discovered, and accessed through the ONET network.

---

## What Was Built

### 1. **Unified Agent Service Manager** 
**Location**: `OASIS Architecture/NextGenSoftware.OASIS.API.Core/Managers/UnifiedAgentServiceManager/`

A comprehensive service management system that provides:

- **Service Registration**: Register A2A agents as discoverable services
- **Service Discovery**: Find agents by capability, service name, or category
- **Service Routing**: Intelligent routing with multiple strategies (LeastBusy, RoundRobin, etc.)
- **Health Monitoring**: Automatic health checks and unhealthy service removal
- **Service Caching**: In-memory cache for fast service lookups

**Key Components**:
- `UnifiedAgentServiceManager.cs` - Main service manager
- `UnifiedAgentServiceRouter.cs` - Routing logic
- `UnifiedAgentServiceHealthChecker.cs` - Health monitoring

**Features**:
- Automatic health checking with configurable intervals
- Service status tracking (Available, Busy, Offline, Unhealthy)
- Event-driven architecture (events for service health changes)
- Multiple routing strategies for load balancing

---

### 2. **A2A-SERV Bridge (Extension Methods)**
**Location**: `ONODE/NextGenSoftware.OASIS.API.ONODE.Core/Managers/A2AManager/A2AManagerExtensions-SERV.cs`

Extension methods that bridge A2A Protocol agents with SERV infrastructure:

#### `RegisterAgentAsServiceAsync()`
- Registers an A2A agent as a UnifiedService in SERV
- Maps A2A AgentCard to UnifiedService structure
- Stores agent metadata (services, skills, pricing, etc.)
- Makes agent discoverable via ONET network

#### `DiscoverAgentsViaSERVAsync()`
- Queries SERV infrastructure for registered services
- Filters by service name/capability
- Enriches SERV services with A2A Agent Cards
- Returns list of discoverable agents

**Data Mapping**:
- A2A Agent ID → SERV Service ID
- Agent Capabilities → Service Capabilities
- Agent Endpoint → Service Endpoints
- Agent Status → Service IsActive flag
- Agent Metadata → Service Metadata dictionary

---

### 3. **OpenSERV Bridge Service**
**Location**: `ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI/Services/OpenServBridgeService.cs`

HTTP client wrapper for integrating with external OpenSERV AI agents:

**Features**:
- HTTP client with retry logic (3 retries with exponential backoff)
- Authentication via Bearer tokens
- Request/response serialization
- Error handling and logging
- Execution time tracking

**Methods**:
- `ExecuteAgentAsync()` - Execute an OpenSERV agent request
- `ExecuteWorkflowAsync()` - Execute an OpenSERV workflow

**Integration Points**:
- Can register OpenSERV agents as A2A agents
- Routes OpenSERV requests through A2A Protocol
- Handles webhook callbacks from OpenSERV

---

### 4. **ONET Unified Architecture Integration**

SERV is built on top of ONET (OASIS Network) Unified Architecture, which provides:

#### Service Registry
- Centralized service registry (`_unifiedServices` dictionary)
- Service categories (Identity, Reputation, Data, Finance, Digital Assets, etc.)
- Integration layers (WEB4, ONET, HyperDrive)

#### Discovery Mechanisms
ONET provides multiple discovery protocols:

1. **DHT (Distributed Hash Table)** - Kademlia-based peer discovery
2. **mDNS (multicast DNS)** - Local network service discovery  
3. **Blockchain Discovery** - Smart contract-based node registry
4. **Bootstrap Discovery** - Centralized fallback servers

#### Service Registration Flow
```
A2A Agent Registration
    ↓
AgentManager.RegisterAgentCapabilitiesAsync()
    ↓
A2AManager.RegisterAgentAsServiceAsync() (extension)
    ↓
ONETUnifiedArchitecture.RegisterUnifiedServiceAsync()
    ↓
Create UnifiedService from AgentCard
    ↓
Store in _unifiedServices dictionary
    ↓
Register with ONETDiscovery (optional)
    ↓
Agent is now discoverable via SERV
```

---

## Architecture Diagram

```
┌─────────────────────────────────────────────────────────────┐
│                    OASIS Ecosystem                          │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  ┌──────────────┐         ┌──────────────┐                │
│  │ A2A Protocol │────────►│ SERV Bridge  │                │
│  │   Agents     │         │  (Extension │                │
│  └──────────────┘         │   Methods)   │                │
│                           └──────────────┘                │
│                                  │                          │
│                                  ▼                          │
│  ┌──────────────────────────────────────────┐              │
│  │   UnifiedAgentServiceManager              │              │
│  │   - Registration                          │              │
│  │   - Discovery                             │              │
│  │   - Routing                               │              │
│  │   - Health Monitoring                     │              │
│  └──────────────────────────────────────────┘              │
│                                  │                          │
│                                  ▼                          │
│  ┌──────────────────────────────────────────┐              │
│  │   ONET Unified Architecture               │              │
│  │   - Service Registry                      │              │
│  │   - Discovery (DHT, mDNS, Blockchain)     │              │
│  │   - Routing                               │              │
│  │   - Security                              │              │
│  └──────────────────────────────────────────┘              │
│                                  │                          │
│                                  ▼                          │
│  ┌──────────────────────────────────────────┐              │
│  │   ONET Network                            │              │
│  │   - Decentralized P2P Network             │              │
│  │   - Node Discovery                        │              │
│  │   - Service Routing                       │              │
│  └──────────────────────────────────────────┘              │
└─────────────────────────────────────────────────────────────┘
```

---

## Key Features

### 1. **Service Registration**
- A2A agents can register themselves as SERV services
- Automatic mapping from A2A AgentCard to UnifiedService
- Metadata preservation (services, skills, pricing, etc.)
- Integration with ONET discovery mechanisms

### 2. **Service Discovery**
- Discover agents by service name/capability
- Filter by health status (healthy only)
- Query across multiple capabilities
- Integration with ONET discovery (DHT, mDNS, Blockchain)

### 3. **Intelligent Routing**
Multiple routing strategies:
- **LeastBusy** - Route to service with least active tasks
- **RoundRobin** - Distribute requests evenly
- **Geographic** - Route based on location
- **CapabilityMatch** - Route to best matching service

### 4. **Health Monitoring**
- Automatic health checks with configurable intervals
- Service status tracking (Available, Busy, Offline, Unhealthy)
- Automatic removal of unhealthy services
- Event notifications for health changes

### 5. **OpenSERV Integration**
- Bridge to external OpenSERV AI agents
- HTTP client with retry logic
- Authentication and error handling
- Workflow execution support

---

## API Endpoints

### A2A Controller Endpoints

**Service Discovery**:
- `GET /api/a2a/agents/discover-serv` - Discover agents via SERV
- `GET /api/a2a/agents/discover-serv?service=data-analysis` - Filter by service

**Agent Registration**:
- `POST /api/a2a/agent/capabilities` - Register agent capabilities
- `POST /api/a2a/agent/register-service` - Register as SERV service

**Agent Information**:
- `GET /api/a2a/agent-card/{agentId}` - Get agent card
- `GET /api/a2a/agents` - List all agents
- `GET /api/a2a/agents/by-service/{service}` - Find agents by service

**A2A Communication**:
- `POST /api/a2a/jsonrpc` - Send JSON-RPC 2.0 requests
- `GET /api/a2a/messages` - Get pending messages
- `POST /api/a2a/messages/{messageId}/process` - Mark message processed

**OpenSERV Integration**:
- `POST /api/a2a/openserv/register` - Register OpenSERV agent
- `POST /api/a2a/workflow/execute` - Execute AI workflow
- `POST /api/serv/agents/{agentId}/chat` - Chat with OpenSERV agent (User or Agent JWT; proxy first, local fallback for dev)

---

## Usage Examples

### Example 1: Register an Agent as SERV Service

```csharp
// 1. Register agent capabilities
var capabilities = new AgentCapabilitiesInfo
{
    Services = new List<string> { "data-analysis", "report-generation" },
    Skills = new List<string> { "Python", "Machine Learning" },
    Status = AgentStatus.Available
};

await AgentManager.Instance.RegisterAgentCapabilitiesAsync(agentId, capabilities);

// 2. Register as SERV service
var servResult = await A2AManager.Instance.RegisterAgentAsServiceAsync(agentId, capabilities);
// Agent is now discoverable via SERV infrastructure
```

### Example 2: Discover Agents via SERV

```csharp
// Discover all agents
var allAgents = await A2AManager.Instance.DiscoverAgentsViaSERVAsync();

// Discover agents for specific service
var dataAnalysisAgents = await A2AManager.Instance.DiscoverAgentsViaSERVAsync("data-analysis");
```

### Example 3: Use Unified Service Manager

```csharp
// Register a service
var service = new MyUnifiedAgentService { ... };
await UnifiedAgentServiceManager.Instance.RegisterServiceAsync(service);

// Discover services by capability
var services = await UnifiedAgentServiceManager.Instance.DiscoverServicesAsync("data-analysis");

// Route a service request
var routedService = await UnifiedAgentServiceManager.Instance.RouteServiceAsync(
    "data-analysis",
    RoutingStrategy.LeastBusy
);

// Execute a service
var result = await UnifiedAgentServiceManager.Instance.ExecuteServiceAsync(
    "data-analysis",
    new Dictionary<string, object> { { "query", "analyze sales data" } }
);
```

### Example 4: OpenSERV Integration

```csharp
var bridgeService = new OpenServBridgeService(httpClient, baseUrl, apiKey);

// Execute an OpenSERV agent
var request = new OpenServAgentRequest
{
    AgentId = "openserv-agent-123",
    Endpoint = "/api/agents/execute",
    Input = "Analyze this data: ...",
    Parameters = new Dictionary<string, object> { ... }
};

var result = await bridgeService.ExecuteAgentAsync(request);
```

---

## MCP Integration

The SERV functionality is also exposed through the MCP (Model Context Protocol) server, allowing Cursor to interact with SERV agents via natural language:

**MCP Tools**:
- `oasis_discover_agents_via_serv` - Discover SERV agents
- `oasis_register_agent_as_serv_service` - Register agent as SERV service
- `oasis_get_agents_by_service` - Find agents by service name
- `oasis_register_openserv_agent` - Register OpenSERV agent

**Example**:
> "Discover SERV agents for data analysis"

Cursor will use the MCP tools to query SERV infrastructure and return discoverable agents.

---

## Technical Details

### Data Structures

**UnifiedService** (SERV):
```csharp
public class UnifiedService
{
    public string ServiceId { get; set; }
    public string ServiceName { get; set; }
    public string ServiceType { get; set; }  // "A2A_Agent"
    public string Name { get; set; }
    public string Description { get; set; }
    public string Category { get; set; }  // "Agent", "Data", "Finance", etc.
    public List<string> Capabilities { get; set; }
    public string Endpoint { get; set; }
    public string Protocol { get; set; }  // "A2A_JSON-RPC_2.0"
    public List<string> IntegrationLayers { get; set; }  // ["A2A", "ONET"]
    public List<string> Endpoints { get; set; }
    public bool IsActive { get; set; }
    public Dictionary<string, object> Metadata { get; set; }  // A2A-specific data
}
```

**IAgentCard** (A2A):
```csharp
public interface IAgentCard
{
    Guid AgentId { get; set; }
    string Name { get; set; }
    string Version { get; set; }
    IAgentCapabilitiesInfo Capabilities { get; set; }
    IAgentConnectionInfo Connection { get; set; }
    Dictionary<string, object> Metadata { get; set; }
}
```

### Service Category Mapping

A2A services are automatically categorized:
- `data-*`, `analysis-*` → "Data"
- `payment-*`, `wallet-*` → "Finance"
- `identity-*`, `avatar-*` → "Identity"
- `nft-*`, `token-*` → "Digital Assets"
- `search-*`, `discovery-*` → "Discovery"
- Default → "Agent"

---

## Benefits

### For Developers
- **Unified Discovery**: Single interface to discover all services (A2A, OpenSERV, native OASIS)
- **Intelligent Routing**: Automatic load balancing and service selection
- **Health Monitoring**: Automatic detection and removal of unhealthy services
- **Decentralized**: Services discoverable across ONET network

### For Agents
- **Discoverable**: Agents can be found via SERV infrastructure
- **Interoperable**: Works with A2A Protocol, OpenSERV, and native OASIS services
- **Scalable**: Automatic routing and load balancing
- **Resilient**: Health monitoring ensures only healthy services are used

### For the Ecosystem
- **Service Marketplace**: Foundation for agent/service marketplace
- **Network Effects**: More services = more value
- **Decentralized**: No single point of failure
- **Extensible**: Easy to add new service types

---

## Current Status

✅ **Complete**:
- UnifiedAgentServiceManager implementation
- A2A-SERV bridge (extension methods)
- OpenSERV bridge service
- MCP integration
- API endpoints
- Health monitoring
- Service routing
- User chat with OpenSERV agents (`POST /api/serv/agents/{agentId}/chat`) — proxy first, local fallback for dev

⚠️ **Future Enhancements**:
- Enhanced ONET discovery integration (currently basic)
- Service versioning support
- Advanced load balancing algorithms
- Service reputation scoring
- Service usage analytics

---

## Related Documentation

- **A2A Protocol**: `/A2A/README.md`
- **SERV Infrastructure Analysis**: `/A2A/docs/SERV_INFRASTRUCTURE_ANALYSIS.md`
- **OpenSERV Bridge Design**: `/A2A/docs/OPENSERV_BRIDGE_DESIGN.md`
- **MCP Integration**: `/MCP/A2A_SERV_INTEGRATION.md`
- **ONET Architecture**: `ONODE/NextGenSoftware.OASIS.API.ONODE.Core/Network/ONETUnifiedArchitecture.cs`

---

**Last Updated**: February 2026  
**Status**: ✅ Production Ready

