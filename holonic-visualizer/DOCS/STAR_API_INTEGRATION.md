# STAR API Integration Guide

## Overview

The holonic visualizer has been integrated with the STAR API (WEB5) in addition to the existing OASIS API (WEB4) support. Users can now switch between both APIs using the UI controls.

## Key Differences

### OASIS API (WEB4)
- **Port**: 5003 (HTTP) or 5004 (HTTPS)
- **Authentication**: JWT tokens via `/api/avatar/authenticate`
- **Endpoints**: `/api/data/load-all-holons`, `/api/avatar/*`
- **Response Format**: Nested `result.result` structure

### STAR API (WEB5)
- **Port**: 50564 (HTTP)
- **Authentication**: JWT tokens from OASIS API via `/api/avatar/authenticate`
- **Endpoints**: `/api/holons`, `/api/oapps`, `/api/star/*`
- **Response Format**: `OASISResult<T>` with `Result` property
- **Note**: STAR API requires authentication with OASIS API first to obtain JWT token

## Architecture

### New Files

1. **`src/api/STARClient.js`**
   - Handles STAR API authentication (beam-in)
   - Manages session-based authentication
   - Provides methods for fetching holons and OAPPs
   - Handles `OASISResult<T>` response format

### Updated Files

1. **`src/main.js`**
   - Added `STARClient` import and initialization
   - Added `loadFromSTAR()` function
   - Updated `loadFromAPI()` to route to appropriate API
   - Added API mode toggle functionality
   - Updated event listeners for new button IDs

2. **`index.html`**
   - Added API mode radio buttons (STAR/OASIS)
   - Updated button IDs (`btn-load-api` instead of `btn-load-oasis`)
   - Added API mode label display

## Usage

### Switching API Modes

1. Use the radio buttons in the control panel to switch between:
   - **STAR (WEB5)**: Default, uses STAR API
   - **OASIS (WEB4)**: Uses OASIS API

2. The current API mode is displayed below the radio buttons

### Loading Data

1. Click **"Load from API"** to fetch data from the currently selected API
2. The visualizer will:
   - Authenticate with the selected API
   - Fetch OAPPs and holons
   - Transform and visualize the data

### STAR API Specific Features

- **JWT Authentication**: Authenticates with OASIS API first to get JWT token
- **Auto-ignition**: If STAR is not ignited, the visualizer will attempt to ignite it
- **Beam-in**: Automatically beams in (authenticates) before making requests
- **Token-based**: Uses JWT tokens in Authorization header for all requests

## Configuration

### Environment Variables

Create a `.env` file in the project root:

```env
# OASIS API (WEB4)
VITE_OASIS_API_URL=http://localhost:5003
VITE_OASIS_USERNAME=OASIS_ADMIN
VITE_OASIS_PASSWORD=Uppermall1!

# STAR API (WEB5)
VITE_STAR_API_URL=http://localhost:50564
VITE_STAR_USERNAME=OASIS_ADMIN
VITE_STAR_PASSWORD=Uppermall1!

# OASIS API (WEB4) - Required for JWT authentication
VITE_OASIS_API_URL=https://localhost:5004
```

## API Endpoints Used

### STAR API

- `POST /api/star/beam-in` - Beam in (requires JWT token)
- `GET /api/star/status` - Check if STAR is ignited (no auth required)
- `POST /api/star/ignite` - Ignite STAR (requires JWT token)
- `GET /api/holons` - Get all holons (requires JWT token)
- `GET /api/oapps` - Get all OAPPs (requires JWT token)
- `GET /api/holons/load-all-for-avatar` - Get holons for current avatar (requires JWT token)
- `GET /api/holons/search?query=...` - Search holons (requires JWT token)

### OASIS API (for JWT authentication)

- `POST /api/avatar/authenticate` - Get JWT token (required before STAR API calls)

### OASIS API (for JWT authentication)

- `POST /api/avatar/authenticate` - Get JWT token (required before STAR API calls)

## Response Format Handling

### STAR API Response Format

```javascript
{
  Result: [...],  // or result: [...]
  IsError: false, // or isError: false
  Message: "..."  // or message: "..."
}
```

The `STARClient` automatically extracts the `Result` array from the `OASISResult<T>` wrapper.

### OASIS API Response Format

```javascript
{
  result: {
    result: [...]
  }
}
```

The `OASISClient` handles the nested structure.

## Data Transformation

The `OASISDataTransformer` works with both APIs because it:
- Handles both camelCase and PascalCase property names
- Supports both string and integer enum values for holon types
- Normalizes data structures from both APIs to a common format

## Error Handling

Both clients handle errors gracefully:
- Network errors show user-friendly notifications
- Authentication failures trigger re-authentication
- Empty results are handled without crashing
- Large datasets are automatically sampled for performance

## Future Enhancements

Potential improvements:
1. Add STAR API-specific seeding function
2. Support for more STAR API endpoints (missions, quests, etc.)
3. Real-time updates via WebSocket
4. Better error recovery and retry logic
5. Caching for frequently accessed data
