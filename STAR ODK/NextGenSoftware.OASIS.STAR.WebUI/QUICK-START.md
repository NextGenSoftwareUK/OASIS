# 🌟 STAR Web UI - Quick Start Guide

## 🚀 Getting Started

The STAR Web UI now uses a clean REST API approach that directly calls the STAR API from STAR ODK!

### Option 0: Test REST API (Recommended)
1. **Double-click** `Test-REST-API.bat`
2. This will:
   - Build both backend and frontend
   - Start the backend server
   - Test the REST API endpoints
3. Shows ✅ or ❌ for each component

### Option 1: Production Build
1. **Double-click** `Start-STAR-WebUI.bat`
2. This will:
   - Install frontend dependencies
   - Build the React app
   - Start the .NET backend server
3. **Open your browser** to: `https://localhost:7001`

### Option 2: Development Mode
1. **Double-click** `Start-Development.bat`
2. This will start both frontend and backend in separate windows
3. **Frontend**: `http://localhost:3000` (with hot reload)
4. **Backend**: `https://localhost:7001`

## 🎯 What You'll See

### 🌟 **Dashboard**
- STAR connection status
- Real-time stats (OAPPs, Quests, NFTs, Avatars)
- Current avatar information
- Recent activity feed

### 🛠 **STARNET Asset Management**
- **OAPPs** - Omniverse Applications
- **Quests** - Interactive adventures
- **NFTs** - Digital assets
- **GeoNFTs** - Location-based NFTs
- **Missions** - Mission management
- **Chapters** - Story content
- **Celestial Bodies** - Planets, stars, moons
- **Celestial Spaces** - Galaxies, universes
- **Runtimes** - Runtime environments
- **Libraries** - Code libraries
- **Templates** - OAPP templates
- **Inventory** - Item management
- **Plugins** - Extensions
- **Geo Hotspots** - Location-based interactions

### 🏪 **STARNET Store**
- Browse community assets
- Search and discover content
- Download and install assets

## 🔧 **Features**

### ✨ **Real-time Updates**
- Live connection status
- Instant notifications
- Progress tracking
- SignalR integration

### 🎨 **Modern UI**
- Dark theme with gradient accents
- Smooth animations (Framer Motion)
- Responsive design
- Material-UI components

### 🔌 **API Integration**
- RESTful API endpoints
- Swagger documentation at `/swagger`
- SignalR real-time communication
- CORS enabled for frontend

## 🛠 **Technical Stack**

### Frontend
- **React 18** with TypeScript
- **Material-UI** for components
- **Framer Motion** for animations
- **React Query** for data fetching
- **SignalR** for real-time updates
- **React Router** for navigation

### Backend
- **ASP.NET Core 8.0**
- **SignalR** for real-time communication
- **Swagger** for API documentation
- **AutoMapper** for object mapping
- **CORS** enabled

## 🚨 **Troubleshooting**

### If you get SSL certificate errors:
1. Open `https://localhost:7001` in Chrome
2. Click "Advanced" → "Proceed to localhost (unsafe)"
3. Or use `http://localhost:5000` (HTTP version)

### If frontend dependencies fail to install:
1. Make sure you have Node.js 18+ installed
2. Run `npm install` manually in the `ClientApp` folder

### If backend fails to start:
1. Make sure you have .NET 8.0 SDK installed
2. Run `dotnet restore` manually in the project folder

## 🎉 **You're Ready!**

The STAR Web UI is a complete, modern web interface for the OASIS STAR CLI. It provides all the functionality of the command-line interface with a beautiful, responsive web UI.

**Happy coding in the OASIS Omniverse!** 🌟
