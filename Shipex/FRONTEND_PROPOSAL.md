# Shipex Pro Frontend Components Proposal

## Overview

Based on analysis of the backend API, OASIS avatar authentication patterns, and modern logistics app UI inspiration, here's the comprehensive frontend component proposal.

---

## 🔐 Authentication Strategy

### **Use OASIS Avatar Auth (Extend Existing)**

**Recommendation:** Extend OASIS avatar authentication instead of creating separate merchant auth endpoints.

**Why:**
- Avatar auth already exists and works (`/api/avatar/authenticate`, `/api/avatar/register`)
- Merchants can use their OASIS avatar credentials
- Reuses existing JWT token system
- Consistent with zypherpunk-wallet-ui pattern

**Implementation:**
- Use same `avatarAPI.login()` and `avatarAPI.register()` from zypherpunk-wallet-ui
- After avatar login, fetch/create merchant profile linked to avatar ID
- Store merchant info (company name, tier, etc.) in merchant collection with `AvatarId` foreign key
- Frontend stores both avatar token and merchant context

**New Backend Endpoint Needed:**
```
GET /api/shipexpro/merchant/by-avatar/{avatarId}
POST /api/shipexpro/merchant/create-from-avatar
```
- These would link avatar to merchant profile
- Create merchant profile on first login if doesn't exist

---

## 📦 Shipments List Endpoint

### **Recommendation: Add GET Endpoint**

**Why:** Easiest and most efficient approach

**Proposed Endpoint:**
```
GET /api/shipexpro/shipments?merchantId={guid}&status={status}&limit={limit}&offset={offset}
```

**Implementation:**
- Add to `IShipmentService`:
  ```csharp
  Task<OASISResult<List<Shipment>>> GetShipmentsByMerchantIdAsync(
      Guid merchantId, 
      ShipmentStatus? status = null,
      int limit = 50,
      int offset = 0
  );
  ```
- Add to `ShipexProMongoRepository`:
  ```csharp
  Task<OASISResult<List<Shipment>>> GetShipmentsByMerchantIdAsync(
      Guid merchantId, 
      ShipmentStatus? status = null,
      int limit = 50,
      int offset = 0
  );
  ```
- Add controller endpoint in new `ShipmentController` or extend existing

**Alternative (Not Recommended):** Client-side tracking would require storing all shipment IDs locally, which doesn't scale.

---

## 🎨 UI Design Inspiration

Based on comprehensive research of leading logistics apps (Easyship, Shippo, project44, ShipStation, Flexport):

### **Primary Inspiration:**
- **Easyship** - Clean, beginner-friendly dashboard (primary reference)
- **project44** - Real-time tracking visualization with timelines
- **Shippo** - Simplicity and clarity

### **Key Design Principles:**
1. **Dark Theme** - Match portal aesthetic (black/dark gray backgrounds: `#0a0a0a`, `#111111`)
2. **Real-time Status Indicators** - Color-coded badges:
   - Quote Requested: `#666666` (Gray)
   - In Transit: `#3b82f6` (Blue)
   - Delivered: `#10b981` (Green)
   - Error/Cancelled: `#ef4444` (Red)
3. **Timeline Visualization** - Vertical timeline for tracking history (inspired by project44)
4. **Card-based Layout** - Clean cards for shipments, quotes, stats (Easyship pattern)
5. **Minimalist Navigation** - Sidebar (desktop) / Bottom nav (mobile)
6. **Mobile-First** - Responsive design, touch-friendly targets
7. **Quick Actions** - Common tasks in 1-2 clicks (Shippo pattern)
8. **Progressive Disclosure** - Show advanced features only when needed

### **Color Scheme (Matching Portal):**
- Background: `#0a0a0a` (primary), `#111111` (secondary)
- Text: `#ffffff` (primary), `#999999` (secondary), `#666666` (tertiary)
- Borders: `#333333`
- Accents: White buttons, subtle gradients
- Status Colors (from research):
  - Quote Requested: `#666666` (Gray)
  - In Transit: `#3b82f6` (Blue)
  - Delivered: `#10b981` (Green)
  - Error/Cancelled: `#ef4444` (Red)
