# Zenith Homes Exchange - RWA Platform

**Zenith Homes Exchange** is a premium Real World Asset (RWA) platform built on OASIS, enabling users to tokenize, fractionalize, buy, and sell real-world assets across multiple blockchains.

## 🌟 Recent Updates

### ✨ New Features

- **3D Property Visualization**: Interactive Three.js-powered 3D house models with clickable fractional ownership shares
- **Premium Property Cards**: Modern, elegant UI showcasing real estate assets with multi-chain blockchain support
- **Fractional Ownership Visualization**: Visual share wall displaying 1000+ fractional ownership shares as interactive bricks
- **Multi-Chain NFT Support**: Claim NFTs on your choice of blockchain (Solana, Ethereum, Arbitrum, Optimism, Base, Avalanche, BNB, Arweave, Aztec, Miden, Zcash)
- **Modular Home Listings**: Specialized support for modern modular/prefab homes
- **USD Currency Formatting**: All prices displayed in USD for clarity and accessibility

## 🏗️ Project Overview

The platform focuses on two core functions:

### 1. RWA Marketplace
- **Tokenization** of real-world assets (Real Estate, Automobiles, Collectibles, etc.)
- **Trading platform** for tokenized assets with multi-chain payment options
- **Verification and documentation** of asset ownership
- **Secure storage** of ownership documents on IPFS
- **Price history tracking** and ownership transfer monitoring
- **Fractional ownership** with visual 3D representation

### 2. Smart Trust Creation
- **Wyoming Statutory Trust** creation wizard
- **Automated contract generation** for property tokenization
- **Trust setup and configuration** with legal compliance
- **Property details management** with geolocation support

## 🎨 Key Features

### Property Visualization
- **3D Interactive Models**: Explore properties in 3D with clickable fractional shares
- **Share Wall Component**: Visual representation of fractional ownership (up to 1000 shares)
- **Property Cards**: Premium card design with image galleries, price tracking, and multi-chain indicators
- **Property Details Pages**: Comprehensive property information with interactive share purchasing

### Multi-Chain Support
Zenith Homes Exchange supports multiple blockchains through OASIS:
- **Solana** - High-performance blockchain for fast transactions
- **Ethereum** - Industry-leading smart contract platform
- **Arbitrum** - Layer 2 scaling solution
- **Optimism** - Optimistic rollup for Ethereum
- **Base** - Coinbase's Layer 2 network
- **Avalanche** - High-throughput blockchain
- **BNB Chain** - Binance Smart Chain
- **Arweave** - Permanent data storage
- **Aztec** - Privacy-focused blockchain
- **Miden** - Zero-knowledge virtual machine
- **Zcash** - Privacy-preserving cryptocurrency

### Asset Management
- **Comprehensive RWA tokenization** tools with support for various asset types
- **Secure ownership verification** and documentation
- **Trading interface** with price history tracking
- **Identity management** (registration, authentication, account recovery)
- **Network management** for different blockchains
- **Wallet linking** and management
- **IPFS integration** for decentralized storage

## 🏛️ Architecture

The project follows a microservice architecture with:

