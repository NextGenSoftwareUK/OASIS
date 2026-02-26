# OASIS Portal — Space

A space-themed 3D OASIS portal. Drop in, orbit the scene, and enter **DOOM** or **Quake** through large portal-style **O** rings. Your OASIS avatar identity (onode + token) is passed to each game so you play with the same identity across all titles.

## Features

- **Space theme** — Cyberspace effects (stars, grid, particles) reused from holonic-visualizer
- **Portal O rings** — Large torus geometry for each game
- **OASIS auth** — Login once, launch games with `?onode=...&token=...`
- **DOOM & Quake** — Click a portal to open the game in a new tab

## Quick Start

```bash
cd oportal-space
npm install
npm run dev
```

Open http://localhost:3010 and log in with your OASIS credentials.

## Requirements

- **ONODE** (OASIS API) running for auth — e.g. `dotnet run` in `ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI`
- **DOOM** — Run `npm start` in `doom-browser/` (port 8765)
- **Quake** — Run your Quake browser app (port 8767 by default; override via `VITE_QUAKE_URL`)

## Environment

Copy `.env.example` to `.env` and adjust:

```env
VITE_OASIS_API_URL=http://localhost:5003
VITE_OASIS_USERNAME=OASIS_ADMIN
VITE_OASIS_PASSWORD=Uppermall1!
VITE_DOOM_URL=http://localhost:8765
VITE_QUAKE_URL=http://localhost:8767
```

## Controls

- **Orbit** — Mouse drag
- ** Zoom** — Scroll
- **Enter game** — Click a portal ring

## Architecture

- **PortalScene** — Three.js scene, camera, OrbitControls, raycasting
- **PortalRing** — Torus "O" for each game, click to launch
- **CyberspaceEffects** — Stars, grid, particles (from holonic-visualizer)
- **auth.js** — OASIS login, token storage, avatar fetch
- **games.js** — Game config, `getGameLaunchUrl()` with onode + token

## Quake Logo

The Quake portal uses an **open-source 2D logo** from a public-domain SVG in `public/quake-logo.svg`. The design is based on the Quake "Q" (simple geometric shapes, PD-textlogo per Wikimedia Commons). No external model download required.

To use a 3D GLB model instead: set `modelUrl: '/quake-logo.glb'` and remove `logoUrl` in `games.js`, then add a `.glb` file to `public/`.

## Adding More Games

Edit `src/lib/games.js`:

```javascript
export const GAMES = [
  { id: 'doom', name: 'DOOM', playPath: '...', supportsStar: true, color: 0xc41e1e },
  { id: 'quake', name: 'Quake', playPath: '...', supportsStar: true, color: 0x4a90d9 },
  { id: 'mygame', name: 'My Game', playPath: 'http://localhost:8888/', supportsStar: true, color: 0x00ff00 }
];
```

Add a matching portal position in `PortalScene.js`.
