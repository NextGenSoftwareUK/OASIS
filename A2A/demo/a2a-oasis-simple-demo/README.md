# A2A + OASIS Simple Demo

A simple demo showing two agents communicating via A2A protocol, both registered as OASIS Avatars.

---

## Prerequisites

1. **OASIS API running locally** (see setup below)
2. Python 3.8+
3. pip

---

## Step 1: Start OASIS API Locally

The OASIS API must be running before starting the agents.

### Option 1: Using .NET Core

```bash
# Navigate to OASIS API directory
cd /Volumes/Storage/OASIS_CLEAN/NextGenSoftware.OASIS.API.ONODE.WebAPI

# Run the API
dotnet run
```

**Expected:** API starts on `https://localhost:5002` (or `http://localhost:5000`)

### Option 2: Check if Already Running

```bash
# Test if API is accessible
curl https://localhost:5002/api/avatar/authenticate \
  -k \
  -X POST \
  -H "Content-Type: application/json" \
  -d '{"username":"test","password":"test"}'
```

If you get a response (even an error), the API is running.

### Note: SSL Certificate

The API may use self-signed certificates. The demo code handles this by:
- Using `verify=False` in requests (for development only)
- Or using `https://localhost:5002` with SSL verification disabled

---

## Step 2: Setup Demo

```bash
cd /Volumes/Storage/OASIS_CLEAN/A2A/demo/a2a-oasis-simple-demo

# Create virtual environment
python -m venv venv
source venv/bin/activate  # Windows: venv\Scripts\activate

# Install dependencies
pip install flask requests
```

---

## Step 3: Configure OASIS API URL

Edit `config.py` to set your OASIS API URL:

```python
OASIS_API_URL = "https://localhost:5002"  # Or http://localhost:5000
OASIS_VERIFY_SSL = False  # Set to True if using valid SSL
```

---

## Step 4: Run the Demo

### Terminal 1: Start Answer Agent

```bash
python answer_agent.py
```

Expected output:
```
🔐 Registering with OASIS...
✅ Registered as OASIS Avatar: <avatar-id>
✅ Wallet: <wallet-address>
📍 A2A Endpoint: http://localhost:5001/a2a
✅ Ready to receive A2A messages!
```

### Terminal 2: Start Question Agent

```bash
python question_agent.py
```

Expected output:
```
🔐 Registering Question Agent with OASIS...
✅ Using existing OASIS Avatar
1️⃣  Discovering Answer Agent...
   ✅ Found: Answer Agent
   🆔 OASIS Avatar ID: <avatar-id>
2️⃣  Sending questions via A2A protocol...
```

---

## Troubleshooting

### OASIS API Connection Issues

**Problem:** `Connection refused` or `SSL errors`

**Solutions:**
1. Verify OASIS API is running:
   ```bash
   curl -k https://localhost:5002/api/avatar/authenticate -X POST
   ```

2. Check the URL in `config.py` matches your API

3. If using HTTP instead of HTTPS:
   ```python
   OASIS_API_URL = "http://localhost:5000"
   ```

4. Disable SSL verification in `config.py`:
   ```python
   OASIS_VERIFY_SSL = False
   ```

### Registration Errors

**Problem:** `Username already exists`

**Solution:** Delete credential files:
```bash
rm answer_agent_credentials.json question_agent_credentials.json
```

### Port Already in Use

**Problem:** `Port 5001 is already in use`

**Solution:** Change port in `answer_agent.py`:
```python
app.run(port=5002, debug=True)  # Use different port
```

---

## File Structure

```
a2a-oasis-simple-demo/
├── README.md                    # This file
├── config.py                    # OASIS API configuration
├── oasis_client.py              # OASIS API client
├── answer_agent.py              # Answer Agent (server)
├── question_agent.py            # Question Agent (client)
├── answer_agent_credentials.json # Auto-generated credentials
└── question_agent_credentials.json # Auto-generated credentials
```

---

## What the Demo Shows

✅ **OASIS Avatar Registration** - Both agents register with OASIS  
✅ **Agent Discovery** - Question Agent discovers Answer Agent via Agent Card  
✅ **A2A Communication** - Messages sent via JSON-RPC 2.0  
✅ **OASIS Identity** - Agent Cards include OASIS avatar_id, wallet, karma  
✅ **Trust Building** - Karma updates after successful answers  

---

## Next Steps

1. Check OASIS API logs to see registration requests
2. Try different questions in interactive mode
3. Check karma increases after answers
4. View Agent Cards at `http://localhost:5001/agent-card`













