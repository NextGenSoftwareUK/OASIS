# Quick Start Guide

## Prerequisites

1. **Python 3.8+** installed
2. **OASIS API** running (see below)
3. **MNEE Contract Address** (get from hackathon organizers)

## Setup

### 1. Install Dependencies

```bash
cd /Volumes/Storage/OASIS_CLEAN/mnee-hackathon-submission
python -m venv venv
source venv/bin/activate  # Windows: venv\Scripts\activate
pip install -r requirements.txt
```

### 2. Configure

The system is pre-configured to use **SOL on Solana devnet** for testing. No changes needed!

To switch to MNEE for production, edit `config.py`:

```python
# For testing (current):
PAYMENT_PROVIDER_TYPE = "SolanaOASIS"
PAYMENT_TOKEN_SYMBOL = "SOL"

# For production (update when you have MNEE address):
# PAYMENT_PROVIDER_TYPE = "EthereumOASIS"
# PAYMENT_TOKEN_SYMBOL = "MNEE"
# MNEE_CONTRACT_ADDRESS = "0x..."  # Get from hackathon organizers
```

### 3. Start OASIS API (if running locally)

```bash
# Navigate to OASIS API directory
cd /Volumes/Storage/OASIS_CLEAN/NextGenSoftware.OASIS.API.ONODE.WebAPI

# Run the API
dotnet run
```

The API should start on `https://localhost:5002` or `http://localhost:5000`.

## Running the Demo

### Terminal 1: Start Data Analyzer Agent

```bash
python demo/data_analyzer_agent.py
```

Expected output:
```
============================================================
Data Analyzer Agent - MNEE Hackathon Submission
============================================================

🔐 Registering with OASIS...
✅ Registered as OASIS Avatar: <avatar-id>
✅ Wallet: <wallet-address>
🚀 Starting Data Analyzer Agent agent server...
   Endpoint: http://0.0.0.0:8080/a2a
   Agent Card: http://0.0.0.0:8080/agent-card
```

### Terminal 2: Run Demo Script

```bash
python demo/run_demo.py
```

This will:
1. Register a requester agent
2. Discover the Data Analyzer agent
3. Execute a market analysis task
4. Process MNEE payment
5. Update karma for both agents

## What the Demo Shows

✅ **Agent Registration** - Agents register with OASIS and get wallets  
✅ **Agent Discovery** - Agents discover each other via A2A Protocol  
✅ **Task Execution** - Agents execute tasks autonomously  
✅ **MNEE Payments** - Payments processed via OASIS Wallet API  
✅ **Trust System** - Karma updated after successful transactions  

## Troubleshooting

### OASIS API Connection Issues

**Problem:** `Connection refused` or `SSL errors`

**Solutions:**
1. Verify OASIS API is running:
   ```bash
   curl -k https://localhost:5002/api/avatar/authenticate -X POST
   ```

2. Update `config.py`:
   ```python
   OASIS_API_URL = "http://localhost:5000"  # Use HTTP if HTTPS fails
   OASIS_VERIFY_SSL = False  # Disable SSL verification for dev
   ```

### Payment Issues

**Problem:** Payment fails with "Solana wallet not found"

**Solution:** 
- Make sure agents have Solana wallets generated
- Check OASIS API has Solana provider enabled
- Fund wallets with SOL from faucet: https://faucet.solana.com/

### Port Already in Use

**Problem:** `Port 8080 is already in use`

**Solution:** Update `AGENT_DISCOVERY_PORT` in `config.py` to use a different port.

## Next Steps

1. **Get MNEE Contract Address** from hackathon organizers
2. **Test with real MNEE** on testnet
3. **Add more agents** with different capabilities
4. **Build your own agent** using `BaseAgent` class

## Support

- **OASIS API Docs:** `/Volumes/Storage/OASIS_CLEAN/Docs/Devs/API Documentation/`
- **A2A Protocol:** `/Volumes/Storage/OASIS_CLEAN/A2A/`
- **MNEE Hackathon:** https://mnee-eth.devpost.com/

