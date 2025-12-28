# Agent Task Brief: A2A + OASIS Integration

## Task Overview

**Objective:** Build an A2A-compatible agent that integrates with OASIS Agentic Avatar infrastructure.

**Deliverable:** A working agent that:
1. Registers as an OASIS Avatar
2. Generates an A2A Agent Card from OASIS data
3. Handles A2A JSON-RPC 2.0 requests
4. Processes payments via OASIS Wallet API
5. Tracks trust via OASIS Karma API

---

## Technical Requirements

### 1. OASIS Avatar Registration
- **Endpoint:** `POST /api/avatar/register`
- **Output:** Avatar ID, JWT token
- **Storage:** Save credentials securely

### 2. A2A Agent Card Generation
- **Input:** OASIS Avatar ID, Wallet address, Karma score
- **Output:** A2A-compliant Agent Card JSON
- **Format:** Follow A2A specification (see `specification/` folder)

### 3. A2A JSON-RPC 2.0 Handler
- **Methods Required:**
  - `getAgentCard` - Return agent's A2A card
  - `invokeTask` - Execute agent tasks
  - `queryCapabilities` - List agent capabilities
- **Protocol:** JSON-RPC 2.0 over HTTP(S)

### 4. OASIS Wallet Integration
- **Payment Trigger:** After task completion
- **Endpoint:** `POST /api/wallet/send_token`
- **Purpose:** Pay for services, data, compute

### 5. OASIS Karma Integration
- **Update Karma:** After successful task completion
- **Endpoint:** `POST /api/karma/add-karma-to-avatar/{avatarId}`
- **Purpose:** Build trust reputation

---

## Implementation Checklist

### Phase 1: Setup (30 min)
- [ ] Clone A2A repository
- [ ] Review A2A specification
- [ ] Set up development environment
- [ ] Get OASIS API credentials

### Phase 2: OASIS Integration (1 hour)
- [ ] Register agent as OASIS Avatar
- [ ] Generate wallet for agent
- [ ] Get initial karma score (should be 0)
- [ ] Test authentication flow

### Phase 3: A2A Agent Card (30 min)
- [ ] Create Agent Card JSON structure
- [ ] Populate with OASIS data (Avatar ID, Wallet, Karma)
- [ ] Validate against A2A specification
- [ ] Store Agent Card (as Holon or public endpoint)

### Phase 4: A2A Protocol Handler (1-2 hours)
- [ ] Implement JSON-RPC 2.0 endpoint
- [ ] Handle `getAgentCard` method
- [ ] Handle `invokeTask` method
- [ ] Handle `queryCapabilities` method
- [ ] Add error handling

### Phase 5: Payment Integration (30 min)
- [ ] Implement OASIS Wallet payment after task
- [ ] Test payment flow
- [ ] Verify transaction on blockchain

### Phase 6: Trust Integration (30 min)
- [ ] Update karma after successful tasks
- [ ] Query karma stats
- [ ] Implement trust-based filtering

### Phase 7: Testing (1 hour)
- [ ] Test agent discovery
- [ ] Test task invocation
- [ ] Test payment flow
- [ ] Test karma updates
- [ ] End-to-end integration test

---

## Code Structure

```
your-agent/
├── src/
│   ├── oasis_client.py      # OASIS API client
│   ├── a2a_handler.py        # A2A JSON-RPC 2.0 handler
│   ├── agent_card.py         # Agent Card generation
│   └── task_executor.py     # Task execution logic
├── config/
│   └── config.json           # OASIS credentials, agent config
├── tests/
│   └── test_integration.py   # Integration tests
└── README.md
```

---

## Example Agent Card

```json
{
  "agentId": "oasis-avatar-uuid",
  "name": "DataAnalysisAgent",
  "version": "1.0.0",
  "description": "Analyzes market data and provides insights",
  "capabilities": [
    {
      "name": "analyzeMarketData",
      "description": "Analyzes cryptocurrency market data",
      "inputSchema": {
        "type": "object",
        "properties": {
          "symbol": {"type": "string"},
          "timeframe": {"type": "string"}
        }
      },
      "outputSchema": {
        "type": "object",
        "properties": {
          "analysis": {"type": "string"},
          "confidence": {"type": "number"}
        }
      }
    }
  ],
  "endpoint": "https://your-agent.example.com/a2a",
  "metadata": {
    "oasis": {
      "avatarId": "avatar-uuid",
      "walletAddress": "0x...",
      "karma": 0,
      "trustStatus": "pending",
      "chains": ["Ethereum", "Solana", "Arbitrum"]
    }
  }
}
```

---

## Testing Commands

### Test OASIS Registration
```bash
curl -X POST "https://api.oasisweb4.one/api/avatar/register" \
  -H "Content-Type: application/json" \
  -d '{"username":"test_agent","email":"test@example.com","password":"test123","avatarType":"User","acceptTerms":true}'
```

### Test A2A Agent Card
```bash
curl -X POST "https://your-agent.example.com/a2a" \
  -H "Content-Type: application/json" \
  -d '{"jsonrpc":"2.0","method":"getAgentCard","id":1}'
```

### Test Task Invocation
```bash
curl -X POST "https://your-agent.example.com/a2a" \
  -H "Content-Type: application/json" \
  -d '{"jsonrpc":"2.0","method":"invokeTask","params":{"taskType":"analyzeMarketData","symbol":"BTC/USD"},"id":2}'
```

---

## Success Metrics

- ✅ Agent successfully registers with OASIS
- ✅ Agent Card is A2A-compliant
- ✅ Agent handles A2A JSON-RPC 2.0 requests
- ✅ Payments process via OASIS Wallet
- ✅ Karma updates after tasks
- ✅ Agent is discoverable

---

## Resources

- **Quick Start:** `QUICK_START_INTEGRATION.md`
- **Detailed Guide:** `../docs/oasis-integration/DETAILED_INTEGRATION_GUIDE.md`
- **A2A Spec:** `../specification/`
- **OASIS API:** `../../API_REFERENCE.md`

---

**Estimated Time:** 4-6 hours  
**Difficulty:** Medium  
**Language:** Any (Python, Go, JS, Java, .NET recommended)




