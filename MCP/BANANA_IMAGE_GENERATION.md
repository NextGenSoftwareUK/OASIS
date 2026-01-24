# Banana.dev Image Generation Integration

## Overview

The MCP server now includes integration with **Banana.dev** API for AI-powered image generation. This allows you to generate images using Stable Diffusion or other AI models, then automatically use those images for NFT minting.

## Setup

### 1. Get Banana.dev API Key

1. Sign up at [banana.dev](https://www.banana.dev)
2. Create an API key in your dashboard
3. Set the environment variable:

```bash
export BANANA_API_KEY="your-api-key-here"
```

Or add it to your `.env` file in the MCP directory:

```
BANANA_API_KEY=your-api-key-here
```

### 2. Optional: Configure Model Key

By default, the integration uses `stable-diffusion-v1-5`. You can specify a different model key when generating images.

## Usage

### Via MCP Tool

**Tool Name:** `banana_generate_image`

**Example:**
```
Generate an image with prompt "A Gundam robot in neo-tokyo style"
```

**Parameters:**
- `prompt` (required): Text description of the image to generate
- `negativePrompt` (optional): Things to avoid in the image
- `modelKey` (optional): Banana.dev model key (default: stable-diffusion-v1-5)
- `width` (optional): Image width in pixels (default: 512)
- `height` (optional): Image height in pixels (default: 512)
- `savePath` (optional): Where to save the image (default: NFT_Content/generated-{timestamp}.png)

### Complete Workflow: Generate → Mint NFT

**Step 1: Generate Image**
```
Generate an image with prompt "A futuristic Gundam robot in cyberpunk style"
```

**Step 2: Mint NFT with Generated Image**
```
Mint a Solana NFT using the image at NFT_Content/generated-{timestamp}.png
```

The MCP will:
1. Generate the image using Banana.dev
2. Save it to `NFT_Content/` folder
3. Return the file path
4. You can then use that path for NFT minting

## Integration with NFT Minting

The generated images are automatically saved to the `NFT_Content/` folder, making them ready for NFT minting:

```typescript
// Example workflow:
1. Generate image → saves to NFT_Content/generated-1234567890.png
2. Mint NFT → use ImageUrl: NFT_Content/generated-1234567890.png
3. Auto-upload to Pinata → image uploaded to IPFS
4. NFT minted with IPFS image URL
```

## Benefits

✅ **No Region Restrictions**: Generated images are created fresh, avoiding geo-blocking issues  
✅ **Custom Prompts**: Generate exactly what you need for your NFTs  
✅ **Automatic Integration**: Images saved directly to NFT_Content folder  
✅ **Multiple Models**: Support for different AI models via modelKey parameter  
✅ **High Quality**: Uses Stable Diffusion and other state-of-the-art models  

## Example Prompts

- "A Gundam robot in neo-tokyo cyberpunk style, highly detailed, 4k"
- "Abstract toroidal data flows, holographic, OASIS architecture"
- "Futuristic space station, OASIS logo, web3 web4 web5 integration"
- "Digital art NFT, vibrant colors, geometric patterns"

## Troubleshooting

### "Banana.dev API key not configured"
- Set the `BANANA_API_KEY` environment variable
- Restart Cursor/MCP server after setting the variable

### "Image generation failed"
- Check your Banana.dev account has credits
- Verify the model key is correct
- Check network connectivity

### Generated image not found
- Check the `NFT_Content/` folder exists
- Verify the save path is correct
- Ensure write permissions to the folder

## Next Steps

1. Set up your Banana.dev API key
2. Test image generation with a simple prompt
3. Generate images for your NFT collection
4. Mint NFTs using the generated images
