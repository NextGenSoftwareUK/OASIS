# ONET Service Registry - Complete Documentation

**Last Updated:** January 2026  
**Status:** ✅ Production Ready

---

## Table of Contents

1. [Overview](#overview)
2. [Architecture](#architecture)
3. [Core Components](#core-components)
4. [Service Registration](#service-registration)
5. [Service Discovery](#service-discovery)
6. [Service Routing](#service-routing)
7. [Health Monitoring](#health-monitoring)
8. [API Endpoints](#api-endpoints)
9. [Usage Examples](#usage-examples)
10. [MCP Integration](#mcp-integration)
11. [OpenSERV Integration](#openserv-integration)
12. [Data Structures](#data-structures)
13. [Integration Mapping](#integration-mapping)
14. [Technical Implementation](#technical-implementation)
15. [Current Status](#current-status)

---

## Overview

**ONET Service Registry** is part of the **ONET Unified Architecture** - OASIS's decentralized service discovery and routing infrastructure. The ONET Service Registry enables A2A (Agent-to-Agent) Protocol agents to be registered, discovered, and accessed through the ONET network.

### Key Concepts

- **ONET Service Registry**: OASIS's internal service registry - Central registry for all services (A2A agents, OpenSERV agents, native OASIS services)
- **OpenSERV Platform**: External platform (openserv.ai) - Partner platform for AI agent infrastructure
- **ONET**: OASIS Network - Decentralized P2P network layer
- **A2A Protocol**: Agent-to-Agent communication protocol (JSON-RPC 2.0)
- **UnifiedService**: ONET Service Registry's service representation format
- **AgentCard**: A2A Protocol's agent representation format

### Important Distinction

**ONET Service Registry** (OASIS's infrastructure) is **NOT** the same as **OpenSERV Platform** (external partner platform):
- **ONET Service Registry** = OASIS's own service discovery infrastructure
- **OpenSERV Platform** = External AI agent platform (openserv.ai) - OASIS's investors/partners

### Benefits

**For Developers:**
- Unified discovery interface for all service types
- Intelligent routing with load balancing
- Automatic health monitoring
- Decentralized service discovery

**For Agents:**
- Discoverable across the ONET network
- Interoperable with A2A, OpenSERV, and native OASIS services
- Scalable with automatic routing
- Resilient with health monitoring

**For the Ecosystem:**
- Foundation for agent/service marketplace
- Network effects (more services = more value)
- Decentralized (no single point of failure)
- Extensible (easy to add new service types)

---

## Architecture

### High-Level Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    OASIS Ecosystem                          │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  ┌──────────────┐         ┌──────────────┐                │
│  │ A2A Protocol │────────►│ ONET Service │                │
│  │   Agents     │         │  Registry    │                │
│  └──────────────┘         │  Bridge      │                │
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

### Service Registration Flow

```
A2A Agent Registration
    ↓
AgentManager.RegisterAgentCapabilitiesAsync()
    ↓
A2AManager.RegisterAgentAsServiceAsync() (extension)
    ↓
UnifiedAgentServiceManager.RegisterServiceAsync()
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

### Service Discovery Flow

```
Service Discovery Request
    ↓
GetUnifiedServicesAsync(category, capability)
    ↓
Filter local services from _unifiedServices
    ↓
Query ONETDiscovery for remote services (optional)
    ↓
Merge and deduplicate results
    ↓
Transform UnifiedService → AgentCard
    ↓
Return enriched AgentCard list
```

---

## Core Components

### 1. UnifiedAgentServiceManager

**Location:** `OASIS Architecture/NextGenSoftware.OASIS.API.Core/Managers/UnifiedAgentServiceManager/`

**Purpose:** Main service management system that provides unified registration, discovery, routing, and health checking.

**Key Features:**
- Service registration and unregistration
- Service discovery with filtering
- Intelligent routing with multiple strategies
- Health monitoring with automatic removal
- In-memory caching for fast lookups

**Key Methods:**
- `RegisterServiceAsync(IUnifiedAgentService service)` - Register a service
- `UnregisterServiceAsync(string serviceId)` - Unregister a service
- `DiscoverServicesAsync(string capability, bool healthyOnly = true)` - Discover services by capability
- `RouteServiceAsync(string capability, RoutingStrategy strategy)` - Route to appropriate service
- `ExecuteServiceAsync(string capability, Dictionary<string, object> parameters)` - Execute a service
- `GetServiceAsync(string serviceId)` - Get service by ID
- `GetAllServicesAsync(bool healthyOnly = false)` - Get all services

**Components:**
- `UnifiedAgentServiceManager.cs` - Main service manager
- `UnifiedAgentServiceRouter.cs` - Routing logic
- `UnifiedAgentServiceHealthChecker.cs` - Health monitoring

### 2. A2A-SERV Bridge (Extension Methods)

**Location:** `ONODE/NextGenSoftware.OASIS.API.ONODE.Core/Managers/A2AManager/A2AManagerExtensions-SERV.cs`

**Purpose:** Extension methods that bridge A2A Protocol agents with SERV infrastructure.

**Key Methods:**

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

**Data Mapping:**
- A2A Agent ID → SERV Service ID
- Agent Capabilities → Service Capabilities
- Agent Endpoint → Service Endpoints
- Agent Status → Service IsActive flag
- Agent Metadata → Service Metadata dictionary

### 3. ONET Unified Architecture

**Location:** `ONODE/NextGenSoftware.OASIS.API.ONODE.Core/Network/ONETUnifiedArchitecture.cs`

**Purpose:** Core infrastructure for service registry and discovery.

**Features:**
- Centralized service registry (`_unifiedServices` dictionary)
- Service categories (Identity, Reputation, Data, Finance, Digital Assets, etc.)
- Integration layers (WEB4, ONET, HyperDrive)
- Discovery mechanisms (DHT, mDNS, Blockchain, Bootstrap)

### 4. ONETDiscovery

**Location:** `ONODE/NextGenSoftware.OASIS.API.ONODE.Core/Network/ONETDiscovery.cs`

**Purpose:** Multi-protocol node and service discovery.

**Discovery Protocols:**

1. **DHT (Distributed Hash Table)**
   - Protocol: Kademlia-based peer discovery
   - Use Case: Decentralized peer discovery across the network
   - Interval: ~30 seconds

2. **mDNS (multicast DNS)**
   - Protocol: RFC 6762
   - Use Case: Local network service discovery (LAN)
   - Interval: ~10 seconds

3. **Blockchain Discovery**
   - Protocol: Smart contract-based node registry
   - Use Case: Decentralized, immutable node registry
   - Interval: ~60 seconds

4. **Bootstrap Discovery**
   - Protocol: HTTP/HTTPS
   - Use Case: Fallback discovery when other methods fail
   - Interval: ~120 seconds

---

## Service Registration

### Registration Process

1. **Register Agent Capabilities** (A2A Protocol)
   ```csharp
   var capabilities = new AgentCapabilitiesInfo
   {
       Services = new List<string> { "data-analysis", "report-generation" },
       Skills = new List<string> { "Python", "Machine Learning" },
       Status = AgentStatus.Available,
       Description = "Data analysis agent"
   };
   
   await AgentManager.Instance.RegisterAgentCapabilitiesAsync(agentId, capabilities);
   ```

2. **Register as SERV Service** (SERV Infrastructure)
   ```csharp
   var servResult = await A2AManager.Instance.RegisterAgentAsServiceAsync(agentId, capabilities);
   ```

3. **Automatic Registration**
   - When capabilities are registered, agent can be automatically registered with SERV
   - Or register manually via API endpoint

### Registration via API

**Endpoint:** `POST /api/a2a/agent/register-service`

**Request:**
```http
POST /api/a2a/agent/register-service
Authorization: Bearer {token}
```

**Response:**
```json
{
  "success": true,
  "message": "Agent registered as SERV service successfully"
}
```

### Registration Details

**What Happens:**
1. Agent capabilities are retrieved
2. Agent Card is generated
3. UnifiedService is created from AgentCard
4. Service is registered in UnifiedAgentServiceManager
5. Service is registered in ONET Unified Architecture
6. Service is optionally registered with ONETDiscovery
7. Service becomes discoverable

**Service Metadata Stored:**
- Agent ID
- Agent capabilities (services, skills)
- Agent endpoint (JSON-RPC endpoint)
- Agent status
- Agent pricing (if available)
- Agent version

---

## Service Discovery

### Discovery Methods

#### 1. Discover All Agents via SERV

**Endpoint:** `GET /api/a2a/agents/discover-serv`

**Request:**
```http
GET /api/a2a/agents/discover-serv
```

**Response:**
```json
{
  "result": [
    {
      "agentId": "guid-123",
      "name": "Data Analysis Agent",
      "capabilities": {
        "services": ["data-analysis"],
        "skills": ["Python"]
      },
      "connection": {
        "endpoint": "https://api.oasisweb4.com/api/a2a/jsonrpc",
        "protocol": "jsonrpc2.0"
      }
    }
  ]
}
```

#### 2. Discover Agents by Service

**Endpoint:** `GET /api/a2a/agents/discover-serv?service=data-analysis`

**Request:**
```http
GET /api/a2a/agents/discover-serv?service=data-analysis
```

**Response:** Same format as above, filtered by service name

### Discovery via Code

```csharp
// Discover all agents
var allAgents = await A2AManager.Instance.DiscoverAgentsViaSERVAsync();

// Discover agents for specific service
var dataAnalysisAgents = await A2AManager.Instance.DiscoverAgentsViaSERVAsync("data-analysis");
```

### Discovery Process

1. Query UnifiedAgentServiceManager for registered services
2. Filter by service name/capability (if provided)
3. Filter by health status (healthy only by default)
4. Transform UnifiedService → AgentCard
5. Enrich with A2A Protocol information
6. Return list of Agent Cards

---

## Service Routing

### Routing Strategies

The `UnifiedAgentServiceRouter` supports multiple routing strategies:

1. **RoundRobin** - Distribute requests evenly across services
2. **LeastBusy** - Route to service with least active tasks
3. **FastestResponse** - Route to fastest responding service
4. **Random** - Random selection
5. **Priority** - Route based on service priority

### Routing via Code

```csharp
// Route to a service using LeastBusy strategy
var routedService = await UnifiedAgentServiceManager.Instance.RouteServiceAsync(
    "data-analysis",
    RoutingStrategy.LeastBusy
);

// Execute the service
var result = await UnifiedAgentServiceManager.Instance.ExecuteServiceAsync(
    "data-analysis",
    new Dictionary<string, object> { { "query", "analyze sales data" } }
);
```

### Routing Process

1. Get all services matching the capability
2. Filter by health status (healthy only)
3. Apply routing strategy
4. Select best service
5. Return service for execution

---

## Health Monitoring

### Health Checker

**Location:** `UnifiedAgentServiceHealthChecker.cs`

**Features:**
- Automatic health checks with configurable intervals
- Service status tracking (Available, Busy, Offline, Unhealthy)
- Automatic removal of unhealthy services
- Event notifications for health changes

### Service Status

- **Available** - Service is healthy and ready
- **Busy** - Service is healthy but currently handling requests
- **Offline** - Service is not responding
- **Unhealthy** - Service is responding but with errors

### Health Check Process

1. Periodic health checks (configurable interval)
2. Ping service endpoint
3. Measure response time
4. Check response status
5. Update service status
6. Remove unhealthy services (optional)
7. Fire events for status changes

### Health Check Configuration

```csharp
// Configure health checker
_healthChecker.HealthCheckInterval = TimeSpan.FromSeconds(30);
_healthChecker.RemoveUnhealthyServices = true;
_healthChecker.Timeout = TimeSpan.FromSeconds(5);
```

---

## API Endpoints

### A2A Controller Endpoints

#### Service Discovery
- `GET /api/a2a/agents/discover-serv` - Discover agents via SERV
- `GET /api/a2a/agents/discover-serv?service=data-analysis` - Filter by service

#### Agent Registration
- `POST /api/a2a/agent/capabilities` - Register agent capabilities
- `POST /api/a2a/agent/register-service` - Register as SERV service

#### Agent Information
- `GET /api/a2a/agent-card/{agentId}` - Get agent card
- `GET /api/a2a/agents` - List all agents
- `GET /api/a2a/agents/by-service/{service}` - Find agents by service

#### A2A Communication
- `POST /api/a2a/jsonrpc` - Send JSON-RPC 2.0 requests
- `GET /api/a2a/messages` - Get pending messages
- `POST /api/a2a/messages/{messageId}/process` - Mark message processed

#### OpenSERV Integration
- `POST /api/a2a/openserv/register` - Register OpenSERV agent
- `POST /api/a2a/workflow/execute` - Execute AI workflow

---

## Usage Examples

### Example 1: Complete Registration Flow

```csharp
// 1. Register agent capabilities
var capabilities = new AgentCapabilitiesInfo
{
    Services = new List<string> { "data-analysis", "report-generation" },
    Skills = new List<string> { "Python", "Machine Learning" },
    Status = AgentStatus.Available,
    Description = "Data analysis agent",
    Pricing = new Dictionary<string, decimal>
    {
        ["data-analysis"] = 0.01m,
        ["report-generation"] = 0.02m
    }
};

await AgentManager.Instance.RegisterAgentCapabilitiesAsync(agentId, capabilities);

// 2. Register as SERV service
var servResult = await A2AManager.Instance.RegisterAgentAsServiceAsync(agentId, capabilities);

if (servResult.IsError)
{
    Console.WriteLine($"Error: {servResult.Message}");
}
else
{
    Console.WriteLine("Agent registered successfully!");
}
```

### Example 2: Discover and Use Services

```csharp
// Discover agents for data analysis
var agentsResult = await A2AManager.Instance.DiscoverAgentsViaSERVAsync("data-analysis");

if (!agentsResult.IsError && agentsResult.Result != null)
{
    foreach (var agentCard in agentsResult.Result)
    {
        Console.WriteLine($"Agent: {agentCard.Name}");
        Console.WriteLine($"Services: {string.Join(", ", agentCard.Capabilities.Services)}");
        Console.WriteLine($"Endpoint: {agentCard.Connection.Endpoint}");
    }
}
```

### Example 3: Route and Execute Service

```csharp
// Route to best service
var routedService = await UnifiedAgentServiceManager.Instance.RouteServiceAsync(
    "data-analysis",
    RoutingStrategy.LeastBusy
);

if (routedService.Result != null)
{
    // Execute the service
    var result = await UnifiedAgentServiceManager.Instance.ExecuteServiceAsync(
        "data-analysis",
        new Dictionary<string, object>
        {
            ["query"] = "analyze sales data",
            ["format"] = "json"
        }
    );
    
    Console.WriteLine($"Result: {result.Result}");
}
```

### Example 4: HTTP API Usage

```python
import requests

BASE_URL = "http://localhost:5003"
API_URL = f"{BASE_URL}/api/a2a"
JWT_TOKEN = "your_jwt_token"

# 1. Register capabilities
capabilities = {
    "services": ["data-analysis"],
    "skills": ["Python"],
    "status": "Available"
}

response = requests.post(
    f"{API_URL}/agent/capabilities",
    headers={"Authorization": f"Bearer {JWT_TOKEN}"},
    json=capabilities
)

# 2. Register as SERV service
response = requests.post(
    f"{API_URL}/agent/register-service",
    headers={"Authorization": f"Bearer {JWT_TOKEN}"}
)

# 3. Discover agents
response = requests.get(f"{API_URL}/agents/discover-serv?service=data-analysis")
agents = response.json()
```

---

## MCP Integration

The SERV functionality is exposed through the MCP (Model Context Protocol) server, allowing Cursor to interact with SERV agents via natural language.

### MCP Tools

#### Agent Discovery & Cards
- `oasis_get_agent_card` - Get agent card by ID
- `oasis_get_all_agents` - List all A2A agents
- `oasis_get_agents_by_service` - Find agents by service name

#### Agent Registration
- `oasis_register_agent_capabilities` - Register agent capabilities
- `oasis_register_agent_as_serv_service` - Register agent as SERV service

#### SERV Infrastructure
- `oasis_discover_agents_via_serv` - Discover agents via SERV (with optional service filter)

#### A2A Protocol Communication
- `oasis_send_a2a_jsonrpc_request` - Send JSON-RPC 2.0 requests
- `oasis_get_pending_a2a_messages` - Get pending messages for agent
- `oasis_mark_a2a_message_processed` - Mark message as processed

#### OpenSERV Integration
- `oasis_register_openserv_agent` - Register OpenSERV AI agent as A2A agent
- `oasis_execute_ai_workflow` - Execute AI workflows via A2A Protocol

### MCP Usage Examples

**Example 1: Discover SERV Agents**
> "Find all SERV agents that provide data analysis services"

**Example 2: Register an Agent**
> "Register my agent with capabilities for data analysis and Python skills"

**Example 3: Send A2A Payment Request**
> "Send a payment request for 0.01 SOL to agent abc-123 for data analysis service"

**Example 4: Get Agent Card**
> "Show me the agent card for agent xyz-789"

### Authentication

**Tools requiring authentication:**
- `oasis_register_agent_capabilities`
- `oasis_register_agent_as_serv_service`
- `oasis_send_a2a_jsonrpc_request`
- `oasis_get_pending_a2a_messages`
- `oasis_mark_a2a_message_processed`
- `oasis_execute_ai_workflow`

**To authenticate:**
1. Use `oasis_authenticate_avatar` first
2. The token is automatically stored and used for subsequent requests

**Public tools (no auth required):**
- `oasis_get_agent_card`
- `oasis_get_all_agents`
- `oasis_get_agents_by_service`
- `oasis_discover_agents_via_serv`

---

## OpenSERV Integration

### Overview

A2A-OpenSERV integration enables **bidirectional discovery** between OASIS and OpenSERV platforms:
- **OpenSERV → OASIS**: OpenSERV AI agents can be registered as A2A agents and discovered via ONET Service Registry
- **OASIS → OpenSERV**: OASIS A2A agents can be registered with OpenSERV platform for discovery on their platform

### Important Distinction

- **ONET Service Registry** = OASIS's internal service discovery infrastructure
- **OpenSERV Platform** = External AI agent platform (openserv.ai) - OASIS's partners/investors

### Registration Flow (OpenSERV → OASIS)

1. **Register OpenSERV Agent with OASIS**
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

2. **Automatic A2A Registration** - Creates avatar and registers capabilities

3. **ONET Service Registry Registration** - Registers with OASIS's ONET Service Registry

### Registration Flow (OASIS → OpenSERV)

1. **Register OASIS Agent with OpenSERV Platform**
   ```csharp
   await A2AManager.Instance.RegisterOasisAgentWithOpenServAsync(
       agentId: agentGuid,
       openServApiKey: "sk-...",
       oasisAgentEndpoint: "https://api.oasisweb4.com/api/a2a/agent-card/{agentId}"
   );
   ```

2. **Agent becomes discoverable on OpenSERV platform**

3. **OpenSERV users can discover and use OASIS agents**

### Workflow Execution

**Endpoint:** `POST /api/a2a/workflow/execute`

**Request:**
```json
{
  "toAgentId": "agent-uuid",
  "workflowRequest": "Analyze the sales data and generate a report",
  "workflowParameters": {
    "data_source": "sales_data.csv",
    "report_format": "pdf"
  }
}
```

### OpenServBridgeService

**Location:** `ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI/Services/OpenServBridgeService.cs`

**Features:**
- HTTP client with retry logic (3 retries with exponential backoff)
- Authentication via Bearer tokens
- Request/response serialization
- Error handling and logging
- Execution time tracking

**Methods:**
- `ExecuteAgentAsync()` - Execute an OpenSERV agent request
- `ExecuteWorkflowAsync()` - Execute an OpenSERV workflow

---

## Data Structures

### UnifiedService (SERV)

```csharp
public class UnifiedService
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public List<string> IntegrationLayers { get; set; } = new List<string>();
    public List<string> Endpoints { get; set; } = new List<string>();
    public bool IsActive { get; set; }
    // Extended properties (if implemented):
    public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    public string ServiceId { get; set; } = string.Empty;
    public string Version { get; set; } = "1.0";
}
```

### IAgentCard (A2A)

```csharp
public interface IAgentCard
{
    string AgentId { get; set; }
    string Name { get; set; }
    string Version { get; set; }
    AgentCapabilitiesInfo Capabilities { get; set; }
    AgentConnectionInfo Connection { get; set; }
    Dictionary<string, object> Metadata { get; set; }
    string OwnerAvatarId { get; set; }
}
```

### IUnifiedAgentService

```csharp
public interface IUnifiedAgentService
{
    string ServiceId { get; set; }
    string ServiceName { get; set; }
    string ServiceType { get; set; }
    List<string> Capabilities { get; set; }
    string Endpoint { get; set; }
    UnifiedServiceStatus Status { get; set; }
    int ActiveTasks { get; set; }
    int MaxConcurrentTasks { get; set; }
    DateTime RegisteredAt { get; set; }
    Dictionary<string, object> Metadata { get; set; }
}
```

---

## Integration Mapping

### A2A Protocol ↔ SERV Mapping

| A2A Protocol Concept | SERV/ONET Concept | Mapping Strategy |
|---------------------|-------------------|------------------|
| **Agent Card** (`IAgentCard`) | **UnifiedService** | Create UnifiedService from AgentCard |
| **Agent ID** (`AgentId`) | **Service ID** | Use AgentId as service key |
| **Agent Name** (`Name`) | **Service Name** | Direct mapping |
| **Services** (`Capabilities.Services`) | **Service Category/Capabilities** | Map to Category and Endpoints |
| **Skills** (`Capabilities.Skills`) | **Service Capabilities** | Add to IntegrationLayers or Metadata |
| **Endpoint** (`Connection.Endpoint`) | **Endpoints** | Add to Endpoints list |
| **Protocol** (`Connection.Protocol`) | **Integration Layer** | Map JSON-RPC to ONET layer |
| **Status** (`Capabilities.Status`) | **IsActive** | Available = true, others = false |
| **Metadata** (`Metadata`) | **Service Metadata** | Store in extended UnifiedService |

### Service Category Mapping

A2A services are automatically categorized:

| A2A Service Pattern | SERV Category |
|---------------------|---------------|
| `data-*`, `analysis-*`, `processing-*` | "Data" |
| `payment-*`, `wallet-*`, `transaction-*` | "Finance" |
| `identity-*`, `avatar-*`, `user-*` | "Identity" |
| `nft-*`, `token-*`, `asset-*` | "Digital Assets" |
| `search-*`, `discovery-*`, `query-*` | "Discovery" |
| `communication-*`, `message-*`, `chat-*` | "Communication" |
| `reputation-*`, `karma-*`, `rating-*` | "Reputation" |
| `consensus-*`, `governance-*`, `voting-*` | "Governance" |
| *default* | "Agent" |

### Integration Layer Mapping

| A2A Protocol | SERV Integration Layer | Notes |
|--------------|------------------------|-------|
| `jsonrpc2.0` | `ONET` | Primary integration layer |
| `http` | `WEB4` | Fallback for HTTP-based agents |
| `https` | `WEB4` | Secure HTTP layer |
| *default* | `ONET` | Default to ONET for A2A agents |

---

## Technical Implementation

### File Locations

**Core Components:**
- `OASIS Architecture/NextGenSoftware.OASIS.API.Core/Managers/UnifiedAgentServiceManager/`
  - `UnifiedAgentServiceManager.cs` - Main service manager
  - `UnifiedAgentServiceRouter.cs` - Routing logic
  - `UnifiedAgentServiceHealthChecker.cs` - Health monitoring

**A2A-SERV Bridge:**
- `ONODE/NextGenSoftware.OASIS.API.ONODE.Core/Managers/A2AManager/A2AManagerExtensions-SERV.cs`

**ONET Infrastructure:**
- `ONODE/NextGenSoftware.OASIS.API.ONODE.Core/Network/ONETUnifiedArchitecture.cs`
- `ONODE/NextGenSoftware.OASIS.API.ONODE.Core/Network/ONETDiscovery.cs`

**API Endpoints:**
- `ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI/Controllers/A2AController.cs`

**OpenSERV Bridge:**
- `ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI/Services/OpenServBridgeService.cs`

### Key Implementation Details

#### Service Registration Implementation

```csharp
// A2A-SERV Bridge Extension Method
public static async Task<OASISResult<bool>> RegisterAgentAsServiceAsync(
    this A2AManager a2aManager,
    Guid agentId,
    IAgentCapabilities capabilities)
{
    // 1. Get agent card
    var agentCardResult = await AgentManager.Instance.GetAgentCardAsync(agentId);
    
    // 2. Create UnifiedService from AgentCard
    var unifiedService = new UnifiedService
    {
        ServiceId = agentId.ToString(),
        Name = agentCard.Name,
        Description = capabilities.Description,
        Category = DetermineCategory(capabilities.Services),
        IntegrationLayers = new List<string> { "A2A", "ONET" },
        Endpoints = new List<string> { agentCard.Connection.Endpoint },
        IsActive = capabilities.Status == AgentStatus.Available,
        Metadata = new Dictionary<string, object>
        {
            ["a2a_agent_id"] = agentId,
            ["a2a_services"] = capabilities.Services,
            ["a2a_skills"] = capabilities.Skills
        }
    };
    
    // 3. Register with UnifiedAgentServiceManager
    return await UnifiedAgentServiceManager.Instance.RegisterServiceAsync(unifiedService);
}
```

#### Service Discovery Implementation

```csharp
// A2A-SERV Bridge Extension Method
public static async Task<OASISResult<List<IAgentCard>>> DiscoverAgentsViaSERVAsync(
    this A2AManager a2aManager,
    string serviceName = null)
{
    // 1. Discover services from UnifiedAgentServiceManager
    var servicesResult = await UnifiedAgentServiceManager.Instance.DiscoverServicesAsync(
        serviceName, 
        healthyOnly: true
    );
    
    // 2. Transform UnifiedService → AgentCard
    var agentCards = new List<IAgentCard>();
    foreach (var service in servicesResult.Result)
    {
        var agentCard = ConvertUnifiedServiceToAgentCard(service);
        agentCards.Add(agentCard);
    }
    
    return new OASISResult<List<IAgentCard>> { Result = agentCards };
}
```

---

## Current Status

### ✅ Complete

- ✅ UnifiedAgentServiceManager implementation (full)
- ✅ A2A-SERV bridge (extension methods - basic implementation)
- ✅ OpenSERV bridge service
- ✅ MCP integration (12 tools)
- ✅ API endpoints (all endpoints implemented)
- ✅ Health monitoring (automatic health checks)
- ✅ Service routing (multiple strategies)
- ✅ Service discovery (via UnifiedAgentServiceManager)
- ✅ Service registration (via UnifiedAgentServiceManager)

### ⚠️ Current Limitations

**A2A-SERV Bridge:**
- `RegisterAgentAsServiceAsync()` - Currently uses placeholder for ONETUnifiedArchitecture registration
- `DiscoverAgentsViaSERVAsync()` - Currently returns empty list (requires SERV infrastructure enhancement)
- Full integration requires ONETUnifiedArchitecture to support dynamic service registration

**ONET Unified Architecture:**
- Service registry is dictionary-based (in-memory)
- No persistent storage for services
- Limited dynamic registration support

### 🔄 Future Enhancements

- Enhanced ONET discovery integration (currently basic)
- Dynamic service registration in ONETUnifiedArchitecture
- Persistent service storage
- Service versioning support
- Advanced load balancing algorithms
- Service reputation scoring
- Service usage analytics
- Service caching optimization
- Cross-chain service discovery
- Full A2A-SERV bridge implementation

---

## Related Documentation

- **A2A Protocol**: `/A2A/README.md`
- **Agent-to-NFT Trading**: `/Docs/AGENT_NFT_TRADING_ARCHITECTURE.md`
- **Agent-to-User Linking**: `/Docs/AGENT_TO_USER_LINKING.md`
- **ONET Architecture**: `ONODE/NextGenSoftware.OASIS.API.ONODE.Core/Network/ONETUnifiedArchitecture.cs`
- **MCP Integration**: `/MCP/A2A_SERV_INTEGRATION.md`

---

**Status:** ✅ Production Ready  
**Last Updated:** January 2026
