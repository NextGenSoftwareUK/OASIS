# Detailed A2A + OASIS Integration Guide

## Table of Contents

1. [Architecture Overview](#architecture-overview)
2. [OASIS API Integration](#oasis-api-integration)
3. [A2A Protocol Implementation](#a2a-protocol-implementation)
4. [Agent Card Generation](#agent-card-generation)
5. [Payment Flow](#payment-flow)
6. [Trust & Karma Integration](#trust--karma-integration)
7. [Advanced Features](#advanced-features)
8. [Troubleshooting](#troubleshooting)

---

## Architecture Overview

### System Components

```
┌─────────────────────────────────────────────────────────┐
│                    Your Agent Application               │
├─────────────────────────────────────────────────────────┤
│                                                         │
│  ┌──────────────────┐    ┌──────────────────┐         │
│  │  A2A Handler     │    │  OASIS Client    │         │
│  │  (JSON-RPC 2.0)  │◄───┤  (REST API)      │         │
│  └──────────────────┘    └──────────────────┘         │
│         │                        │                      │
│         │                        │                      │
│         ▼                        ▼                      │
│  ┌──────────────────────────────────────────┐         │
│  │      Agent Card Generator                  │         │
│  │  (OASIS Data → A2A Format)                │         │
│  └──────────────────────────────────────────┘         │
│                                                         │
└─────────────────────────────────────────────────────────┘
         │                        │
         │                        │
         ▼                        ▼
┌──────────────────┐    ┌──────────────────┐
│  A2A Protocol    │    │  OASIS APIs      │
│  (Communication) │    │  (Infrastructure)│
└──────────────────┘    └──────────────────┘
```

### Data Flow

1. **Registration:** Agent → OASIS Avatar API → Avatar ID + Token
2. **Wallet Setup:** Agent → OASIS Wallet API → Wallet Address
3. **Agent Card:** OASIS Data → Agent Card Generator → A2A Agent Card
4. **Task Request:** Client → A2A Handler → Task Execution
5. **Payment:** Task Completion → OASIS Wallet API → Blockchain Transaction
6. **Karma Update:** Task Success → OASIS Karma API → Trust Score

---

## OASIS API Integration

### 1. Avatar Registration

**Endpoint:** `POST /api/avatar/register`

**Request:**
```json
{
  "username": "my_agent",
  "email": "agent@example.com",
  "password": "secure_password",
  "avatarType": "User",
  "acceptTerms": true
}
```

**Response:**
```json
{
  "result": {
    "id": "550e8400-e29b-41d4-a716-446655440000",
    "username": "my_agent",
    "email": "agent@example.com",
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "refreshToken": "refresh_token_here"
  },
  "isError": false
}
```

**Implementation:**
```python
import requests

class OASISClient:
    def __init__(self, base_url="https://api.oasisweb4.one"):
        self.base_url = base_url
        self.token = None
        self.avatar_id = None
    
    def register_avatar(self, username, email, password):
        response = requests.post(
            f"{self.base_url}/api/avatar/register",
            json={
                "username": username,
                "email": email,
                "password": password,
                "avatarType": "User",
                "acceptTerms": True
            }
        )
        data = response.json()
        if not data.get("isError"):
            self.avatar_id = data["result"]["id"]
            self.token = data["result"]["token"]
        return data
```

### 2. Authentication

**Endpoint:** `POST /api/avatar/authenticate`

**Request:**
```json
{
  "username": "my_agent",
  "password": "secure_password"
}
```

**Implementation:**
```python
def authenticate(self, username, password):
    response = requests.post(
        f"{self.base_url}/api/avatar/authenticate",
        json={"username": username, "password": password}
    )
    data = response.json()
    if not data.get("isError"):
        self.token = data["result"]["token"]
        self.avatar_id = data["result"]["id"]
    return data
```

### 3. Wallet Generation

**Endpoint:** `POST /api/wallet/avatar/{avatarId}/generate`

**Request:**
```json
{
  "providerType": "EthereumOASIS",
  "setAsDefault": true
}
```

**Implementation:**
```python
def generate_wallet(self, provider_type="EthereumOASIS"):
    response = requests.post(
        f"{self.base_url}/api/wallet/avatar/{self.avatar_id}/generate",
        headers={"Authorization": f"Bearer {self.token}"},
        json={"providerType": provider_type, "setAsDefault": True}
    )
    return response.json()
```

### 4. Get Wallet Address

**Endpoint:** `GET /api/wallet/avatar/{avatarId}/wallets`

**Implementation:**
```python
def get_wallets(self):
    response = requests.get(
        f"{self.base_url}/api/wallet/avatar/{self.avatar_id}/wallets",
        headers={"Authorization": f"Bearer {self.token}"}
    )
    return response.json()
```

### 5. Send Payment

**Endpoint:** `POST /api/wallet/send_token`

**Request:**
```json
{
  "fromAvatarId": "avatar-uuid",
  "toAvatarId": "recipient-uuid",
  "amount": "0.001",
  "tokenAddress": "0x...",
  "chainId": 1,
  "description": "Payment for task completion"
}
```

**Implementation:**
```python
def send_payment(self, to_avatar_id, amount, description=""):
    response = requests.post(
        f"{self.base_url}/api/wallet/send_token",
        headers={"Authorization": f"Bearer {self.token}"},
        json={
            "fromAvatarId": self.avatar_id,
            "toAvatarId": to_avatar_id,
            "amount": str(amount),
            "description": description
        }
    )
    return response.json()
```

### 6. Get Karma Stats

**Endpoint:** `GET /api/karma/get-karma-stats/{avatarId}`

**Implementation:**
```python
def get_karma_stats(self):
    response = requests.get(
        f"{self.base_url}/api/karma/get-karma-stats/{self.avatar_id}",
        headers={"Authorization": f"Bearer {self.token}"}
    )
    return response.json()
```

### 7. Add Karma

**Endpoint:** `POST /api/karma/add-karma-to-avatar/{avatarId}`

**Request:**
```json
{
  "karmaType": "Helpful",
  "karmaSourceType": "App",
  "karamSourceTitle": "Task Completion",
  "karmaSourceDesc": "Successfully completed A2A task"
}
```

**Implementation:**
```python
def add_karma(self, karma_type="Helpful", source_title="Task Completion"):
    response = requests.post(
        f"{self.base_url}/api/karma/add-karma-to-avatar/{self.avatar_id}",
        headers={"Authorization": f"Bearer {self.token}"},
        json={
            "karmaType": karma_type,
            "karmaSourceType": "App",
            "karamSourceTitle": source_title,
            "karmaSourceDesc": "A2A task completion"
        }
    )
    return response.json()
```

---

## A2A Protocol Implementation

### JSON-RPC 2.0 Handler

**Full Implementation:**

```python
from flask import Flask, request, jsonify
import json

app = Flask(__name__)
oasis_client = OASISClient()

@app.route('/a2a', methods=['POST'])
def handle_a2a():
    """Handle A2A JSON-RPC 2.0 requests"""
    try:
        data = request.json
        
        # Validate JSON-RPC 2.0 format
        if data.get('jsonrpc') != '2.0':
            return jsonify({
                "jsonrpc": "2.0",
                "error": {"code": -32600, "message": "Invalid Request"},
                "id": data.get('id')
            }), 400
        
        method = data.get('method')
        params = data.get('params', {})
        request_id = data.get('id')
        
        # Route to method handler
        if method == 'getAgentCard':
            result = get_agent_card()
        elif method == 'invokeTask':
            result = invoke_task(params)
        elif method == 'queryCapabilities':
            result = query_capabilities()
        else:
            return jsonify({
                "jsonrpc": "2.0",
                "error": {"code": -32601, "message": "Method not found"},
                "id": request_id
            }), 404
        
        return jsonify({
            "jsonrpc": "2.0",
            "result": result,
            "id": request_id
        })
    
    except Exception as e:
        return jsonify({
            "jsonrpc": "2.0",
            "error": {"code": -32603, "message": str(e)},
            "id": data.get('id')
        }), 500

def get_agent_card():
    """Return A2A Agent Card"""
    wallets = oasis_client.get_wallets()
    karma = oasis_client.get_karma_stats()
    
    # Extract wallet address
    wallet_address = None
    if wallets.get('result'):
        for provider, wallet_list in wallets['result'].items():
            if wallet_list:
                wallet_address = wallet_list[0].get('address')
                break
    
    # Extract karma score
    karma_score = 0
    if karma.get('result'):
        karma_score = karma['result'].get('total', 0)
    
    return {
        "agentId": oasis_client.avatar_id,
        "name": "MyAgent",
        "version": "1.0.0",
        "description": "A2A agent with OASIS integration",
        "capabilities": [
            {
                "name": "analyzeData",
                "description": "Analyzes data and provides insights"
            }
        ],
        "endpoint": "https://your-agent.example.com/a2a",
        "metadata": {
            "oasis": {
                "avatarId": oasis_client.avatar_id,
                "walletAddress": wallet_address,
                "karma": karma_score,
                "trustStatus": "verified" if karma_score > 75 else "pending"
            }
        }
    }

def invoke_task(params):
    """Execute A2A task"""
    task_type = params.get('taskType')
    task_input = params.get('input', {})
    
    # Execute your agent's task logic here
    task_result = execute_agent_task(task_type, task_input)
    
    # If task requires payment, process via OASIS
    if task_result.get('requiresPayment'):
        payment_result = oasis_client.send_payment(
            to_avatar_id=task_result['payeeAvatarId'],
            amount=task_result['paymentAmount'],
            description=f"Payment for {task_type} task"
        )
        task_result['payment'] = payment_result
    
    # Update karma after successful task
    if task_result.get('status') == 'completed':
        oasis_client.add_karma(
            karma_type="Helpful",
            source_title=f"Completed {task_type} task"
        )
    
    return task_result

def query_capabilities():
    """Return agent capabilities"""
    return {
        "capabilities": [
            {
                "name": "analyzeData",
                "description": "Analyzes data and provides insights",
                "inputSchema": {
                    "type": "object",
                    "properties": {
                        "data": {"type": "string"}
                    }
                }
            }
        ]
    }

def execute_agent_task(task_type, task_input):
    """Your agent's task execution logic"""
    # Implement your agent's specific task logic
    return {
        "status": "completed",
        "result": "Task completed successfully",
        "requiresPayment": False
    }

if __name__ == '__main__':
    # Initialize OASIS connection
    oasis_client.register_avatar("my_agent", "agent@example.com", "password")
    oasis_client.generate_wallet()
    
    # Start A2A server
    app.run(host='0.0.0.0', port=8080)
```

---

## Agent Card Generation

### Complete Agent Card Structure

```python
def generate_agent_card(oasis_client):
    """Generate A2A Agent Card from OASIS data"""
    
    # Fetch OASIS data
    wallets = oasis_client.get_wallets()
    karma = oasis_client.get_karma_stats()
    avatar = oasis_client.get_avatar()
    
    # Extract wallet addresses (multi-chain)
    wallet_addresses = {}
    if wallets.get('result'):
        for provider, wallet_list in wallets['result'].items():
            if wallet_list:
                wallet_addresses[provider] = wallet_list[0].get('address')
    
    # Extract karma data
    karma_total = 0
    karma_history = []
    if karma.get('result'):
        karma_total = karma['result'].get('total', 0)
        karma_history = karma['result'].get('history', [])
    
    # Build A2A Agent Card
    agent_card = {
        "agentId": oasis_client.avatar_id,
        "name": avatar.get('username', 'Unknown'),
        "version": "1.0.0",
        "description": "A2A-compatible agent with OASIS infrastructure",
        "capabilities": [
            {
                "name": "analyzeMarketData",
                "description": "Analyzes cryptocurrency market data",
                "inputSchema": {
                    "type": "object",
                    "properties": {
                        "symbol": {"type": "string"},
                        "timeframe": {"type": "string"}
                    },
                    "required": ["symbol"]
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
        "endpoint": f"https://your-agent.example.com/a2a",
        "metadata": {
            "oasis": {
                "avatarId": oasis_client.avatar_id,
                "walletAddresses": wallet_addresses,
                "karma": {
                    "total": karma_total,
                    "history": karma_history[-10:]  # Last 10 entries
                },
                "trustStatus": "verified" if karma_total > 75 else "pending",
                "chains": list(wallet_addresses.keys())
            }
        }
    }
    
    return agent_card
```

---

## Payment Flow

### Complete Payment Integration

```python
def handle_task_with_payment(task_params, payee_avatar_id, payment_amount):
    """Execute task and process payment"""
    
    # 1. Execute task
    task_result = execute_agent_task(task_params)
    
    # 2. If task successful, process payment
    if task_result.get('status') == 'completed':
        payment_result = oasis_client.send_payment(
            to_avatar_id=payee_avatar_id,
            amount=payment_amount,
            description=f"Payment for {task_params.get('taskType')} task"
        )
        
        # 3. Update karma for both parties
        oasis_client.add_karma(
            karma_type="Helpful",
            source_title="Task completion payment"
        )
        
        # 4. Return combined result
        return {
            "task": task_result,
            "payment": payment_result,
            "karmaUpdated": True
        }
    
    return task_result
```

---

## Trust & Karma Integration

### Trust-Based Task Filtering

```python
def should_accept_task(requester_avatar_id, min_karma=50):
    """Check if requester meets karma threshold"""
    requester_karma = oasis_client.get_karma_stats(requester_avatar_id)
    
    if requester_karma.get('result'):
        total_karma = requester_karma['result'].get('total', 0)
        return total_karma >= min_karma
    
    return False

@app.route('/a2a', methods=['POST'])
def handle_a2a_with_trust():
    """A2A handler with trust filtering"""
    data = request.json
    
    if data.get('method') == 'invokeTask':
        # Check requester's karma
        requester_id = data.get('params', {}).get('requesterAvatarId')
        if requester_id and not should_accept_task(requester_id):
            return jsonify({
                "jsonrpc": "2.0",
                "error": {
                    "code": -32000,
                    "message": "Insufficient karma. Minimum: 50"
                },
                "id": data.get('id')
            }), 403
    
    # Continue with normal handling
    return handle_a2a()
```

---

## Advanced Features

### 1. Streaming Support (SSE)

```python
from flask import Response, stream_with_context

@app.route('/a2a/stream/<task_id>')
def stream_task(task_id):
    """Stream task progress via Server-Sent Events"""
    def generate():
        yield f"data: {json.dumps({'status': 'started'})}\n\n"
        
        # Execute task and stream updates
        for update in execute_task_streaming(task_id):
            yield f"data: {json.dumps(update)}\n\n"
        
        yield f"data: {json.dumps({'status': 'completed'})}\n\n"
    
    return Response(
        stream_with_context(generate()),
        mimetype='text/event-stream'
    )
```

### 2. Multi-Chain Wallet Support

```python
def get_wallet_for_chain(chain_name):
    """Get wallet address for specific chain"""
    wallets = oasis_client.get_wallets()
    
    provider_map = {
        "ethereum": "EthereumOASIS",
        "solana": "SolanaOASIS",
        "arbitrum": "ArbitrumOASIS"
    }
    
    provider = provider_map.get(chain_name.lower())
    if provider and wallets.get('result', {}).get(provider):
        return wallets['result'][provider][0].get('address')
    
    return None
```

### 3. Agent Discovery via HyperDrive

```python
def discover_agents(capability=None, min_karma=None):
    """Discover agents via OASIS HyperDrive"""
    query = {
        "holonType": "A2AAgentCard"
    }
    
    if capability:
        query["capabilities"] = capability
    
    if min_karma:
        query["karma"] = f">{min_karma}"
    
    response = requests.post(
        f"{oasis_client.base_url}/api/data/search",
        headers={"Authorization": f"Bearer {oasis_client.token}"},
        json=query
    )
    
    return response.json()
```

---

## Troubleshooting

### Common Issues

**1. Authentication Fails**
- Check token expiration
- Re-authenticate if needed
- Verify credentials

**2. Wallet Not Found**
- Ensure wallet is generated
- Check provider type
- Verify avatar ID

**3. A2A Request Format Error**
- Validate JSON-RPC 2.0 format
- Check method name spelling
- Verify parameters schema

**4. Payment Fails**
- Check wallet balance
- Verify recipient avatar ID
- Check chain compatibility

**5. Karma Not Updating**
- Verify karma type enum values
- Check source type
- Ensure authentication

---

## Resources

- **A2A Specification:** `../specification/`
- **OASIS API Reference:** `../../../API_REFERENCE.md`
- **Quick Start:** `../../integration-briefs/QUICK_START_INTEGRATION.md`
- **Task Brief:** `../../integration-briefs/AGENT_TASK_BRIEF.md`

---

**Last Updated:** January 2026




