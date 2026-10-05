# Today's Simple Demo: A2A + OASIS Integration

**Time:** 45-60 minutes  
**Complexity:** Low  
**Prerequisites:** Python 3.8+, pip, internet connection, OASIS API access

---

## What We're Building

Two agents that communicate via A2A protocol, both registered as OASIS Avatars:
1. **Answer Agent** (Server) - Answers questions, registered as OASIS Avatar
2. **Question Agent** (Client) - Asks questions, registered as OASIS Avatar

**This demonstrates:** A2A communication + OASIS identity integration!

---

## Step 1: Setup (5 minutes)

```bash
# Create directory
mkdir a2a-oasis-demo && cd a2a-oasis-demo

# Create virtual environment
python -m venv venv
source venv/bin/activate  # Windows: venv\Scripts\activate

# Install dependencies
pip install flask requests
```

**Note:** You'll need access to the OASIS API at `https://api.oasisweb4.one`

---

## Step 2: OASIS Client Helper (10 minutes)

**File: `oasis_client.py`**

```python
import requests
import json
import os

class OASISClient:
    """Simple OASIS API client for agent registration"""
    
    def __init__(self, base_url="https://api.oasisweb4.one"):
        self.base_url = base_url
        self.token = None
        self.avatar_id = None
        self.wallet_address = None
    
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
            print(f"✅ Registered as OASIS Avatar: {self.avatar_id}")
        else:
            print(f"❌ Registration error: {data.get('message', 'Unknown error')}")
        return data
    
    def authenticate(self, username, password):
        """Authenticate existing avatar"""
        response = requests.post(
            f"{self.base_url}/api/avatar/authenticate",
            json={"username": username, "password": password}
        )
        data = response.json()
        if not data.get("isError"):
            self.avatar_id = data["result"]["id"]
            self.token = data["result"]["token"]
            print(f"✅ Authenticated as OASIS Avatar: {self.avatar_id}")
        return data
    
    def generate_wallet(self, provider_type="EthereumOASIS"):
        """Generate wallet for avatar"""
        if not self.token or not self.avatar_id:
            print("❌ Must register/authenticate first")
            return None
        
        response = requests.post(
            f"{self.base_url}/api/wallet/avatar/{self.avatar_id}/generate",
            headers={"Authorization": f"Bearer {self.token}"},
            json={"providerType": provider_type, "setAsDefault": True}
        )
        data = response.json()
        if not data.get("isError"):
            # Get wallet address
            self.get_wallets()
        return data
    
    def get_wallets(self):
        """Get avatar wallets"""
        if not self.token or not self.avatar_id:
            return None
        
        response = requests.get(
            f"{self.base_url}/api/wallet/avatar/{self.avatar_id}/wallets",
            headers={"Authorization": f"Bearer {self.token}"}
        )
        data = response.json()
        if not data.get("isError") and data.get("result"):
            # Extract first wallet address
            for provider, wallets in data["result"].items():
                if wallets and len(wallets) > 0:
                    self.wallet_address = wallets[0].get("address")
                    print(f"✅ Wallet: {self.wallet_address}")
                    break
        return data
    
    def get_karma_stats(self):
        """Get karma statistics"""
        if not self.token or not self.avatar_id:
            return None
        
        response = requests.get(
            f"{self.base_url}/api/karma/get-karma-stats/{self.avatar_id}",
            headers={"Authorization": f"Bearer {self.token}"}
        )
        return response.json()
    
    def add_karma(self, karma_type="Helpful", source_title="Task Completion"):
        """Add karma after successful task"""
        if not self.token or not self.avatar_id:
            return None
        
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

## Step 3: Answer Agent (Server) - 20 minutes

**File: `answer_agent.py`**

```python
from flask import Flask, request, jsonify
import uuid
import os
from oasis_client import OASISClient

app = Flask(__name__)

# Initialize OASIS client
oasis = OASISClient()

# Agent Card (will be populated with OASIS data)
AGENT_CARD = {
    "protocol_version": "1.0",
    "name": "Answer Agent",
    "description": "A simple agent that answers questions (OASIS-powered)",
    "version": "1.0.0",
    "supported_interfaces": [{
        "url": "http://localhost:5001/a2a",
        "protocol_binding": "HTTP+JSON"
    }],
    "default_input_modes": ["text/plain"],
    "default_output_modes": ["text/plain"],
    "skills": [{
        "id": "answering",
        "name": "Answer Questions",
        "description": "Provides helpful answers to questions",
        "tags": ["qa", "assistant"]
    }],
    "metadata": {}  # Will contain OASIS data
}

