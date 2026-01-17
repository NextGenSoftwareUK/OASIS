# Pinata File Upload via MCP

## Overview

You can now upload images and other files directly to Pinata/IPFS storage through the MCP interactive flow. This makes it easy to upload NFT images and get IPFS URLs for use in your NFT metadata.

## Pinata Credentials

Pinata credentials are configured in `OASIS_DNA.json`:
- **API Key**: Configured
- **Secret Key**: Configured  
- **Gateway**: `https://gateway.pinata.cloud`

The credentials are automatically used when uploading files.

## How to Use

### Option 1: Via Interactive NFT Minting

When minting an NFT, if you don't provide an image URL, you'll be prompted:

```
Please provide an image URL for your NFT. Options:
1) Upload a local image file to Pinata/IPFS (I can help with that)
2) Use a local image server (start server in NFT_Content folder)
3) Use any public image URL
4) Use placeholder: https://via.placeholder.com/512
```

Simply say: **"Upload my image from NFT_Content/my-image.png"** and I'll:
1. Upload it to Pinata/IPFS
2. Get the IPFS URL
3. Use it automatically in your NFT

### Option 2: Direct Upload Tool

You can also upload files directly using the `oasis_upload_file` tool:

**Example:**
```
Upload the file at NFT_Content/my-nft-image.png to Pinata
```

**Response:**
```json
{
  "result": "https://gateway.pinata.cloud/ipfs/QmXxxxxx...",
  "isError": false,
  "message": "File uploaded to IPFS successfully"
}
```

## File Paths

You can provide:
- **Absolute paths**: `/Users/maxgershfield/OASIS_CLEAN/NFT_Content/image.png`
- **Relative paths**: `NFT_Content/image.png` (relative to workspace root)
- **Workspace-relative**: Files in your `NFT_Content/` folder

## Supported File Types

- Images: PNG, JPG, GIF, WebP, SVG
- JSON files
- Any file type supported by Pinata

## Example Workflow

### Complete NFT Minting with Upload

**You:** "Mint a Solana NFT"

**AI:** "I need some information... Image URL?"

**You:** "Upload NFT_Content/my-artwork.png"

**AI:** 
1. Uploads file to Pinata
2. Gets IPFS URL: `https://gateway.pinata.cloud/ipfs/QmXxxxxx...`
3. Uses it as ImageUrl
4. Continues with minting

**Result:** NFT minted with image stored on IPFS via Pinata!

## Benefits

1. **Decentralized Storage**: Files stored on IPFS (decentralized)
2. **Permanent URLs**: IPFS hashes don't change
3. **No Local Server Needed**: No need to run a local HTTP server
4. **Automatic Integration**: Uploaded URLs automatically used in NFT metadata
5. **Reliable**: Pinata provides reliable IPFS pinning service

## API Endpoint

The upload uses the OASIS API endpoint:
```
POST /api/files/upload
```

With:
- `file`: Multipart form data file
- `provider`: "PinataOASIS" (default)

Returns:
- IPFS URL: `https://gateway.pinata.cloud/ipfs/{hash}`

## Troubleshooting

### "File not found"
- Check the file path is correct
- Use absolute path if relative path doesn't work
- Ensure file exists in the specified location

### "Authentication required"
- Make sure you're authenticated (the AI will handle this automatically)
- The upload endpoint requires a valid JWT token

### "Failed to activate Pinata provider"
- Check Pinata credentials in `OASIS_DNA.json`
- Verify API key and secret key are valid
- Check network connectivity to Pinata API

## Pinata Configuration

Current configuration in `OASIS_DNA.json`:
```json
{
  "PinataOASIS": {
    "ConnectionString": "https://api.pinata.cloud?apiKey=...&secretKey=...&gateway=https://gateway.pinata.cloud"
  }
}
```

The credentials are automatically parsed and used for authentication.
