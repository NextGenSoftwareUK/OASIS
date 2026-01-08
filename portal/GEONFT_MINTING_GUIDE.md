# GeoNFT Minting Guide

## Overview
This guide walks you through the process of minting (placing) a GeoNFT using the OASIS Portal.

## Process Flow

### Step 1: Access the GeoNFT Interface
1. Navigate to the **STAR** tab in the OASIS Portal
2. Click the **"Mint GeoNFT"** button in the Quick Actions section

### Step 2: Select a Location
The interface will display a map (defaults to London). You have three ways to select a location:

**Option A: Click on the Map**
- Simply click anywhere on the map
- A marker will appear at the selected location
- The coordinates will be displayed below the map
- You can drag the marker to adjust the position

**Option B: Search for an Address**
- Enter an address in the search box
- Click "Search Location"
- The map will center on the found address
- Click on the map to select the exact location

**Option C: Use Coordinates**
- The coordinates display shows the selected location
- Format: `latitude, longitude` (e.g., `51.5074, -0.1278`)

### Step 3: Enter GeoNFT ID
- In the "GeoNFT ID" field, enter the ID of the NFT you want to place
- This should be an existing OASIS NFT ID that you own
- Note: You must own the NFT to place it as a GeoNFT

### Step 4: Configure Options
- **Allow others to collect**: Check this box if you want other players to be able to collect this GeoNFT
- If unchecked, only you can interact with it

### Step 5: Mint the GeoNFT
- Click the **"Mint GeoNFT"** button
- The button will show "Placing..." while the request is processing
- Upon success, you'll see a success message and be redirected to the map view

## Technical Details

### API Endpoint
The process uses the STAR API endpoint:
- **Endpoint**: `POST /api/GeoNFTs/place-geo-nft` (or `/api/nft/place-geo-nft` depending on API version)
- **Authentication**: Requires Bearer token (JWT)

### Request Parameters
```json
{
  "originalOASISNFTId": "string",  // The NFT ID to place
  "lat": number,                    // Latitude in micro-degrees (multiply by 1,000,000)
  "long": number,                   // Longitude in micro-degrees (multiply by 1,000,000)
  "allowOtherPlayersToAlsoCollect": boolean,
  "permSpawn": false,
  "globalSpawnQuantity": 1,
  "playerSpawnQuantity": 1,
  "respawnDurationInSeconds": 0,
  "geoNFTMetaDataProvider": "MongoDBOASIS",
  "placedByAvatarId": "string"      // Your avatar ID
}
```

### Coordinate Conversion
- The map uses standard degrees (e.g., `51.5074, -0.1278`)
- The API requires micro-degrees (multiplied by 1,000,000)
- Example: `51.5074` becomes `51507400` in micro-degrees
- The conversion is handled automatically by the interface

## Requirements
1. **Authentication**: You must be logged in to the OASIS Portal
2. **NFT Ownership**: You must own the NFT you're trying to place
3. **Valid Location**: You must select a valid location on the map
4. **NFT ID**: You must provide a valid OASIS NFT ID

## Troubleshooting

### "Please select a location on the map"
- Make sure you've clicked on the map to select a location
- The coordinates should be displayed below the map
- If not visible, try clicking again

### "Please enter a GeoNFT ID"
- Ensure you've entered a valid NFT ID
- The ID should be an existing NFT you own

### "You must be logged in to place a GeoNFT"
- Check that you're authenticated
- Try logging out and back in
- Check browser console for authentication errors

### API Errors
- Check the browser console for detailed error messages
- Verify the STAR API is running and accessible
- Ensure your authentication token is valid
- Check network connectivity

### Map Not Loading
- Ensure Leaflet.js is loaded (check browser console)
- Check internet connection (map tiles are loaded from OpenStreetMap)
- Try refreshing the page

## Viewing Your GeoNFT
After successfully minting a GeoNFT:
1. You'll be redirected to the GeoNFT Map view
2. Your newly placed GeoNFT should appear on the map
3. Click on the marker to view details
4. You can also browse all GeoNFTs in the list below the map

## Additional Features
- **Reverse Geocoding**: When you select a location, the interface attempts to find the address
- **Draggable Marker**: In placement mode, you can drag the marker to fine-tune the location
- **Map Navigation**: Use mouse wheel to zoom, click and drag to pan

## Notes
- GeoNFTs are permanent once placed (depending on configuration)
- The location precision is limited by the coordinate system (micro-degrees)
- Multiple GeoNFTs can be placed at the same location
- GeoNFT placement requires blockchain interaction (gas fees may apply depending on the blockchain)






























