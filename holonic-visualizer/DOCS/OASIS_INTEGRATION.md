# OASIS Holon Data Integration Guide

## Overview
This document provides a complete guide for connecting the holonic visualizer to real holon data stored in OASIS MongoDB via the OASIS API.

## OASIS API Configuration

### Base URLs
- **Primary**: `http://oasisweb4.one`
- **AWS Alternative**: `http://44.202.138.7:8080`
- **API Base Path**: `/api`

### Authentication
```http
POST /api/avatar/authenticate
Content-Type: application/json

{
  "username": "metabricks_admin",
  "password": "Uppermall1!"
}
```

**Response Structure:**
```json
{
  "result": {
    "result": {
      "jwtToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
      "avatarId": "5f7daa80-160e-4213-9e81-94500390f31e",
      "username": "metabricks_admin"
    }
  }
}
```

**Note**: Token expires in 15 minutes. Store at `result.result.jwtToken`.

## API Endpoints

### Holon Endpoints

#### WEB4 OASIS API (Core)
```http
POST /api/holon/save-holon
POST /api/holon/search-holons
GET /api/holon/load-holon/{id}
```

#### WEB5 STAR API (STAR-specific)
```http
GET /api/holons
GET /api/holons/{id}
GET /api/holons/by-type/{type}
GET /api/holons/by-parent/{parentId}
GET /api/holons/search
GET /api/holons/load-all-for-avatar
POST /api/holons/search
```

### OAPP Endpoints
```http
GET /api/oapps
GET /api/oapps/{id}
GET /api/oapps/search
GET /api/oapps/load-all-for-avatar
POST /api/oapps/search
```

## Data Structures

### Holon Object
```javascript
{
  id: "guid",
  name: "Holon Name", // or Name
  holonType: "Mission" | "NFT" | "Wallet" | "DeFi" | "Identity" | ...,
  metadata: {}, // or MetaData
  providerKeys: {
    mongodb: "ObjectId",
    ethereum: "0x...",
    solana: "AccountPublicKey"
  },
  oappId: "oapp-guid", // May be in metadata or separate field
  parentHolonId: "parent-guid",
  createdDate: "ISO timestamp",
  modifiedDate: "ISO timestamp"
}
```

### OAPP Object
```javascript
{
  id: "oapp-guid",
  name: "OAPP Name",
  description: "Description",
  celestialType: "star" | "planet" | "moon", // Optional override
  metadata: {
    version: "1.0.0",
    holonCount: 1234
  }
}
```

## Implementation

### Step 1: Create OASIS Client

**File**: `src/api/OASISClient.js`

```javascript
export class OASISClient {
    constructor(config = {}) {
        this.baseUrl = config.baseUrl || 'http://oasisweb4.one';
        this.username = config.username || 'metabricks_admin';
        this.password = config.password || 'Uppermall1!';
        this.token = null;
        this.tokenExpiry = null;
    }

    async authenticate() {
        const response = await fetch(`${this.baseUrl}/api/avatar/authenticate`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                username: this.username,
                password: this.password
            })
        });

        const data = await response.json();
        const jwtToken = data?.result?.result?.jwtToken;
        
        if (!jwtToken) {
            throw new Error('Authentication failed: No token received');
        }

        this.token = jwtToken;
        this.tokenExpiry = Date.now() + (15 * 60 * 1000);
        return jwtToken;
    }

    async getValidToken() {
        const bufferTime = 30 * 1000;
        if (!this.token || !this.tokenExpiry || Date.now() >= (this.tokenExpiry - bufferTime)) {
            await this.authenticate();
        }
        return this.token;
    }

    async request(endpoint, options = {}) {
        const token = await this.getValidToken();
        
        const response = await fetch(`${this.baseUrl}${endpoint}`, {
            ...options,
            headers: {
                'Authorization': `Bearer ${token}`,
                'Content-Type': 'application/json',
                ...options.headers
            }
        });

        if (!response.ok) {
            if (response.status === 401) {
                await this.authenticate();
                return this.request(endpoint, options);
            }
            throw new Error(`API request failed: ${response.status}`);
        }

        return response.json();
    }

    async getAllHolons() {
        try {
            const data = await this.request('/api/holons');
            if (data.result && Array.isArray(data.result)) {
                return data.result;
            }
        } catch (error) {
            console.warn('WEB5 holons endpoint failed, trying search:', error);
        }

        // Fallback to search
        try {
            const data = await this.request('/api/holons/search', {
                method: 'POST',
                body: JSON.stringify({ ProviderKey: 'MongoOASIS' })
            });
            return data.result || [];
        } catch (error) {
            console.error('Failed to fetch holons:', error);
            return [];
        }
    }

    async getAllOAPPs() {
        try {
            const data = await this.request('/api/oapps');
            return data.result || [];
        } catch (error) {
            console.error('Failed to fetch OAPPs:', error);
            return [];
        }
    }
}
```

