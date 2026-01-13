# OASIS Visualizer Troubleshooting & Implementation Guide

## Overview
This document covers the issues encountered while integrating the holonic visualizer with the local OASIS API and how they were resolved.

## Project Location
```
/Volumes/Storage/OASIS_CLEAN/holonic-visualizer/
```

## Local OASIS API Configuration

### Base URL
- **HTTP**: `http://localhost:5003` (for local development)
- **HTTPS**: `https://localhost:5004` (causes HTTP/2 protocol errors in browsers)

### Authentication
- **Username**: `OASIS_ADMIN`
- **Password**: `Uppermall1!`
- **Endpoint**: `POST /api/avatar/authenticate`
- **Token Expiry**: 15 minutes

## Issues Encountered & Solutions

### Issue 1: HTTP/2 Protocol Errors
**Problem**: `ERR_HTTP2_PROTOCOL_ERROR 200 (OK)` when using HTTPS on localhost

**Solution**: 
- Use HTTP (`http://localhost:5003`) instead of HTTPS
- Disabled HTTPS redirection in `Startup.cs` for Development environment
- Added URL rewriting in `OASISClient.js` to force HTTP for localhost requests

**Files Modified**:
- `/ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI/Startup.cs`
- `/holonic-visualizer/src/api/OASISClient.js`

### Issue 2: CORS Preflight Failures
**Problem**: `Redirect is not allowed for a preflight request` - CORS preflight (OPTIONS) requests were being redirected by HTTPS redirection middleware

**Solution**:
- Conditionally disable `app.UseHttpsRedirection()` in Development environment
- Ensure CORS middleware is registered before HTTPS redirection
- Skip subscription checks for OPTIONS requests and authentication endpoints

**Files Modified**:
- `/ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI/Startup.cs`
- `/ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI/Middleware/SubscriptionMiddleware.cs`

### Issue 3: HolonType Must Be Integer Enum
**Problem**: `400 Bad Request` - `"The JSON value could not be converted to NextGenSoftware.OASIS.API.Core.Enums.HolonType"`

**Root Cause**: The API expects `HolonType` as an integer enum value, not a string.

**Solution**:
- Use integer enum values:
  - `74` for OAPP (not `"OAPP"`)
  - `3` for Avatar (not `"Avatar"`)
  - `0` for default/regular holons (not `"Mission"`, `"NFT"`, etc.)
- Store the type name in `MetaData.holonTypeName` for reference

**Example**:
```javascript
// ❌ WRONG
{
  HolonType: "OAPP"  // String - will fail
}

// ✅ CORRECT
{
  HolonType: 74,  // Integer enum value
  MetaData: {
    holonTypeName: "OAPP"  // Store name for reference
  }
}
```

**Files Modified**:
- `/holonic-visualizer/src/utils/seedOASISData.js`

### Issue 4: ProviderMetaData Validation Error
**Problem**: `400 Bad Request` - `"The JSON value could not be converted to NextGenSoftware.OASIS.API.Core.Enums.ProviderType"` for `ProviderMetaData.collection`

**Root Cause**: `ProviderMetaData.collection` was being interpreted as a `ProviderType` enum, not a string.

**Solution**: Removed `ProviderMetaData` entirely - it's optional and the API will use default MongoDB collection based on `ProviderKey: 'MongoOASIS'`.

**Files Modified**:
- `/holonic-visualizer/src/utils/seedOASISData.js`

### Issue 5: Request Body Property Casing
**Problem**: `400 Bad Request` when saving holons

**Root Cause**: C# API expects PascalCase property names in request body.

**Solution**: Use PascalCase for all request body properties:
```javascript
{
  Holon: { ... },           // Not "holon"
  SaveChildren: false,      // Not "saveChildren"
  Recursive: false,         // Not "recursive"
  MaxChildDepth: 0,         // Not "maxChildDepth"
  ContinueOnError: true,    // Not "continueOnError"
  ShowDetailedSettings: false  // Not "showDetailedSettings"
}
```

**Files Modified**:
- `/holonic-visualizer/src/utils/seedOASISData.js`

### Issue 6: ERR_INCOMPLETE_CHUNKED_ENCODING
**Problem**: When loading OAPPs, the response is too large because it includes all child holons (1200+ holons)

**Root Cause**: GET request to `/api/data/load-all-holons/74` loads OAPPs with all their children by default.

**Solution**: 
- Use POST request with `LoadChildren: false` to load only OAPPs without children
- Created `getAllHolonsWithOptions()` method that always uses POST with options
- Load OAPPs, Avatars, and regular holons separately

**Files Modified**:
- `/holonic-visualizer/src/api/OASISClient.js` - Added `getAllHolonsWithOptions()`
- `/holonic-visualizer/src/main.js` - Updated to use POST with `LoadChildren: false`

### Issue 7: String Enum Values Return 0 Results
**Problem**: Loading holons with string `'OAPP'` or `'Avatar'` returns 0 results

**Root Cause**: API only accepts integer enum values in URL path or request body.

