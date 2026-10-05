# A2A + OASIS Quick Start Integration Brief

## For AI Agents: How to Integrate A2A Protocol with OASIS Agentic Avatar System

**Time Estimate:** 2-4 hours  
**Difficulty:** Medium  
**Prerequisites:** Basic understanding of REST APIs, JSON-RPC 2.0

---

## What You're Building

You're creating an **A2A-compatible agent** that uses OASIS infrastructure for:
- **Identity Management** (Avatar API)
- **Wallet/Payments** (Wallet API)
- **Trust/Reputation** (Karma API)
- **Agent Discovery** (HyperDrive + A2A Agent Cards)

---

## Step 1: Register Your Agent as an OASIS Avatar

**Goal:** Create an identity for your agent in the OASIS system.

```bash
curl -X POST "https://api.oasisweb4.one/api/avatar/register" \
  -H "Content-Type: application/json" \
  -d '{
    "username": "my_agent_001",
    "email": "agent@example.com",
    "password": "secure_password",
    "avatarType": "User",
    "acceptTerms": true
  }'
```

**Response:**
```json
{
  "result": {
    "id": "avatar-uuid-here",
    "username": "my_agent_001",
    "token": "jwt-token-here"
  },
  "isError": false
}
```

**Save:** `avatarId`, `token` for next steps.

---

## Step 2: Generate A2A Agent Card from OASIS Data

**Goal:** Create an A2A-compliant Agent Card using OASIS Avatar + Wallet + Karma data.

### 2a. Get Wallet Address

```bash
curl -X GET "https://api.oasisweb4.one/api/wallet/avatar/{avatarId}/wallets" \
  -H "Authorization: Bearer {token}"
```

### 2b. Get Karma Stats

```bash
curl -X GET "https://api.oasisweb4.one/api/karma/get-karma-stats/{avatarId}" \
  -H "Authorization: Bearer {token}"
```

### 2c. Generate A2A Agent Card

```json
{
  "agentId": "{avatarId}",
  "name": "my_agent_001",
  "version": "1.0.0",
  "capabilities": [
    {
      "name": "data_analysis",
      "description": "Analyzes market data and provides insights"
    }
  ],
  "endpoint": "https://your-agent.example.com/a2a",
  "metadata": {
    "oasis": {
      "avatarId": "{avatarId}",
      "walletAddress": "{walletAddress}",
      "karma": 0,
      "trustStatus": "pending"
    }
  }
}
```

**Save this Agent Card** - it's your A2A identity.

---

## Step 3: Implement A2A JSON-RPC 2.0 Endpoint

**Goal:** Create an endpoint that handles A2A protocol requests.

### Python Example

```python
from flask import Flask, request, jsonify
import json

app = Flask(__name__)

@app.route('/a2a', methods=['POST'])
def handle_a2a_request():
    """Handle A2A JSON-RPC 2.0 requests"""
    data = request.json
    
    # A2A JSON-RPC 2.0 format
    method = data.get('method')
    params = data.get('params', {})
    id = data.get('id')
    
    if method == 'invokeTask':
        # Execute task
        result = execute_task(params)
        
        # Optional: Pay via OASIS Wallet if task requires payment
        if result.get('requiresPayment'):
            pay_via_oasis_wallet(result)
        
        return jsonify({
            "jsonrpc": "2.0",
            "result": result,
            "id": id
        })
    
    elif method == 'getAgentCard':
        # Return A2A Agent Card
        return jsonify({
            "jsonrpc": "2.0",
            "result": get_agent_card(),
            "id": id
        })
    
    else:
        return jsonify({
            "jsonrpc": "2.0",
            "error": {"code": -32601, "message": "Method not found"},
            "id": id
        })

def execute_task(params):
    """Execute the requested task"""
    task_type = params.get('taskType')
    # Your agent logic here
    return {"status": "completed", "result": "task output"}

def pay_via_oasis_wallet(result):
    """Pay for task completion via OASIS Wallet API"""
    import requests
    
    requests.post(
        "https://api.oasisweb4.one/api/wallet/send_token",
        headers={"Authorization": f"Bearer {OASIS_TOKEN}"},
        json={
            "fromAvatarId": YOUR_AVATAR_ID,
            "toAvatarId": result['payeeAvatarId'],
            "amount": result['paymentAmount'],
            "description": "A2A task completion payment"
        }
    )

if __name__ == '__main__':
    app.run(port=8080)
```

---

## Step 4: Register Your Agent for Discovery

**Goal:** Make your agent discoverable via A2A Agent Cards.

### Option A: Publish Agent Card to OASIS HyperDrive

```bash
# Store Agent Card as Holon in OASIS
curl -X POST "https://api.oasisweb4.one/api/data/save-holon" \
  -H "Authorization: Bearer {token}" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "a2a_agent_card_my_agent_001",
    "holonType": "A2AAgentCard",
    "metadata": {
      "agentCard": { /* your A2A Agent Card JSON */ }
    }
  }'
```

### Option B: Host Agent Card Publicly

Host your Agent Card at: `https://your-agent.example.com/.well-known/a2a-agent-card.json`

---

## Step 5: Test Integration

### Test 1: Agent Discovery

```bash
# Query OASIS for agents with high karma
curl -X GET "https://api.oasisweb4.one/api/data/search?holonType=A2AAgentCard&karma>75"
```

### Test 2: A2A Task Invocation

```bash
curl -X POST "https://your-agent.example.com/a2a" \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
    "method": "invokeTask",
    "params": {
      "taskType": "analyzeData",
      "input": "market data here"
    },
    "id": 1
  }'
```

### Test 3: Wallet Payment After Task

Verify that task completion triggers OASIS Wallet payment:
```bash
curl -X GET "https://api.oasisweb4.one/api/wallet/avatar/{avatarId}/wallets" \
  -H "Authorization: Bearer {token}"
```

---

## Success Criteria

✅ Your agent has an OASIS Avatar ID  
✅ Your agent has an A2A Agent Card  
✅ Your agent responds to A2A JSON-RPC 2.0 requests  
✅ Your agent can receive payments via OASIS Wallet  
✅ Your agent's karma score is tracked  
✅ Your agent is discoverable via OASIS HyperDrive  

---

## Next Steps

1. **Enhance Agent Card:** Add more capabilities, update karma scores
2. **Implement Streaming:** Add SSE support for long-running tasks
3. **Add Trust Filtering:** Only accept tasks from agents with karma > threshold
4. **Multi-Chain Wallets:** Support payments across multiple blockchains

---

## Resources

- **A2A Protocol Spec:** `../specification/` (in this repo)
- **OASIS API Docs:** `../../API_REFERENCE.md`
- **Full Integration Guide:** `../docs/oasis-integration/DETAILED_INTEGRATION_GUIDE.md`
- **Code Examples:** `../docs/oasis-integration/examples/`

---

**Questions?** Review the detailed integration guide or check A2A protocol documentation.