def initialize_oasis():
    """Register agent with OASIS on startup"""
    print("\n🔐 Registering with OASIS...")
    
    # Try to load existing credentials
    credentials_file = "answer_agent_credentials.json"
    if os.path.exists(credentials_file):
        import json
        with open(credentials_file, 'r') as f:
            creds = json.load(f)
            # Authenticate with existing credentials
            result = oasis.authenticate(creds["username"], creds["password"])
            if not result.get("isError"):
                print("✅ Using existing OASIS Avatar")
                return True
    
    # Register new avatar
    username = "answer_agent_demo"
    email = f"{username}@example.com"
    password = "demo_password_123"
    
    result = oasis.register_avatar(username, email, password)
    
    if not result.get("isError"):
        # Save credentials
        import json
        with open(credentials_file, 'w') as f:
            json.dump({"username": username, "password": password}, f)
        
        # Generate wallet
        oasis.generate_wallet()
        
        # Update Agent Card with OASIS data
        update_agent_card_with_oasis()
        return True
    else:
        print(f"⚠️  OASIS registration failed, but continuing...")
        return False

def update_agent_card_with_oasis():
    """Update Agent Card with OASIS information"""
    karma = oasis.get_karma_stats()
    karma_total = 0
    if karma and not karma.get("isError"):
        karma_total = karma.get("result", {}).get("total", 0)
    
    AGENT_CARD["metadata"] = {
        "oasis": {
            "avatar_id": oasis.avatar_id,
            "wallet_address": oasis.wallet_address,
            "karma": karma_total,
            "trust_status": "verified" if karma_total > 0 else "new"
        }
    }

@app.route('/a2a', methods=['POST'])
def handle_a2a():
    """Handle A2A JSON-RPC 2.0 request"""
    try:
        data = request.json
        
        # Validate JSON-RPC 2.0
        if data.get('jsonrpc') != '2.0':
            return jsonify({
                "jsonrpc": "2.0",
                "error": {"code": -32600, "message": "Invalid Request"},
                "id": data.get('id')
            }), 400
        
        method = data.get('method')
        params = data.get('params', {})
        request_id = data.get('id')
        
        # Handle message.send method
        if method == 'message.send':
            return handle_message(params, request_id)
        else:
            return jsonify({
                "jsonrpc": "2.0",
                "error": {"code": -32601, "message": f"Method not found: {method}"},
                "id": request_id
            }), 404
            
    except Exception as e:
        return jsonify({
            "jsonrpc": "2.0",
            "error": {"code": -32603, "message": str(e)},
            "id": request.json.get('id')
        }), 500

def handle_message(params, request_id):
    """Handle incoming message and respond"""
    message = params.get('message', {})
    
    # Extract question text
    question = ""
    for part in message.get('parts', []):
        if 'text' in part:
            question += part['text']
    
    # Simple answer logic (you can make this smarter!)
    answer = answer_question(question)
    
    # After successful answer, update karma (OASIS)
    if oasis.avatar_id:
        try:
            oasis.add_karma(
                karma_type="Helpful",
                source_title="Answered question"
            )
            # Refresh karma in Agent Card
            update_agent_card_with_oasis()
        except Exception as e:
            print(f"⚠️  Could not update karma: {e}")
    
    # Create A2A response (Task format)
    task = {
        "id": str(uuid.uuid4()),
        "context_id": message.get('context_id', str(uuid.uuid4())),
        "status": {
            "state": "TASK_STATE_COMPLETED",
            "message": {
                "message_id": str(uuid.uuid4()),
                "role": "ROLE_AGENT",
                "context_id": message.get('context_id'),
                "parts": [{"text": answer}]
            }
        }
    }
    
    return jsonify({
        "jsonrpc": "2.0",
        "result": {"task": task},
        "id": request_id
    })

def answer_question(question):
    """Simple question answering - replace with your logic!"""
    question_lower = question.lower()
    
    if "hello" in question_lower or "hi" in question_lower:
        return "Hello! I'm an Answer Agent. How can I help you?"
    elif "weather" in question_lower:
        return "I don't have access to weather data, but I hope it's nice where you are!"
    elif "time" in question_lower:
        from datetime import datetime
        return f"The current time is {datetime.now().strftime('%H:%M:%S')}"
    elif "name" in question_lower:
        return "I'm Answer Agent, an A2A-compatible agent!"
    elif "?" in question:
        return "That's a great question! I'm a simple demo agent, but I'm learning."
    else:
        return f"I received your message: '{question}'. I'm a simple Answer Agent that responds to messages via the A2A protocol!"

@app.route('/agent-card', methods=['GET'])
def get_agent_card():
    """Return Agent Card with OASIS data"""
    update_agent_card_with_oasis()  # Refresh OASIS data
    return jsonify(AGENT_CARD)

