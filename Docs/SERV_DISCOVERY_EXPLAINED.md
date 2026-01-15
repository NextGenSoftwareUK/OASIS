# SERV Discovery - How It Works

**Last Updated:** January 2026

---

## Key Concept: SERV is OASIS's Own Infrastructure

**Important Distinction:**
- **SERV** = OASIS's Service Registry (part of ONET Unified Architecture) - **Our infrastructure**
- **OpenServ Platform** = External platform (platform.openserv.ai) - **Their UI**

SERV discovery happens **within OASIS**, not by connecting to an external platform.

---

## How SERV Discovery Works

### Architecture Overview

```
┌─────────────────────────────────────────────────────────┐
│              OASIS Portal (oportal-repo)                │
│  User clicks "Browse Agents"                            │
└──────────────────────┬──────────────────────────────────┘
                       │
                       │ HTTP Request
                       │ GET /api/a2a/agents/discover-serv
                       ▼
┌─────────────────────────────────────────────────────────┐
│         OASIS API (NextGenSoftware.OASIS.API)          │
│  A2AController.DiscoverAgentsViaSERV()                  │
└──────────────────────┬──────────────────────────────────┘
                       │
                       │ Calls
                       ▼
┌─────────────────────────────────────────────────────────┐
│      A2AManagerExtensions-SERV.cs                       │
│  DiscoverAgentsViaSERVAsync()                           │
└──────────────────────┬──────────────────────────────────┘
                       │
                       │ Queries
                       ▼
┌─────────────────────────────────────────────────────────┐
│   UnifiedAgentServiceManager (SERV Infrastructure)      │
│  - In-memory service cache (_serviceCache)              │
│  - Contains all registered A2A agents                    │
│  - Filters by capability/service name                    │
│  - Health checks (only healthy agents)                 │
└──────────────────────┬──────────────────────────────────┘
                       │
                       │ Returns
                       │ List<IUnifiedAgentService>
                       ▼
┌─────────────────────────────────────────────────────────┐
│      A2AManagerExtensions-SERV.cs                       │
│  - Enriches with Agent Cards                            │
│  - Transforms UnifiedService → AgentCard                │
└──────────────────────┬──────────────────────────────────┘
                       │
                       │ Returns
                       │ List<IAgentCard>
                       ▼
┌─────────────────────────────────────────────────────────┐
│         OASIS API                                       │
│  Returns JSON to portal                                 │
└──────────────────────┬──────────────────────────────────┘
                       │
                       │ JSON Response
                       ▼
┌─────────────────────────────────────────────────────────┐
│              OASIS Portal                                │
│  Displays agents in UI                                   │
└─────────────────────────────────────────────────────────┘
```

---

## Step-by-Step Flow

### 1. Agent Registration (How Agents Get Into SERV)

```
User creates agent in OASIS Portal
    ↓
POST /api/a2a/agent/capabilities
    ↓
AgentManager.RegisterAgentCapabilitiesAsync()
    ↓
User clicks "Register with SERV"
    ↓
POST /api/a2a/agent/register-service
    ↓
A2AManager.RegisterAgentAsServiceAsync()
    ↓
Creates UnifiedAgentService from AgentCard
    ↓
UnifiedAgentServiceManager.RegisterServiceAsync()
    ↓
Agent stored in _serviceCache (in-memory)
    ↓
Agent is now discoverable via SERV
```

**What Gets Stored:**
- Service ID (Agent ID)
- Service Name (Agent Name)
- Capabilities (Services list)
- Endpoint (A2A JSON-RPC endpoint)
- Status (Available/Busy/Offline)
- Metadata (skills, pricing, etc.)

### 2. Agent Discovery (How Portal Finds Agents)

```
User clicks "Browse Agents" in OASIS Portal
    ↓
JavaScript: fetch('/api/a2a/agents/discover-serv?service=data-analysis')
    ↓
OASIS API: A2AController endpoint
    ↓
A2AManager.DiscoverAgentsViaSERVAsync("data-analysis")
    ↓
UnifiedAgentServiceManager.DiscoverServicesAsync("data-analysis")
    ↓
UnifiedAgentServiceRouter.GetServicesForCapability("data-analysis")
    ↓
Queries _serviceCache for services with "data-analysis" capability
    ↓
Filters by health status (healthyOnly: true)
    ↓
Returns List<IUnifiedAgentService>
    ↓
For each service, get AgentCard by AgentId
    ↓
Returns List<IAgentCard> to portal
    ↓
Portal displays agents in UI
```

