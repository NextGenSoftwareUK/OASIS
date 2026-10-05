# Troubleshooting Frontend Issues

## Common Issues

### 1. Port 5004 Error (Connection Refused)

**Error:** `GET https://localhost:5004/api/shipexpro/merchant/undefined net::ERR_CONNECTION_REFUSED`

**Cause:** Browser has cached old JavaScript with port 5004

**Solution:**
1. **Hard refresh the browser:**
   - Chrome/Edge: `Ctrl+Shift+R` (Windows/Linux) or `Cmd+Shift+R` (Mac)
   - Firefox: `Ctrl+F5` (Windows/Linux) or `Cmd+Shift+R` (Mac)
   - Safari: `Cmd+Option+R`

2. **Clear browser cache:**
   - Open DevTools (F12)
   - Right-click the refresh button
   - Select "Empty Cache and Hard Reload"

3. **Verify API is running:**
   ```bash
   # Check if APIs are running
   lsof -ti:5002  # OASIS API
   lsof -ti:5005  # Shipex Pro API
   ```

### 2. Merchant/Undefined Error

**Error:** `GET .../merchant/undefined`

**Cause:** Function naming conflict (now fixed)

**Solution:**
- Hard refresh browser to load updated JavaScript
- The async `getMerchant(merchantId)` has been renamed to `getMerchantById(merchantId)`

### 3. Authentication Errors

**Error:** `No token received from authentication`

**Possible Causes:**
1. OASIS API not running on port 5002
2. Wrong OASIS API URL in frontend
3. Invalid credentials

**Solution:**
1. Check OASIS API is running:
   ```bash
   curl -k https://localhost:5002/api/avatar/authenticate
   ```

2. Verify frontend configuration in `js/shipex-api.js`:
   ```javascript
   this.oasisApiURL = 'https://localhost:5002';  // Should be 5002
   ```

3. Try "Skip for Testing" option first to test UI

### 4. CORS Errors

**Error:** `Access to fetch at 'https://localhost:5005' from origin 'http://localhost:8000' has been blocked by CORS policy`

**Solution:**
- Shipex Pro API has CORS configured in `Program.cs`
- Make sure API is running
- Check browser console for specific CORS error

## Quick Fixes

### Clear All Cache and Reload
```javascript
// In browser console (F12):
localStorage.clear();
sessionStorage.clear();
location.reload(true);
```

### Check API Status
```bash
# Terminal 1: OASIS API
cd /Volumes/Storage/OASIS_CLEAN/ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI
dotnet run

# Terminal 2: Shipex Pro API  
cd /Volumes/Storage/OASIS_CLEAN/Shipex/ShipexPro.API
dotnet run

# Terminal 3: Frontend
cd /Volumes/Storage/OASIS_CLEAN/Shipex/shipex-pro-frontend
./start.sh
```

### Verify Ports
- **OASIS API:** `https://localhost:5002`
- **Shipex Pro API:** `https://localhost:5005`
- **Frontend:** `http://localhost:8000`

## Current Configuration

### Frontend (`js/shipex-api.js`)
```javascript
this.baseURL = 'https://localhost:5005';      // Shipex Pro API
this.oasisApiURL = 'https://localhost:5002';  // OASIS API
```

### API Endpoints
- OASIS API: `https://localhost:5002/api/avatar/authenticate`
- Shipex Pro: `https://localhost:5005/api/shipexpro/merchant/by-avatar/{avatarId}`

---

**Last Updated:** After fixing getMerchant naming conflict
