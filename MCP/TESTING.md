# Testing the MCP Server

## 🧪 Quick Test Methods

### Method 1: Test MCP Server Directly (Recommended First)

Test the server without Cursor to verify it's working:

```bash
cd /Users/maxgershfield/OASIS_CLEAN/MCP

# Run in dev mode (shows logs)
npm run dev
```

In another terminal, test with JSON-RPC:

```bash
# Test 1: List available tools
echo '{"jsonrpc":"2.0","id":1,"method":"tools/list","params":{}}' | npm run dev

# Test 2: Call a tool (health check)
echo '{"jsonrpc":"2.0","id":2,"method":"tools/call","params":{"name":"oasis_health_check","arguments":{}}}' | npm run dev
```

**Expected Output:**
- Should see list of tools (oasis_* and scgen_*)
- Health check should return API status

### Method 2: Test with Node.js Script

Create a test script:

```bash
# Create test script
cat > test-mcp.js << 'EOF'
import { spawn } from 'child_process';

const mcp = spawn('node', ['dist/index.js'], {
  stdio: ['pipe', 'pipe', 'pipe']
});

// List tools
mcp.stdin.write(JSON.stringify({
  jsonrpc: '2.0',
  id: 1,
  method: 'tools/list',
  params: {}
}) + '\n');

mcp.stdout.on('data', (data) => {
  console.log('Response:', data.toString());
});

mcp.stderr.on('data', (data) => {
  console.error('Log:', data.toString());
});

setTimeout(() => mcp.kill(), 5000);
EOF

node test-mcp.js
```

### Method 3: Test with Cursor (Full Integration)

1. **Configure Cursor:**
   ```bash
   # Copy config template
   cp cursor-mcp-config.json ~/.cursor/mcp.json
   
   # Or manually edit ~/.cursor/mcp.json
   ```

2. **Restart Cursor:**
   - Quit completely (Cmd+Q)
   - Reopen Cursor

3. **Check Connection:**
   - View → Output → Select "MCP" from dropdown
   - Should see connection logs

4. **Test in Chat:**
   - Ask: "Check OASIS API health"
   - Ask: "List all available MCP tools"

## ✅ Test Checklist

### Prerequisites
- [ ] OASIS API running on `http://localhost:5000`
- [ ] SmartContractGenerator API running (if testing contracts)
- [ ] MCP server built: `npm run build`
- [ ] Dependencies installed: `npm install`

### Basic Tests

#### Test 1: Server Starts
```bash
cd /Users/maxgershfield/OASIS_CLEAN/MCP
npm run dev
```
**Expected:** Should see startup logs without errors

#### Test 2: List Tools
```bash
echo '{"jsonrpc":"2.0","id":1,"method":"tools/list","params":{}}' | npm run dev
```
**Expected:** Should return list of 19 tools (14 OASIS + 5 Smart Contract)

#### Test 3: Health Check
```bash
echo '{"jsonrpc":"2.0","id":2,"method":"tools/call","params":{"name":"oasis_health_check","arguments":{}}}' | npm run dev
```
**Expected:** Should return health status

### OASIS API Tests

#### Test 4: Get Avatar (if you have test data)
```bash
echo '{"jsonrpc":"2.0","id":3,"method":"tools/call","params":{"name":"oasis_get_avatar","arguments":{"username":"testuser"}}}' | npm run dev
```

#### Test 5: Register Avatar
```bash
echo '{"jsonrpc":"2.0","id":4,"method":"tools/call","params":{"name":"oasis_register_avatar","arguments":{"username":"testuser","email":"test@example.com","password":"test123"}}}' | npm run dev
```

### Smart Contract Tests

#### Test 6: Generate Contract
```bash
cat > test-spec.json << 'EOF'
{
  "programName": "test_contract",
  "instructions": [
    {
      "name": "initialize",
      "params": []
    }
  ]
}
EOF

echo '{"jsonrpc":"2.0","id":5,"method":"tools/call","params":{"name":"scgen_generate_contract","arguments":{"jsonSpec":'$(cat test-spec.json)',"blockchain":"Solana"}}}' | npm run dev
```

