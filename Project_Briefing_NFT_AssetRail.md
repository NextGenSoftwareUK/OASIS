# Project Briefing: NFT Mint Frontend & AssetRail Integration

## Executive Summary

This briefing covers two key projects within the OASIS ecosystem:

1. **NFT Mint Frontend** - A Next.js-based wizard interface for cross-chain NFT minting
2. **AssetRail** - A Kadena Chainweb EVM sandbox environment with real estate tokenization capabilities

Both projects are positioned to work together within the broader OASIS API framework, providing a complete solution for digital asset creation and management.

---

## Project 1: NFT Mint Frontend (`nft-mint-frontend/`)

### Overview
A modern Next.js 15 application that provides a wizard-based interface for minting NFTs across multiple blockchain networks. Currently focused on Solana but designed for multi-chain expansion.

### Technical Stack
- **Framework**: Next.js 15.5.4 with React 19.1.0
- **Styling**: Tailwind CSS 4 with custom design system
- **TypeScript**: Full type safety throughout
- **Wallet Integration**: Solana wallet adapters (Phantom, etc.)
- **API Integration**: OASIS API for provider management and NFT minting

### Key Features

#### 1. Multi-Step Wizard Interface
- **Step 1**: Solana Configuration (Metaplex Standard, Editions, Compressed NFTs)
- **Step 2**: Authentication & Provider Management (SolanaOASIS + MongoDBOASIS)
- **Step 3**: Asset Upload & Metadata Management
- **Step 4**: Review & Mint Execution

#### 2. Provider Management System
- Dynamic provider registration and activation
- Support for multiple OASIS providers (SolanaOASIS, MongoDBOASIS)
- Real-time status tracking and error handling
- JWT-based authentication with Site Avatar credentials

#### 3. Asset Management
- Image and JSON metadata upload via Pinata IPFS
- Asset validation and preview
- Recipient wallet address management
- Metadata template system

#### 4. Multi-Chain Architecture (Planned)
- Current: Solana-focused with SPL token support
- Planned: Arbitrum, Polygon, Base, Rootstock integration
- Unified wizard shell with chain-specific configurations
- Provider mapping system for different blockchain networks

### Architecture Highlights

#### Component Structure
```
src/
├── app/                    # Next.js App Router
│   ├── (routes)/
│   │   └── page-content.tsx # Main wizard implementation
│   └── api/               # API routes for Pinata integration
├── components/
│   ├── wizard/            # Wizard shell and step components
│   ├── auth/              # Authentication and provider panels
│   ├── assets/            # Asset upload and management
│   ├── mint/              # Mint review and execution
│   └── ui/                # Reusable UI components
├── hooks/
│   └── use-oasis-api.ts   # OASIS API integration hook
└── types/
    └── chains.ts          # Chain configuration types
```

#### Key Integration Points
- **OASIS API**: HTTP client with JWT authentication
- **Pinata**: IPFS file and metadata storage
- **Wallet Adapters**: Solana wallet integration
- **Provider System**: Dynamic blockchain provider management

### Current Status
- ✅ Solana minting workflow complete
- ✅ Provider management system functional
- ✅ Asset upload and metadata handling
- ✅ Wizard UI/UX polished
- 🔄 Multi-chain expansion in planning phase

---

## Project 2: AssetRail (`AssetRail/`)

### Overview
A comprehensive Kadena Chainweb EVM development environment that provides:
- Local blockchain development network
- Cross-chain token transfer capabilities
- Real estate tokenization smart contracts
- Wyoming Statutory Trust implementation

### Technical Stack
- **Blockchain**: Kadena Chainweb EVM
- **Smart Contracts**: Solidity 0.8.x with OpenZeppelin
- **Development**: Hardhat with Kadena-specific plugins
- **Infrastructure**: Docker Compose for local development
- **Block Explorer**: Blockscout integration

### Key Components

#### 1. Kadena EVM Sandbox (`kadena-evm-sandbox/`)
- **Purpose**: Local development environment for Kadena Chainweb EVM
- **Features**: 
  - 5 EVM chains (20-24) for cross-chain testing
  - Mining client and consensus simulation
  - Pre-allocated test accounts
  - Network management tools

#### 2. Smart Contract Suite (`solidity/`)

##### SimpleToken Contract
- **Purpose**: Cross-chain ERC-20 token with burn/mint mechanics
- **Features**:
  - Cross-chain transfers using SPV proofs
  - Precompile integration for Chainweb chain ID
  - Event-based cross-chain messaging
  - Comprehensive error handling

##### WyomingTrustTokenization Contract
- **Purpose**: Real estate tokenization via Wyoming Statutory Trust
- **Features**:
  - 1000-year perpetual trust structure
  - Property fractionalization (1 token = 1 sq ft)
  - Annual income distribution (90% to holders, 10% to reserve)
  - Notarized transfer requirements
  - Visitation rights for 30%+ holders
  - Trust protector and trustee succession

##### EntityOASIS Contract
- **Purpose**: Basic entity management structure
- **Features**:
  - Entity ID and external ID mapping
  - Flexible info storage (bytes)