- Typography: Inter font family (matching portal)
- Spacing: 8px base unit grid

---

## 📱 Component Structure

### **1. Authentication Component** (`auth.html` / `auth.js`)

**Purpose:** Merchant login/registration using OASIS avatar

**Contains:**
- Login form (username/email, password)
- Registration form (username, email, password, company name, tier selection)
- Toggle between login/register modes
- "Connect with OASIS Avatar" messaging
- After successful auth:
  - Check if merchant profile exists
  - If not, show merchant profile creation form (company name, tier)
  - Link avatar to merchant profile
- Error handling and validation
- Loading states

**API Calls:**
- `avatarAPI.login(username, password)` - OASIS avatar auth
- `avatarAPI.register(registrationData)` - OASIS avatar registration
- `GET /api/shipexpro/merchant/by-avatar/{avatarId}` - Get merchant profile
- `POST /api/shipexpro/merchant/create-from-avatar` - Create merchant profile

**Design:** Similar to `AvatarAuthScreen.tsx` but with merchant-specific fields

---

### **2. Dashboard Component** (`dashboard.html` / `dashboard.js`)

**Purpose:** Main merchant dashboard with stats and shipment list

**Contains:**
- **Header:**
  - Company name
  - Rate limit tier badge
  - Quick actions (New Shipment, Settings)
  - Logout button

- **Stats Cards (4 cards in row):**
  - Total Shipments (with trend arrow)
  - Active Shipments (in transit)
  - Completed (delivered)
  - Total Revenue (if available)

- **Shipments Table/List:**
  - Columns: Tracking #, Status, Carrier, Origin → Destination, Date, Actions
  - Status badges (color-coded)
  - Click row to view details
  - Filter dropdown (All, In Transit, Delivered, etc.)
  - Search box (by tracking number)
  - Pagination

- **Quick Actions Sidebar:**
  - Request Quote
  - Track Shipment
  - View Markups
  - QuickBooks Settings

**API Calls:**
- `GET /api/shipexpro/shipments?merchantId={id}` - List shipments
- `GET /api/shipexpro/merchant/{id}` - Get merchant stats

**Design:** Card-based layout, clean table, modern stats visualization

---

### **3. Quote Request Component** (`quote-request.html` / `quote-request.js`)

**Purpose:** Request shipping quotes

**Contains:**
- **Two-column form layout:**

  **Left Column - Package Details:**
  - Dimensions (L x W x H) with unit selector (inches/cm)
  - Weight with unit selector (lbs/kg)
  - Service level selector (Standard/Express/Overnight)

  **Right Column - Addresses:**
  - Origin address (Street, City, State, Postal Code, Country)
  - Destination address (same fields)
  - "Use saved address" toggle/selector

- **Quote Results Display:**
  - List of quote options in cards
  - Each card shows: Carrier logo/name, Service name, Price, Estimated days, "Select" button
  - Sort by price or speed
  - Expiration timer
  - "Request New Quote" button

- **Selected Quote Summary:**
  - Shows selected quote details
  - "Confirm Shipment" button (navigates to confirmation)

**API Calls:**
- `POST /api/shipexpro/shipox/quote-request` - Request quotes

**Design:** Clean form, card-based quote display, clear pricing

---

### **4. Shipment Confirmation Component** (`confirm-shipment.html` / `confirm-shipment.js`)

**Purpose:** Confirm and create shipment after quote selection

**Contains:**
- **Review Section:**
  - Selected quote summary (carrier, service, price, ETA)
  - Package details (dimensions, weight)
  - Origin and destination addresses

- **Customer Information Form:**
  - Name
  - Email
  - Phone
  - Delivery instructions (optional)

- **Actions:**
  - "Confirm & Create Shipment" button (primary)
  - "Back to Quotes" button
  - "Save as Draft" (optional)

- **Success State:**
  - Tracking number display (large, copyable)
  - Label download button
  - "Track Shipment" link
  - "Create Another" button

**API Calls:**
- `POST /api/shipexpro/shipox/confirm-shipment` - Create shipment

