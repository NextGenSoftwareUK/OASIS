# Remotion Cloud API Integration Guide

## Overview

This guide shows how to use Remotion Cloud API to render videos without setting up a local Remotion service. There are two main approaches:

1. **Remotion Cloud Run** (GCP-based, requires GCP setup)
2. **Remotion Lambda** (AWS-based alternative)
3. **Simple Node.js Service** (Easiest, self-hosted)

## Option 1: Remotion Cloud Run API (GCP)

### Setup Requirements

- Google Cloud Platform account
- GCP project with billing enabled
- `@remotion/cloudrun` package

### Installation

```bash
npm install @remotion/cloudrun
```

### Configuration

Set up GCP credentials:

```bash
# Install GCP CLI
# https://cloud.google.com/sdk/docs/install

# Authenticate
gcloud auth login
gcloud config set project YOUR_PROJECT_ID
```

### Using from C# Service

Update `RemotionService.cs` to use Cloud Run API:

```csharp
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.Core.Helpers;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Services
{
    public class RemotionCloudService : IRemotionService
    {
        private readonly string _cloudRunServiceUrl;
        private readonly string _apiKey;
        private readonly HttpClient _httpClient;

        public RemotionCloudService()
        {
            _cloudRunServiceUrl = Environment.GetEnvironmentVariable("REMOTION_CLOUD_RUN_URL") 
                ?? "https://your-service.run.app";
            _apiKey = Environment.GetEnvironmentVariable("REMOTION_CLOUD_API_KEY");
            _httpClient = new HttpClient();
            
            if (!string.IsNullOrEmpty(_apiKey))
            {
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
            }
        }

        public async Task<OASISResult<string>> RenderCompositionAsync(
            string templateName,
            Dictionary<string, object> parameters,
            string outputPath = null)
        {
            var result = new OASISResult<string>();

            try
            {
                // Call Remotion Cloud Run API
                var request = new
                {
                    compositionId = templateName,
                    inputProps = parameters,
                    codec = "h264",
                    imageFormat = "jpeg",
                    crf = 18,
                    pixelFormat = "yuv420p",
                    proResProfile = "4444",
                    x264Preset = "medium",
                    jpegQuality = 80,
                    scale = 1,
                    everyNthFrame = 1,
                    numberOfGifLoops = 0,
                    frameRange = null as int?,
                    outName = outputPath ?? $"remotion-{DateTime.UtcNow.Ticks}.mp4",
                    timeoutInMilliseconds = 300000, // 5 minutes
                    chromiumOptions = new
                    {
                        ignoreDefaultArgs = new string[0],
                        args = new string[0]
                    },
                    envVariables = new Dictionary<string, string>(),
                    logLevel = "info"
                };

                var json = JsonConvert.SerializeObject(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(
                    $"{_cloudRunServiceUrl}/api/render",
                    content
                );

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    result.IsError = true;
                    result.Message = $"Remotion Cloud API error: {errorContent}";
                    return result;
                }

                var responseJson = await response.Content.ReadAsStringAsync();
                var responseObj = JsonConvert.DeserializeObject<dynamic>(responseJson);

                // Cloud Run returns a render ID, you need to poll for completion
                var renderId = responseObj.renderId?.ToString();
                
                if (string.IsNullOrEmpty(renderId))
                {
                    result.IsError = true;
                    result.Message = "No render ID returned from Remotion Cloud";
                    return result;
                }

                // Poll for render completion
                var videoUrl = await PollForRenderCompletion(renderId);
                
                if (string.IsNullOrEmpty(videoUrl))
                {
                    result.IsError = true;
                    result.Message = "Render timed out or failed";
                    return result;
                }

                // Download video to local path
                var localPath = await DownloadVideo(videoUrl, outputPath);
                
                result.Result = localPath;
                result.Message = "Remotion video rendered successfully via Cloud API";
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error rendering with Remotion Cloud: {ex.Message}";
            }

            return result;
        }

        private async Task<string> PollForRenderCompletion(string renderId, int maxAttempts = 60)
        {
            for (int i = 0; i < maxAttempts; i++)
            {
                await Task.Delay(5000); // Wait 5 seconds between polls

                var response = await _httpClient.GetAsync(
                    $"{_cloudRunServiceUrl}/api/render/{renderId}"
                );

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var status = JsonConvert.DeserializeObject<dynamic>(content);

                    if (status.status?.ToString() == "done")
                    {
                        return status.outputUrl?.ToString();
                    }
                    else if (status.status?.ToString() == "error")
                    {
                        throw new Exception($"Render failed: {status.error}");
                    }
                }
            }

            return null; // Timeout
        }

        private async Task<string> DownloadVideo(string videoUrl, string outputPath)
        {
            var response = await _httpClient.GetAsync(videoUrl);
            response.EnsureSuccessStatusCode();

            var videoBytes = await response.Content.ReadAsByteArrayAsync();
            var localPath = outputPath ?? $"./NFT_Content/remotion-{DateTime.UtcNow.Ticks}.mp4";

            System.IO.File.WriteAllBytes(localPath, videoBytes);
            return localPath;
        }

        // Implement other interface methods...
        public Task<OASISResult<List<RemotionTemplateInfo>>> GetAvailableTemplatesAsync()
        {
            // Return hardcoded templates or fetch from Cloud Run
            throw new NotImplementedException();
        }

        public Task<OASISResult<RemotionTemplateInfo>> GetTemplateInfoAsync(string templateName)
        {
            throw new NotImplementedException();
        }

        public Task<OASISResult<bool>> ValidateParametersAsync(string templateName, Dictionary<string, object> parameters)
        {
            throw new NotImplementedException();
        }
    }
}
```