---

## Technical Details

### Where Services Are Stored

**UnifiedAgentServiceManager** maintains an **in-memory cache**:

```csharp
private readonly Dictionary<string, IUnifiedAgentService> _serviceCache = new Dictionary<string, IUnifiedAgentService>();
```

**Key Points:**
- Services are stored in memory (not in database)
- Each service is keyed by `ServiceId` (which is the Agent ID)
- Services are registered when agents call `RegisterAgentAsServiceAsync()`
- Services persist for the lifetime of the API instance

### How Discovery Works

**1. Query by Capability:**
```csharp
// In UnifiedAgentServiceManager
public async Task<OASISResult<List<IUnifiedAgentService>>> DiscoverServicesAsync(
    string capability,
    bool healthyOnly = true)
{
    // Get services from router
    var services = _router.GetServicesForCapability(capability);
    
    // Filter by health if requested
    if (healthyOnly)
    {
        services = services
            .Where(s => s.Status == UnifiedServiceStatus.Available || 
                       s.Status == UnifiedServiceStatus.Busy)
            .ToList();
    }
    
    return services;
}
```

**2. Router Looks Up Services:**
```csharp
// In UnifiedAgentServiceRouter
public List<IUnifiedAgentService> GetServicesForCapability(string capability)
{
    // Returns all services that have this capability in their Capabilities list
    return _serviceRegistry
        .Where(kvp => kvp.Value.Any(s => s.Capabilities.Contains(capability)))
        .SelectMany(kvp => kvp.Value)
        .ToList();
}
```

**3. Enrich with Agent Cards:**
```csharp
// In A2AManagerExtensions-SERV
foreach (var service in services)
{
    // Extract agent ID
    Guid agentId = service.AgentId.Value;
    
    // Get full Agent Card
    var agentCard = await AgentManager.Instance.GetAgentCardAsync(agentId);
    
    // Add to results
    agentCards.Add(agentCard);
}
```

---

## How Portal Connects to SERV

### The Connection is Through OASIS API

**Important:** The portal doesn't connect directly to SERV. It connects to **OASIS API**, which then queries SERV infrastructure.

### Portal → API Flow

```javascript
// In oportal-repo/agents-dashboard.js

// 1. User wants to browse agents
async function browseAgents(serviceName = null) {
    // 2. Portal calls OASIS API endpoint
    const url = serviceName 
        ? `${OASIS_API}/api/a2a/agents/discover-serv?service=${serviceName}`
        : `${OASIS_API}/api/a2a/agents/discover-serv`;
    
    // 3. API queries SERV infrastructure internally
    const response = await fetch(url);
    const agents = await response.json();
    
    // 4. Portal displays agents
    displayAgents(agents);
}
```

### API → SERV Flow

```csharp
// In A2AController.cs
[HttpGet("agents/discover-serv")]
public async Task<IActionResult> DiscoverAgentsViaSERV([FromQuery] string service = null)
{
    // 1. API calls A2AManager extension method
    var result = await A2AManager.Instance.DiscoverAgentsViaSERVAsync(service);
    
    // 2. This queries UnifiedAgentServiceManager (SERV)
    // 3. Returns list of Agent Cards
    return Ok(result.Result);
}
```

---

## What "SERV Discovery" Actually Means

### SERV = Service Registry (OASIS Infrastructure)

**SERV is:**
- ✅ Part of OASIS's ONET Unified Architecture
- ✅ UnifiedAgentServiceManager (in-memory service registry)
- ✅ Service routing and health monitoring
- ✅ OASIS's own infrastructure

**SERV is NOT:**
- ❌ External OpenServ platform
- ❌ Requires external API calls
- ❌ Separate service you connect to

### Discovery Process

