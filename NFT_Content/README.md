# NFT Content Folder

This folder is for storing images and metadata files that you want to use when minting NFTs via the MCP endpoint.

## Usage

1. **Add your images here** - Drop any image files (PNG, JPG, GIF, etc.) you want to use for NFTs
2. **Reference them in MCP** - Use the file paths or URLs when calling the `oasis_mint_nft` tool

## Serving Images

To reference images in MCP payloads, you have a few options:

### Option 1: Use a Public URL
- Upload images to a service like:
  - IPFS (via Pinata, NFT.Storage, etc.)
  - Cloud storage (AWS S3, Google Cloud, etc.)
  - Image hosting services
- Use the public URL in your `JSONMetaDataURL` or `ImageUrl` parameter

### Option 2: Local Development Server
For local testing, you can serve images using a simple HTTP server:

```bash
# From the NFT_Content directory
python3 -m http.server 8000
# Or using Node.js
npx http-server -p 8000
```

Then reference as: `http://localhost:8000/your-image.png`

### Option 3: Use Placeholder URLs
For quick testing, you can use placeholder services:
- `https://via.placeholder.com/512` (placeholder image)
- `https://jsonplaceholder.typicode.com/posts/1` (placeholder JSON)

## Example MCP Usage

Once you have an image URL, you can mint an NFT like this:

```typescript
// Via MCP tool call
oasis_mint_nft({
  JSONMetaDataURL: "https://example.com/metadata.json",
  Symbol: "MYNFT",
  Title: "My Awesome NFT",
  ImageUrl: "http://localhost:8000/my-image.png",
  MetaData: {
    description: "A test NFT",
    image: "http://localhost:8000/my-image.png",
    attributes: [
      { trait_type: "Color", value: "Blue" },
      { trait_type: "Rarity", value: "Common" }
    ]
  }
})
```

## Metadata JSON Structure

If you want to create a metadata JSON file, here's a template:

```json
{
  "name": "My NFT Name",
  "symbol": "MYNFT",
  "description": "Description of my NFT",
  "image": "https://example.com/image.png",
  "attributes": [
    {
      "trait_type": "Color",
      "value": "Blue"
    },
    {
      "trait_type": "Rarity",
      "value": "Common"
    }
  ]
}
```

Save this as `metadata.json` and reference it in `JSONMetaDataURL`.