## Option 2: Simple Node.js Service (Recommended - Easier)

This is simpler and doesn't require GCP setup. Create a lightweight Node.js service that uses Remotion's programmatic API.

### Setup

```bash
# Create service directory
mkdir remotion-service
cd remotion-service
npm init -y

# Install Remotion
npm install remotion @remotion/bundler @remotion/renderer express dotenv
npm install -D @types/express @types/node typescript tsx

# Create TypeScript config
npx tsc --init
```

### Simple Express Server

```typescript
// src/server.ts
import express from 'express';
import { bundle } from '@remotion/bundler';
import { renderMedia, selectComposition } from '@remotion/renderer';
import path from 'path';
import fs from 'fs/promises';

const app = express();
app.use(express.json());

const PORT = process.env.PORT || 3001;
const OUTPUT_DIR = process.env.OUTPUT_DIR || './rendered-videos';

// Ensure output directory exists
await fs.mkdir(OUTPUT_DIR, { recursive: true });

app.post('/render', async (req, res) => {
  try {
    const { templateName, parameters, options } = req.body;

    if (!templateName) {
      return res.status(400).json({ error: 'templateName is required' });
    }

    // Path to your Remotion project root
    const entryPoint = path.resolve(__dirname, '../src', `${templateName}.tsx`);
    
    // Bundle the composition
    const bundled = await bundle({
      entryPoint,
      webpackOverride: (config) => config,
    });

    // Select the composition
    const composition = await selectComposition({
      serveUrl: bundled,
      id: templateName,
      inputProps: parameters,
    });

    // Override options if provided
    if (options?.width) composition.width = options.width;
    if (options?.height) composition.height = options.height;
    if (options?.fps) composition.fps = options.fps;
    if (options?.durationInFrames) composition.durationInFrames = options.durationInFrames;

    // Generate output path
    const outputPath = path.join(
      OUTPUT_DIR,
      `${templateName}-${Date.now()}.mp4`
    );

    // Render to file
    await renderMedia({
      composition,
      serveUrl: bundled,
      codec: 'h264',
      outputLocation: outputPath,
      inputProps: parameters,
    });

    res.json({
      success: true,
      outputPath,
      videoUrl: `/videos/${path.basename(outputPath)}`,
    });
  } catch (error: any) {
    console.error('Rendering error:', error);
    res.status(500).json({ error: error.message });
  }
});

app.get('/templates', async (req, res) => {
  res.json({
    templates: [
      {
        name: 'TerminalAnimation',
        description: 'Terminal typing animation',
        parameters: {
          command: { type: 'string', required: true },
          output: { type: 'array', required: false },
        },
      },
    ],
  });
});

app.listen(PORT, () => {
  console.log(`Remotion service running on port ${PORT}`);
});
```

