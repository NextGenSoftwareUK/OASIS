# A2A Quick Start Demo

A simplified, implementable demo to get started with A2A + OASIS integration.

---

## Minimal Viable Demo: Two Agents

### Concept
Two agents that communicate via A2A:
1. **Calculator Agent** - Performs mathematical calculations
2. **Payment Agent** - Handles payments via OASIS Wallet

### Why This Works
- ✅ Simple to understand
- ✅ Demonstrates core A2A concepts
- ✅ Shows OASIS integration
- ✅ Can be built in 2-3 hours
- ✅ Easy to extend later

---

## Quick Implementation

### Step 1: Setup Environment

```bash
# Create project directory
mkdir a2a-demo && cd a2a-demo

# Create virtual environment
python -m venv venv
source venv/bin/activate  # On Windows: venv\Scripts\activate

# Install dependencies
pip install flask requests a2a-sdk
```

### Step 2: Calculator Agent

**File: `calculator_agent.py`**

```python
from flask import Flask, request, jsonify
import uuid
import json

app = Flask(__name__)

# Agent Card
AGENT_CARD = {
    "protocol_version": "1.0",
    "name": "Calculator Agent",
    "description": "Performs mathematical calculations",
    "version": "1.0.0",
    "supported_interfaces": [{
        "url": "http://localhost:5001/a2a",
        "protocol_binding": "HTTP+JSON"
    }],
    "capabilities": {
        "streaming": False,
        "push_notifications": False
    },
    "default_input_modes": ["text/plain"],
    "default_output_modes": ["text/plain", "application/json"],
    "skills": [{
        "id": "calculate",
        "name": "Mathematical Calculation",
        "description": "Performs arithmetic operations",
        "tags": ["math", "calculation", "arithmetic"],
        "examples": [
            "Calculate 5 + 3",
            "What is 10 * 7?",
            "Divide 100 by 4"
        ]
    }]
}

@app.route('/a2a', methods=['POST'])
def handle_a2a():
    """Handle A2A JSON-RPC 2.0 requests"""
    data = request.json
    
    if data.get('jsonrpc') != '2.0':
        return jsonify({
            "jsonrpc": "2.0",
            "error": {"code": -32600, "message": "Invalid Request"},
            "id": data.get('id')
        }), 400
    
    method = data.get('method')
    params = data.get('params', {})
    request_id = data.get('id')
    
    if method == 'message.send':
        return handle_message(params, request_id)
    else:
        return jsonify({
            "jsonrpc": "2.0",
            "error": {"code": -32601, "message": "Method not found"},
            "id": request_id
        }), 404

def handle_message(params, request_id):
    """Handle incoming message"""
    message = params.get('message', {})
    text = ""
    
    # Extract text from message parts
    for part in message.get('parts', []):
        if 'text' in part:
            text += part['text']
    
    # Simple calculation parsing
    result = calculate(text)
    
    # Create response task
    task = {
        "id": str(uuid.uuid4()),
        "context_id": message.get('context_id', str(uuid.uuid4())),
        "status": {
            "state": "TASK_STATE_COMPLETED",
            "message": {
                "message_id": str(uuid.uuid4()),
                "role": "ROLE_AGENT",
                "parts": [{"text": str(result)}]
            }
        },
        "artifacts": [{
            "artifact_id": str(uuid.uuid4()),
            "name": "Calculation Result",
            "description": "Result of mathematical calculation",
            "parts": [{
                "data": {
                    "input": text,
                    "result": result
                }
            }]
        }]
    }
    
    return jsonify({
        "jsonrpc": "2.0",
        "result": {"task": task},
        "id": request_id
    })

def calculate(text):
    """Simple calculator - parse and evaluate"""
    try:
        # Extract numbers and operators
        text = text.lower()
        text = text.replace("calculate", "").replace("what is", "").strip()
        
        # Simple parsing
        if "+" in text:
            parts = text.split("+")
            return float(parts[0].strip()) + float(parts[1].strip())
        elif "-" in text:
            parts = text.split("-")
            return float(parts[0].strip()) - float(parts[1].strip())
        elif "*" in text or "x" in text:
            operator = "*" if "*" in text else "x"
            parts = text.split(operator)
            return float(parts[0].strip()) * float(parts[1].strip())
        elif "/" in text or "divide" in text:
            if "/" in text:
                parts = text.split("/")
            else:
                parts = text.replace("divide", "").split("by")
            return float(parts[0].strip()) / float(parts[1].strip())
        else:
            return f"Could not parse: {text}"
    except Exception as e:
        return f"Error: {str(e)}"

@app.route('/agent-card', methods=['GET'])
def get_agent_card():
    """Return Agent Card"""
    return jsonify(AGENT_CARD)

if __name__ == '__main__':
    print("Calculator Agent starting on http://localhost:5001")
    print("Agent Card: http://localhost:5001/agent-card")
    app.run(port=5001, debug=True)
```

### Step 3: Simple Client

**File: `demo_client.py`**

