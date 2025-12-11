# Shipex Pro Frontend

Modern logistics management frontend for Shipex Pro OASIS provider.

## Features

- ✅ OASIS Avatar Authentication (extends existing auth)
- ✅ Dashboard with stats and shipments list
- ✅ Quote request with multi-carrier results
- ✅ Shipment tracking with timeline visualization
- ✅ Responsive design (mobile-first)
- ✅ Dark theme matching OASIS portal

## Project Structure

```
shipex-pro-frontend/
├── index.html          # Main HTML file
├── css/
│   └── styles.css      # Main stylesheet
├── js/
│   ├── shipex-api.js   # API client
│   ├── router.js       # Hash-based routing
│   ├── auth.js         # Authentication component
│   ├── dashboard.js    # Dashboard component
│   ├── quote.js        # Quote request component
│   ├── tracking.js     # Tracking component
│   ├── markups.js      # Markup management (placeholder)
│   ├── settings.js     # Settings (placeholder)
│   ├── utils.js        # Utility functions
│   └── app.js          # App initialization
└── README.md
```

## Setup

1. **Serve the files** using a local web server:
   ```bash
   # Using Python
   python -m http.server 8000
   
   # Using Node.js (http-server)
   npx http-server -p 8000
   ```

2. **Configure API URL** in `js/shipex-api.js`:
   - Default: `https://localhost:5004` (local OASIS API)
   - Update `baseURL` for production

3. **Open in browser**:
   ```
   http://localhost:8000
   ```

## Usage

### Authentication
- Uses OASIS avatar authentication
- After login, creates/links merchant profile
- Stores JWT token and merchant context

### Dashboard
- View shipment statistics
- Browse recent shipments
- Filter by status
- Search by tracking number

### Quote Request
- Enter package details and addresses
- Get quotes from multiple carriers
- Select quote to create shipment

### Tracking
- Enter tracking number
- View status timeline
- See tracking history

## API Endpoints Used

- `POST /api/avatar/authenticate` - OASIS avatar login
- `POST /api/avatar/register` - OASIS avatar registration
- `GET /api/shipexpro/merchant/by-avatar/{avatarId}` - Get merchant
- `POST /api/shipexpro/merchant/create-from-avatar` - Create merchant
- `GET /api/shipexpro/shipments?merchantId={id}` - List shipments
- `GET /api/shipexpro/shipox/track/{trackingNumber}` - Track shipment
- `POST /api/shipexpro/shipox/quote-request` - Request quotes
- `POST /api/shipexpro/shipox/confirm-shipment` - Create shipment

## Design

Inspired by:
- **Easyship** - Clean, beginner-friendly interface
- **project44** - Real-time tracking visualization
- **Shippo** - Simplicity and clarity
- **OASIS Portal** - Dark theme consistency

## Browser Support

- Chrome/Edge (latest)
- Firefox (latest)
- Safari (latest)

## Development

### Adding New Components

1. Create component class in `js/component-name.js`
2. Register route in `js/app.js`
3. Add screen div in `index.html`
4. Add styles in `css/styles.css`

### API Client

All API calls go through `shipexAPI` singleton:
```javascript
// Example
const shipments = await shipexAPI.getShipments(merchantId);
```

## Status

✅ **Core Components Complete:**
- Authentication
- Dashboard
- Quote Request
- Tracking

⚠️ **Placeholders:**
- Markup Management
- Settings
- Shipment Confirmation

## Next Steps

1. Implement shipment confirmation flow
2. Complete markup management UI
3. Add QuickBooks OAuth UI
4. Add settings page
5. Implement backend endpoints (merchant linking, shipments list)

---

**Last Updated:** January 2025