**Solution**: 
- Try integer enum values first (74, 3, 0)
- Fallback to strings only if needed (though strings don't work for this API)

**Files Modified**:
- `/holonic-visualizer/src/main.js`
- `/holonic-visualizer/src/api/OASISClient.js`

## Current Working Configuration

### API Endpoints

#### Save Holon
```http
POST /api/data/save-holon
Authorization: Bearer {jwtToken}
Content-Type: application/json

{
  "Holon": {
    "Name": "Holon Name",
    "Description": "Description",
    "HolonType": 74,  // Integer enum (74=OAPP, 3=Avatar, 0=Default)
    "ProviderKey": "MongoOASIS",
    "MetaData": {
      "createdBy": "script",
      "createdDate": "2025-12-15T..."
    }
  },
  "SaveChildren": false,
  "Recursive": false,
  "MaxChildDepth": 0,
  "ContinueOnError": true,
  "ShowDetailedSettings": false
}
```

#### Load All Holons (with options)
```http
POST /api/data/load-all-holons
Authorization: Bearer {jwtToken}
Content-Type: application/json

{
  "HolonType": 74,  // Integer enum or "All"
  "LoadChildren": false,  // IMPORTANT: Set to false to avoid large responses
  "Recursive": false,
  "MaxChildDepth": 0,
  "ContinueOnError": true,
  "Version": 0,
  "ProviderKey": "MongoOASIS"
}
```

### Response Structure
```javascript
{
  result: {
    result: [  // Array of holons
      {
        id: "guid",
        Name: "Holon Name",
        HolonType: 74,
        MetaData: { ... },
        // ... other properties
      }
    ],
    isError: false,
    message: ""
  }
}
```

## Data Seeding

### How to Seed Sample Data

1. **Start the OASIS API** (if not already running):
   ```bash
   cd /Volumes/Storage/OASIS_CLEAN/ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI
   dotnet run
   ```

2. **Open the visualizer**:
   ```bash
   cd /Volumes/Storage/OASIS_CLEAN/holonic-visualizer
   npm run dev
   ```

3. **Click "Seed Sample Data"** button in the UI

4. **Wait for completion** - You should see:
   - ✅ Created 5 OAPPs
   - ✅ Created holons across all OAPPs (1200+ holons)
   - ✅ Created 50 unassigned holons

### What Gets Created

- **5 OAPPs** (HolonType: 74):
  - Star Navigation System (300 holons)
  - Planet Explorer (150 holons)
  - Moon Base Manager (50 holons)
  - Galaxy Tracker (500 holons)
  - Solar System Simulator (200 holons)

- **1200+ Regular Holons** (HolonType: 0):
  - Assigned to OAPPs via `MetaData.oappId`
  - Types stored in `MetaData.holonTypeName` (Mission, NFT, Wallet, etc.)

- **50 Unassigned Holons** (HolonType: 0):
  - No `oappId` in metadata

## Loading Data

### Current Status
- ✅ **Seeding works** - Can create OAPPs and holons successfully
- ⚠️ **Loading in progress** - Working on loading holons without chunked encoding errors

### How to Load Data

1. Click **"Load All from OASIS"** button
2. The system will:
   - Authenticate with OASIS API
   - Load OAPPs (without children) using POST with `LoadChildren: false`
   - Load Avatars (without children)
   - Load regular holons separately
   - Transform and visualize the data

### Expected Console Output
```
✅ Authenticated with OASIS API
Loading OAPPs...
getAllHolonsWithOptions('74') returned 5 holons from data.result.result
Loaded 5 OAPPs
Loading Avatars...
getAllHolonsWithOptions('3') returned X holons from data.result.result
Loaded X Avatars
Loading regular holons...
getAllHolonsWithOptions('0') returned X holons from data.result.result
Loaded X regular holons
Found X regular holons, X avatars, X OAPPs
```

## Key Files

### Frontend (Visualizer)
- `/holonic-visualizer/src/main.js` - Main orchestration
- `/holonic-visualizer/src/api/OASISClient.js` - API client with authentication
- `/holonic-visualizer/src/utils/seedOASISData.js` - Data seeding script
- `/holonic-visualizer/src/data/OASISDataTransformer.js` - Data transformation

### Backend (API)
- `/ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI/Startup.cs` - API configuration
- `/ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI/Middleware/SubscriptionMiddleware.cs` - Middleware for request handling

## Best Practices

1. **Always use integer enum values** for `HolonType` (74, 3, 0)
2. **Use POST with `LoadChildren: false`** when loading OAPPs to avoid large responses
3. **Store type names in metadata** for reference (e.g., `MetaData.holonTypeName`)
4. **Use PascalCase** for all request body properties when calling C# API
5. **Handle chunked encoding errors** gracefully - they indicate responses are too large
6. **Load data in smaller batches** if you encounter size issues

## Common Errors & Fixes

| Error | Cause | Fix |
|-------|-------|-----|
| `400 Bad Request - HolonType conversion` | Using string instead of integer | Use `74` not `"OAPP"` |
| `400 Bad Request - ProviderMetaData.collection` | ProviderMetaData structure issue | Remove ProviderMetaData |
| `ERR_INCOMPLETE_CHUNKED_ENCODING` | Response too large | Use POST with `LoadChildren: false` |
| `CORS preflight redirect error` | HTTPS redirection on OPTIONS | Disable HTTPS redirection in Development |
| `0 holons loaded` | String enum in URL | Use integer enum values (74, 3, 0) |

## Next Steps

1. ✅ Seeding works - Can create OAPPs and holons
2. 🔄 Loading works - Need to verify `getAllHolonsWithOptions()` works correctly
3. ⏳ Load child holons separately - After OAPPs load, load their holons individually
4. ⏳ Visualize data - Ensure visualizer displays loaded data correctly

## Testing Checklist

- [x] Authentication works
- [x] Can save OAPPs (HolonType: 74)
- [x] Can save regular holons (HolonType: 0)
- [x] Can save holons with metadata
- [ ] Can load OAPPs without children
- [ ] Can load Avatars
- [ ] Can load regular holons
- [ ] Can visualize loaded data
- [ ] Can load child holons for each OAPP