```python
import requests
import json

def send_message(agent_url, message_text):
    """Send A2A message to agent"""
    payload = {
        "jsonrpc": "2.0",
        "method": "message.send",
        "params": {
            "message": {
                "message_id": "msg-001",
                "role": "ROLE_USER",
                "parts": [{"text": message_text}]
            }
        },
        "id": 1
    }
    
    response = requests.post(
        f"{agent_url}/a2a",
        json=payload,
        headers={"Content-Type": "application/json"}
    )
    
    return response.json()

def get_agent_card(agent_url):
    """Get Agent Card"""
    response = requests.get(f"{agent_url}/agent-card")
    return response.json()

# Demo
if __name__ == '__main__':
    calculator_url = "http://localhost:5001"
    
    print("=== A2A Demo ===\n")
    
    # Get Agent Card
    print("1. Getting Agent Card...")
    card = get_agent_card(calculator_url)
    print(f"   Agent: {card['name']}")
    print(f"   Description: {card['description']}")
    print(f"   Skills: {', '.join([s['name'] for s in card['skills']])}\n")
    
    # Send calculation request
    print("2. Sending calculation request...")
    result = send_message(calculator_url, "Calculate 15 + 27")
    print(f"   Request: Calculate 15 + 27")
    
    if 'result' in result and 'task' in result['result']:
        task = result['result']['task']
        print(f"   Task ID: {task['id']}")
        print(f"   Status: {task['status']['state']}")
        
        # Get result from artifacts
        if task.get('artifacts'):
            artifact = task['artifacts'][0]
            if artifact['parts']:
                part = artifact['parts'][0]
                if 'data' in part:
                    print(f"   Result: {part['data']['result']}")
    
    print("\n=== Demo Complete ===")
```

### Step 4: Run Demo

```bash
# Terminal 1: Start Calculator Agent
python calculator_agent.py

# Terminal 2: Run Demo Client
python demo_client.py
```

---

## Extending to OASIS Integration

### Step 5: Add OASIS Client

**File: `oasis_client.py`**

```python
import requests

class OASISClient:
    def __init__(self, base_url="https://api.oasisweb4.one"):
        self.base_url = base_url
        self.token = None
        self.avatar_id = None
    
    def register_avatar(self, username, email, password):
        """Register agent as OASIS Avatar"""
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
    
    def generate_wallet(self, provider_type="EthereumOASIS"):
        """Generate wallet for agent"""
        response = requests.post(
            f"{self.base_url}/api/wallet/avatar/{self.avatar_id}/generate",
            headers={"Authorization": f"Bearer {self.token}"},
            json={"providerType": provider_type, "setAsDefault": True}
        )
        return response.json()
    
    def get_wallets(self):
        """Get agent wallets"""
        response = requests.get(
            f"{self.base_url}/api/wallet/avatar/{self.avatar_id}/wallets",
            headers={"Authorization": f"Bearer {self.token}"}
        )
        return response.json()
    
    def add_karma(self, karma_type="Helpful", source_title="Task Completion"):
        """Add karma after successful task"""
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

### Step 6: Integrate OASIS into Calculator Agent

**Update `calculator_agent.py`:**

```python
from oasis_client import OASISClient

# Initialize OASIS client
oasis = OASISClient()

# Register agent (run once)
# oasis.register_avatar("calculator_agent", "calc@example.com", "password123")
# oasis.generate_wallet()

# Update Agent Card generation to include OASIS data
def generate_agent_card_with_oasis():
    wallets = oasis.get_wallets()
    wallet_address = None
    
    if wallets.get('result'):
        for provider, wallet_list in wallets['result'].items():
            if wallet_list:
                wallet_address = wallet_list[0].get('address')
                break
    
    card = AGENT_CARD.copy()
    card["metadata"] = {
        "oasis": {
            "avatar_id": oasis.avatar_id,
            "wallet_address": wallet_address
        }
    }
    return card

# Update handle_message to add karma
def handle_message(params, request_id):
    # ... existing calculation logic ...
    
    # After successful calculation, add karma
    if isinstance(result, (int, float)):
        oasis.add_karma(
            karma_type="Helpful",
            source_title="Completed calculation"
        )
    
    # ... rest of function ...
```

---

## Next Steps

1. **Test the basic demo** - Ensure Calculator Agent works
2. **Add OASIS registration** - Register agents with OASIS
3. **Add Payment Agent** - Create second agent for payments
4. **Add agent discovery** - Make agents discover each other
5. **Add more features** - Streaming, multi-turn, etc.

---

## Resources

- **A2A Python SDK Docs:** https://github.com/a2aproject/a2a-python
- **A2A Protocol Spec:** `/Volumes/Storage/OASIS_CLEAN/A2A/specification/`
- **OASIS Integration Guide:** `/Volumes/Storage/OASIS_CLEAN/A2A/docs/oasis-integration/`

---

**Estimated Time:** 2-3 hours for basic demo, 1-2 days for full OASIS integration