#### Test 7: Get Cache Stats
```bash
echo '{"jsonrpc":"2.0","id":6,"method":"tools/call","params":{"name":"scgen_get_cache_stats","arguments":{}}}' | npm run dev
```

## 🔍 Debugging

### Check Logs

**MCP Server Logs:**
```bash
npm run dev
# Watch for errors in stderr
```

**Cursor Logs:**
- View → Output → MCP
- Look for connection errors

### Common Issues

#### Issue: "Cannot find module"
**Fix:**
```bash
cd /Users/maxgershfield/OASIS_CLEAN/MCP
rm -rf node_modules dist
npm install
npm run build
```

#### Issue: "OASIS API connection failed"
**Fix:**
```bash
# Check if OASIS API is running
curl http://localhost:5000/api/health

# Check .env file
cat .env
```

#### Issue: "MCP server not found in Cursor"
**Fix:**
1. Verify path in `~/.cursor/mcp.json` is absolute
2. Check file exists: `ls -la /Users/maxgershfield/OASIS_CLEAN/MCP/dist/index.js`
3. Restart Cursor completely

#### Issue: "Tool not found"
**Fix:**
- Rebuild: `npm run build`
- Restart MCP server
- Check tool name matches exactly

## 🧪 Automated Test Script

Create a comprehensive test script:

```bash
cat > test-all.sh << 'EOF'
#!/bin/bash

echo "🧪 Testing MCP Server..."
echo ""

# Test 1: Build
echo "1. Building..."
npm run build || exit 1
echo "✅ Build successful"
echo ""

# Test 2: List tools
echo "2. Testing tool list..."
RESULT=$(echo '{"jsonrpc":"2.0","id":1,"method":"tools/list","params":{}}' | timeout 5 npm run dev 2>/dev/null | grep -o '"name":"[^"]*"' | wc -l)
if [ "$RESULT" -gt "0" ]; then
  echo "✅ Tools listed: $RESULT tools found"
else
  echo "❌ No tools found"
  exit 1
fi
echo ""

# Test 3: Health check
echo "3. Testing health check..."
HEALTH=$(echo '{"jsonrpc":"2.0","id":2,"method":"tools/call","params":{"name":"oasis_health_check","arguments":{}}}' | timeout 5 npm run dev 2>/dev/null | grep -o '"status":"[^"]*"')
if [ -n "$HEALTH" ]; then
  echo "✅ Health check working: $HEALTH"
else
  echo "⚠️  Health check may have failed (check OASIS API)"
fi
echo ""

echo "🎉 Basic tests complete!"
EOF

chmod +x test-all.sh
./test-all.sh
```

## 📊 Test Results Template

```
MCP Server Test Results
======================

Date: ___________
Tester: ___________

Prerequisites:
[ ] OASIS API running
[ ] SmartContractGenerator API running
[ ] MCP server built
[ ] Cursor configured

Basic Tests:
[ ] Server starts without errors
[ ] Tools list returns 19 tools
[ ] Health check works
[ ] OASIS tools respond
[ ] Smart Contract tools respond

Cursor Integration:
[ ] MCP connects in Cursor
[ ] Can call tools from chat
[ ] Responses are formatted correctly

Issues Found:
_________________________________
_________________________________
_________________________________

Notes:
_________________________________
_________________________________
```

## 🚀 Quick Test Commands

```bash
# Quick health check
echo '{"jsonrpc":"2.0","id":1,"method":"tools/call","params":{"name":"oasis_health_check","arguments":{}}}' | npm run dev

# List all tools
echo '{"jsonrpc":"2.0","id":1,"method":"tools/list","params":{}}' | npm run dev | grep -o '"name":"[^"]*"'

# Test smart contract cache
echo '{"jsonrpc":"2.0","id":1,"method":"tools/call","params":{"name":"scgen_get_cache_stats","arguments":{}}}' | npm run dev
```

## 💡 Pro Tips

1. **Start Simple:** Test health check first
2. **Check Logs:** Always watch stderr for errors
3. **Test One Tool at a Time:** Easier to debug
4. **Verify APIs:** Make sure OASIS/SmartContract APIs are running
5. **Use Cursor Output:** View → Output → MCP shows detailed logs





















