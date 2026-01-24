# How to Distribute MCP Server

## Quick Distribution Steps

### 1. Build the Package

```bash
cd MCP
npm run build
```

### 2. Test Locally

```bash
npm link
# Then test: oasis-mcp
```

### 3. Publish to NPM (Private)

```bash
# Login to NPM
npm login

# Publish (requires NPM account with access)
npm publish --access restricted
```

### 4. Alternative: GitHub Releases

1. Create a release on GitHub
2. Upload `dist/` folder as ZIP
3. Include installation instructions

## For Users

### Install from NPM

```bash
npm install -g @oasis-unified/mcp-server
```

### Configure Cursor

Edit `~/.cursor/mcp.json`:

```json
{
  "mcpServers": {
    "oasis-unified": {
      "command": "oasis-mcp",
      "env": {
        "OASIS_API_URL": "https://api.oasisplatform.world",
        "OASIS_MCP_LICENSE_KEY": "your-key-here"
      }
    }
  }
}
```

### Get License Key

Visit: https://www.oasisweb4.com/products/mcp.html
