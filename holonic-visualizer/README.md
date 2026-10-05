# Holonic Data Visualizer

A Three.js-based visualizer for holonic data that displays OAPPs (OASIS Applications) as celestial bodies (stars, planets, moons) in a cyberspace ontology.

## Features

- **Celestial Body Representation**: OAPPs are visualized as stars, planets, or moons based on their holon count
  - **Moon** (Basic App): 200-800 holons
  - **Planet** (Medium App): 800-3,000 holons
  - **Star** (Major App): 3,000-15,000+ holons
- **Holon Particles**: Holons are sampled and represented as particles orbiting their parent OAPP (performance optimized for large datasets)
- **Cyberspace Aesthetics**: Neon colors, glowing effects, and particle fields
- **Interactive Controls**: Camera controls, orbit toggles, and data generation
- **Real-time Updates**: Smooth animations and particle systems
- **Scalable Visualization**: Handles thousands of holons with intelligent sampling

## Installation

```bash
npm install
```

## Development

```bash
npm run dev
```

The visualizer will open at `http://localhost:3000`

## Build

```bash
npm run build
```

## Usage

1. Click "Generate Mock Data" to create sample holonic data
2. Use mouse to orbit around the visualization
3. Toggle orbits and labels using the control panel
4. Reset camera to return to default view

## Architecture

- **HolonicVisualizer**: Main visualization controller
- **CelestialBody**: Represents OAPPs as celestial bodies
- **HolonParticleSystem**: Manages free-floating holon particles
- **CyberspaceEffects**: Background effects (grid, stars, particles)
- **MockDataGenerator**: Generates sample holonic data

## OASIS API Integration

The visualizer can now connect to the local OASIS API to load real holon data.

### Configuration

Create a `.env` file in the project root (or use environment variables):

```env
VITE_OASIS_API_URL=https://localhost:5004
VITE_OASIS_USERNAME=OASIS_ADMIN
VITE_OASIS_PASSWORD=Uppermall1!
```

### Usage

1. Ensure the local OASIS API is running (see `/ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI`)
2. Click "Load from OASIS" button in the control panel
3. The visualizer will authenticate and fetch all holons and OAPPs
4. Data is automatically transformed and visualized as celestial bodies

### API Client

- **OASISClient**: Handles authentication (JWT tokens) and API requests
- **OASISDataTransformer**: Transforms OASIS API data to visualizer format
- Automatic token refresh and error handling
- Performance optimization for large datasets (sampling)

## Data Structure

```javascript
{
  oapps: [
    {
      id: "oapp-1",
      name: "OAPP Name",
      celestialType: "star" | "planet" | "moon",
      metadata: {}
    }
  ],
  holons: [
    {
      id: "holon-1",
      name: "Holon Name",
      holonType: "Mission" | "NFT" | "Wallet" | ...,
      oappId: "oapp-1",
      metadata: {},
      providerKeys: {}
    }
  ]
}
```

