# Remotion Implementation Example

This document shows a concrete example of how to implement Remotion integration based on the [gist example](https://gist.github.com/JonnyBurger/5b801182176f1b76447901fbeb5a84ac).

## Node.js Remotion Service

Create a separate Node.js service that handles Remotion rendering:

### Project Structure

```
remotion-service/
├── src/
│   ├── templates/
│   │   ├── TerminalAnimation.tsx
│   │   ├── LogoReveal.tsx
│   │   └── index.ts
│   ├── renderer.ts
│   ├── server.ts
│   └── types.ts
├── package.json
├── tsconfig.json
└── Dockerfile
```

### package.json

```json
{
  "name": "oasis-remotion-service",
  "version": "1.0.0",
  "scripts": {
    "dev": "tsx watch src/server.ts",
    "build": "tsc",
    "start": "node dist/server.js",
    "render": "remotion render"
  },
  "dependencies": {
    "remotion": "^4.0.0",
    "express": "^4.18.0",
    "dotenv": "^16.0.0"
  },
  "devDependencies": {
    "@types/express": "^4.17.0",
    "@types/node": "^20.0.0",
    "tsx": "^4.0.0",
    "typescript": "^5.0.0"
  }
}
```

### Terminal Animation Template (Based on Gist)

```typescript
// src/templates/TerminalAnimation.tsx
import { AbsoluteFill, Sequence, useCurrentFrame, useVideoConfig, interpolate, spring } from "remotion";

interface TerminalParams {
  command: string;
  output?: string[];
  width?: number;
  height?: number;
  backgroundColor?: string;
  textColor?: string;
}

export const TerminalAnimation: React.FC<TerminalParams> = ({
  command = "npx skills add remotion-dev/skills",
  output = [],
  width = 1080,
  height = 700,
  backgroundColor = "#f8fafc",
  textColor = "#333"
}) => {
  const frame = useCurrentFrame();
  const { fps, durationInFrames } = useVideoConfig();

  // Spring animation for slide-in
  const slideIn = spring({
    frame,
    fps,
    config: {
      damping: 200,
      stiffness: 100,
    },
  });

  const translateY = interpolate(slideIn, [0, 1], [700, 100]);
  const rotateY = interpolate(frame, [0, durationInFrames], [10, -10]);
  const scale = interpolate(frame, [0, durationInFrames], [0.9, 1]);

  return (
    <AbsoluteFill style={{ backgroundColor, perspective: 1000 }}>
      <Sequence
        durationInFrames={durationInFrames}
        style={{
          transform: `translateY(${translateY}px) rotateX(20deg) rotateY(${rotateY}deg) scale(${scale})`,
        }}
      >
        <MacTerminal command={command} output={output} textColor={textColor} />
      </Sequence>
    </AbsoluteFill>
  );
};

// Mac Terminal Component
const MacTerminal: React.FC<{ command: string; output: string[]; textColor: string }> = ({
  command,
  output,
  textColor
}) => {
  return (
    <AbsoluteFill className="p-8">
      <div className="w-full h-full flex flex-col rounded-xl overflow-hidden shadow-2xl">
        {/* Title bar */}
        <div className="h-14 bg-[#f6f6f6] flex items-center px-5 border-b border-[#e0e0e0]">
          <div className="flex gap-2.5">
            <div className="w-4 h-4 rounded-full bg-[#ff5f57]" />
            <div className="w-4 h-4 rounded-full bg-[#febc2e]" />
            <div className="w-4 h-4 rounded-full bg-[#28c840]" />
          </div>
          <div className="flex-1 text-center">
            <span className="text-[#4d4d4d] text-base font-medium">Terminal</span>
          </div>
          <div className="w-16" />
        </div>
        <TerminalContent command={command} output={output} textColor={textColor} />
      </div>
    </AbsoluteFill>
  );
};

// Terminal Content Component
const TerminalContent: React.FC<{ command: string; output: string[]; textColor: string }> = ({
  command,
  output,
  textColor
}) => {
  const frame = useCurrentFrame();
  const { fps } = useVideoConfig();
  
  const charsPerSecond = 15;
  const framesPerChar = fps / charsPerSecond;
  const typingEndFrame = command.length * framesPerChar;
  const outputStartFrame = typingEndFrame + fps * 0.5;

  const visibleChars = Math.floor(
    interpolate(frame, [0, typingEndFrame], [0, command.length], {
      extrapolateRight: "clamp",
    })
  );

  const displayedText = command.slice(0, visibleChars);
  const isTyping = visibleChars < command.length;
  const showOutput = frame >= outputStartFrame;

  const framesPerLine = fps * 0.05; // 50ms per line
  const linesStartFrame = outputStartFrame + framesPerLine;

  const visibleLines = Math.floor(
    interpolate(
      frame,
      [linesStartFrame, linesStartFrame + output.length * framesPerLine],
      [0, output.length],
      { extrapolateLeft: "clamp", extrapolateRight: "clamp" }
    )
  );

  return (
    <div className="flex-1 bg-white p-6 font-mono text-4xl overflow-hidden">
      <div className="flex items-center" style={{ color: textColor }}>
        <span className="text-[#2ecc71] font-semibold">~</span>
        <span className="mx-2">$</span>
        <span>{displayedText}</span>
        {!showOutput && <Cursor blinking={!isTyping} />}
      </div>

      {showOutput && (
        <div className="mt-4 text-lg leading-tight">
          {output.slice(0, visibleLines).map((line, i) => (
            <div key={i} style={{ color: textColor }}>
              {line}
            </div>
          ))}
        </div>
      )}
    </div>
  );
};

// Cursor Component
const Cursor: React.FC<{ blinking: boolean }> = ({ blinking }) => {
  const frame = useCurrentFrame();
  const { fps } = useVideoConfig();
  
  const opacity = blinking
    ? interpolate(frame % (fps * 0.5), [0, fps * 0.25], [1, 0], {
        extrapolateRight: "clamp",
      })
    : 1;

  return (
    <span
      className="w-4 h-10 bg-[#333] ml-0.5 inline-block"
      style={{ opacity }}
    />
  );
};
```

### Renderer Service

```typescript
// src/renderer.ts
import { bundle } from "@remotion/bundler";
import { renderMedia, selectComposition } from "@remotion/renderer";
import { webpack } from "@remotion/bundler";
import path from "path";
import fs from "fs/promises";

export async function renderRemotionComposition(
  templateName: string,
  parameters: Record<string, any>,
  outputPath: string,
  options: {
    width?: number;
    height?: number;
    fps?: number;
    durationInFrames?: number;
  } = {}
): Promise<string> {
  const entryPoint = path.resolve(__dirname, "templates", `${templateName}.tsx`);
  
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
  if (options.width) composition.width = options.width;
  if (options.height) composition.height = options.height;
  if (options.fps) composition.fps = options.fps;
  if (options.durationInFrames) composition.durationInFrames = options.durationInFrames;

  // Render to file
  await renderMedia({
    composition,
    serveUrl: bundled,
    codec: "h264",
    outputLocation: outputPath,
    inputProps: parameters,
  });

  return outputPath;
}
```

### Express Server

```typescript
// src/server.ts
import express from "express";
import { renderRemotionComposition } from "./renderer";
import path from "path";
import fs from "fs/promises";

const app = express();
app.use(express.json());

const PORT = process.env.PORT || 3001;
const OUTPUT_DIR = process.env.OUTPUT_DIR || "./rendered-videos";

// Ensure output directory exists
await fs.mkdir(OUTPUT_DIR, { recursive: true });

app.post("/render", async (req, res) => {
  try {
    const { templateName, parameters, options } = req.body;

    if (!templateName) {
      return res.status(400).json({ error: "templateName is required" });
    }

    const outputPath = path.join(
      OUTPUT_DIR,
      `${templateName}-${Date.now()}.mp4`
    );

    await renderRemotionComposition(templateName, parameters, outputPath, options);

    res.json({
      success: true,
      outputPath,
      videoUrl: `/videos/${path.basename(outputPath)}`,
    });
  } catch (error) {
    console.error("Rendering error:", error);
    res.status(500).json({ error: error.message });
  }
});

app.get("/templates", async (req, res) => {
  // Return list of available templates
  res.json({
    templates: [
      {
        name: "TerminalAnimation",
        description: "Terminal typing animation with output",
        parameters: {
          command: { type: "string", required: true },
          output: { type: "array", required: false },
          width: { type: "number", default: 1080 },
          height: { type: "number", default: 700 },
        },
      },
      // ... more templates
    ],
  });
});

app.listen(PORT, () => {
  console.log(`Remotion service running on port ${PORT}`);
});
```

## C# Service Implementation

```csharp
// ONODE/NextGenSoftware.OASIS.API.ONODE.Core/Services/RemotionService.cs
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Helpers;
using Newtonsoft.Json;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Services
{
    public class RemotionService : IRemotionService
    {
        private readonly string _remotionServiceUrl;
        private readonly string _outputDirectory;

        public RemotionService()
        {
            _remotionServiceUrl = Environment.GetEnvironmentVariable("REMOTION_SERVICE_URL") 
                ?? "http://localhost:3001";
            _outputDirectory = Path.Combine(
                Environment.GetEnvironmentVariable("NFT_CONTENT_DIR") ?? "./NFT_Content",
                "remotion-videos"
            );
            
            Directory.CreateDirectory(_outputDirectory);
        }

        public async Task<OASISResult<string>> RenderCompositionAsync(
            string templateName,
            Dictionary<string, object> parameters,
            string outputPath = null)
        {
            var result = new OASISResult<string>();

            try
            {
                // Call Node.js Remotion service
                using (var client = new System.Net.Http.HttpClient())
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
                    var content = new System.Net.Http.StringContent(
                        json,
                        System.Text.Encoding.UTF8,
                        "application/json"
                    );

                    var response = await client.PostAsync(
                        $"{_remotionServiceUrl}/render",
                        content
                    );

                    if (!response.IsSuccessStatusCode)
                    {
                        result.IsError = true;
                        result.Message = await response.Content.ReadAsStringAsync();
                        return result;
                    }

                    var responseJson = await response.Content.ReadAsStringAsync();
                    var responseObj = JsonConvert.DeserializeObject<dynamic>(responseJson);

                    result.Result = responseObj.outputPath.ToString();
                    result.Message = "Remotion composition rendered successfully";
                }
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error rendering Remotion composition: {ex.Message}";
            }

            return result;
        }

        public async Task<OASISResult<List<RemotionTemplateInfo>>> GetAvailableTemplatesAsync()
        {
            // Implementation to fetch templates from Remotion service
            // ...
        }

        public async Task<OASISResult<RemotionTemplateInfo>> GetTemplateInfoAsync(string templateName)
        {
            // Implementation to fetch template info
            // ...
        }

        public async Task<OASISResult<bool>> ValidateParametersAsync(
            string templateName,
            Dictionary<string, object> parameters)
        {
            // Implementation to validate parameters
            // ...
        }
    }
}
```

## Usage Example

### API Request

```bash
POST /api/remotion/mint-nft
Content-Type: application/json
Authorization: Bearer <token>

{
  "remotionTemplate": "TerminalAnimation",
  "remotionParameters": {
    "command": "npx skills add remotion-dev/skills",
    "output": [
      "Installing remotion-dev/skills...",
      "✓ Repository cloned",
      "✓ Found 1 skill",
      "✓ Installation complete"
    ],
    "width": 1080,
    "height": 700
  },
  "title": "Skills Installation Animation",
  "description": "An animated NFT showing the skills installation process",
  "onChainProvider": "SolanaOASIS",
  "offChainProvider": "IPFSOASIS"
}
```

### Response

```json
{
  "isError": false,
  "result": {
    "id": "guid-here",
    "title": "Skills Installation Animation",
    "imageUrl": "https://gateway.pinata.cloud/ipfs/Qm...",
    "onChainHash": "hash-here"
  },
  "message": "NFT minted successfully"
}
```

## Next Steps

1. Set up Node.js Remotion service
2. Create initial templates
3. Integrate with NFT minting flow
4. Add IPFS/Pinata upload
5. Test end-to-end workflow
