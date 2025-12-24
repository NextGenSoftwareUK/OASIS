# Testing with SOL on Solana Devnet

This guide explains how to test the autonomous agent payment network using SOL on Solana devnet instead of MNEE.

## Why SOL for Testing?

- ✅ **Easy to get test tokens** - Solana devnet has a faucet
- ✅ **Fast transactions** - Solana has sub-second finality
- ✅ **No gas fees** - Devnet transactions are free
- ✅ **Same architecture** - Works identically to MNEE payments

## Configuration

The system is already configured to use SOL by default. Check `config.py`:

```python
PAYMENT_PROVIDER_TYPE = "SolanaOASIS"  # Using Solana
PAYMENT_TOKEN_SYMBOL = "SOL"  # Using SOL token
SOLANA_NETWORK = "devnet"  # Using devnet
```

## Getting Test SOL

### Option 1: Solana Faucet

1. Visit: https://faucet.solana.com/
2. Enter your wallet address
3. Request devnet SOL
4. You'll receive 1-2 SOL for testing

### Option 2: Using OASIS API

If your OASIS API is configured with Solana devnet, wallets will be created automatically. You may need to fund them manually via the faucet.

## Running the Demo

### Step 1: Start OASIS API

Make sure OASIS API is running with Solana provider enabled:

```bash
cd /Volumes/Storage/OASIS_CLEAN/NextGenSoftware.OASIS.API.ONODE.WebAPI
dotnet run
```

### Step 2: Start Data Analyzer Agent

```bash
cd /Volumes/Storage/OASIS_CLEAN/mnee-hackathon-submission
source venv/bin/activate
python demo/data_analyzer_agent.py
```

The agent will:
- Register with OASIS
- Generate a Solana wallet
- Start A2A Protocol server

### Step 3: Fund Agent Wallets (if needed)

If agents don't have SOL, you can fund them:

1. Get wallet addresses from agent output
2. Use Solana faucet: https://faucet.solana.com/
3. Enter wallet address and request SOL

### Step 4: Run Demo

```bash
python demo/run_demo.py
```

The demo will:
1. Register requester agent
2. Discover data analyzer agent
3. Execute market analysis task
4. Send SOL payment
5. Update karma

## Payment Flow

When using SOL:

1. **Amount Conversion**: SOL amounts are converted to lamports (1 SOL = 1e9 lamports)
2. **Transaction**: Uses `/api/solana/send` endpoint
3. **Network**: All transactions on Solana devnet
4. **Verification**: Check transaction on Solana Explorer: https://explorer.solana.com/?cluster=devnet

## Switching to MNEE (Production)

To switch to MNEE for production:

1. Update `config.py`:
   ```python
   PAYMENT_PROVIDER_TYPE = "EthereumOASIS"
   PAYMENT_TOKEN_SYMBOL = "MNEE"
   MNEE_CONTRACT_ADDRESS = "0x..."  # Get from hackathon organizers
   ```

2. Update agent pricing in:
   - `src/agents/data_analyzer.py` (change "0.01 SOL" to "0.01 MNEE")
   - `src/agents/image_generator.py` (change "0.05 SOL" to "0.05 MNEE")

3. Restart agents and demo

## Troubleshooting

### "Solana wallet not found"

**Problem:** Agent doesn't have a Solana wallet

**Solution:**
- Make sure `generate_wallet()` is called with `provider_type="SolanaOASIS"`
- Check OASIS API has Solana provider enabled

### "Insufficient funds"

**Problem:** Agent wallet doesn't have enough SOL

**Solution:**
- Use Solana faucet to fund wallet: https://faucet.solana.com/
- Check balance via OASIS API: `GET /api/wallet/{walletId}/balance`

### "Transaction failed"

**Problem:** Payment transaction fails

**Solution:**
- Check Solana devnet RPC is accessible
- Verify wallet addresses are correct
- Check transaction on Solana Explorer

## Example Transaction

After running the demo, you can view the transaction on Solana Explorer:

1. Get transaction hash from demo output
2. Visit: https://explorer.solana.com/?cluster=devnet
3. Search for transaction hash
4. View transaction details

## Next Steps

1. ✅ Test with SOL on devnet
2. ✅ Verify all payment flows work
3. ✅ Get MNEE contract address
4. ✅ Switch to MNEE for production
5. ✅ Test on Ethereum testnet/mainnet

---

**Note:** SOL on devnet is for testing only. For the hackathon submission, you'll want to use MNEE on Ethereum.