- **Backend API**: Core service exposing RESTful endpoints (C# .NET)
- **Database Migrator**: Service for database schema management
- **Bridge SDK**: Client library for blockchain interactions (Solana, Radix)
- **Frontend**: User interface built with Next.js 15 and React 19
- **Smart Trust Wizard**: Trust creation and contract generation interface

### Technology Stack

**Frontend:**
- Next.js 15.3.2
- React 19
- TypeScript
- Tailwind CSS 4
- Three.js & React Three Fiber (3D visualization)
- Radix UI components
- Zustand (state management)
- React Query (data fetching)

**Backend:**
- .NET Core
- C# API with RESTful endpoints
- PostgreSQL database
- IPFS integration
- Multi-chain bridge SDK

## 🚀 Getting Started

### Prerequisites

- Node.js 18+ (for frontend)
- .NET SDK (for backend)
- Docker (optional, for containerized deployment)

### Frontend Setup

```bash
cd frontend
npm install
npm run dev
```

The frontend will be available at `http://localhost:3000`

### Environment Variables

Create a `.env.local` file in the `frontend` directory:

```env
NEXT_PUBLIC_API_URL=http://localhost:5233/api/v1
NEXT_PUBLIC_SOLANA_ENVIRONMENT=devnet
```

### Backend Setup

```bash
cd backend
dotnet restore
dotnet run --project src/api/API/API.csproj
```

The API will be available at `http://localhost:5233`

## 📦 Docker Deployment

### Build Docker Image

```bash
docker build -t zenith-homes-frontend -f frontend/Dockerfile frontend/
```

### Run Container

**For Devnet:**
```bash
docker run -d -p 3000:3000 \
  -e NEXT_PUBLIC_API_URL=https://api.qstreetrwa.com/api/v1 \
  -e NEXT_PUBLIC_SOLANA_ENVIRONMENT=devnet \
  zenith-homes-frontend
```

**For Mainnet:**
```bash
docker run -d -p 3000:3000 \
  -e NEXT_PUBLIC_API_URL=https://api.qstreetrwa.com/api/v1 \
  zenith-homes-frontend
```

## 🎯 Core Functionality

### RWA Tokenization

- **Asset Types**: Real Estate, Automobiles, Industrial Equipment, Jewelry, Collectibles
- **Ownership Verification**: Documents stored on IPFS with blockchain references
- **Price Management**: Update prices and track price history
- **Purchase Options**: Multi-chain payment support
- **Royalties**: Support for royalties on secondary sales
- **Fractional Ownership**: Divide assets into 1000+ shares with visual representation

### API Endpoints

- `POST /api/v1/rwa/tokenize`: Create and tokenize a real-world asset
- `GET /api/v1/rwa/{token_id}`: Get details of a specific RWA token
- `GET /api/v1/rwa`: List all RWA tokens with filtering and pagination
- `PUT /api/v1/rwa/{token_id}`: Update an RWA token's details
- `GET /api/v1/rwa/{token_id}/history`: Get transaction and price history

### Smart Trust Creation

- **Trust Setup**: Configure Wyoming Statutory Trust parameters
- **Property Details**: Add property information with geolocation
- **Token Configuration**: Set up fractional ownership parameters
- **Beneficiary Rights**: Define beneficiary rights and distributions
- **Contract Generation**: Automated smart contract creation

## 🎨 UI/UX Highlights

- **Premium Design**: Dark theme with elegant gradients and smooth animations
- **Responsive Layout**: Mobile-first design that works on all devices
- **Interactive 3D**: Explore properties in immersive 3D environments
- **Real-time Updates**: Live price tracking and ownership status
- **Accessibility**: WCAG-compliant design with keyboard navigation

## 🔐 Security Features

- **OASIS Authentication**: Secure user authentication and authorization
- **IPFS Storage**: Decentralized document storage
- **Multi-signature Support**: Enhanced security for high-value transactions
- **Audit Trails**: Complete transaction history and logging

## 📚 Documentation

- [3D Models Guide](./frontend/3D_MODELS_GUIDE.md) - How to use external 3D house models
- [Smart Trust Documentation](./Smart-Trust/) - Trust creation and management
- [Backend API Documentation](./backend/) - API endpoints and services

## 🤝 Contributing

This is a private repository. For contributions or questions, please contact the development team.

## 📄 License

Proprietary - All rights reserved

## 🔗 Links

- **Platform**: [Zenith Homes Exchange](https://zenithhomes.exchange)
- **API**: [API Documentation](https://api.qstreetrwa.com)
- **OASIS Platform**: [OASIS Platform](https://oasisplatform.world)

---

Built with ❤️ using OASIS multi-chain infrastructure
