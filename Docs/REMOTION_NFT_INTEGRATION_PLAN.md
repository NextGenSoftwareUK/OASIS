# Remotion NFT Integration Plan

## Overview

This document outlines the integration of **Remotion** (React-based video creation library) into the OASIS NFT API to enable programmatic generation of tokenized animations.

## Why Remotion?

Based on the [gist example](https://gist.github.com/JonnyBurger/5b801182176f1b76447901fbeb5a84ac), Remotion offers:

✅ **Programmatic Video Creation** - Create videos using React components  
✅ **Rich Animations** - 3D transforms, spring animations, typewriter effects, etc.  
✅ **Composable** - Build complex animations from simple components  
✅ **Deterministic** - Same inputs = same output (perfect for NFTs)  
✅ **Flexible** - Can generate videos from JSON/config, making it API-friendly  

## Current State

### Existing Video Generation
- ✅ **LTX.io Integration** - Already integrated for AI-generated videos
- ✅ **VideoController** - `/api/video/generate` endpoint exists
- ✅ **NFT Minting** - Supports `ImageUrl` which can be a video URL
- ✅ **IPFS/Pinata** - Already used for storing NFT media

### What's Missing
- ❌ Programmatic video generation from templates/compositions
- ❌ React-based animation system
- ❌ Customizable animation parameters via API
- ❌ Remotion rendering service

## Architecture Proposal

### 1. Remotion Service Layer

Create a new service that wraps Remotion's rendering capabilities:

```
ONODE/NextGenSoftware.OASIS.API.ONODE.Core/Services/RemotionService.cs
```

**Responsibilities:**
- Accept animation configuration (JSON)
- Generate Remotion composition code dynamically
- Render video to MP4
- Handle file management and cleanup

### 2. Remotion Controller

Extend the existing `VideoController` or create a new `RemotionController`:

```
ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI/Controllers/RemotionController.cs
```

**Endpoints:**
- `POST /api/remotion/render` - Render a Remotion composition
- `POST /api/remotion/mint-nft` - Render and mint NFT in one call
- `GET /api/remotion/templates` - List available animation templates

### 3. Remotion Templates System

Create a library of reusable Remotion compositions:

```
ONODE/NextGenSoftware.OASIS.API.ONODE.Core/Templates/RemotionTemplates/
```

**Example Templates:**
- `TerminalAnimation` - Terminal typing animation (like in the gist)
- `LogoReveal` - Logo animation with text
- `DataVisualization` - Animated charts/graphs
- `TextAnimation` - Typewriter, fade-in, etc.
- `3DTransform` - 3D rotations and transforms

### 4. Integration with NFT Minting

Extend `MintNFTTransactionRequest` to support Remotion:

```csharp
public class MintNFTTransactionRequest
{
    // ... existing fields ...
    
    // Remotion-specific fields
    public string RemotionTemplate { get; set; } // e.g., "TerminalAnimation"
    public Dictionary<string, object> RemotionParams { get; set; } // Template parameters
    public bool RenderRemotionVideo { get; set; } // Flag to enable Remotion rendering
}
```

## Implementation Steps

### Phase 1: Remotion Infrastructure Setup

1. **Install Remotion Dependencies**
   - Create a Node.js service or use Remotion's programmatic API
   - Set up Remotion project structure
   - Configure rendering environment

2. **Create Remotion Service**
   ```csharp
   public class RemotionService
   {
       public async Task<OASISResult<string>> RenderCompositionAsync(
           string templateName,
           Dictionary<string, object> parameters,
           RenderOptions options
       );
   }
   ```

3. **Template System**
   - Create base template structure
   - Implement parameter injection system
   - Build template registry

### Phase 2: API Integration

1. **Remotion Controller**
   - Implement render endpoint
   - Add template listing
   - Error handling and validation

2. **NFT Integration**
   - Extend mint request model
   - Update `NftController.MintNftAsync` to handle Remotion
   - Auto-upload rendered video to IPFS/Pinata

### Phase 3: Template Library

1. **Core Templates**
   - Terminal animation (from gist example)
   - Logo reveal
   - Text animations
   - Basic 3D transforms

2. **Advanced Templates**
   - Data visualization
   - Interactive elements
   - Complex sequences

## Technical Implementation Details

### Remotion Rendering Options

**Option A: Node.js Service (Recommended)**
- Create separate Node.js service that handles Remotion rendering
- C# API calls Node.js service via HTTP or process invocation
- Pros: Native Remotion support, easier to maintain
- Cons: Requires Node.js runtime

**Option B: Embedded Remotion**
- Use Remotion's programmatic API from C#
- Requires .NET Node.js interop (e.g., Edge.js, Jint)
- Pros: Single codebase
- Cons: More complex, potential performance issues

**Option C: Docker Container**
- Run Remotion in Docker container
- C# API invokes container for rendering
- Pros: Isolated, scalable
- Cons: Infrastructure overhead

### Recommended: Option A (Node.js Service)

```
remotion-service/
├── src/
│   ├── templates/
│   │   ├── TerminalAnimation.tsx
│   │   ├── LogoReveal.tsx
│   │   └── ...
│   ├── renderer.ts
│   └── server.ts
├── package.json
└── Dockerfile
```

### API Request Example

```json
POST /api/remotion/mint-nft
{
  "title": "My Animated NFT",
  "description": "A tokenized animation",
  "remotionTemplate": "TerminalAnimation",
  "remotionParams": {
    "command": "npx skills add remotion-dev/skills",
    "output": ["Installing...", "Complete!"],
    "width": 1080,
    "height": 700,
    "duration": 8
  },
  "onChainProvider": "SolanaOASIS",
  "offChainProvider": "IPFSOASIS"
}
```

### Workflow

1. **User submits request** with Remotion template and parameters
2. **API validates** request and template
3. **Remotion service renders** video to MP4
4. **Video uploaded** to IPFS/Pinata
5. **NFT minted** with video URL as `ImageUrl`
6. **Response returned** with NFT details

## Template Examples

### Terminal Animation Template

```typescript
// Based on gist example
export const TerminalAnimation: React.FC<TerminalParams> = ({ command, output }) => {
  return (
    <AbsoluteFill>
      <MacTerminal>
        <TerminalContent command={command} output={output} />
      </MacTerminal>
    </AbsoluteFill>
  );
};
```

### Logo Reveal Template

```typescript
export const LogoReveal: React.FC<LogoParams> = ({ logoUrl, text, duration }) => {
  return (
    <Series>
      <Series.Sequence durationInFrames={60}>
        <AnnouncementText text={text} />
      </Series.Sequence>
      <Series.Sequence durationInFrames={120}>
        <Logos logoUrl={logoUrl} />
      </Series.Sequence>
    </Series>
  );
};
```

## Benefits

1. **Programmatic Animations** - Generate unique animations via API
2. **Deterministic** - Same parameters = same video (important for NFTs)
3. **Composable** - Build complex animations from templates
4. **Flexible** - Easy to add new templates
5. **Professional** - High-quality animations with smooth transitions

## Comparison with LTX.io

| Feature | Remotion | LTX.io |
|--------|----------|--------|
| **Type** | Programmatic (React) | AI-Generated |
| **Deterministic** | ✅ Yes | ❌ No |
| **Customization** | ✅ Full control | ⚠️ Limited |
| **Templates** | ✅ Reusable | ❌ One-off |
| **Use Case** | Structured animations | Creative/artistic |
| **Best For** | Data viz, UI animations | Creative content |

**Recommendation:** Use both! Remotion for structured, programmatic animations; LTX for creative, AI-generated content.

## Next Steps

1. **Proof of Concept**
   - Set up basic Remotion service
   - Create one template (Terminal Animation)
   - Test rendering and NFT minting flow

2. **Production Implementation**
   - Build full service architecture
   - Create template library
   - Integrate with NFT API
   - Add monitoring and error handling

3. **Documentation**
   - Template creation guide
   - API documentation
   - Example use cases

## Questions to Consider

1. **Rendering Performance**
   - How long does rendering take?
   - Should we queue long renders?
   - Cache rendered videos?

2. **Template Management**
   - User-uploaded templates?
   - Template marketplace?
   - Version control?

3. **Costs**
   - Compute resources for rendering
   - Storage for rendered videos
   - IPFS/Pinata upload costs

## References

- [Remotion Documentation](https://www.remotion.dev/docs)
- [Remotion Programmatic API](https://www.remotion.dev/docs/render)
- [Example Gist](https://gist.github.com/JonnyBurger/5b801182176f1b76447901fbeb5a84ac)
- [Current Video Generation](./LTX_VIDEO_GENERATION.md)
