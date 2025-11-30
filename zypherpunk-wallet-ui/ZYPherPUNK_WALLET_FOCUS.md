# Zypherpunk Wallet - Focused Implementation Plan

## 🎯 Core Focus

The Zypherpunk wallet should showcase **ONLY** the Zypherpunk hackathon features:

1. **Zcash ↔ Aztec Private Bridge** - The main feature
2. **Zcash-Backed Stablecoin on Aztec** - Track 3 feature
3. **Zcash Provider Features** - Shielded transactions, viewing keys
4. **Aztec Provider Features** - Private notes, private transactions
5. **OASIS Infrastructure** - Show Holons, Provider Manager, HyperDrive in action

## 🚫 What to Remove/De-emphasize

- Generic wallet features (Send/Receive for other chains)
- Generic swap functionality
- Generic buy functionality
- Privacy dashboard (unless it shows Zcash/Aztec specific metrics)

## ✅ What to Build

### 1. **Zcash ↔ Aztec Bridge Interface**

**Main Screen:**
- Bridge status dashboard
- Initiate bridge (Zcash → Aztec)
- Initiate bridge (Aztec → Zcash)
- Bridge history
- Viewing key management (for auditability)

**Features:**
- Show locked ZEC on Zcash
- Show minted notes on Aztec
- Bridge transaction status
- Partial note support
- Viewing key generation/management

### 2. **Zcash-Backed Stablecoin Interface**

**Main Screen:**
- Stablecoin dashboard (zUSD or zUSDC)
- Current collateral ratio
- ZEC price from oracle
- Total supply / Total collateral

**Actions:**
- **Mint Stablecoin** - Lock ZEC, mint stablecoin
- **Redeem Stablecoin** - Burn stablecoin, unlock ZEC
- **View Positions** - User's collateral positions
- **Yield Dashboard** - Private yield generation
- **Risk Metrics** - Position health, liquidation threshold

### 3. **Zcash Provider Features**

**Shielded Transactions:**
- Create shielded transaction
- View shielded balance
- Viewing key management
- Partial note creation

**UI Elements:**
- Shield icon for shielded transactions
- Privacy score based on shielded vs transparent balance
- Viewing key export/import

### 4. **Aztec Provider Features**

**Private Notes:**
- Create private note
- View private balance
- Send private transaction
- Bridge operations

**UI Elements:**
- Private note indicators
- Aztec account status
- Private transaction history

### 5. **OASIS Infrastructure Showcase**

**Show How OASIS Powers Everything:**
- **Holons** - Show data stored as holons (bridge state, positions, etc.)
- **Provider Manager** - Show auto-failover, replication
- **HyperDrive** - Show 100% uptime guarantee
- **Auto-Replication** - Show data replicated to MongoDB, IPFS, Arbitrum

**UI Elements:**
- Provider status indicators
- Replication status
- Failover notifications
- Holon visualization

## 📱 Proposed UI Structure

### Home Screen
```
┌─────────────────────────────────────┐
│  Zypherpunk Privacy Wallet          │
│  Zcash ↔ Aztec Bridge Enabled       │
├─────────────────────────────────────┤
│  [Bridge Status]                    │
│  • Zcash Locked: 10 ZEC            │
│  • Aztec Minted: 10 notes          │
│  • Active Bridges: 2               │
├─────────────────────────────────────┤
│  [Stablecoin Dashboard]             │
│  • zUSD Supply: 1,000              │
│  • Collateral: 1,500 ZEC            │
│  • Collateral Ratio: 150%          │
│  • Your Position: 100 zUSD         │
├─────────────────────────────────────┤
│  [Quick Actions]                    │
│  [Bridge ZEC → Aztec]  [Mint zUSD] │
│  [Bridge Aztec → ZEC]  [Redeem]    │
│  [Shielded Send]      [Private]    │
└─────────────────────────────────────┘
```

### Bridge Screen
```
┌─────────────────────────────────────┐
│  Zcash ↔ Aztec Private Bridge       │
├─────────────────────────────────────┤
│  Direction: [Zcash → Aztec ▼]      │
│  Amount: [10 ZEC]                   │
│  Destination: [Aztec Address]       │
│                                     │
│  Privacy Options:                   │
│  ☑ Use Partial Notes               │
│  ☑ Generate Viewing Key            │
│                                     │
│  [Initiate Bridge]                 │
├─────────────────────────────────────┤
│  Bridge History                     │
│  • Bridge #1: 10 ZEC → Aztec ✓     │
│  • Bridge #2: 5 ZEC → Aztec ⏳     │
└─────────────────────────────────────┘
```

### Stablecoin Screen
```
┌─────────────────────────────────────┐
│  Zcash-Backed Stablecoin (zUSD)     │
├─────────────────────────────────────┤
│  System Status:                     │
│  • Total Supply: 1,000 zUSD         │
│  • Total Collateral: 1,500 ZEC      │
│  • Collateral Ratio: 150%           │
│  • ZEC Price: $50 (Oracle)         │
│  • Current APY: 5.2%                │
├─────────────────────────────────────┤
│  Your Position:                     │
│  • Collateral: 100 ZEC              │
│  • Debt: 66.67 zUSD                │
│  • Health: 150% (Safe)              │
│                                     │
│  [Mint zUSD]  [Redeem]  [Add Collateral] │
├─────────────────────────────────────┤
│  Yield Generation:                  │
│  • Strategy: Private Lending       │
│  • Yield Earned: 5.2 zUSD          │
│  • Last Updated: 2 hours ago       │
└─────────────────────────────────────┘
```

## 🔌 API Integration Points

### Bridge API
```
POST /api/bridge/zcash-to-aztec
  - amount: decimal
  - destination: string
  - usePartialNotes: bool
  - generateViewingKey: bool

GET /api/bridge/status/{bridgeId}
GET /api/bridge/history
```

### Stablecoin API
```
POST /api/stablecoin/mint
  - zecAmount: decimal
  - stablecoinAmount: decimal

POST /api/stablecoin/redeem
  - stablecoinAmount: decimal

GET /api/stablecoin/position/{positionId}
GET /api/stablecoin/system-status
```

### Zcash Provider API
```
POST /api/zcash/shielded-send
GET /api/zcash/shielded-balance
POST /api/zcash/generate-viewing-key
GET /api/zcash/viewing-keys
```

### Aztec Provider API
```
POST /api/aztec/private-send
GET /api/aztec/private-balance
POST /api/aztec/create-private-note
```

## 🎨 UI Theme

Keep the Zypherpunk theme but focus on:
- **Bridge Status** - Show active bridges prominently
- **Stablecoin Metrics** - Show system health
- **Privacy Indicators** - Show shielded/private balances
- **OASIS Infrastructure** - Show provider status, replication

## 📝 Next Steps

1. **Remove generic features** - Remove generic send/receive/swap
2. **Build bridge interface** - Focus on Zcash ↔ Aztec bridge
3. **Build stablecoin interface** - Focus on minting/redeeming
4. **Connect to actual APIs** - Integrate with OASIS backend
5. **Show OASIS infrastructure** - Visualize Holons, providers, replication

---

**Goal**: The wallet should be a **showcase** of the Zypherpunk hackathon features, not a generic multi-chain wallet.

