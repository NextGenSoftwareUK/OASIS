# Setup Demo Agents

**Quick Setup Script for Agent-to-Agent MNEE Payment Demo**

This script creates 2 agent avatars and links Ethereum wallets for both, ready for the MNEE payment demo.

---

## Quick Start

```bash
cd MCP
npx tsx setup-demo-agents.ts
```

**With custom credentials:**
```bash
AGENT_A_USERNAME="agent_a" \
AGENT_A_PASSWORD="pass_a" \
AGENT_B_USERNAME="agent_b" \
AGENT_B_PASSWORD="pass_b" \
npx tsx setup-demo-agents.ts
```

---

## What It Does

1. ✅ Authenticates as `OASIS_ADMIN` (or uses env vars)
2. ✅ Creates Agent A avatar
3. ✅ Creates Agent B avatar
4. ✅ Links Ethereum wallet for Agent A
5. ✅ Links Ethereum wallet for Agent B
6. ✅ Prints summary with credentials and wallet addresses

---

## Default Credentials

If not specified via environment variables:

- **Agent A:**
  - Username: `demo_agent_a`
  - Password: `DemoAgentA123!`
  - Email: `demo_agent_a@oasis.demo`

- **Agent B:**
  - Username: `demo_agent_b`
  - Password: `DemoAgentB123!`
  - Email: `demo_agent_b@oasis.demo`

---

## Environment Variables

- `API_BASE_URL` - OASIS API URL (default: `http://localhost:5003`)
- `OASIS_ADMIN_USERNAME` - Admin username (default: `OASIS_ADMIN`)
- `OASIS_ADMIN_PASSWORD` - Admin password (default: `Uppermall1!`)
- `AGENT_A_USERNAME` - Agent A username
- `AGENT_A_PASSWORD` - Agent A password
- `AGENT_A_EMAIL` - Agent A email
- `AGENT_B_USERNAME` - Agent B username
- `AGENT_B_PASSWORD` - Agent B password
- `AGENT_B_EMAIL` - Agent B email

---

## Output Example

```
============================================================
Demo Agent Setup
Creating 2 agent avatars with Ethereum wallets
============================================================

🔐 Authenticating as OASIS_ADMIN...
✅ Admin authenticated

============================================================
Setting up: demo_agent_a
============================================================
📝 Registering agent: demo_agent_a...
✅ Agent demo_agent_a registered (ID: 12345678...)
🔐 Authenticating demo_agent_a...
✅ demo_agent_a authenticated
💰 Creating Ethereum wallet for demo_agent_a...
   Creating Ethereum wallet...
   ✅ Keypair generated (address: 0x1234abcd...)
   Linking public key (address: 0x1234abcd...)...
   ✅ Public key linked (Wallet ID: abc12345...)
   Linking private key...
   ✅ Private key linked
✅ Ethereum wallet created: 0x1234abcd...

============================================================
Setting up: demo_agent_b
============================================================
...

============================================================
Setup Summary
============================================================
✅ Both agents created successfully!

Agent: demo_agent_a
  Avatar ID: 12345678-1234-1234-1234-123456789abc
  Wallet Address: 0x1234abcd5678ef9012345678901234567890abcd
  Email: demo_agent_a@oasis.demo

Agent: demo_agent_b
  Avatar ID: 87654321-4321-4321-4321-cba987654321
  Wallet Address: 0xabcd1234567890fedcba0987654321fedcba09
  Email: demo_agent_b@oasis.demo

============================================================
Next Steps:
============================================================
1. Fund Agent A with MNEE tokens for the demo
2. Run the payment demo:
   cd MCP
   AGENT_A_USERNAME="demo_agent_a" \
   AGENT_A_PASSWORD="DemoAgentA123!" \
   AGENT_B_USERNAME="demo_agent_b" \
   AGENT_B_PASSWORD="DemoAgentB123!" \
   npx tsx demo-agent-mnee-payment.ts
```

---

## Troubleshooting

### "Admin authentication failed"
- Check that `OASIS_ADMIN` credentials are correct
- Ensure API is running and accessible

### "Agent already exists"
- Script will attempt to authenticate and use existing agent
- If authentication fails, you may need to delete the agent first

### "Failed to create wallet"
- Ensure Ethereum provider is activated in OASIS_DNA.json
- Check that API endpoints are accessible

### "Provider not activated"
- Activate EthereumOASIS provider in OASIS_DNA.json
- Restart the API

---

## Next Steps

After running this script:

1. **Fund Agent A** with MNEE tokens (for sending payments)
2. **Run the payment demo**: `npx tsx demo-agent-mnee-payment.ts`
3. **Show the demo** to demonstrate autonomous agent-to-agent payments!

---

**Ready to demo!** 🎬
