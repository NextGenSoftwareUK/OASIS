# Testing Guide - Autonomous Agent Payment Network

## Quick Start Testing

### Option 1: Simple Registration Test (Fastest)
```bash
cd /Volumes/Storage/OASIS_CLEAN/mnee-hackathon-submission
source venv/bin/activate
python test_agent_registration.py
```

**Expected Result:**
- ✅ Agent registers
- ✅ Agent authenticates
- ✅ Wallet generated
- ✅ SOL requested from faucet

---

### Option 2: Full End-to-End Payment Flow (Complete Demo)

**Terminal 1: Start Data Analyzer Agent**
```bash
cd /Volumes/Storage/OASIS_CLEAN/mnee-hackathon-submission
source venv/bin/activate
python demo/data_analyzer_agent.py
```

**Terminal 2: Start Image Generator Agent (Optional)**
```bash
cd /Volumes/Storage/OASIS_CLEAN/mnee-hackathon-submission
source venv/bin/activate
python demo/image_generator_agent.py
```

**Terminal 3: Run Full Demo**
```bash
cd /Volumes/Storage/OASIS_CLEAN/mnee-hackathon-submission
source venv/bin/activate
python demo/run_demo.py
```

---

## Test Scenarios

### Test 1: Agent Registration & Wallet Creation
**File:** `test_agent_registration.py`

**What it tests:**
- Agent registration with auto-generated email
- Authentication without email verification
- Solana wallet generation
- Automatic devnet SOL request

**Run:**
```bash
python test_agent_registration.py
```

---

### Test 2: Single Agent Service
**File:** `demo/data_analyzer_agent.py`

**What it tests:**
- Agent registers and starts service
- Agent Card generation
- A2A Protocol endpoint
- Service capability exposure

**Run:**
```bash
python demo/data_analyzer_agent.py
```

**Expected Output:**
- Agent registers with OASIS
- Agent Card created
- HTTP server starts on port 5001
- Service ready to accept requests

---

### Test 3: Full Payment Flow
**File:** `demo/run_demo.py`

**What it tests:**
- Requester agent registration
- Service provider discovery via A2A
- Payment negotiation
- Task execution
- Payment processing
- Karma updates

**Prerequisites:**
- Data Analyzer Agent running (Terminal 1)
- Both agents need SOL in wallets

**Run:**
```bash
python demo/run_demo.py
```

---

## Step-by-Step Manual Testing

### Step 1: Verify OASIS API is Running
```bash
curl http://localhost:5004/api/health
# or
curl http://localhost:5004/api/avatar/register -X POST -H "Content-Type: application/json" -d '{"test":"ping"}'
```

### Step 2: Test Agent Registration
```bash
python test_agent_registration.py
```

**Verify:**
- ✅ Avatar ID returned
- ✅ JWT token received
- ✅ Wallet address generated
- ✅ SOL requested (check transaction)

### Step 3: Start Service Agent
```bash
python demo/data_analyzer_agent.py
```

**Verify:**
- ✅ Agent registered
- ✅ Agent Card created
- ✅ Server started on port 5001
- ✅ Endpoint accessible: `http://localhost:5001/agent-card`

### Step 4: Test Agent Discovery
```bash
curl http://localhost:5001/agent-card
```

**Expected:** JSON with agent capabilities

### Step 5: Run Full Payment Demo
```bash
python demo/run_demo.py
```

**Verify:**
- ✅ Requester agent registered
- ✅ Service agent discovered
- ✅ Payment negotiated
- ✅ Task executed
- ✅ Payment processed
- ✅ Karma updated

---

## Troubleshooting

### Issue: "Connection refused" to OASIS API
**Solution:** Make sure OASIS API is running on port 5004
```bash
# Check if running
curl http://localhost:5004/api/health

# If not, start it (in OASIS API directory)
cd ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI
dotnet run
```

### Issue: "No agent found at endpoint"
**Solution:** Start the service agent first
```bash
python demo/data_analyzer_agent.py
```

### Issue: "Insufficient balance" for payment
**Solution:** Request more devnet SOL
```bash
# Check balance
curl -X POST https://api.devnet.solana.com \
  -H "Content-Type: application/json" \
  -d '{"jsonrpc":"2.0","id":1,"method":"getBalance","params":["YOUR_WALLET_ADDRESS"]}'

# Request more SOL
# Use: https://faucet.solana.com/?address=YOUR_WALLET_ADDRESS
```

### Issue: "Authentication failed"
**Solution:** 
- Verify API is running latest code (with agent email bypass)
- Check agent email ends with `@agents.local`
- Try registering a fresh agent

---

## Expected Test Results

### Test 1: Registration Test
```
✅ Avatar registered: [UUID]
✅ Wallet created: [Solana Address]
✅ SOL requested: 2.0 SOL
✅ Balance: [should show SOL after a few seconds]
```

### Test 2: Service Agent
```
✅ Registered as OASIS Avatar: [UUID]
✅ Agent Card created
✅ Server started on http://localhost:5001
✅ Ready to accept requests
```

### Test 3: Full Payment Flow
```
✅ Requester agent registered
✅ Service provider agent discovered
✅ Payment negotiated: 0.01 SOL
✅ Task executed: Market data analysis
✅ Payment processed: Transaction [hash]
✅ Karma updated for both agents
```

---

## Next Steps After Testing

1. **Verify Transactions on Solana Explorer**
   - Visit: https://explorer.solana.com/?cluster=devnet
   - Search for wallet addresses or transaction signatures

2. **Check Agent Cards**
   - Visit: `http://localhost:5001/agent-card`
   - Verify capabilities and pricing

3. **Test Multiple Agents**
   - Start both Data Analyzer and Image Generator
   - Test discovery of multiple agents
   - Test different payment amounts

4. **Test Payment Failures**
   - Try payment with insufficient balance
   - Verify error handling

---

## Performance Testing

### Load Test: Multiple Agents
```bash
# Start multiple service agents
for i in {1..5}; do
  python demo/data_analyzer_agent.py &
done
```

### Stress Test: Multiple Payments
- Run demo multiple times
- Verify all payments process
- Check karma updates

---

## Success Criteria

✅ All agents register successfully  
✅ All agents authenticate without email verification  
✅ All agents generate Solana wallets  
✅ All agents receive devnet SOL automatically  
✅ Service agents expose capabilities via A2A  
✅ Requester agents discover service agents  
✅ Payments process successfully  
✅ Karma updates correctly  
✅ Full end-to-end flow works without manual intervention  

---

**Ready to test? Start with Option 1 (Simple Registration Test) to verify everything is working!**

