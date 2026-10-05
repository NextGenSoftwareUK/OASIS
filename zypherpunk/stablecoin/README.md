# Zcash-Backed Stablecoin Implementation

## 🎯 Overview

This is the implementation of the Zcash-backed stablecoin on Aztec with private yield generation for the Zypherpunk hackathon (Track 3).

## 📁 Structure

```
stablecoin/
├── backend/              # .NET/C# implementation
│   ├── Managers/        # StablecoinManager, RiskManager, YieldManager
│   ├── Services/        # OracleService, CollateralService
│   ├── Holons/          # Data models (Position, System, Oracle, etc.)
│   ├── Controllers/     # API endpoints
│   └── Contracts/       # Aztec smart contracts
├── frontend/            # Wallet UI components (if needed)
│   └── components/      # React/Next.js components
├── contracts/           # Smart contract source files
│   └── aztec/          # Aztec contract code
└── docs/               # Implementation documentation
```

## 🚀 Quick Start

### Backend Implementation

The backend follows OASIS architecture:
- **Managers**: Business logic (StablecoinManager, RiskManager, YieldManager)
- **Services**: External integrations (OracleService)
- **Holons**: Data models that auto-replicate
- **Controllers**: REST API endpoints

### Integration Points

- **Zcash Provider**: Lock/release ZEC (shielded transactions)
- **Aztec Provider**: Mint/burn stablecoin (private notes)
- **Oracle Service**: ZEC price aggregation
- **Holon Manager**: Data storage and replication

## 📚 Documentation

- [Architecture](./docs/ARCHITECTURE.md)
- [API Reference](./docs/API.md)
- [Deployment Guide](./docs/DEPLOYMENT.md)

## 🔗 Related Documentation

- [Stablecoin Architecture](../../STABLECOIN_ARCHITECTURE.md)
- [OASIS Value Proposition](../../OASIS_VALUE_PROPOSITION_STABLECOIN.md)
- [Architecture Diagrams](../../STABLECOIN_ARCHITECTURE_DIAGRAM.md)

