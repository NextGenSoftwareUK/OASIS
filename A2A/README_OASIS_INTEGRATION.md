# A2A Protocol + OASIS Agentic Avatar Integration

This folder contains the [A2A (Agent2Agent) Protocol](https://github.com/a2aproject/A2A) repository and OASIS-specific integration documentation.

## What is A2A?

A2A is an open protocol enabling communication and interoperability between opaque agentic applications. It provides:
- **Standardized Communication:** JSON-RPC 2.0 over HTTP(S)
- **Agent Discovery:** Via "Agent Cards"
- **Opaque Collaboration:** Agents work together without exposing internal state
- **Multi-Language SDKs:** Python, Go, JavaScript, Java, .NET

## Why A2A + OASIS?

**A2A** provides the **communication protocol** layer.  
**OASIS** provides the **infrastructure** layer (identity, wallet, trust, multi-chain).

Together, they create a complete agent ecosystem that enables:
- ✅ Agent-to-agent communication (A2A)
- ✅ Agent identity management (OASIS Avatar API)
- ✅ Agent payments (OASIS Wallet API)
- ✅ Agent trust/reputation (OASIS Karma API)
- ✅ Agent discovery (A2A Agent Cards + OASIS HyperDrive)

## Quick Start

1. **Read the Quick Start Guide:**
   - [`integration-briefs/QUICK_START_INTEGRATION.md`](integration-briefs/QUICK_START_INTEGRATION.md)

2. **Review the Task Brief:**
   - [`integration-briefs/AGENT_TASK_BRIEF.md`](integration-briefs/AGENT_TASK_BRIEF.md)

3. **Study the Detailed Guide:**
   - [`docs/oasis-integration/DETAILED_INTEGRATION_GUIDE.md`](docs/oasis-integration/DETAILED_INTEGRATION_GUIDE.md)

## Folder Structure

```
A2A/
├── README_OASIS_INTEGRATION.md          # This file
├── integration-briefs/                   # Quick reference guides
│   ├── QUICK_START_INTEGRATION.md        # 30-min quick start
│   └── AGENT_TASK_BRIEF.md               # Task checklist
├── docs/
│   └── oasis-integration/
│       └── DETAILED_INTEGRATION_GUIDE.md # Complete guide
├── specification/                        # A2A protocol spec (from repo)
└── [A2A repository files]                # Original A2A codebase
```

## Integration Architecture

```
┌─────────────────────────────────────────────────────────┐
│              Your A2A-Compatible Agent                  │
├─────────────────────────────────────────────────────────┤
│                                                         │
│  A2A JSON-RPC 2.0 Handler  ←→  OASIS REST API Client   │
│         │                              │                │
│         │                              │                │
│         └──────────┬───────────────────┘                │
│                    │                                    │
│         Agent Card Generator                            │
│         (OASIS Data → A2A Format)                      │
│                                                         │
└─────────────────────────────────────────────────────────┘
         │                    │
         ▼                    ▼
┌──────────────────┐  ┌──────────────────┐
│  A2A Protocol    │  │  OASIS APIs      │
│  (Communication) │  │  (Infrastructure)│
└──────────────────┘  └──────────────────┘
```

## Key Integration Points

### 1. Agent Registration
- **OASIS:** Register agent as Avatar → Get Avatar ID + Token
- **A2A:** Generate Agent Card from Avatar data

### 2. Agent Discovery
- **A2A:** Agent Cards describe capabilities
- **OASIS:** HyperDrive routes discovery queries
- **OASIS:** Karma scores filter trusted agents

### 3. Task Execution
- **A2A:** JSON-RPC 2.0 `invokeTask` method
- **OASIS:** Wallet API processes payments
- **OASIS:** Karma API updates trust scores

### 4. Agent Communication
- **A2A:** Standardized JSON-RPC 2.0 messaging
- **OASIS:** Multi-chain wallet addresses
- **OASIS:** Trust-based access control

## Example: Complete Integration Flow

```python
# 1. Register with OASIS
oasis_client.register_avatar("my_agent", "agent@example.com", "password")
oasis_client.generate_wallet()

# 2. Generate A2A Agent Card
agent_card = generate_agent_card(oasis_client)

# 3. Handle A2A requests
@app.route('/a2a', methods=['POST'])
def handle_a2a():
    method = request.json.get('method')
    if method == 'invokeTask':
        # Execute task
        result = execute_task(request.json['params'])
        
        # Pay via OASIS Wallet
        oasis_client.send_payment(to_avatar_id, amount)
        
        # Update karma
        oasis_client.add_karma("Helpful", "Task completion")
        
        return jsonify({"jsonrpc": "2.0", "result": result, "id": request.json['id']})
```

## Resources

### OASIS Documentation
- **API Reference:** `../API_REFERENCE.md`
- **Agentic Avatar System:** `../AGENTIC_AVATAR_SYSTEM.md`
- **A2A Protocol Analysis:** `../A2A_PROTOCOL_ANALYSIS.md`

### A2A Documentation
- **A2A Protocol Site:** https://a2a-protocol.org/
- **A2A Specification:** `specification/` (in this repo)
- **A2A GitHub:** https://github.com/a2aproject/A2A

### SDKs
- **Python:** `pip install a2a-sdk`
- **Go:** `go get github.com/a2aproject/a2a-go`
- **JavaScript:** `npm install @a2a-js/sdk`
- **Java:** Maven package
- **.NET:** `dotnet add package A2A`

## Next Steps

1. **For Quick Integration:** Start with `integration-briefs/QUICK_START_INTEGRATION.md`
2. **For Detailed Implementation:** Read `docs/oasis-integration/DETAILED_INTEGRATION_GUIDE.md`
3. **For Task Assignment:** Use `integration-briefs/AGENT_TASK_BRIEF.md`

## Support

- **A2A Protocol Issues:** https://github.com/a2aproject/A2A/issues
- **OASIS API Support:** See `../API_REFERENCE.md` for contact info
- **Integration Questions:** Review the detailed integration guide

---

**Last Updated:** January 2026  
**Status:** Ready for Integration




