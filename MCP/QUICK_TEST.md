# 🚀 Quick Test Guide

## Fastest Way to Test

### Step 1: Build (if not already done)
```bash
cd /Users/maxgershfield/OASIS_CLEAN/MCP
npm run build
```

### Step 2: Run Test Script
```bash
./test-simple.sh
```

### Step 3: Test Manually
```bash
# Start server in dev mode
npm run dev

# In another terminal, test:
echo '{"jsonrpc":"2.0","id":1,"method":"tools/list","params":{}}' | node dist/index.js
```

## Test with Cursor

### 1. Configure
```bash
cp cursor-mcp-config.json ~/.cursor/mcp.json
```

### 2. Restart Cursor
- Quit completely (Cmd+Q)
- Reopen

### 3. Test in Chat
Try these commands:
- "Check OASIS API health"
- "List all MCP tools"
- "Get cache stats for smart contract compiler"

## What to Expect

### ✅ Success Looks Like:
- Server starts without errors
- Tools list shows 19 tools
- Health check returns status
- Cursor can call tools

### ❌ Common Issues:
- **"Cannot find module"** → Run `npm install && npm run build`
- **"Connection refused"** → Check OASIS API is running: `curl http://localhost:5000/api/health`
- **"Tool not found"** → Rebuild: `npm run build`

## Quick Commands

```bash
# Test health check
echo '{"jsonrpc":"2.0","id":1,"method":"tools/call","params":{"name":"oasis_health_check","arguments":{}}}' | npm run dev

# List tools
echo '{"jsonrpc":"2.0","id":1,"method":"tools/list","params":{}}' | npm run dev | grep '"name"'
```

That's it! 🎉





















