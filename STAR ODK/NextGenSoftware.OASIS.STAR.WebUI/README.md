# STAR Web UI

A modern web interface for the OASIS STAR CLI, providing the same functionality as the command-line interface with a beautiful, responsive web UI.

## Features

### 🌟 Core STAR Operations
- **STAR Management** - Ignite/extinguish STAR with real-time status
- **Avatar Management** - Create, manage, and beam in avatars
- **Karma System** - Track and manage karma points

### 🚀 STARNET Asset Management
- **OAPPs** - Omniverse Applications (create, publish, install, manage)
- **Quests** - Interactive quests and adventures
- **NFTs** - Digital assets and collectibles
- **GeoNFTs** - Location-based NFTs
- **Missions** - Mission management and tracking
- **Chapters** - Story chapters and content
- **Celestial Bodies** - Planets, stars, moons
- **Celestial Spaces** - Galaxies, universes
- **Runtimes** - Runtime environments
- **Libraries** - Code libraries and components
- **Templates** - OAPP templates and starters
- **Inventory** - Item management
- **Plugins** - Extensions and plugins
- **Geo Hotspots** - Location-based interactive points

### 🏪 STARNET Store
- Asset marketplace
- Community sharing
- Search and discovery

### 🔄 Real-time Features
- Live status updates via SignalR
- Real-time notifications
- Progress tracking
- Connection monitoring

## Technology Stack

### Backend
- **ASP.NET Core 8.0** - Web API
- **SignalR** - Real-time communication
- **AutoMapper** - Object mapping
- **Swagger** - API documentation

### Frontend
- **React 18** - UI framework
- **TypeScript** - Type safety
- **Material-UI (MUI)** - Component library
- **Framer Motion** - Animations
- **React Query** - Data fetching and caching
- **React Router** - Navigation
- **SignalR Client** - Real-time updates

## Getting Started

### Prerequisites
- .NET 8.0 SDK
- Node.js 18+ and npm
- Visual Studio 2022 or VS Code

### Backend Setup

1. Navigate to the backend directory:
```bash
cd "STAR ODK/NextGenSoftware.OASIS.STAR.WebUI"
```

2. Restore NuGet packages:
```bash
dotnet restore
```

3. Update connection strings in `appsettings.json` if needed

4. Run the backend:
```bash
dotnet run
```

The API will be available at `https://localhost:7001`

### Frontend Setup

1. Navigate to the frontend directory:
```bash
cd "STAR ODK/NextGenSoftware.OASIS.STAR.WebUI/ClientApp"
```

2. Install dependencies:
```bash
npm install
```

3. Start the development server:
```bash
npm start
```

The web UI will be available at `http://localhost:3000`

## Usage

### 1. Connect to STAR
- Open the web UI
- Click "Ignite STAR" to connect to the OASIS
- Monitor connection status in the top navigation

### 2. Manage Avatars
- Navigate to Avatars section
- Create new avatars or beam in existing ones
- Manage karma and user profiles

### 3. Create and Manage Assets
- Use the sidebar to navigate to different asset types
- Create new OAPPs, Quests, NFTs, etc.
- Publish assets to STARNET
- Install and manage downloaded assets

### 4. Explore STARNET Store
- Browse the marketplace
- Search for assets
- Download and install community creations

## API Endpoints

### STAR Operations
- `POST /api/star/ignite` - Ignite STAR
- `POST /api/star/extinguish` - Extinguish STAR
- `GET /api/star/status` - Get STAR status
- `GET /api/star/avatar/current` - Get current avatar

### STARNET Operations
- `GET /api/starnet/oapps` - List all OAPPs
- `POST /api/starnet/oapps` - Create OAPP
- `PUT /api/starnet/oapps/{id}` - Update OAPP
- `DELETE /api/starnet/oapps/{id}` - Delete OAPP
- `POST /api/starnet/oapps/{id}/publish` - Publish OAPP
- `POST /api/starnet/oapps/{id}/download-install` - Install OAPP

Similar endpoints exist for all asset types (quests, nfts, etc.)

## Real-time Updates

The application uses SignalR for real-time updates:
- STAR status changes
- Asset creation/updates
- Progress notifications
- Error messages

## Development

### Project Structure
```
STAR ODK/NextGenSoftware.OASIS.STAR.WebUI/
├── Controllers/          # API controllers
├── Services/            # Business logic services
├── Hubs/               # SignalR hubs
├── ClientApp/          # React frontend
│   ├── src/
│   │   ├── components/ # React components
│   │   ├── pages/      # Page components
│   │   ├── services/   # API services
│   │   ├── hooks/      # Custom hooks
│   │   └── types/      # TypeScript types
│   └── public/         # Static assets
└── README.md
```

### Adding New Features

1. **Backend**: Add new endpoints to controllers
2. **Frontend**: Create new pages/components
3. **Types**: Define TypeScript interfaces
4. **Services**: Add API service methods
5. **Real-time**: Add SignalR events if needed

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Test thoroughly
5. Submit a pull request

## License

This project is part of the OASIS ecosystem. Please refer to the main OASIS license.

## Support

For issues and questions:
- Check the OASIS documentation
- Create an issue in the repository
- Join the OASIS community

---

**STAR Web UI** - Bringing the power of the OASIS STAR CLI to the web! 🌟
