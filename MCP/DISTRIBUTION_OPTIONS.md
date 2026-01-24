# MCP Server Distribution Options

## Current Status

The MCP server is **not yet published** to NPM. Users need to install from source.

## Distribution Options

### Option 1: GitHub Releases (Recommended for Now)

**Pros:**
- Quick to set up
- No NPM account needed
- Can include license keys in release notes
- Version control

**Steps:**
1. Build: `npm run build`
2. Create GitHub release
3. Upload `dist/` folder as ZIP
4. Include installation instructions

**User Installation:**
```bash
# Download from GitHub release
# Extract to ~/oasis-mcp-server/
# Configure IDE with path to dist/index.js
```

### Option 2: NPM Package (Future)

**Pros:**
- Standard installation method
- Easy updates (`npm update`)
- Version management
- Works with `npm link` for development

**Steps:**
1. Update `package.json` with proper metadata
2. Build: `npm run build`
3. Test: `npm link` locally
4. Publish: `npm publish --access restricted`

**User Installation:**
```bash
npm install -g @oasis-unified/mcp-server
```

### Option 3: GitHub Clone (Current Method)

**Pros:**
- Always up-to-date
- Easy for developers
- Can contribute back

**Cons:**
- Requires Git
- Users need to build themselves
- More complex for non-developers

**User Installation:**
```bash
git clone https://github.com/NextGenSoftwareUK/OASIS.git
cd OASIS/MCP
npm install
npm run build
```

### Option 4: Docker (Optional)

**Pros:**
- Consistent environment
- Easy deployment
- No Node.js installation needed

**Cons:**
- More complex setup
- Requires Docker knowledge

## Recommended Approach

**Phase 1 (Now):** GitHub Releases
- Create releases with pre-built `dist/` folder
- Users download ZIP and extract
- Simple installation instructions

**Phase 2 (Soon):** NPM Package
- Publish to NPM as `@oasis-unified/mcp-server`
- Update installation guide
- One-command install

## Quick Setup Script

Create a simple install script for users:

```bash
#!/bin/bash
# install-oasis-mcp.sh

echo "Installing OASIS MCP Server..."

# Download from GitHub release
# Extract
# Configure
# Done
```

## Next Steps

1. ✅ Update website installation guide (done)
2. ⏳ Create GitHub release with dist folder
3. ⏳ Publish to NPM when ready
4. ⏳ Update installation guide when NPM is live
