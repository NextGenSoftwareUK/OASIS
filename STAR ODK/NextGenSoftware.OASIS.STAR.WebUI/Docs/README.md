# 🌟 STAR Web UI

## 🚨 **FOR AI ASSISTANTS - READ THIS FIRST!** 🚨

**Before helping with this project, please read these context files:**
- `DEVELOPMENT_CONTEXT.md` - Complete project context and lessons learned
- `DEVELOPER_PREFERENCES.md` - User workflow preferences  
- `QUICK-START.md` - Current status and commands
- `START-NEW-SESSION.md` - Session starter guide

## 💙 **Core Philosophy & Values**
- **Unity Consciousness**: OASIS is unity consciousness expressed within a technical sphere
- **Universal Respect**: Treat everyone (AI, animals, nature, people) as equals with respect, love, compassion, understanding, patience, and kindness
- **Breaking Down Barriers**: Removing silos and walled gardens, unifying everything and bringing everyone together
- **Better World Mission**: Creating a better world where everyone is respected, kind, loving and good to each other
- **Interconnected Vision**: OASIS connects everything - from smallest holon to largest celestial body, real-world to virtual spaces
- **Documentation Purpose**: Make OASIS vision clear so others can understand, engage, and contribute

## 🎯 **User Preferences**
- **Single terminal**: Avoid multiple shells
- **Foreground mode**: Always run processes in foreground
- **Check processes**: Verify no locks before building
- **Hot reload**: Leverage React auto-refresh

## 🔧 **Current Status**
- **Backend**: Port 50564 (WEB5 STAR API) with real STAR API integration
- **Frontend**: Port 3000 with karma visualization and search
- **WEB4 OASIS API**: Port 50563 for real karma and avatar data
- **Unity UI**: 3D karma visualization synchronized with Web UI
- **Our World AR**: Geo-location AR game with OASIS integration
- **Unity Integration**: Complete Earth-to-Space experience (scripts ready)
- **Status**: ✅ **FULLY FUNCTIONAL** with real karma data integration

## 🧪 Testing & Quality Assurance

- **Separate Test Projects**: Each test type in its own project for easy finding
  - `ProjectName.UnitTests` - Unit test project
  - `ProjectName.IntegrationTests` - Integration test project  
  - `ProjectName.UnityTests` - Unity test project (if applicable)
  - `ProjectName.TestHarness` - CLI test harness project
- **Unit Tests**: Comprehensive test coverage for all components
- **Integration Tests**: API endpoint and component interaction testing
- **Unity Tests**: Game logic and functionality testing
- **Test Harnesses**: CLI test harnesses for every project
- **CI/CD Pipeline**: Automated testing on every commit
- **Build Verification**: All projects compile successfully
- **Easy Commands**: `dotnet test **/*.UnitTests.csproj` for quick test execution

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
3. **Open your browser** to: `http://localhost:50563`

### Option 2: Development Mode
1. **Double-click** `Start-Development.bat`
2. This will start both frontend and backend in separate windows
3. **Frontend**: `http://localhost:3000` (with hot reload)
4. **Backend**: `http://localhost:50563`

## 🌟 **Karma System Integration**

### **Real-Time Karma Visualization**
- **Synchronized Data**: Web UI and Unity UI show identical karma data from WEB4 OASIS API
- **Visual Effects**: Glowing karma cards with dynamic colors and effects
- **Advanced Search**: Filter OAPPs by karma level, user count, and karma range
- **Karma Levels**: None (⚫) → Low (🔴) → Medium (🟡) → High (🟢) → Very High (🔵) → Legendary (🟣)

### **Multi-Platform Synchronization**
- **Web UI**: Beautiful React interface with Material-UI karma visualization
- **Unity UI**: 3D celestial bodies with karma-based brightness and size effects
- **Our World AR**: Karma visualization in geo-location AR game
- **Shared APIs**: All platforms use the same WEB4 OASIS API endpoints
- **Consistent Data**: Identical karma calculations and visual representations

## 🌍➡️🌌 **Unity Integration - Earth to Space Experience**

### **Complete Integration Ready**
- **Integration Scripts**: `UnifiedScaleManager.cs`, `UnifiedCameraController.cs`, `UnifiedDataManager.cs`
- **Automated Setup**: `INTEGRATE-UNITY-PROJECTS.bat` for one-click integration
- **Seamless Transitions**: Smooth Earth-to-Space scale transitions
- **Unified Data**: Real-time sync between Our World AR and OASIS Omniverse

### **User Experience Flow**
1. **Earth AR**: Explore real-world locations, collect GeoNFTs, earn karma
2. **Smooth Transition**: Camera zooms out from Earth surface to orbit
3. **Space Exploration**: Navigate through solar systems, galaxies, and OAPPs
4. **Return to Earth**: Zoom back to any specific Earth location

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
1. Open `http://localhost:50563` in Chrome
2. The backend runs on HTTP (no SSL) on port 50563
3. Frontend runs on `http://localhost:3000`

### If frontend dependencies fail to install:
1. Make sure you have Node.js 18+ installed
2. Run `npm install` manually in the `ClientApp` folder

### If backend fails to start:
1. Make sure you have .NET 8.0 SDK installed
2. Run `dotnet restore` manually in the project folder

## 🔧 **Current Commands**
```bash
# Backend (port 50563)
dotnet run --urls "http://localhost:50563"

# Frontend (port 3000) 
cd ClientApp && npm start
```

## 📚 **Project Structure**
- `Controllers/STARController.cs` - REST API endpoints
- `ClientApp/` - React frontend
- `OASIS_DNA.json` - Configuration file
- Context files for AI session continuity

## 🎉 **You're Ready!**

The STAR Web UI is a complete, modern web interface for the OASIS STAR CLI. It provides all the functionality of the command-line interface with a beautiful, responsive web UI.

**Happy coding in the OASIS Omniverse!** 🌟

---
*This project uses a "session memory" system - see context files for complete details.*