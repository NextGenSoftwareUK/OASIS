# Glif.app Image Generation Integration

## Overview

The MCP server now includes integration with **Glif.app API** for AI-powered image generation. This is the **easiest option** - it uses pre-built workflows, so you don't need to configure models or understand technical details.

## Why Glif.app is Easier

✅ **No model configuration** - Uses pre-built workflows  
✅ **Simple API** - Just provide a prompt  
✅ **Public workflows** - Can use existing workflows from the community  
✅ **Direct image URLs** - Returns ready-to-use image URLs  
✅ **Free tier available** - Get started with free credits  

## Setup (Super Simple!)

### 1. Get Your Free API Token

1. Go to [https://glif.app/settings/api-tokens](https://glif.app/settings/api-tokens)
2. Create a free API token
3. Set the environment variable:

```bash
export GLIF_API_TOKEN="your-token-here"
```

Or add it to your `.env` file in the MCP directory:

```
GLIF_API_TOKEN=your-token-here
```

That's it! No model keys, no complex configuration.

## Usage

### Via MCP Tool

**Tool Name:** `glif_generate_image`

**Example:**
```
Generate an image with prompt "A Gundam robot in neo-tokyo style"
```

**Parameters:**
- `prompt` (required): Text description of the image to generate
- `workflowId` (optional): Specific Glif workflow ID (uses default if not provided)
- `savePath` (optional): Where to save the image (default: NFT_Content/generated-{timestamp}.png)

### Complete Workflow: Generate → Mint NFT

**For agents:** Exact curl and MCP steps (auth, Glif, mint) are in [AGENT_NFT_MINT_WORKFLOW.md](AGENT_NFT_MINT_WORKFLOW.md).

**Step 1: Generate Image**
```
Generate an image with prompt "A futuristic Gundam robot in cyberpunk style"
```

**Step 2: Mint NFT with Generated Image**
```
Mint a Solana NFT using the image at NFT_Content/generated-{timestamp}.png
```

The MCP will:
1. Generate the image using Glif.app
2. Download and save it to `NFT_Content/` folder
3. Return the file path
4. You can then use that path for NFT minting

## How It Works

1. **You provide a prompt** → "A Gundam robot"
2. **Glif.app runs a workflow** → Uses a pre-built image generation workflow
3. **Returns image URL** → Direct link to the generated image
4. **MCP downloads it** → Saves to NFT_Content folder
5. **Ready for NFT minting** → Use the saved image path

## Finding Workflows

You can use:
- **Default workflow** (automatic) - Works for most cases
- **Public workflows** - Browse [glif.app](https://glif.app) for community workflows
- **Your own workflows** - Create custom workflows on Glif.app

To use a specific workflow, provide the `workflowId` parameter.

## Example Prompts

- "A Gundam robot in neo-tokyo cyberpunk style, highly detailed, 4k"
- "Abstract toroidal data flows, holographic, OASIS architecture"
- "Futuristic space station, OASIS logo, web3 web4 web5 integration"
- "Digital art NFT, vibrant colors, geometric patterns"

## Troubleshooting

### "Glif.app API token not configured"
- Get your free token at [https://glif.app/settings/api-tokens](https://glif.app/settings/api-tokens)
- Set the `GLIF_API_TOKEN` environment variable
- Restart Cursor/MCP server

### "Image generation failed"
- Check your Glif.app account has credits (free tier available)
- Verify the API token is correct
- Try a different prompt

### Generated image not found
- Check the `NFT_Content/` folder exists
- Verify the save path is correct
- Ensure write permissions to the folder

## Credits

Glif.app uses a credit system. You can:
- Start with free credits
- Purchase more at [https://glif.app/pricing](https://glif.app/pricing)
- Check your usage in your account dashboard

## Next Steps

1. Get your free API token at [glif.app/settings/api-tokens](https://glif.app/settings/api-tokens)
2. Set the `GLIF_API_TOKEN` environment variable
3. Restart Cursor to load the new MCP tool
4. Test by generating a Gundam image and minting it as an NFT!

## Reference

- [Glif.app API Documentation](https://docs.glif.app/api/getting-started)
- [Get API Token](https://glif.app/settings/api-tokens)
- [Browse Workflows](https://glif.app)
