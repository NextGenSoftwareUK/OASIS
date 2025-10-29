# 🔮 OASIS Multi-Chain Oracle Dashboard

**Real-time cross-chain data aggregation, verification, and analytics across 20+ blockchains**

---

## 🎯 Overview

The OASIS Oracle Dashboard provides a unified interface for monitoring and interacting with the OASIS cross-chain oracle network. It offers real-time price feeds, transaction verification, arbitrage detection, and multi-chain analytics.

### Key Features

- 📊 **Real-Time Price Feeds** - Aggregated prices from 8+ sources
- ⛓️ **Chain Monitoring** - Live status for 20+ blockchains
- ✅ **Transaction Verification** - Cross-chain transaction validation
- 💰 **Arbitrage Detection** - Auto-discovery of profit opportunities
- 🎨 **NFT Transfer Tracking** - Cross-chain NFT provenance verification
- 🗳️ **DAO Voting Aggregation** - Multi-chain governance results
- 📈 **Yield Tracking** - Portfolio-wide yield farming analytics

---

## 🚀 Quick Start

### Prerequisites

- Node.js 18+ (20+ recommended)
- npm or yarn

### Installation

```bash
# Install dependencies
npm install

# Run development server
npm run dev

# Open browser
# http://localhost:3000
```

### Build for Production

```bash
# Build
npm run build

# Start production server
npm start
```

---

## 🏗️ Tech Stack

- **Next.js 15** - React framework with App Router
- **React 19** - Latest React with Server Components
- **TypeScript 5** - Type-safe development
- **Tailwind CSS 4** - Utility-first styling
- **Framer Motion** - Smooth animations
- **Recharts** - Data visualization
- **Zustand** - State management
- **WebSockets** - Real-time updates

---

## 📁 Project Structure

```
oasis-oracle-frontend/
├── src/
│   ├── app/                    # Next.js App Router
│   │   ├── layout.tsx         # Root layout
│   │   ├── page.tsx           # Dashboard home
│   │   ├── globals.css        # Global styles
│   │   ├── verify/            # Verification view
│   │   ├── prices/            # Price aggregation
│   │   ├── arbitrage/         # Arbitrage finder
│   │   ├── nft-transfer/      # NFT tracking
│   │   └── dao/               # DAO voting
│   ├── components/
│   │   ├── layout/            # Layout components
│   │   ├── dashboard/         # Dashboard widgets
│   │   ├── oracle/            # Oracle-specific components
│   │   └── ui/                # Reusable UI components
│   ├── hooks/                 # Custom React hooks
│   ├── lib/                   # Utilities & API clients
│   └── types/                 # TypeScript definitions
├── public/                    # Static assets
└── docs/                      # Documentation
```

---

## 🎨 Design System

### Color Palette

Matches the OASIS NFT frontend aesthetic:

- **Background**: Deep space blue-black (#050510)
- **Accent**: Bright cyan (#22d3ee)
- **Foreground**: Crisp white-blue (#e2f4ff)
- **Success**: Green (#22c55e)
- **Warning**: Yellow (#facc15)
- **Danger**: Red (#ef4444)

### Typography

- **Primary**: Geist Sans
- **Monospace**: Geist Mono
- **Headers**: Semibold, tracking-tight
- **Labels**: Uppercase, tracking-[0.5em]

### Effects

- Frosted glass backgrounds with `backdrop-blur-xl`
- Glowing shadows with cyan tints
- Radial gradients for depth
- Smooth transitions and animations

---

## 🔌 API Integration

### Environment Variables

Create `.env.local`:

```bash
NEXT_PUBLIC_API_URL=https://api.oasis.com
NEXT_PUBLIC_WS_URL=wss://api.oasis.com
```

### API Endpoints

- `GET /api/oracle/price/{token}` - Get token price
- `POST /api/oracle/verify/transaction` - Verify transaction
- `GET /api/oracle/chain/{chain}/status` - Get chain status
- `GET /api/oracle/arbitrage/{token}` - Find arbitrage

---

## 📊 Views

### 1. Dashboard (`/`)
- Oracle status overview
- Live price feeds
- Chain health indicators
- Consensus meter

### 2. Verification (`/verify`)
- Transaction verification form
- Cross-chain validation results
- Provenance tracking

### 3. Price Aggregation (`/prices`)
- Aggregated price display
- Source breakdown
- Historical price chart

### 4. Arbitrage Finder (`/arbitrage`)
- Opportunity scanner
- Profit calculator
- Cross-chain price comparison

### 5. NFT Transfer (`/nft-transfer`)
- Transfer flow visualizer
- Metadata comparison
- Verification checklist

### 6. DAO Voting (`/dao`)
- Aggregated voting results
- Votes by chain breakdown
- Top voters list

---

## 🧪 Development

### Run Development Server

```bash
npm run dev
```

### Type Checking

```bash
npm run type-check
```

### Linting

```bash
npm run lint
```

---

## 📦 Deployment

### Vercel (Recommended)

```bash
# Install Vercel CLI
npm i -g vercel

# Deploy
vercel
```

### Docker

```bash
# Build image
docker build -t oasis-oracle-frontend .

# Run container
docker run -p 3000:3000 oasis-oracle-frontend
```

---

## 🔄 Real-Time Features

### WebSocket Connection

```typescript
import { useWebSocket } from '@/hooks/use-websocket';

const { data, isConnected } = useWebSocket('/oracle/prices');
```

### Auto-Refresh

```typescript
import { usePriceFeed } from '@/hooks/use-price-feed';

const { price, isLoading } = usePriceFeed('SOL');
```

---

## 🎯 Roadmap

- [x] Phase 0: Planning & Design
- [x] Phase 1.1: Project Setup
- [ ] Phase 1.2: Global Styles & Layout
- [ ] Phase 1.3: Core UI Components
- [ ] Phase 2: Dashboard View
- [ ] Phase 3: Verification View
- [ ] Phase 4: Price Aggregation
- [ ] Phase 5: Arbitrage Finder
- [ ] Phase 7: Real-Time Features

See [ORACLE_IMPLEMENTATION_ROADMAP.md](../ORACLE_IMPLEMENTATION_ROADMAP.md) for complete roadmap.

---

## 📚 Documentation

- [Architecture](./docs/ARCHITECTURE.md) *(coming soon)*
- [Components](./docs/COMPONENTS.md) *(coming soon)*
- [API Integration](./docs/API.md) *(coming soon)*

---

## 🤝 Contributing

This is part of the OASIS ecosystem. See main OASIS documentation for contribution guidelines.

---

## 📄 License

Part of the OASIS project.

---

## 🔗 Related Projects

- [OASIS Core](../OASIS%20Architecture/)
- [NFT Mint Frontend](../nft-mint-frontend/)
- [OASIS Wallet UI](../oasis-wallet-ui/)

---

**Built with ❤️ for the OASIS ecosystem**

