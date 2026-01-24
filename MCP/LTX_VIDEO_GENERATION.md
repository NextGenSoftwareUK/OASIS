# LTX.io Video Generation Integration

## Overview

The MCP server now includes integration with **LTX.io API** for AI-powered video generation. LTX-2 is a production-grade video generation model that supports text-to-video and image-to-video generation with synchronized audio.

## Why LTX.io?

✅ **High-quality video generation** - Native 4K resolution, up to 50 fps  
✅ **Multiple generation modes** - Text-to-video and image-to-video  
✅ **Synchronized audio** - Motion, dialogue, and ambient sound in a single pass  
✅ **Production-ready** - Up to 20 seconds of continuous video  
✅ **Two performance modes** - Fast iteration or maximum quality  

## Setup

### 1. Get Your API Key

1. Go to [https://ltx.io/model/api](https://ltx.io/model/api)
2. Sign up and get your API key from the Developer Console
3. Set the environment variable:

```bash
export LTX_API_TOKEN="your-api-key-here"
```

Or add it to your `.env` file in the MCP directory:

```
LTX_API_TOKEN=your-api-key-here
```

## Usage

### Via MCP Tool

**Tool Name:** `ltx_generate_video`

### Text-to-Video Generation

Generate a video from a text prompt:

**Example:**
```
Generate a video with prompt "A futuristic Gundam robot walking through neo-tokyo streets at night, cyberpunk style, neon lights"
```

**Parameters:**
- `prompt` (required for text-to-video): Text description of the video to generate
- `model` (optional): `ltx-2-fast` (default, faster) or `ltx-2-pro` (higher quality)
- `duration` (optional): Video duration in seconds (1-20, default: 5)
- `resolution` (optional): Video resolution (e.g., "1920x1080", "3840x2160", default: "1920x1080")
- `aspectRatio` (optional): Aspect ratio (e.g., "16:9", "9:16", "1:1", default: "16:9")
- `fps` (optional): Frames per second (up to 50, default: 24)
- `savePath` (optional): Where to save the video (default: NFT_Content/generated-video-{timestamp}.mp4)

### Image-to-Video Generation

Animate a static image:

**Example:**
```
Generate a video from image at NFT_Content/generated-1234567890.png with prompt "gentle motion, camera panning"
```

**Parameters:**
- `imageUrl` (required for image-to-video): URL of the image to animate
- `imageBase64` (alternative): Base64 encoded image (alternative to imageUrl)
- `prompt` (optional): Text prompt to guide the motion/animation
- `model`, `duration`, `resolution`, `aspectRatio`, `fps`, `savePath` (same as text-to-video)

## Complete Workflow Examples

### Workflow 1: Generate Image → Animate → Mint NFT

**Step 1: Generate Image**
```
Generate an image with prompt "A futuristic Gundam robot in cyberpunk style"
```

**Step 2: Animate the Image**
```
Generate a video from image at NFT_Content/generated-{timestamp}.png with prompt "robot walking forward, camera following"
```

**Step 3: Mint NFT with Video**
```
Mint a Solana NFT using the video at NFT_Content/generated-video-{timestamp}.mp4
```

### Workflow 2: Direct Text-to-Video

**Step 1: Generate Video**
```
Generate a video with prompt "Abstract toroidal data flows, holographic, OASIS architecture, smooth motion"
```

**Step 2: Use Video**
The video is saved to `NFT_Content/generated-video-{timestamp}.mp4` and ready for use.

## How It Works

### Text-to-Video
1. **You provide a prompt** → "A Gundam robot walking"
2. **LTX.io generates video** → Creates MP4 with synchronized audio
3. **Returns video data** → Base64 encoded MP4
4. **MCP saves it** → Saves to NFT_Content folder
5. **Ready for use** → Use the saved video path

### Image-to-Video
1. **You provide an image** → URL or base64 encoded
2. **Optional prompt guides motion** → "camera panning", "gentle motion"
3. **LTX.io animates image** → Creates MP4 with motion
4. **Returns video data** → Base64 encoded MP4
5. **MCP saves it** → Saves to NFT_Content folder

## Model Options

### ltx-2-fast (Default)
- **Optimized for:** Speed and rapid iteration
- **Best for:** Testing, quick previews, rapid prototyping
- **Use when:** You need quick results and are iterating on prompts

### ltx-2-pro
- **Optimized for:** Maximum visual detail and stability
- **Best for:** Final production videos, high-quality content
- **Use when:** You need the highest quality output

## Technical Specifications

- **Resolution:** Up to 4K (3840x2160)
- **Frame Rate:** Up to 50 fps
- **Duration:** Up to 20 seconds per clip
- **Audio:** Synchronized motion, dialogue, and ambient sound
- **Format:** MP4 (H.264)

## Example Prompts

### Text-to-Video
- "A futuristic Gundam robot walking through neo-tokyo streets at night, cyberpunk style, neon lights, smooth camera movement"
- "Abstract toroidal data flows, holographic, OASIS architecture, fluid motion"
- "Futuristic space station, OASIS logo, web3 web4 web5 integration, camera orbiting"
- "Digital art NFT, vibrant colors, geometric patterns, morphing animation"

### Image-to-Video Motion Prompts
- "gentle motion, camera panning left"
- "slow zoom in, subtle movement"
- "camera orbiting around the subject"
- "smooth forward motion, cinematic"
- "gentle swaying, natural movement"

## Troubleshooting

### "LTX.io API token not configured"
- Get your API key at [https://ltx.io/model/api](https://ltx.io/model/api)
- Set the `LTX_API_TOKEN` environment variable
- Restart Cursor/MCP server

### "Video generation failed"
- Check your LTX.io account has credits
- Verify the API key is correct
- Check the error message for specific issues (rate limits, invalid parameters, etc.)
- Try a different prompt or shorter duration

### Generated video not found
- Check the `NFT_Content/` folder exists
- Verify the save path is correct
- Ensure write permissions to the folder

### Video generation takes too long
- Use `ltx-2-fast` model for faster generation
- Reduce video duration
- Lower resolution (e.g., "1280x720" instead of "1920x1080")

### Rate Limit Errors (429)
- You've exceeded your plan's rate limits
- Wait before retrying
- Consider upgrading your LTX.io plan

## API Limits and Pricing

LTX.io uses a credit-based system:
- Check your usage in the [LTX Developer Console](https://ltx.io/model/api)
- Different models have different credit costs
- `ltx-2-fast` uses fewer credits than `ltx-2-pro`
- Higher resolution and longer duration use more credits

## Best Practices

1. **Start with fast model** - Use `ltx-2-fast` for testing and iteration
2. **Use appropriate duration** - Shorter videos (5-10s) are faster and cheaper
3. **Optimize prompts** - Be specific about motion and style
4. **For image-to-video** - Provide motion prompts to guide animation
5. **Save paths** - Use descriptive save paths for organization
6. **Production quality** - Switch to `ltx-2-pro` for final output

## Integration with Other Tools

### Generate Image → Animate → Mint NFT
1. Use `glif_generate_image` to create an image
2. Use `ltx_generate_video` with `imageUrl` to animate it
3. Use `oasis_mint_nft` or `solana_mint_nft` to mint the video as an NFT

### Direct Video Generation
1. Use `ltx_generate_video` with a text prompt
2. Video is saved and ready for use
3. Can be uploaded to IPFS, minted as NFT, or used in other workflows

## Reference

- [LTX.io Model Page](https://ltx.io/model)
- [LTX.io API Documentation](https://docs.ltx.video/)
- [Get API Key](https://ltx.io/model/api)
- [LTX-2 Open Source](https://github.com/Lightricks/LTX-2)

## Next Steps

1. Get your API key at [ltx.io/model/api](https://ltx.io/model/api)
2. Set the `LTX_API_TOKEN` environment variable
3. Restart Cursor to load the new MCP tool
4. Test by generating a video from a prompt or animating an existing image!