if __name__ == '__main__':
    print("\n" + "="*60)
    print("🤖 Answer Agent - A2A + OASIS Demo")
    print("="*60)
    
    # Initialize OASIS
    initialize_oasis()
    
    print("\n📍 A2A Endpoint: http://localhost:5001/a2a")
    print("📄 Agent Card: http://localhost:5001/agent-card")
    if oasis.avatar_id:
        print(f"🆔 OASIS Avatar ID: {oasis.avatar_id}")
        if oasis.wallet_address:
            print(f"💼 Wallet: {oasis.wallet_address}")
    print("✅ Ready to receive A2A messages!\n")
    
    app.run(port=5001, debug=True)
```

---

## Step 4: Question Agent (Client) - 15 minutes

**File: `question_agent.py`**

```python
import requests
import json
import sys
import os
from oasis_client import OASISClient

ANSWER_AGENT_URL = "http://localhost:5001"

# Initialize OASIS client for Question Agent
oasis = OASISClient()

def initialize_oasis():
    """Register Question Agent with OASIS"""
    print("🔐 Registering Question Agent with OASIS...")
    
    # Try to load existing credentials
    credentials_file = "question_agent_credentials.json"
    if os.path.exists(credentials_file):
        with open(credentials_file, 'r') as f:
            creds = json.load(f)
            result = oasis.authenticate(creds["username"], creds["password"])
            if not result.get("isError"):
                print("✅ Using existing OASIS Avatar")
                return True
    
    # Register new avatar
    username = "question_agent_demo"
    email = f"{username}@example.com"
    password = "demo_password_123"
    
    result = oasis.register_avatar(username, email, password)
    
    if not result.get("isError"):
        # Save credentials
        with open(credentials_file, 'w') as f:
            json.dump({"username": username, "password": password}, f)
        
        # Generate wallet
        oasis.generate_wallet()
        return True
    
    print(f"⚠️  OASIS registration failed, but continuing...")
    return False

def get_agent_card():
    """Get Answer Agent's card"""
    try:
        response = requests.get(f"{ANSWER_AGENT_URL}/agent-card")
        return response.json()
    except Exception as e:
        print(f"❌ Error getting agent card: {e}")
        return None

def send_question(question_text):
    """Send question to Answer Agent via A2A"""
    payload = {
        "jsonrpc": "2.0",
        "method": "message.send",
        "params": {
            "message": {
                "message_id": f"msg-{hash(question_text) % 10000}",
                "role": "ROLE_USER",
                "parts": [{"text": question_text}]
            }
        },
        "id": 1
    }
    
    try:
        response = requests.post(
            f"{ANSWER_AGENT_URL}/a2a",
            json=payload,
            headers={"Content-Type": "application/json"},
            timeout=10
        )
        return response.json()
    except Exception as e:
        return {"error": str(e)}

def main():
    print("\n" + "="*60)
    print("🤖 Question Agent - A2A + OASIS Demo")
    print("="*60 + "\n")
    
    # Initialize OASIS
    initialize_oasis()
    if oasis.avatar_id:
        print(f"🆔 OASIS Avatar ID: {oasis.avatar_id}\n")
    
    # Step 1: Get Agent Card
    print("1️⃣  Discovering Answer Agent...")
    card = get_agent_card()
    
    if card:
        print(f"   ✅ Found: {card['name']}")
        print(f"   📝 Description: {card['description']}")
        print(f"   🎯 Skills: {', '.join([s['name'] for s in card['skills']])}")
        
        # Show OASIS info if available
        if card.get('metadata', {}).get('oasis'):
            oasis_info = card['metadata']['oasis']
            print(f"   🆔 OASIS Avatar ID: {oasis_info.get('avatar_id', 'N/A')}")
            if oasis_info.get('wallet_address'):
                print(f"   💼 Wallet: {oasis_info['wallet_address']}")
            print(f"   ⭐ Karma: {oasis_info.get('karma', 0)}")
        print()
    else:
        print("   ⚠️  Could not get agent card, but continuing...\n")
    
    # Step 2: Ask questions
    print("2️⃣  Sending questions via A2A protocol...\n")
    
    questions = [
        "Hello! What's your name?",
        "What time is it?",
        "How does A2A protocol work?",
    ]
    
    for i, question in enumerate(questions, 1):
        print(f"   Question {i}: {question}")
        
        result = send_question(question)
        
        if "error" in result:
            print(f"   ❌ Error: {result['error']}\n")
            continue
        
        if "result" in result and "task" in result["result"]:
            task = result["result"]["task"]
            status = task.get("status", {})
            message = status.get("message", {})
            
            # Extract answer from message parts
            answer = ""
            for part in message.get("parts", []):
                if "text" in part:
                    answer += part["text"]
            
            print(f"   ✅ Answer: {answer}\n")
        else:
            print(f"   ⚠️  Unexpected response: {json.dumps(result, indent=2)}\n")
    
    # Interactive mode
    print("3️⃣  Interactive mode - ask your own questions!")
    print("   (Type 'quit' to exit)\n")
    
    while True:
        try:
            question = input("You: ").strip()
            
            if question.lower() in ['quit', 'exit', 'q']:
                print("\n👋 Goodbye!\n")
                break
            
            if not question:
                continue
            
            result = send_question(question)
            
            if "error" in result:
                print(f"❌ Error: {result['error']}\n")
                continue
            
            if "result" in result and "task" in result["result"]:
                task = result["result"]["task"]
                status = task.get("status", {})
                message = status.get("message", {})
                
                answer = ""
                for part in message.get("parts", []):
                    if "text" in part:
                        answer += part["text"]
                
                print(f"Answer Agent: {answer}\n")
            else:
                print(f"⚠️  Unexpected response\n")
                
        except KeyboardInterrupt:
            print("\n\n👋 Goodbye!\n")
            break
        except Exception as e:
            print(f"❌ Error: {e}\n")

