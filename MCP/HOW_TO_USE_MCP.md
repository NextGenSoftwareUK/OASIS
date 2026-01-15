# How to Use the OASIS Unified MCP

## Understanding MCP in Cursor

The Model Context Protocol (MCP) allows Cursor to connect to external services and expose their capabilities as **tools** that I (the AI assistant) can use when you ask me to do things.

## How It Works

1. **MCP Server** (`/Users/maxgershfield/OASIS_CLEAN/MCP/dist/index.js`)
   - Runs as a background process when Cursor starts
   - Exposes tools like `oasis_health_check`, `oasis_get_avatar`, `oasis_mint_nft`, etc.

2. **Cursor Integration**
   - Cursor reads `~/.cursor/mcp.json` on startup
   - Connects to the MCP server via stdio
   - Makes the tools available to me (the AI)

3. **When You Ask Me to Do Something**
   - You: "Check the OASIS API health status"
   - Cursor should automatically use the `oasis_health_check` tool
   - I get the result and respond to you

## Verifying MCP is Working

### 1. Check MCP Server Status in Cursor

1. Open Cursor Settings (Cmd+,)
2. Look for "MCP" or "Model Context Protocol" section
3. You should see `oasis-unified` listed as a connected server
4. Check for any error messages

### 2. Check Cursor Logs

1. Open Cursor's Developer Tools (Help → Toggle Developer Tools)
2. Look for MCP-related logs
3. Check for errors like "Invalid package config" or connection issues

### 3. Test MCP Tools Directly

You can test if the MCP server is working by running it manually:

```bash
cd /Users/maxgershfield/OASIS_CLEAN/MCP
node dist/index.js
```

If it starts without errors and shows:
```
[MCP] OASIS Unified MCP Server started
[MCP] OASIS API URL: http://127.0.0.1:5003
[MCP] Smart Contract API URL: http://127.0.0.1:5001
```

Then the server is working correctly.

## Current Configuration

Your `~/.cursor/mcp.json` should look like this:

```json
{
  "mcpServers": {
    "oasis-unified": {
      "command": "node",
      "args": ["/Users/maxgershfield/OASIS_CLEAN/MCP/dist/index.js"],
      "env": {
        "OASIS_API_URL": "http://127.0.0.1:5003",
        "SMART_CONTRACT_API_URL": "http://127.0.0.1:5001",
        "OASIS_API_KEY": ""
      }
    }
  }
}
```

## Troubleshooting

### If MCP Tools Aren't Available

1. **Restart Cursor completely** (Cmd+Q, then reopen)
   - MCP servers are loaded on startup
   - Changes to `mcp.json` require a full restart

2. **Check the MCP server builds correctly:**
   ```bash
   cd /Users/maxgershfield/OASIS_CLEAN/MCP
   npm run build
   ```

3. **Verify APIs are running:**
   ```bash
   curl http://127.0.0.1:5003/api/health
   curl http://127.0.0.1:5001/api/v1/contracts/cache-stats
   ```

4. **Check for errors in Cursor's MCP logs:**
   - Look for the MCP server name in Cursor's logs
   - Check for connection errors or "Invalid package config" messages

### If I Still Use curl Instead of MCP Tools

If you notice I'm using `curl` or `run_terminal_cmd` instead of MCP tools, it means:
- The MCP tools aren't available to me yet
- Cursor hasn't successfully connected to the MCP server
- There may be a configuration issue

**Solution:** Restart Cursor and check the MCP connection status.

## Available MCP Tools

Once MCP is working, I'll be able to use these tools automatically:

### OASIS Tools
- `oasis_health_check` - Check API health
- `oasis_get_avatar` - Get avatar info
- `oasis_get_karma` - Get karma score
- `oasis_get_nfts` - List NFTs
- `oasis_mint_nft` - Mint new NFT
- `oasis_register_avatar` - Create avatar
- `oasis_save_holon` - Save data object
- And more...

### Smart Contract Tools
- `scgen_generate_contract` - Generate contract
- `scgen_compile_contract` - Compile contract
- `scgen_deploy_contract` - Deploy contract
- And more...

## Example Usage

Once MCP is working, you can ask me:

- "Check the OASIS API health status" → Uses `oasis_health_check`
- "Get avatar info for username 'testuser'" → Uses `oasis_get_avatar`
- "Mint an NFT with this metadata..." → Uses `oasis_mint_nft`
- "Generate a smart contract for..." → Uses `scgen_generate_contract`

And I'll automatically use the appropriate MCP tool instead of making direct API calls.
















