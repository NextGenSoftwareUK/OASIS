# Remotion NFT Integration - Quick Start

## Overview

This integration adds **Remotion** (React-based video creation) to your NFT API, enabling programmatic generation of tokenized animations.

## What is Remotion?

Remotion lets you create videos using React components. Based on the [example gist](https://gist.github.com/JonnyBurger/5b801182176f1b76447901fbeb5a84ac), you can create:

- ✅ Terminal typing animations
- ✅ Logo reveals with text
- ✅ 3D transforms and rotations
- ✅ Spring animations
- ✅ Complex sequences

## Key Benefits

1. **Deterministic** - Same inputs = same video (perfect for NFTs)
2. **Programmatic** - Generate via API with parameters
3. **Composable** - Build complex animations from templates
4. **Professional** - High-quality, smooth animations

## Architecture

```
┌─────────────────┐
│   C# API        │
│  RemotionController │
└────────┬────────┘
         │ HTTP
         ▼
┌─────────────────┐
│  Node.js Service│
│  Remotion Renderer│
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│  Rendered MP4   │
│  Upload to IPFS │
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│  NFT Minted    │
│  with Video URL│
└─────────────────┘
```

## Files Created

1. **Integration Plan**: `Docs/REMOTION_NFT_INTEGRATION_PLAN.md`
   - Complete architecture overview
   - Implementation phases
   - Comparison with LTX.io

2. **Service Interface**: `ONODE/.../Services/IRemotionService.cs`
   - Service contract for Remotion operations

3. **Controller**: `ONODE/.../Controllers/RemotionController.cs`
   - API endpoints for rendering and minting

4. **Implementation Example**: `Docs/REMOTION_IMPLEMENTATION_EXAMPLE.md`
   - Node.js service code
   - Template examples
   - C# integration

## Quick Implementation Steps

### 1. Set Up Node.js Remotion Service

```bash
# Create new directory
mkdir remotion-service
cd remotion-service

# Initialize Node.js project
npm init -y
npm install remotion express dotenv
npm install -D @types/express @types/node typescript tsx

# Create basic structure
mkdir -p src/templates
```

### 2. Create First Template

Copy the Terminal Animation template from `REMOTION_IMPLEMENTATION_EXAMPLE.md` to `src/templates/TerminalAnimation.tsx`

### 3. Implement C# Service

Implement `IRemotionService` in `RemotionService.cs` (see example in `REMOTION_IMPLEMENTATION_EXAMPLE.md`)

### 4. Register Service

In `Startup.cs`:

```csharp
services.AddScoped<IRemotionService, RemotionService>();
```

### 5. Test Endpoint

```bash
POST /api/remotion/render
{
  "templateName": "TerminalAnimation",
  "parameters": {
    "command": "npx skills add remotion-dev/skills",
    "output": ["Installing...", "Complete!"]
  }
}
```

## Example Use Cases

### 1. Terminal Animation NFT

```json
POST /api/remotion/mint-nft
{
  "remotionTemplate": "TerminalAnimation",
  "remotionParameters": {
    "command": "npx skills add remotion-dev/skills",
    "output": ["✓ Installed", "✓ Ready to use"]
  },
  "title": "Skills Installation",
  "description": "Animated terminal showing skills installation"
}
```

### 2. Logo Reveal NFT

```json
{
  "remotionTemplate": "LogoReveal",
  "remotionParameters": {
    "logoUrl": "https://example.com/logo.png",
    "text": "Welcome to OASIS",
    "duration": 5
  },
  "title": "OASIS Logo Reveal"
}
```

## Integration with Existing NFT Flow

The Remotion integration works alongside your existing NFT minting:

1. **Render Video** → Remotion service generates MP4
2. **Upload to IPFS** → Use existing `IPFSOASIS` or `PinataOASIS`
3. **Mint NFT** → Use existing `NFTManager.MintNftAsync` with video URL

## Comparison: Remotion vs LTX.io

| Feature | Remotion | LTX.io |
|---------|----------|--------|
| **Type** | Programmatic | AI-Generated |
| **Deterministic** | ✅ Yes | ❌ No |
| **Best For** | Structured animations | Creative content |
| **Use Case** | Data viz, UI animations | Artistic videos |

**Recommendation**: Use both! They complement each other.

## Next Steps

1. ✅ Review integration plan
2. ✅ Set up Node.js service
3. ✅ Create first template
4. ✅ Test rendering
5. ✅ Integrate with NFT minting
6. ✅ Add IPFS upload
7. ✅ Create more templates

## Resources

- [Remotion Docs](https://www.remotion.dev/docs)
- [Example Gist](https://gist.github.com/JonnyBurger/5b801182176f1b76447901fbeb5a84ac)
- [Integration Plan](./REMOTION_NFT_INTEGRATION_PLAN.md)
- [Implementation Example](./REMOTION_IMPLEMENTATION_EXAMPLE.md)

## Questions?

See the detailed documentation:
- **Architecture & Planning**: `REMOTION_NFT_INTEGRATION_PLAN.md`
- **Code Examples**: `REMOTION_IMPLEMENTATION_EXAMPLE.md`
