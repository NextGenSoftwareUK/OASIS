# How to Run MCP Endpoint Tests

**Quick Start Guide**

---

## Step-by-Step Instructions

### 1. Open Terminal

Open your terminal and navigate to the MCP directory:

```bash
cd /Users/maxgershfield/OASIS_CLEAN/MCP
```

### 2. Set Environment Variables

Copy and paste these commands into your terminal:

```bash
export TEST_AVATAR_ID="0df19747-fa32-4c2f-a6b8-b55ed76d04af"
export OASIS_PASSWORD="Uppermall1!"
```

**What this does:**
- `TEST_AVATAR_ID` - Sets your avatar ID for testing
- `OASIS_PASSWORD` - Sets your password for authentication

**Note:** These are only set for the current terminal session. If you close the terminal, you'll need to run them again.

### 3. Run the Test Script

After setting the environment variables, run:

```bash
npx tsx test-all-write-endpoints.ts
```

---

## Alternative: Add to .env File (Permanent)

Instead of exporting each time, you can add these to your `.env` file:

```bash
# Open or create .env file
nano .env
# or
code .env
```

Add these lines:
```env
TEST_AVATAR_ID=0df19747-fa32-4c2f-a6b8-b55ed76d04af
OASIS_PASSWORD=Uppermall1!
OASIS_USERNAME=OASIS_ADMIN
```

Then the test script will automatically load them (no need to export).

---

## Complete Example

Here's the complete sequence in one go:

```bash
# Navigate to MCP directory
cd /Users/maxgershfield/OASIS_CLEAN/MCP

# Set environment variables
export TEST_AVATAR_ID="0df19747-fa32-4c2f-a6b8-b55ed76d04af"
export OASIS_PASSWORD="Uppermall1!"

# Run the test
npx tsx test-all-write-endpoints.ts
```

---

## What Happens

1. ✅ Script authenticates using your credentials
2. ✅ Tests all write/create endpoints
3. ✅ Creates a Solana wallet (safe operation)
4. ✅ Creates a test holon (safe operation)
5. ✅ Updates avatar info (safe operation)
6. 📊 Shows summary of all tests

---

## Other Test Scripts

### Basic Tests (Read Operations)
```bash
npx tsx test-mcp-endpoints.ts
```

### Comprehensive Tests (All Operations)
```bash
export TEST_AVATAR_ID="0df19747-fa32-4c2f-a6b8-b55ed76d04af"
export OASIS_PASSWORD="Uppermall1!"
npx tsx test-mcp-endpoints-comprehensive.ts
```

---

## Troubleshooting

### "Command not found: npx"
Install Node.js first, then npx will be available.

### "Cannot find module"
Run `npm install` in the MCP directory first.

### "Unauthorized" errors
Make sure you set `OASIS_PASSWORD` correctly.

### Tests fail
Check that:
- OASIS API is running
- `OASIS_API_URL` in `.env` is correct
- Your credentials are valid

---

## Quick Reference

**One-liner to run all tests:**
```bash
cd /Users/maxgershfield/OASIS_CLEAN/MCP && export TEST_AVATAR_ID="0df19747-fa32-4c2f-a6b8-b55ed76d04af" && export OASIS_PASSWORD="Uppermall1!" && npx tsx test-all-write-endpoints.ts
```