if __name__ == '__main__':
    main()
```

---

## Step 5: Run It! (2 minutes)

**Terminal 1: Start Answer Agent**
```bash
python answer_agent.py
```

**Terminal 2: Run Question Agent**
```bash
python question_agent.py
```

**That's it!** You should see:
1. Both agents register with OASIS (or authenticate if already registered)
2. Question Agent discovers Answer Agent's card (including OASIS info)
3. Questions sent via A2A protocol
4. Answers received and displayed
5. Karma updates after successful answers

---

## What Just Happened?

✅ **OASIS Integration** - Both agents registered as OASIS Avatars  
✅ **Agent Discovery** - Question Agent found Answer Agent's capabilities + OASIS identity  
✅ **A2A Communication** - Messages sent using JSON-RPC 2.0 format  
✅ **Task-Based Response** - Answer Agent returned a Task with the answer  
✅ **Trust Building** - Answer Agent earns karma after successful answers  
✅ **Agent-to-Agent** - Two independent agents communicating with verified identities!

---

## Key Features Demonstrated

1. **OASIS Avatar Registration** - Each agent has a unique identity
2. **Wallet Generation** - Agents have blockchain wallets
3. **Agent Cards with OASIS Data** - Agent Cards include OASIS metadata
4. **Karma Tracking** - Agents build reputation through successful tasks
5. **A2A Protocol** - Standard agent-to-agent communication

---

## What's Next?

Now that you have A2A + OASIS working, try:

1. **Better Answers** - Connect to an LLM (OpenAI, Anthropic)
2. **Payments** - Add OASIS Wallet payments between agents
3. **Trust Filtering** - Only accept tasks from agents with karma > threshold
4. **Agent Discovery** - Query OASIS HyperDrive to find agents automatically
5. **Multiple Agents** - Add more specialized agents to the network

---

## Troubleshooting

**Port already in use?**
```bash
# Change port in answer_agent.py
app.run(port=5002, debug=True)  # Use different port
```

**Connection refused?**
- Make sure Answer Agent is running first
- Check the URL matches in question_agent.py

**Import errors?**
```bash
pip install flask requests
```

---

## Success Criteria

✅ Both agents register with OASIS (or authenticate)  
✅ Answer Agent shows OASIS Avatar ID and wallet  
✅ Question Agent shows OASIS Avatar ID  
✅ Agent Card includes OASIS metadata (avatar_id, wallet, karma)  
✅ Questions are sent via A2A protocol  
✅ Answers are received and displayed  
✅ Karma updates after successful answers  
✅ Agents communicate with verified identities!

---

## Troubleshooting

**OASIS API Connection Issues?**
- Verify API URL: `https://api.oasisweb4.one`
- Check internet connection
- Verify API is accessible from your network

**Registration Fails?**
- Username/email might already exist - change the username
- Check API response for specific error message
- Try authenticating instead if avatar already exists

**Credentials File Created?**
- Credentials saved in `answer_agent_credentials.json` and `question_agent_credentials.json`
- Delete these files to re-register

---

**Total Time:** 45-60 minutes  
**Files:** 3 Python files (~350 lines total)  
**Dependencies:** flask, requests  
**Complexity:** Low  
**OASIS Required:** Yes (Avatar registration)

**This is the simplest A2A + OASIS integration demo!** 🎉