**Design:** Review-focused, clear confirmation flow

---

### **5. Tracking Component** (`tracking.html` / `tracking.js`)

**Purpose:** Track individual shipments

**Contains:**
- **Tracking Number Input:**
  - Search box at top
  - "Track" button
  - Recent tracking numbers (dropdown)

- **Tracking Results:**
  - **Current Status Card:**
    - Large status badge (color-coded)
    - Current location (if available)
    - Estimated delivery date
    - Tracking number (copyable)

  - **Timeline Visualization:**
    - Vertical timeline showing status history
    - Each event: Status name, timestamp, location, description
    - Current status highlighted
    - Animated progress indicator

  - **Shipment Details:**
    - Origin and destination
    - Carrier info
    - Package details
    - Label download

- **Actions:**
  - "Refresh Status" button
  - "Share Tracking" button (generates shareable link)
  - "View Label" button

**API Calls:**
- `GET /api/shipexpro/shipox/track/{trackingNumber}` - Get tracking info

**Design:** Timeline-focused, clear status progression, visual tracking

---

### **6. QuickBooks OAuth Component** (`quickbooks-connect.html` / `quickbooks-connect.js`)

**Purpose:** Connect QuickBooks for automated invoicing

**Contains:**
- **Connection Status:**
  - "Not Connected" / "Connected" badge
  - Company name (if connected)
  - Last sync time

- **Connect Flow:**
  - "Connect QuickBooks" button
  - Opens QuickBooks OAuth in popup/new window
  - Shows connection progress
  - Success message on completion

- **Settings (if connected):**
  - Auto-invoice toggle
  - Invoice template selector
  - Sync frequency
  - "Disconnect" button

- **Test Connection:**
  - "Test Sync" button
  - Shows last invoice created

**API Calls:**
- `GET /api/shipexpro/quickbooks/authorize?merchantId={id}` - Get auth URL
- `GET /api/shipexpro/quickbooks/callback?code=...&realmId=...` - OAuth callback
- `POST /api/shipexpro/quickbooks/refresh-token?merchantId={id}` - Refresh token

**Design:** Simple connection flow, clear status indicators

---

### **7. Markup Management Component** (`markups.html` / `markups.js`)

**Purpose:** Manage pricing markups

**Contains:**
- **Markups List:**
  - Table/cards showing existing markups
  - Columns: Name, Type, Value, Conditions, Actions
  - "Create New" button

- **Create/Edit Markup Form:**
  - Markup name
  - Type selector (Percentage / Fixed Amount)
  - Value input
  - Conditions section:
    - Carrier selector (multi-select)
    - Service level selector
    - Weight range (min/max)
    - Destination country selector
  - "Save" / "Cancel" buttons

- **Delete Confirmation:**
  - Modal confirming deletion

**API Calls:**
- `GET /api/shipexpro/markups?merchantId={id}` - List markups
- `GET /api/shipexpro/markups/{id}` - Get markup
- `POST /api/shipexpro/markups` - Create markup
- `PUT /api/shipexpro/markups/{id}` - Update markup
- `DELETE /api/shipexpro/markups/{id}` - Delete markup

**Design:** Table-based list, modal forms for create/edit

---

### **8. Webhook Management Component** (`webhooks-admin.html` / `webhooks-admin.js`)

**Purpose:** Admin view of webhook events (OPTIONAL)

**Recommendation:** **Start without this, add later if needed**

**Why:**
- Most merchants don't need to see webhook events
- Backend logging is sufficient for debugging
- Can add later if merchants request it

**If we add it, should contain:**
- List of webhook events (table)
- Filter by event type, status, date
- Event details modal (payload, response, retry count)
- "Retry Failed" button
- "Retry All Failed" button

**API Calls:**
- `GET /api/shipexpro/admin/webhooks` - List events
- `GET /api/shipexpro/admin/webhooks/{id}` - Get event
- `POST /api/shipexpro/admin/webhooks/{id}/retry` - Retry event
- `POST /api/shipexpro/admin/webhooks/retry-all` - Retry all failed

---

### **9. API Client Module** (`shipex-api.js`)

