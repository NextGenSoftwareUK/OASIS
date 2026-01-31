# Remotion Cloud API - Quick Start Guide

## Three Options for Using Remotion

### ✅ Option 1: Simple Node.js Service (Recommended for Getting Started)

**Best for:** Development, testing, quick setup

**Setup:**
```bash
cd remotion-service
npm install remotion @remotion/bundler @remotion/renderer express dotenv
npm install -D @types/express @types/node typescript tsx
```

**Use in C#:**
```csharp
// Register service in Startup.cs
services.AddScoped<IRemotionService, RemotionHttpService>();

// Set environment variable
export REMOTION_SERVICE_URL="http://localhost:3001"
```

**Pros:**
- ✅ No cloud account needed
- ✅ Easy to set up (5 minutes)
- ✅ Full control
- ✅ Free (self-hosted)

**Cons:**
- ❌ You manage the server
- ❌ Limited scalability

---

### Option 2: Remotion Lambda (AWS)

**Best for:** Production, high volume, AWS users

**Setup:**
1. Install AWS SDK: `dotnet add package AWSSDK.Lambda`
2. Deploy Remotion site to S3
3. Deploy Lambda function
4. Configure AWS credentials

**Use in C#:**
```csharp
services.AddScoped<IRemotionService, RemotionLambdaService>();
```

**Pros:**
- ✅ Highly scalable
- ✅ Pay per use
- ✅ Managed infrastructure

**Cons:**
- ❌ Requires AWS setup
- ❌ More complex configuration

---

### Option 3: Remotion Cloud Run (GCP)

**Best for:** Production, GCP users

**Setup:**
1. Install `@remotion/cloudrun` package
2. Set up GCP project
3. Deploy to Cloud Run

**Pros:**
- ✅ Scalable
- ✅ Managed by GCP

**Cons:**
- ❌ Requires GCP setup
- ❌ Currently in Alpha

---

## Recommended: Start with Option 1

### Step-by-Step Setup

#### 1. Install Node.js Service

```bash
cd remotion-service
npm install remotion @remotion/bundler @remotion/renderer express dotenv
npm install -D @types/express @types/node typescript tsx
```

#### 2. Create Server File

Create `src/server.ts` (see `Docs/REMOTION_CLOUD_API_GUIDE.md` for full code)

#### 3. Create Template

Create `src/templates/TerminalAnimation.tsx` (see `Docs/REMOTION_IMPLEMENTATION_EXAMPLE.md`)

#### 4. Run Service

```bash
npm run dev
```

#### 5. Configure C# API

In `Startup.cs`:
```csharp
services.AddScoped<IRemotionService, RemotionHttpService>();
```

Set environment variable:
```bash
export REMOTION_SERVICE_URL="http://localhost:3001"
```

#### 6. Use in Your Code

```csharp
// In RemotionController or NftController
var renderResult = await _remotionService.RenderCompositionAsync(
    "TerminalAnimation",
    new Dictionary<string, object>
    {
        { "command", "npx skills add remotion-dev/skills" },
        { "output", new[] { "Installing...", "Complete!" } }
    }
);

if (!renderResult.IsError)
{
    var videoPath = renderResult.Result;
    // Upload to IPFS and mint NFT
}
```

## API Usage Examples

### Render a Video

```bash
POST http://localhost:3001/render
Content-Type: application/json

{
  "templateName": "TerminalAnimation",
  "parameters": {
    "command": "npx skills add remotion-dev/skills",
    "output": ["Installing...", "Complete!"]
  }
}
```

### List Templates

```bash
GET http://localhost:3001/templates
```

### Mint NFT with Remotion Video

```bash
POST /api/remotion/mint-nft
Authorization: Bearer <token>

{
  "remotionTemplate": "TerminalAnimation",
  "remotionParameters": {
    "command": "npx skills add remotion-dev/skills",
    "output": ["✓ Installed", "✓ Ready"]
  },
  "title": "My Animated NFT",
  "description": "Animated terminal",
  "onChainProvider": "SolanaOASIS"
}
```

## Environment Variables

```bash
# Required
REMOTION_SERVICE_URL=http://localhost:3001

# Optional
REMOTION_OUTPUT_DIR=./NFT_Content/remotion-videos
REMOTION_TIMEOUT=600000  # 10 minutes in milliseconds
```

## Troubleshooting

### Service Not Found
- Make sure Node.js service is running: `npm run dev`
- Check `REMOTION_SERVICE_URL` environment variable
- Verify port 3001 is not in use

### Rendering Fails
- Check Node.js service logs
- Verify template exists in `src/templates/`
- Ensure all template parameters are provided

### Timeout Errors
- Increase timeout: `REMOTION_TIMEOUT=900000` (15 minutes)
- Check video duration (longer videos take more time)

## Next Steps

1. ✅ Set up Node.js service (Option 1)
2. ✅ Create first template
3. ✅ Test rendering
4. ✅ Integrate with NFT minting
5. ⬆️ Scale to Lambda/Cloud Run if needed

## Files Created

- `RemotionHttpService.cs` - Simple HTTP-based service (use this!)
- `RemotionLambdaService.cs` - AWS Lambda service (for production)
- `Docs/REMOTION_CLOUD_API_GUIDE.md` - Full documentation
- `remotion-service/README.md` - Service setup guide

## Summary

**For now:** Use `RemotionHttpService` with a simple Node.js service
**Later:** Migrate to `RemotionLambdaService` for production scale

The HTTP service is the easiest way to get started and works perfectly for development and moderate production use.
