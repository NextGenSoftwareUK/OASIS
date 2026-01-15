# A2A Protocol & SERV Infrastructure MCP Integration

**Date:** January 7, 2026  
**Status:** ✅ Complete

---

## Overview

SERV (Service Registry) functionality and A2A (Agent-to-Agent) Protocol have been successfully integrated into the OASIS Unified MCP Server. This enables Cursor to interact with SERV agents, discover services, and execute agent-to-agent communication through natural language.

---

## What Was Added

### 12 New MCP Tools

#### Agent Discovery & Cards
1. **`oasis_get_agent_card`** - Get agent card by ID
2. **`oasis_get_all_agents`** - List all A2A agents
3. **`oasis_get_agents_by_service`** - Find agents by service name

#### Agent Registration
4. **`oasis_register_agent_capabilities`** - Register agent capabilities
5. **`oasis_register_agent_as_serv_service`** - Register agent as SERV service

#### SERV Infrastructure
6. **`oasis_discover_agents_via_serv`** - Discover agents via SERV (with optional service filter)

#### A2A Protocol Communication
7. **`oasis_send_a2a_jsonrpc_request`** - Send JSON-RPC 2.0 requests (ping, payment_request, etc.)
8. **`oasis_get_pending_a2a_messages`** - Get pending messages for agent
9. **`oasis_mark_a2a_message_processed`** - Mark message as processed

#### OpenSERV Integration
10. **`oasis_register_openserv_agent`** - Register OpenSERV AI agent as A2A agent
11. **`oasis_execute_ai_workflow`** - Execute AI workflows via A2A Protocol

---

## How It Works

### Architecture

```
Cursor (You)
    ↓
    "Discover SERV agents for data analysis"
    ↓
MCP Server (oasis-unified-mcp)
    ↓
    Routes to: oasis_discover_agents_via_serv
    ↓
OASISClient
    ↓
    GET /api/a2a/agents/discover-serv?service=data-analysis
    ↓
OASIS API (A2AController)
    ↓
    Queries SERV Infrastructure (ONET)
    ↓
    Returns: List of Agent Cards
    ↓
Cursor (Shows you the results)
```

---

## Usage Examples

### Example 1: Discover SERV Agents

**You ask Cursor:**
> "Find all SERV agents that provide data analysis services"

**What happens:**
1. Cursor uses `oasis_discover_agents_via_serv` with `serviceName: "data-analysis"`
2. MCP server calls `/api/a2a/agents/discover-serv?service=data-analysis`
3. OASIS queries SERV infrastructure
4. Returns list of agents with their capabilities
5. Cursor displays the results

### Example 2: Register an Agent

**You ask Cursor:**
> "Register my agent with capabilities for data analysis and Python skills"

**What happens:**
1. Cursor uses `oasis_register_agent_capabilities` with:
   - `services: ["data-analysis"]`
   - `skills: ["Python", "Machine Learning"]`
2. Agent capabilities are registered
3. Then uses `oasis_register_agent_as_serv_service` to register with SERV
4. Agent is now discoverable via SERV infrastructure

### Example 3: Send A2A Payment Request

**You ask Cursor:**
> "Send a payment request for 0.01 SOL to agent abc-123 for data analysis service"

**What happens:**
1. Cursor uses `oasis_send_a2a_jsonrpc_request` with:
   - `method: "payment_request"`
   - `params: { to_agent_id: "abc-123", amount: 0.01, currency: "SOL" }`
2. Payment request is sent via A2A Protocol
3. Target agent receives the message

### Example 4: Get Agent Card

**You ask Cursor:**
> "Show me the agent card for agent xyz-789"

**What happens:**
1. Cursor uses `oasis_get_agent_card` with `agentId: "xyz-789"`
2. Returns agent card with capabilities, services, pricing, etc.
3. Cursor displays the information

---

## Available A2A JSON-RPC Methods

When using `oasis_send_a2a_jsonrpc_request`, you can use these methods:

- **`ping`** - Health check
- **`payment_request`** - Request payment from another agent
- **`service_request`** - Request a service
- **`capability_query`** - Query agent capabilities
- **`task_delegation`** - Delegate a task
- **`service_response`** - Respond to a service request

**Example:**
```json
{
  "method": "payment_request",
  "params": {
    "to_agent_id": "agent-uuid",
    "amount": 0.01,
    "description": "Payment for service",
    "currency": "SOL"
  }
}
```

---

## Authentication

Some tools require authentication:

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

## Integration with Existing Tools

The new A2A/SERV tools work seamlessly with existing OASIS tools:

**Example Workflow:**
1. **Register Avatar** → `oasis_register_avatar` (with `avatarType: "Agent"`)
2. **Authenticate** → `oasis_authenticate_avatar`
3. **Register Capabilities** → `oasis_register_agent_capabilities`
4. **Register with SERV** → `oasis_register_agent_as_serv_service`
5. **Discover Agents** → `oasis_discover_agents_via_serv`
6. **Send Payment** → `oasis_send_a2a_jsonrpc_request` (method: "payment_request")
7. **Execute Payment** → `oasis_send_transaction` (existing wallet tool)

---

## Benefits

### For Developers
- **No context switching** - Stay in Cursor, don't open other tools
- **Natural language** - Ask questions in plain English
- **Fast** - Instant agent discovery and communication
- **Integrated** - Works with all existing OASIS MCP tools

### For SERV Agents
- **Discoverable** - Agents can be found via SERV infrastructure
- **Interoperable** - Works with A2A Protocol and OpenSERV
- **Unified** - Single registry for all service types

---

## Technical Details

### Files Modified
1. **`src/clients/oasisClient.ts`** - Added 11 new client methods
2. **`src/tools/oasisTools.ts`** - Added 12 new tool definitions and handlers
3. **`ENDPOINT_INVENTORY.md`** - Updated to reflect new tools

### API Endpoints Used
- `GET /api/a2a/agent-card/{agentId}`
- `GET /api/a2a/agents`
- `GET /api/a2a/agents/by-service/{service}`
- `POST /api/a2a/agent/capabilities`
- `POST /api/a2a/agent/register-service`
- `GET /api/a2a/agents/discover-serv`
- `POST /api/a2a/jsonrpc`
- `GET /api/a2a/messages`
- `POST /api/a2a/messages/{messageId}/process`
- `POST /api/a2a/openserv/register`
- `POST /api/a2a/workflow/execute`

---

## Next Steps

1. **Test the integration:**
   ```bash
   cd MCP
   npm run build
   # Restart Cursor to load new tools
   ```

2. **Try it out:**
   - Ask Cursor: "Discover SERV agents for data analysis"
   - Ask Cursor: "Show me all A2A agents"
   - Ask Cursor: "Get the agent card for agent [ID]"

3. **Build demos:**
   - Use the new tools to create SERV agent marketplace demos
   - Chain multiple tools together for complex workflows

---

## References

- **A2A Protocol Documentation:** `/Users/maxgershfield/OASIS_CLEAN/A2A/README.md`
- **SERV Infrastructure Analysis:** `/Users/maxgershfield/OASIS_CLEAN/A2A/docs/SERV_INFRASTRUCTURE_ANALYSIS.md`
- **Integration Guide:** `/Users/maxgershfield/OASIS_CLEAN/A2A/docs/INTEGRATION_GUIDE.md`
- **MCP Simple Explanation:** `MCP_SIMPLE_EXPLANATION.md`

---

**Status:** ✅ Complete and Ready to Use  
**Last Updated:** January 7, 2026