**Purpose:** Centralized API client (similar to `oasisApi.js`)

**Contains:**
- Base URL configuration
- Auth header management (JWT token from avatar auth)
- Request helper methods:
  ```javascript
  // Merchant
  getMerchantByAvatar(avatarId)
  createMerchantFromAvatar(avatarId, merchantData)
  
  // Shipments
  getShipments(merchantId, filters)
  getShipment(shipmentId)
  trackShipment(trackingNumber)
  
  // Quotes
  requestQuote(rateRequest)
  confirmShipment(orderRequest)
  
  // Markups
  getMarkups(merchantId)
  createMarkup(markup)
  updateMarkup(markupId, markup)
  deleteMarkup(markupId)
  
  // QuickBooks
  getQuickBooksAuthUrl(merchantId)
  refreshQuickBooksToken(merchantId)
  ```
- Error handling (OASISResult format)
- Response normalization

---

### **10. Shared Styles** (`shipex-styles.css`)

**Purpose:** Consistent styling matching portal design

**Contains:**
- CSS variables (matching portal)
- Component styles:
  - Forms (inputs, selects, buttons)
  - Cards (stats, shipments, quotes)
  - Tables
  - Status badges
  - Timeline
  - Modals
- Responsive breakpoints
- Loading spinners
- Animations (fade-in, slide)
- Dark theme utilities

---

## 🏗️ Application Structure

### **Standalone App Structure:**
```
shipex-pro-frontend/
├── index.html              # Main entry, routing
├── auth.html              # Authentication page
├── dashboard.html         # Main dashboard
├── quote-request.html     # Quote request page
├── confirm-shipment.html  # Shipment confirmation
├── tracking.html          # Tracking page
├── markups.html           # Markup management
├── quickbooks-connect.html # QuickBooks OAuth
├── js/
│   ├── shipex-api.js      # API client
│   ├── auth.js            # Auth logic
│   ├── dashboard.js       # Dashboard logic
│   ├── quote.js           # Quote logic
│   ├── tracking.js        # Tracking logic
│   ├── markups.js         # Markup logic
│   └── quickbooks.js      # QuickBooks logic
├── css/
│   ├── shipex-styles.css  # Main styles
│   └── components.css     # Component styles
└── assets/
    └── icons/             # SVG icons
```

### **Routing:**
- Simple hash-based routing or vanilla JS router
- Example: `#dashboard`, `#quote`, `#tracking/{trackingNumber}`

---

## 📋 Implementation Priority

### **Phase 1: Core Functionality** (MVP)
1. ✅ Authentication (extend avatar auth)
2. ✅ Dashboard (stats + shipment list)
3. ✅ Quote Request
4. ✅ Shipment Confirmation
5. ✅ Tracking

### **Phase 2: Enhanced Features**
6. ✅ Markup Management
7. ✅ QuickBooks OAuth

### **Phase 3: Optional**
8. ⚠️ Webhook Admin UI (if needed)

---

## 🔧 Backend Changes Needed

### **Required:**
1. **Merchant-Avatar Linking:**
   - `GET /api/shipexpro/merchant/by-avatar/{avatarId}`
   - `POST /api/shipexpro/merchant/create-from-avatar`

2. **Shipments List:**
   - `GET /api/shipexpro/shipments?merchantId={id}&status={status}&limit={limit}&offset={offset}`
   - Add to `IShipmentService` and `IShipexProRepository`

3. **Merchant Stats:**
   - `GET /api/shipexpro/merchant/{id}/stats` (optional, can calculate client-side)

### **Optional:**
- Webhook admin endpoints (already exist, just need UI)

---

## 🎯 Next Steps

1. **Confirm approach:**
   - ✅ Use avatar auth (extend existing)
   - ✅ Add shipments list endpoint
   - ✅ Standalone app (not in portal)
   - ✅ Include QuickBooks OAuth UI
   - ⚠️ Skip webhook admin UI for now

2. **Create backend endpoints** (if confirmed)

3. **Build frontend components** (start with Phase 1)

4. **Test integration** with OASIS API

---

**Status:** Ready for implementation  
**Last Updated:** January 2025
