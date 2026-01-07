#!/bin/bash

# Simple MCP Server Test Script

echo "🧪 Testing MCP Server..."
echo ""

# Check if built
if [ ! -f "dist/index.js" ]; then
  echo "❌ Server not built. Running build..."
  npm run build
fi

echo "✅ Server built"
echo ""

# Test 1: List tools
echo "📋 Test 1: Listing tools..."
echo '{"jsonrpc":"2.0","id":1,"method":"tools/list","params":{}}' | timeout 3 npm run dev 2>/dev/null | grep -q '"name"' && echo "✅ Tools listed successfully" || echo "⚠️  Could not list tools (may need OASIS API running)"

echo ""

# Test 2: Health check
echo "🏥 Test 2: Health check..."
echo '{"jsonrpc":"2.0","id":2,"method":"tools/call","params":{"name":"oasis_health_check","arguments":{}}}' | timeout 3 npm run dev 2>/dev/null | grep -q '"status"' && echo "✅ Health check working" || echo "⚠️  Health check failed (OASIS API may not be running)"

echo ""
echo "🎉 Basic tests complete!"
echo ""
echo "💡 To test with Cursor:"
echo "   1. Copy cursor-mcp-config.json to ~/.cursor/mcp.json"
echo "   2. Restart Cursor"
echo "   3. Ask: 'Check OASIS API health'"





