#### 3. Development Tools
- **Network Management**: `./network` command-line tool
- **Testing**: Comprehensive test suites with Hardhat
- **Deployment**: Create2 deterministic deployment support
- **Exploration**: Blockscout block explorer integration

### Architecture Highlights

#### Cross-Chain Protocol
1. **Event Emission**: Source chain emits cross-chain events
2. **Proof Generation**: Off-chain endpoint generates SPV proofs
3. **Verification**: Target chain verifies against shared history
4. **Execution**: Smart contract completes cross-chain operation

#### Real Estate Tokenization Flow
1. **Property Addition**: Trustee adds properties to trust
2. **Token Minting**: Fractional tokens minted (1 token = 1 sq ft)
3. **Income Distribution**: Annual distributions based on ownership
4. **Transfer Management**: Notarized transfers with legal compliance

### Current Status
- ✅ Kadena EVM sandbox fully operational
- ✅ Cross-chain token transfers working
- ✅ Wyoming Trust tokenization contract complete
- ✅ Development tools and testing framework ready
- ✅ Blockscout integration available

---

## Integration Opportunities

### 1. NFT Mint Frontend + AssetRail
**Potential Integration**: Use the NFT minting wizard to create property tokens through the Wyoming Trust system.

**Implementation Path**:
- Extend NFT frontend to support ERC-721 property tokens
- Integrate with Kadena Chainweb EVM for property tokenization
- Add Wyoming Trust-specific minting workflows
- Implement property-specific metadata standards

### 2. Cross-Chain Asset Management
**Opportunity**: Leverage both projects for comprehensive cross-chain asset creation and management.

**Use Cases**:
- Mint NFTs on Solana for digital assets
- Tokenize real estate on Kadena via Wyoming Trust
- Cross-chain asset transfers between networks
- Unified portfolio management across chains

### 3. OASIS API Integration
**Synergy**: Both projects can leverage the OASIS API for:
- Unified authentication and provider management
- Cross-chain provider orchestration
- Asset metadata standardization
- Portfolio tracking and analytics

---

## Technical Recommendations

### For NFT Mint Frontend
1. **Complete Multi-Chain Expansion**: Implement the planned Arbitrum, Polygon, Base, and Rootstock support
2. **Wallet Integration**: Add MetaMask/WalletConnect for EVM chains
3. **Metadata Standards**: Implement chain-specific metadata templates
4. **Provider Abstraction**: Create unified provider interface for all chains

### For AssetRail
1. **Frontend Integration**: Build web interface for property tokenization
2. **Legal Compliance**: Enhance notarization and compliance features
3. **Analytics Dashboard**: Add property performance tracking
4. **Cross-Chain Bridge**: Implement bridges to other EVM networks

### For Combined System
1. **Unified Dashboard**: Single interface for all asset types
2. **Portfolio Management**: Cross-chain asset tracking and management
3. **Compliance Framework**: Integrated legal and regulatory compliance
4. **API Standardization**: Unified API for all asset operations

---

## Business Value Proposition

### NFT Mint Frontend
- **Creator-Friendly**: Intuitive wizard interface reduces technical barriers
- **Multi-Chain Ready**: Future-proof architecture for blockchain expansion
- **Provider Agnostic**: Flexible provider system for different use cases
- **Production Ready**: Polished UI/UX suitable for end users

### AssetRail
- **Legal Compliance**: Wyoming Statutory Trust provides regulatory clarity
- **Real Estate Focus**: Specialized for property tokenization
- **Cross-Chain Capable**: Built on Kadena's proven cross-chain technology
- **Development Ready**: Complete tooling for rapid deployment

### Combined Ecosystem
- **Comprehensive Solution**: Covers both digital and real-world assets
- **Cross-Chain Interoperability**: Assets can move between networks
- **Regulatory Compliance**: Built-in legal frameworks
- **Developer Friendly**: Extensive tooling and documentation

---

## Next Steps

### Immediate (1-2 weeks)
1. **Complete NFT Frontend Multi-Chain**: Implement remaining blockchain support
2. **AssetRail Web Interface**: Build frontend for property tokenization
3. **Integration Testing**: Test cross-project compatibility

### Short-term (1-3 months)
1. **Unified Dashboard**: Combine both projects into single interface
2. **Cross-Chain Bridges**: Implement asset transfers between networks
3. **Compliance Enhancement**: Strengthen legal and regulatory features

### Long-term (3-6 months)
1. **Production Deployment**: Deploy both systems to production environments
2. **Ecosystem Expansion**: Add support for additional asset types
3. **Partnership Integration**: Integrate with external services and platforms

---

## Conclusion

Both the NFT Mint Frontend and AssetRail projects represent sophisticated, production-ready solutions within their respective domains. The NFT frontend provides an excellent foundation for multi-chain digital asset creation, while AssetRail offers a robust framework for real-world asset tokenization.

The integration potential between these projects is significant, offering the opportunity to create a comprehensive digital asset ecosystem that spans both digital and real-world assets across multiple blockchain networks. The technical architecture is sound, the legal frameworks are compliant, and the development tooling is comprehensive.

With proper execution of the recommended next steps, these projects can form the foundation of a leading digital asset platform that combines technical excellence with regulatory compliance and user-friendly interfaces.



