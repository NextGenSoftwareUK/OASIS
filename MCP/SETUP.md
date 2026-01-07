# MCP Server Setup Guide

## 🚀 Quick Setup (5 minutes)

### Step 1: Install Dependencies

```bash
cd /Users/maxgershfield/OASIS_CLEAN/MCP
npm install
```

### Step 2: Configure Environment

Create a `.env` file:

```bash
# Copy the example
cp env.example .env

# Edit .env with your settings
# OASIS_API_URL=http://localhost:5000
# OASIS_API_KEY=your_key_here (optional)
```

### Step 3: Build

```bash
npm run build
```

### Step 4: Configure Cursor

Create or edit `~/.cursor/mcp.json`:

```json
{
  "mcpServers": {
    "oasis-unified": {
      "command": "node",
      "args": ["/Users/maxgershfield/OASIS_CLEAN/MCP/dist/index.js"],
      "env": {
        "OASIS_API_URL": "http://localhost:5000"
      }
    }
  }
}
```

**Important:** 
- Use **absolute paths** (full path from root)
- Replace the path with your actual path if different
- On macOS, you can get the full path with: `realpath /Users/maxgershfield/OASIS_CLEAN/MCP/dist/index.js`

### Step 5: Restart Cursor

1. Close Cursor completely
2. Reopen Cursor
3. Check MCP connection in View → Output → MCP

### Step 6: Test It!

In Cursor, try asking:
- "Check OASIS API health"
- "Get avatar information for username testuser"
- "Show me all NFTs for avatar ID abc123"

## 🔍 Verify Installation

### Check if MCP server runs:

```bash
cd /Users/maxgershfield/OASIS_CLEAN/MCP
npm run dev
```

You should see:
```
[MCP] OASIS Unified MCP Server started
[MCP] OASIS API URL: http://localhost:5000
[MCP] Mode: stdio
```

### Check Cursor connection:

1. Open Cursor
2. Go to View → Output
3. Select "MCP" from dropdown
4. You should see connection logs

## 🐛 Troubleshooting

### "Command not found" or "Cannot find module"

**Fix:** Use absolute path in `mcp.json`

```bash
# Get absolute path
cd /Users/maxgershfield/OASIS_CLEAN/MCP
realpath dist/index.js  # macOS/Linux
```

### "OASIS API connection failed"

**Fix:** 
1. Make sure OASIS API is running: `curl http://localhost:5000/api/health`
2. Check `.env` file has correct `OASIS_API_URL`
3. Verify no firewall blocking localhost:5000

### "MCP server not showing in Cursor"

**Fix:**
1. Verify `~/.cursor/mcp.json` exists and is valid JSON
2. Restart Cursor completely (quit and reopen)
3. Check Cursor logs: View → Output → MCP
4. Verify the path in `mcp.json` points to the built file: `dist/index.js`

### Build errors

```bash
# Clean and rebuild
rm -rf node_modules dist
npm install
npm run build
```

## ✅ Success Checklist

- [ ] Dependencies installed (`npm install`)
- [ ] `.env` file created and configured
- [ ] Server builds successfully (`npm run build`)
- [ ] `~/.cursor/mcp.json` created with absolute path
- [ ] Cursor restarted
- [ ] MCP connection visible in Cursor logs
- [ ] Can query OASIS API via Cursor

## 🎯 Next Steps

Once working, you can:
1. Add more OASIS tools
2. Integrate OpenSERV
3. Add STAR dApp creation
4. Add NFT minting
5. Add smart contract generation





















