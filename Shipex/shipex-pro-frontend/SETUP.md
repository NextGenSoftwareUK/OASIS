# Shipex Pro Frontend - Setup Guide

## Quick Start

### 1. Prerequisites

- A web server (Python, Node.js, or any static file server)
- OASIS API running and accessible
- Modern web browser (Chrome, Firefox, Safari, Edge)

### 2. Serve the Frontend

#### Option A: Python (Recommended for Quick Testing)

```bash
cd /Volumes/Storage/OASIS_CLEAN/Shipex/shipex-pro-frontend
python3 -m http.server 8000
```

Then open: `http://localhost:8000`

#### Option B: Node.js (http-server)

```bash
cd /Volumes/Storage/OASIS_CLEAN/Shipex/shipex-pro-frontend
npx http-server -p 8000 -c-1
```

#### Option C: VS Code Live Server

1. Install "Live Server" extension in VS Code
2. Right-click `index.html`
3. Select "Open with Live Server"

### 3. Configure API URL

Edit `js/shipex-api.js` and update the `baseURL`:

```javascript
this.baseURL = window.location.hostname === 'localhost' || window.location.hostname === '127.0.0.1'
    ? 'https://localhost:5004'  // Local OASIS API
    : 'https://api.oasisweb4.com';  // Production API
```

### 4. CORS Configuration

If you encounter CORS errors, you may need to:

1. **Configure OASIS API** to allow CORS from your frontend origin
2. **Use a proxy** (see Proxy Setup below)
3. **Disable CORS in browser** (development only, not recommended)

---

## Proxy Setup (For Development)

If CORS is an issue, create a simple proxy:

### Node.js Proxy (proxy-server.js)

```javascript
const express = require('express');
const { createProxyMiddleware } = require('http-proxy-middleware');
const cors = require('cors');

const app = express();
app.use(cors());

app.use('/api', createProxyMiddleware({
    target: 'https://localhost:5004',
    changeOrigin: true,
    secure: false, // For self-signed certs
    logLevel: 'debug'
}));

app.use(express.static('.'));

app.listen(8000, () => {
    console.log('Proxy server running on http://localhost:8000');
});
```

Install dependencies:
```bash
npm install express http-proxy-middleware cors
```

Run:
```bash
node proxy-server.js
```

---

## Production Deployment

### Option 1: Static Hosting (Vercel, Netlify, GitHub Pages)

1. **Push to Git repository**
2. **Connect to hosting service**
3. **Set environment variables:**
   - `OASIS_API_URL` - Your OASIS API URL
4. **Update `baseURL` in `shipex-api.js`** to use environment variable

### Option 2: Nginx

```nginx
server {
    listen 80;
    server_name shipex.yourdomain.com;

    root /path/to/shipex-pro-frontend;
    index index.html;

    location / {
        try_files $uri $uri/ /index.html;
    }

    location /api {
        proxy_pass https://api.oasisweb4.com;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
    }
}
```

### Option 3: Apache

```apache
<VirtualHost *:80>
    ServerName shipex.yourdomain.com
    DocumentRoot /path/to/shipex-pro-frontend

    <Directory /path/to/shipex-pro-frontend>
        Options Indexes FollowSymLinks
        AllowOverride All
        Require all granted
    </Directory>

    ProxyPass /api https://api.oasisweb4.com/api
    ProxyPassReverse /api https://api.oasisweb4.com/api
</VirtualHost>
```

---

## Testing Checklist

### Authentication Flow
- [ ] Can register new OASIS avatar
- [ ] Can login with existing avatar
- [ ] Merchant profile created/linked correctly
- [ ] JWT token stored and used for API calls

### Dashboard
- [ ] Stats cards display correctly
- [ ] Shipments table loads
- [ ] Filters work (status, search)
- [ ] Navigation works

### Quote Request
- [ ] Form validation works
- [ ] Quotes returned from API
- [ ] Quote selection works
- [ ] Navigation to confirmation

### Shipment Confirmation
- [ ] Selected quote displayed
- [ ] Customer form works
- [ ] Shipment created successfully
- [ ] Tracking number displayed

### Tracking
- [ ] Can search by tracking number
- [ ] Timeline displays correctly
- [ ] Status badges show correct colors
- [ ] Copy tracking number works

### Markups
- [ ] List displays markups
- [ ] Create markup works
- [ ] Edit markup works
- [ ] Delete markup works

### Settings
- [ ] Merchant info displays
- [ ] QuickBooks OAuth opens popup
- [ ] Connection status updates

---

## Troubleshooting

### CORS Errors

**Error:** `Access to fetch at '...' from origin '...' has been blocked by CORS policy`

**Solution:**
1. Use proxy server (see Proxy Setup)
2. Configure OASIS API CORS settings
3. Use browser extension to disable CORS (dev only)

### Authentication Fails

**Error:** `Authentication failed` or `No token received`

**Solution:**
1. Check OASIS API is running
2. Verify API URL in `shipex-api.js`
3. Check browser console for detailed errors
4. Verify avatar credentials

### API Calls Fail

**Error:** `Network error` or `Failed to fetch`

**Solution:**
1. Check OASIS API is accessible
2. Verify API endpoints exist
3. Check browser network tab for request details
4. Verify JWT token is being sent

### Merchant Not Found

**Error:** `Merchant not found for this avatar`

**Solution:**
1. Ensure `AvatarId` field is in Merchant model
2. Check merchant was created via `create-from-avatar`
3. Verify avatar ID is correct

---

## Development Tips

### Hot Reload

Use a development server with hot reload:
- **VS Code Live Server** - Auto-reloads on file changes
- **BrowserSync** - `npx browser-sync start --server --files "**/*"`

### Debugging

1. **Browser DevTools:**
   - F12 to open
   - Check Console for errors
   - Check Network tab for API calls
   - Check Application tab for localStorage

2. **API Debugging:**
   - Add `console.log` in `shipex-api.js`
   - Check request/response in Network tab
   - Verify JWT token in request headers

3. **State Debugging:**
   - Check localStorage: `localStorage.getItem('shipex_token')`
   - Check merchant: `localStorage.getItem('shipex_merchant')`

---

## File Structure

```
shipex-pro-frontend/
├── index.html          # Main entry point
├── css/
│   └── styles.css      # All styles
├── js/
│   ├── shipex-api.js   # API client
│   ├── router.js       # Routing
│   ├── auth.js         # Authentication
│   ├── dashboard.js    # Dashboard
│   ├── quote.js        # Quote request
│   ├── confirm.js      # Shipment confirmation
│   ├── tracking.js     # Tracking
│   ├── markups.js      # Markup management
│   ├── settings.js     # Settings
│   ├── utils.js        # Utilities
│   └── app.js          # App initialization
└── README.md           # Documentation
```

---

## Next Steps

1. **Test locally** with OASIS API
2. **Fix any CORS issues** using proxy if needed
3. **Test all flows** (auth, quote, shipment, tracking)
4. **Deploy to staging** environment
5. **Test in production** environment

---

**Status:** Ready to run  
**Last Updated:** January 2025