1. **Registration**: Agents register themselves with SERV (UnifiedAgentServiceManager)
2. **Storage**: Services stored in in-memory cache
3. **Discovery**: Query cache by capability/service name
4. **Enrichment**: Add Agent Card details
5. **Return**: List of discoverable agents

---

## Current Implementation Status

### ✅ What Works Now

**Registration:**
- Agents can register with SERV via `RegisterAgentAsServiceAsync()`
- Services stored in `UnifiedAgentServiceManager._serviceCache`
- Services registered with router for capability lookup

**Discovery:**
- Can discover all agents: `GetAllServicesAsync()`
- Can discover by capability: `DiscoverServicesAsync(capability)`
- Health filtering works (healthyOnly flag)
- Agent Card enrichment works

**API Endpoints:**
- `GET /api/a2a/agents/discover-serv` - Discover all agents
- `GET /api/a2a/agents/discover-serv?service=data-analysis` - Filter by service

### ⚠️ Current Limitations

**In-Memory Storage:**
- Services are stored in memory (lost on API restart)
- Not persisted to database
- Not shared across multiple API instances

**No Cross-Instance Discovery:**
- Each API instance has its own service cache
- Services registered on one instance aren't visible to others
- No distributed discovery yet

**No ONET Network Discovery:**
- Currently only queries local cache
- Doesn't query ONET network (DHT, mDNS, Blockchain) for remote services
- Future enhancement planned

---

## How Portal Would Use This

### Example: Browse Agents Tab

```javascript
// oportal-repo/agents-dashboard.js

class AgentsDashboard {
    async loadAgents(serviceFilter = null) {
        try {
            // Call OASIS API endpoint
            const url = serviceFilter
                ? `${OASIS_API}/api/a2a/agents/discover-serv?service=${serviceFilter}`
                : `${OASIS_API}/api/a2a/agents/discover-serv`;
            
            const response = await fetch(url);
            const result = await response.json();
            
            if (result.result) {
                // Display agents
                this.displayAgents(result.result);
            }
        } catch (error) {
            console.error('Error loading agents:', error);
        }
    }
    
    displayAgents(agentCards) {
        // agentCards is List<IAgentCard>
        agentCards.forEach(agent => {
            // Display agent card with:
            // - agent.agentId
            // - agent.name
            // - agent.capabilities.services
            // - agent.capabilities.skills
            // - agent.connection.endpoint
            // - agent.metadata (pricing, status, etc.)
        });
    }
}
```

### Example: Register Agent with SERV

```javascript
// When user clicks "Register with SERV" button

async function registerAgentWithSERV(agentId) {
    const response = await fetch(`${OASIS_API}/api/a2a/agent/register-service`, {
        method: 'POST',
        headers: {
            'Authorization': `Bearer ${token}`,
            'Content-Type': 'application/json'
        }
    });
    
    const result = await response.json();
    
    if (result.success) {
        // Agent is now discoverable via SERV
        showNotification('Agent registered with SERV successfully!');
    }
}
```

---

## Summary

### How SERV Discovery Works

1. **Agents Register**: When an agent registers with SERV, it's stored in `UnifiedAgentServiceManager._serviceCache`
2. **Portal Queries API**: Portal calls `/api/a2a/agents/discover-serv`
3. **API Queries SERV**: API calls `UnifiedAgentServiceManager.DiscoverServicesAsync()`
4. **SERV Returns Services**: SERV queries its in-memory cache and returns matching services
5. **Enrichment**: Services are enriched with Agent Cards
6. **Portal Displays**: Portal receives Agent Cards and displays them

### Key Points

- ✅ **SERV is OASIS's own infrastructure** (not external)
- ✅ **Portal connects via OASIS API** (standard REST calls)
- ✅ **Services stored in-memory** (UnifiedAgentServiceManager)
- ✅ **Discovery queries local cache** (fast, but instance-specific)
- ⚠️ **Not yet distributed** (future: ONET network discovery)

### Future Enhancements

- Persistent storage (database)
- Distributed discovery (ONET network)
- Cross-instance service sharing
- Real-time service updates

---

**Status:** ✅ Current Implementation Explained  
**Last Updated:** January 2026
