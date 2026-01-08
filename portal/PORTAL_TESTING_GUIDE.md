# Portal Testing Guide

## Prerequisites

Before testing the portal, ensure you have:

1. **STAR API running** (local or production)
   - Local: `http://localhost:5001`
   - Production: `https://star-api.oasisplatform.world`

2. **OASIS API running** (local or production)
   - Local: `https://localhost:5000` or `http://localhost:5000`
   - Production: `https://api.oasisplatform.world`

3. **Web browser** (Chrome, Firefox, Safari, or Edge)
   - Recommended: Chrome or Firefox for best debugging

4. **User account** - You'll need to be able to authenticate

## Quick Start - Testing the Portal

### Option 1: Simple HTTP Server (Recommended)

The portal is a static HTML file that can be served with any HTTP server.

#### Using Python 3 (if installed):
```bash
cd /Users/maxgershfield/OASIS_CLEAN/portal
python3 -m http.server 8080
```

Then open: `http://localhost:8080/portal.html`

#### Using Python 2 (if Python 3 not available):
```bash
cd /Users/maxgershfield/OASIS_CLEAN/portal
python -m SimpleHTTPServer 8080
```

Then open: `http://localhost:8080/portal.html`

#### Using Node.js (if installed):
```bash
cd /Users/maxgershfield/OASIS_CLEAN/portal
npx http-server -p 8080
```

Then open: `http://localhost:8080/portal.html`

### Option 2: Open Directly in Browser

**Note:** Opening directly may have CORS issues with API calls.

1. Navigate to: `/Users/maxgershfield/OASIS_CLEAN/portal/portal.html`
2. Right-click → Open with → Your browser

### Option 3: Using the Live Portal

The portal is already live at: **https://oportal.oasisweb4.com/**

You can test directly there if you prefer.

---

## Step-by-Step Testing Procedure

### Step 1: Start Local HTTP Server

```bash
cd /Users/maxgershfield/OASIS_CLEAN/portal
python3 -m http.server 8080
```

You should see:
```
Serving HTTP on 0.0.0.0 port 8080 (http://0.0.0.0:8080/) ...
```

**Keep this terminal window open** - the server needs to keep running.

### Step 2: Open Portal in Browser

Open your browser and navigate to:
```
http://localhost:8080/portal.html
```

Or if you prefer:
```
http://127.0.0.1:8080/portal.html
```

### Step 3: Verify Portal Loads

✅ **Check:**
- Portal page loads without errors
- No 404 errors in browser console
- Portal navigation tabs are visible
- No JavaScript errors in console (F12 → Console tab)

### Step 4: Check Browser Console

Press `F12` (or `Cmd+Option+I` on Mac) to open Developer Tools.

**Check Console Tab:**
- Look for any red error messages
- Check if `STARAPIClient` is loaded (type `STARAPIClient` in console - should show object)
- Check if `getSTARAPIBaseURL()` function exists

**Expected Console Messages:**
- Should see STAR API client loaded
- May see some warnings about API connectivity (normal if APIs not running)

### Step 5: Test Authentication

1. **Try to log in:**
   - Click "Login" button (if visible)
   - Enter credentials
   - Check if authentication works

2. **Or check if already logged in:**
   - Check localStorage: `F12` → Application/Storage → Local Storage → `oasis_auth`
   - Should contain authentication token and avatar ID

### Step 6: Test STAR Tab

1. **Navigate to STAR tab**
   - Click "STAR" tab in the portal navigation

2. **Check STAR Dashboard loads:**
   - Should see stats cards (NFTs, GeoNFTs, Quests, Missions, etc.)
   - Check browser console for API calls

3. **Verify API Calls:**
   - Open Network tab in Developer Tools (F12 → Network)
   - Filter by "Fetch/XHR"
   - Click STAR tab
   - Should see API calls to STAR API endpoints:
     - `/api/NFTs/load-all-for-avatar`
     - `/api/Quests/load-all-for-avatar`
     - `/api/Missions/load-all-for-avatar`
     - `/api/GeoNFTs/load-all-for-avatar`
     - `/api/OAPPs/load-all-for-avatar`

4. **Check API Responses:**
   - Click on each API call in Network tab
   - Check Response tab - should see JSON response
   - Check if responses are successful (200 status) or if there are errors

### Step 7: Test Quest Functionality

1. **Navigate to STAR tab** (if not already there)

2. **Test Quest Interface:**
   - Look for quest-related UI elements
   - Check if quests load (if you have any)
   - Check browser console for quest API calls

3. **Test Quest Builder:**
   - Try to create a new quest (if button/link exists)
   - Fill in quest form
   - Submit quest creation
   - Check Network tab for `POST /api/Quests` call
   - Verify response

### Step 8: Test Mission Functionality

1. **Navigate to STAR tab**

2. **Test Mission Interface:**
   - Look for mission-related UI elements
   - Check if missions load
   - Check browser console for mission API calls

3. **Test Mission Creation:**
   - Try to create a new mission (if button/link exists)
   - Fill in mission form
   - Submit mission creation
   - Check Network tab for `POST /api/Missions` call
   - Verify response

### Step 9: Test Other Tabs

Test other portal tabs to ensure nothing broke:

1. **Dashboard tab** - Should show overview
2. **Avatar tab** - Should show avatar info (if implemented)
3. **Wallets tab** - Should show wallets (if implemented)
4. **NFTs tab** - Should show NFT mint studio
5. **Smart Contracts tab** - Should show contract generator (if implemented)
6. **Bridges tab** - Should show bridge interface (if implemented)
7. **Developer tab** - Should show developer tools (if implemented)

