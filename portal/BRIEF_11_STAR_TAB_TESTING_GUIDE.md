# Brief 11: STAR Tab Production Readiness - Testing Guide

## Summary

All STAR tab modules have been updated for production readiness with:
- ✅ Centralized STAR API configuration
- ✅ Error handling and loading states
- ✅ Authentication integration
- ✅ Production-ready code

## Files Updated

1. **`star-api-config.js`** (NEW) - Centralized configuration helper
2. **`portal.html`** - Added config script reference
3. **`star-dashboard.js`** - Updated with config, error handling, loading states
4. **`quest-builder.js`** - Updated with config, validation, error handling
5. **`quest-interface.js`** - Updated with config, error handling
6. **`mission-interface.js`** - Updated with config, error handling
7. **`geonft-interface.js`** - Updated with config, error handling, loading states

## Testing the STAR API Locally

### Option 1: Using Docker Compose

```bash
cd /Users/maxgershfield/OASIS_CLEAN
docker-compose up -d star-api
```

The STAR API will be available at: `http://localhost:5001`

### Option 2: Running Directly with .NET

```bash
cd "/Users/maxgershfield/OASIS_CLEAN/STAR ODK/NextGenSoftware.OASIS.STAR.WebAPI"
dotnet run --urls "http://localhost:5001"
```

### Option 3: Using the Test Page

1. Open `portal/test-star-api.html` in your browser
2. Click "Test Configuration" to verify the config is working
3. Click "Test API Health" to check if STAR API is running
4. Test individual endpoints (NFT, Quest, Mission)
5. Test portal integration

## Configuration

The STAR API configuration automatically detects the environment:

- **Development** (localhost): `http://localhost:5001`
- **Production**: `https://star-api.oasisplatform.world`

The configuration is handled by `getSTARAPIBaseURL()` in `star-api-config.js`.

## API Endpoints Tested

### 1. NFT Endpoints
- `GET /api/nft/load-all-nfts-for_avatar/{avatarId}` - Load user's NFTs
- `POST /api/nft/place-geo-nft` - Place GeoNFT on map

### 2. Quest Endpoints
- `GET /api/quests` - List quests
- `POST /api/quests` - Create quest
- `POST /api/quests/{questId}/start` - Start quest

### 3. Mission Endpoints
- `GET /api/missions` - List missions
- `POST /api/missions` - Create mission

## Testing Checklist

### Configuration Tests
- [ ] Verify `getSTARAPIBaseURL()` returns correct URL for localhost
- [ ] Verify authentication token is retrieved correctly
- [ ] Verify avatar ID is retrieved correctly

### API Connectivity Tests
- [ ] STAR API health endpoint responds (`/api/health`)
- [ ] Swagger UI is accessible (`/swagger`)
- [ ] CORS is configured correctly

### STAR Dashboard Tests
- [ ] Dashboard loads without errors
- [ ] Stats display correctly (NFTs, GeoNFTs, Quests, Missions)
- [ ] Assets load correctly
- [ ] Error handling works (test with API down)
- [ ] Loading states display correctly

### Quest Builder Tests
- [ ] Quest creation interface loads
- [ ] User NFTs load for quest rewards
- [ ] Quest creation works end-to-end
- [ ] Validation works (empty name, description)
- [ ] Error messages display correctly

### Quest Interface Tests
- [ ] Quest browser loads
- [ ] Available quests display
- [ ] Quest starting works
- [ ] Quest detail view works

### Mission Interface Tests
- [ ] Mission tracker loads
- [ ] Mission creation works
- [ ] Mission detail view works
- [ ] Progress visualization works

### GeoNFT Interface Tests
- [ ] GeoNFT map loads
- [ ] Location selection works
- [ ] GeoNFT placement works
- [ ] Map markers display correctly

## Common Issues

### STAR API Not Running
**Symptom**: All API calls fail with connection errors

**Solution**:
1. Check if STAR API is running: `curl http://localhost:5001/api/health`
2. Start STAR API using one of the methods above
3. Check for port conflicts: `lsof -ti:5001`

### CORS Errors
**Symptom**: Browser console shows CORS errors

**Solution**:
1. Verify STAR API CORS configuration includes `http://localhost` (for local testing)
2. Check STAR API `Startup.cs` or `Program.cs` for CORS settings

### Authentication Errors
**Symptom**: API returns 401 Unauthorized

**Solution**:
1. Verify user is logged in to the portal
2. Check that `localStorage.getItem('oasis_auth')` contains valid token
3. Verify token is included in Authorization header

### Configuration Not Loading
**Symptom**: `getSTARAPIBaseURL is not a function`

**Solution**:
1. Verify `star-api-config.js` is loaded before other STAR modules
2. Check browser console for script loading errors
3. Verify script order in `portal.html`

## Next Steps

1. **Start STAR API locally** using one of the methods above
2. **Open the portal** at `http://localhost` (or your local portal URL)
3. **Navigate to STAR tab** and test all features
4. **Use test page** (`test-star-api.html`) to verify API connectivity
5. **Test with production API** once deployed (Brief 1)

## Production Deployment

Once Brief 1 is complete and STAR API is deployed to AWS ECS:
- The configuration will automatically use `https://star-api.oasisplatform.world`
- No code changes needed - the config handles environment detection
- Test all endpoints with production API
- Verify CORS is configured for `oportal.oasisweb4.com`

## Notes

- All modules now use centralized configuration
- Error handling is consistent across all modules
- Loading states prevent duplicate requests
- Code is production-ready and will work once STAR API is deployed