### Step 2: Data Transformer

**File**: `src/data/OASISDataTransformer.js`

```javascript
export class OASISDataTransformer {
    static transformHolon(holon) {
        return {
            id: holon.id || holon.Id,
            name: holon.name || holon.Name || 'Unnamed Holon',
            holonType: holon.holonType || holon.HolonType || 'Unknown',
            oappId: holon.oappId || holon.OAPPId || holon.metadata?.oappId || null,
            metadata: holon.metadata || holon.MetaData || {},
            providerKeys: holon.providerKeys || holon.ProviderKeys || {}
        };
    }

    static transformOAPP(oapp) {
        return {
            id: oapp.id || oapp.Id,
            name: oapp.name || oapp.Name || 'Unnamed OAPP',
            celestialType: oapp.celestialType || oapp.CelestialType || null,
            metadata: oapp.metadata || oapp.MetaData || {}
        };
    }

    static transformToVisualizerFormat(oapps, holons) {
        const transformedOAPPs = oapps.map(oapp => this.transformOAPP(oapp));
        const transformedHolons = holons.map(holon => this.transformHolon(holon));

        // Group holons by OAPP
        const holonsByOAPP = new Map();
        transformedHolons.forEach(holon => {
            const oappId = holon.oappId || 'unassigned';
            if (!holonsByOAPP.has(oappId)) {
                holonsByOAPP.set(oappId, []);
            }
            holonsByOAPP.get(oappId).push(holon);
        });

        // Update OAPP metadata with holon counts
        transformedOAPPs.forEach(oapp => {
            const holonCount = holonsByOAPP.get(oapp.id)?.length || 0;
            oapp.metadata.holonCount = holonCount;
        });

        return {
            oapps: transformedOAPPs,
            holons: transformedHolons
        };
    }
}
```

### Step 3: Integration

**Update**: `src/main.js`

```javascript
import { OASISClient } from './api/OASISClient.js';
import { OASISDataTransformer } from './data/OASISDataTransformer.js';

const oasisClient = new OASISClient({
    baseUrl: import.meta.env.VITE_OASIS_API_URL || 'http://oasisweb4.one',
    username: import.meta.env.VITE_OASIS_USERNAME || 'metabricks_admin',
    password: import.meta.env.VITE_OASIS_PASSWORD || 'Uppermall1!'
});

async function loadFromOASIS() {
    try {
        showNotification('Connecting to OASIS...', 'success');
        
        const [oapps, holons] = await Promise.all([
            oasisClient.getAllOAPPs(),
            oasisClient.getAllHolons()
        ]);

        const data = OASISDataTransformer.transformToVisualizerFormat(oapps, holons);
        visualizer.loadData(data);
        updateStats(data);

        showNotification(
            `Loaded from OASIS\n${oapps.length} OAPP(s)\n${holons.length} holon(s)`,
            'success'
        );
    } catch (error) {
        console.error('Failed to load from OASIS:', error);
        showNotification(`Error: ${error.message}`, 'error');
    }
}

// Add button to HTML and wire it up
document.getElementById('btn-load-oasis')?.addEventListener('click', loadFromOASIS);
```

## Alternative: Direct MongoDB Access

If API access is limited, direct MongoDB connection requires:

1. **MongoDB Connection String** (from OASIS config)
2. **MongoDB Driver**: `npm install mongodb`
3. **Direct Query**:
   ```javascript
   import { MongoClient } from 'mongodb';
   const client = new MongoClient(connectionString);
   await client.connect();
   const db = client.db('oasis');
   const holons = await db.collection('holons').find({}).toArray();
   ```

**Note**: Direct MongoDB bypasses OASIS provider abstraction.

## Environment Variables

Create `.env`:
```env
VITE_OASIS_API_URL=http://oasisweb4.one
VITE_OASIS_USERNAME=metabricks_admin
VITE_OASIS_PASSWORD=Uppermall1!
```

## Error Handling

- **Authentication Failures**: Re-authenticate and retry
- **Network Errors**: Retry with exponential backoff
- **Empty Results**: Handle gracefully, suggest mock data
- **Large Datasets**: Implement pagination if needed

## Testing

1. Test authentication and token refresh
2. Fetch small subset first
3. Test with different holon types
4. Verify holon grouping by OAPP
5. Load real data into visualizer