### Step 10: Verify API Connectivity

**If STAR API is running locally:**

1. Check STAR API is accessible:
   ```bash
   curl http://localhost:5001/api/health
   ```
   Or open in browser: `http://localhost:5001/swagger`

2. Check if portal can reach STAR API:
   - Open portal in browser
   - Check browser console for CORS errors
   - Check Network tab for failed API calls

**If using production APIs:**

- Portal should automatically use production URLs:
  - STAR API: `https://star-api.oasisplatform.world`
  - OASIS API: `https://api.oasisplatform.world`

---

## Common Issues & Solutions

### Issue: "CORS error" in console

**Problem:** Browser blocking API requests due to CORS policy

**Solution:**
- Make sure APIs are running and CORS is configured
- For local testing, you might need to start APIs with CORS enabled for `localhost`
- Or use browser with CORS disabled (development only)

### Issue: "STARAPIClient is not defined"

**Problem:** STAR API client script not loading

**Solution:**
1. Check browser console for 404 errors
2. Verify `api/starApiClient.js` file exists
3. Check Network tab - should see `starApiClient.js` loaded
4. Verify script tag in portal.html is correct

### Issue: "getSTARAPIBaseURL is not a function"

**Problem:** star-api-config.js not loading

**Solution:**
1. Check if `star-api-config.js` is loaded before `starApiClient.js`
2. Check browser console for errors in star-api-config.js
3. Verify script order in portal.html

### Issue: API calls return 404

**Problem:** API endpoints not found

**Solution:**
1. Verify API is running
2. Check API base URL is correct (check console output from `getSTARAPIBaseURL()`)
3. Verify endpoint paths are correct (should be capitalized: `/api/Quests`, not `/api/quests`)
4. Check API Swagger/OpenAPI docs to verify endpoints exist

### Issue: API calls return 401 Unauthorized

**Problem:** Authentication token missing or invalid

**Solution:**
1. Check localStorage for `oasis_auth` token
2. Try logging in again
3. Verify token is being sent in Authorization header
4. Check if token has expired

### Issue: Portal loads but tabs are blank

**Problem:** JavaScript errors preventing tab content from loading

**Solution:**
1. Open browser console (F12)
2. Check for JavaScript errors
3. Check Network tab for failed script loads
4. Verify all JavaScript files are accessible

---

## Testing Checklist

Use this checklist to systematically test the portal:

### Basic Functionality
- [ ] Portal loads without errors
- [ ] Navigation tabs are visible and clickable
- [ ] No JavaScript errors in console
- [ ] All JavaScript files load successfully

### Authentication
- [ ] Can log in (or already logged in)
- [ ] Authentication token stored in localStorage
- [ ] Avatar ID available

### STAR API Integration
- [ ] STAR API client loads (`STARAPIClient` object exists)
- [ ] STAR API base URL configured correctly
- [ ] STAR tab loads without errors
- [ ] Stats load (NFTs, Quests, Missions, GeoNFTs counts)
- [ ] Assets load (NFTs, Quests, Missions, GeoNFTs, OAPPs lists)

### Quest Functionality
- [ ] Quest interface loads
- [ ] Quests list displays (even if empty)
- [ ] Can create quest
- [ ] Quest creation API call succeeds
- [ ] Quest detail loads (if clicking on quest)

### Mission Functionality
- [ ] Mission interface loads
- [ ] Missions list displays (even if empty)
- [ ] Can create mission
- [ ] Mission creation API call succeeds
- [ ] Mission detail loads (if clicking on mission)

### API Calls Verification
- [ ] Network tab shows API calls to STAR API
- [ ] API endpoints use correct paths (capitalized)
- [ ] API responses are successful (200 status) or show meaningful errors
- [ ] No CORS errors

### Error Handling
- [ ] Errors are handled gracefully
- [ ] User-friendly error messages (if any)
- [ ] Console shows helpful error information
- [ ] Portal doesn't crash on API errors

---

## Advanced Testing

### Test with Network Throttling

1. Open Developer Tools (F12)
2. Go to Network tab
3. Set throttling to "Slow 3G"
4. Test portal functionality
5. Verify loading states work correctly

### Test Error Scenarios

1. **Stop STAR API:**
   - Stop local STAR API server
   - Test portal - should handle errors gracefully

2. **Invalid credentials:**
   - Try logging in with wrong credentials
   - Verify error handling

3. **Network failure:**
   - Disconnect internet
   - Test portal - should show appropriate errors

### Test Different Browsers

Test in multiple browsers:
- Chrome
- Firefox
- Safari (Mac)
- Edge

### Test on Mobile/Tablet

1. Use browser dev tools device emulation
2. Or test on actual mobile device
3. Verify responsive design works

---

## Debugging Tips

### Check Console Logs

Always check browser console for:
- Errors (red messages)
- Warnings (yellow messages)
- API call logs
- State information

### Use Network Tab

Network tab shows:
- All HTTP requests
- Request/response headers
- Response data
- Request timing
- Status codes

### Use Application/Storage Tab

Check:
- LocalStorage for authentication data
- SessionStorage (if used)
- Cookies (if used)

### Use Sources/Debugger Tab

- Set breakpoints in JavaScript
- Step through code
- Inspect variables
- Check call stack

---

## Next Steps After Testing

1. **Document any bugs found**
2. **Fix any issues discovered**
3. **Test fixes**
4. **Update integration summary**
5. **Deploy to production** (when ready)






























