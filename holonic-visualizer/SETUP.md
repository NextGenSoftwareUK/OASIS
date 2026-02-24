# Setup Instructions

## Quick Start

1. **Install dependencies:**
   ```bash
   npm install
   ```

2. **Start development server:**
   ```bash
   npm run dev
   ```

3. **Open in browser:**
   The visualizer will automatically open at `http://localhost:3000`

## Troubleshooting

### OrbitControls Import Issue

If you encounter an import error for OrbitControls, try one of these import paths in `src/visualizer/HolonicVisualizer.js`:

```javascript
// Option 1 (Three.js 0.160+)
import { OrbitControls } from 'three/addons/controls/OrbitControls.js';

// Option 2 (Older Three.js versions)
import { OrbitControls } from 'three/examples/jsm/controls/OrbitControls.js';
```

### Port Already in Use

If port 3000 is already in use, modify `vite.config.js`:

```javascript
server: {
    port: 3001, // Change to available port
}
```

## Next Steps

Once the visualizer is working:

1. **Test with mock data**: Click "Generate Mock Data" button
2. **Explore controls**: Try camera rotation, zoom, and toggles
3. **Connect to STAR API**: See integration guide below

## STAR API Integration

To connect to real STAR data, you'll need to:

1. Create a STAR API client in `src/api/STARClient.js`
2. Update `src/main.js` to load data from STAR API
3. Replace `MockDataGenerator` calls with STAR API calls

Example STAR API integration:

```javascript
import { STARClient } from './api/STARClient.js';

const starClient = new STARClient({
    baseUrl: 'https://star-api.oasisweb4.com',
    apiKey: 'your-api-key'
});

// Load OAPPs and holons
const data = await starClient.loadHolonicData();
visualizer.loadData(data);
```

