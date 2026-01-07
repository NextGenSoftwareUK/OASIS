# 🚀 Quick Start - Get MCP Server Running in 3 Steps

## Step 1: Install & Build (Already Done! ✅)

```bash
cd /Users/maxgershfield/OASIS_CLEAN/MCP
npm install  # ✅ Done
npm run build  # ✅ Done
```

## Step 2: Configure Cursor

Copy the configuration to your Cursor settings:

```bash
# Copy the config template
cp cursor-mcp-config.json ~/.cursor/mcp.json
```

Or manually create/edit `~/.cursor/mcp.json`:

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

## Step 3: Restart Cursor

1. **Quit Cursor completely** (Cmd+Q on Mac)
2. **Reopen Cursor**
3. Check connection: View → Output → Select "MCP" from dropdown

## ✅ Test It!

In Cursor, try asking:

- "Check OASIS API health"
- "Get avatar information for username testuser"
- "What's the karma score for avatar abc123?"

## 🐛 Troubleshooting

### MCP not connecting?

1. Verify the path exists:
   ```bash
   ls -la /Users/maxgershfield/OASIS_CLEAN/MCP/dist/index.js
   ```

2. Check Cursor logs: View → Output → MCP

3. Make sure OASIS API is running:
   ```bash
   curl http://localhost:5000/api/health
   ```

### Need to rebuild?

```bash
cd /Users/maxgershfield/OASIS_CLEAN/MCP
npm run build
```

## 🎉 You're Ready!

The MCP server is now set up and ready to use. You can now interact with OASIS through natural language in Cursor!





