### C# Service Integration

```csharp
// RemotionService.cs - Simple HTTP approach
public class RemotionService : IRemotionService
{
    private readonly string _remotionServiceUrl;
    private readonly HttpClient _httpClient;

    public RemotionService()
    {
        _remotionServiceUrl = Environment.GetEnvironmentVariable("REMOTION_SERVICE_URL") 
            ?? "http://localhost:3001";
        _httpClient = new HttpClient();
    }

    public async Task<OASISResult<string>> RenderCompositionAsync(
        string templateName,
        Dictionary<string, object> parameters,
        string outputPath = null)
    {
        var result = new OASISResult<string>();

        try
        {
            var request = new
            {
                templateName,
                parameters,
                options = new
                {
                    width = 1080,
                    height = 700,
                    fps = 30
                }
            };

            var json = JsonConvert.SerializeObject(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(
                $"{_remotionServiceUrl}/render",
                content
            );

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                result.IsError = true;
                result.Message = $"Remotion service error: {errorContent}";
                return result;
            }

            var responseJson = await response.Content.ReadAsStringAsync();
            var responseObj = JsonConvert.DeserializeObject<dynamic>(responseJson);

            result.Result = responseObj.outputPath?.ToString();
            result.Message = "Remotion composition rendered successfully";
        }
        catch (Exception ex)
        {
            result.IsError = true;
            result.Message = $"Error rendering Remotion composition: {ex.Message}";
        }

        return result;
    }

    // ... other methods
}
```

## Option 3: Remotion Lambda (AWS)

Similar to Cloud Run but uses AWS Lambda. Requires AWS setup and `@remotion/lambda` package.

## Comparison

| Option | Setup Complexity | Cost | Scalability | Best For |
|--------|------------------|------|-------------|----------|
| **Cloud Run** | High (GCP setup) | Pay per render | High | Production, high volume |
| **Node.js Service** | Low | Self-hosted | Medium | Development, testing |
| **Lambda** | Medium (AWS setup) | Pay per render | High | Production, AWS users |

## Recommended Approach

**For Development/Testing:** Use Option 2 (Simple Node.js Service)
- Easy to set up
- No cloud account needed
- Full control
- Can run locally or deploy anywhere

**For Production:** Use Option 1 (Cloud Run) or Option 3 (Lambda)
- Better scalability
- Managed infrastructure
- Pay per use

## Quick Start (Node.js Service)

```bash
# 1. Install dependencies
cd remotion-service
npm install remotion @remotion/bundler @remotion/renderer express dotenv
npm install -D @types/express @types/node typescript tsx

# 2. Create server.ts (see above)

# 3. Create a template
# src/TerminalAnimation.tsx (from REMOTION_IMPLEMENTATION_EXAMPLE.md)

# 4. Run service
npm run dev

# 5. Test
curl -X POST http://localhost:3001/render \
  -H "Content-Type: application/json" \
  -d '{
    "templateName": "TerminalAnimation",
    "parameters": {
      "command": "npx skills add remotion-dev/skills"
    }
  }'
```

## Environment Variables

Add to your `.env` or `OASIS_DNA.json`:

```json
{
  "OASIS": {
    "Remotion": {
      "ServiceUrl": "http://localhost:3001",
      "OutputDirectory": "./NFT_Content/remotion-videos"
    }
  }
}
```

Or set environment variables:
```bash
export REMOTION_SERVICE_URL="http://localhost:3001"
export REMOTION_OUTPUT_DIR="./NFT_Content/remotion-videos"
```

## Next Steps

1. Choose an option (recommend Node.js service for now)
2. Set up the service
3. Create your first template
4. Test rendering
5. Integrate with NFT minting
