# OASIS & STARNET Developer Documentation Index

## 📋 **Complete Developer Documentation Guide**

Welcome to the comprehensive developer documentation for OASIS and STARNET. This index provides easy navigation to all documentation, tutorials, and guides.

## 🏗️ **Architecture Overview**

### **Omniverse games (OQuake, ODOOM) + STAR**
- **[STAR Quest System — Developer Guide](./STAR_Quest_System_Developer_Guide.md)** — WEB5 quest API, STARAPIClient, `star_api_*`, extending games
- **[STAR Games — User Guide](./STAR_Games_User_Guide.md)** — Beam-in, inventory, quest UI keys for OQuake / ODOOM
- **[ODOOM quest list + STAR](./ODOOM_Quest_List_STAR.md)** — Quest list CVar/ZScript invariants (developers)

### **System Architecture**
- **[OASIS Architecture Overview](../../../Docs/Devs/OASIS_ARCHITECTURE_OVERVIEW.md)** - Complete system architecture
- **[OASIS Architecture Diagrams](../../../Docs/Devs/OASIS_ARCHITECTURE_DIAGRAMS.md)** - Visual system diagrams
- **[Combined API Overview](../../../Docs/Devs/API%20Documentation/COMBINED_API_OVERVIEW.md)** - WEB4 + WEB5 integration
- **[Current Implementation Status](../../Docs/CURRENT_IMPLEMENTATION_STATUS.md)** - Complete implementation status across all components

### **Core Managers & Systems**
- **[OASIS Managers Complete Guide](../../../Docs/Devs/OASIS-Managers-Complete-Guide.md)** - Core managers (AvatarManager, WalletManager, KeyManager)
- **[OASIS Managers Part 2](../../../Docs/Devs/OASIS-Managers-Part2.md)** - Additional managers (HolonManager, NFTManager, SearchManager, CacheManager, EmailManager)
- **[OASIS Managers Part 3](../../../Docs/Devs/OASIS-Managers-Part3.md)** - Advanced managers & OASISHyperDrive
- **[Wallet Management System](../../../Docs/Devs/Wallet-Management-System.md)** - Comprehensive wallet management guide

### **Core Components**
- **WEB4 OASIS API**: Data aggregation and identity layer
- **WEB5 STAR API**: Gamification and business layer  
- **STARNET Web UI**: Comprehensive web interface and app store
- **STAR CLI**: Command-line interface for developers
- **OAPP Builder**: Drag-and-drop application builder
- **DNA System**: STARNETHolon dependency management
- **Core WEB4 APIs** (from oasisweb4.com): AVATAR, KARMA, DATA, WALLET, NFT, KEYS, SEARCH, STATS, HOLOCHAIN, IPFS
- **ProviderManagement Refactor**: Registry, Selector, Switcher, Configurator, Facade, with `ProviderManagerNew` facade and legacy `ProviderManager` marked [Obsolete]
- **Blockchain Providers**: Bitcoin, Ethereum, Solana, Polygon, Arbitrum, Avalanche, BNB Chain, Cardano, NEAR, Polkadot, Cosmos, Fantom, Optimism, Rootstock, TRON, Telos, Sui, Aptos, Elrond, Hashgraph, EOSIO, BlockStack, ChainLink, Moralis, Web3Core
- **Cloud Providers**: AWS, Azure, Google Cloud, Azure Cosmos DB
- **Storage Providers**: MongoDB, Neo4j, Neo4j Aura, SQLite, Local File
- **Network Providers**: Holochain, IPFS, ActivityPub, Scuttlebutt, SOLID, ThreeFold, Pinata
- **Map Providers**: Mapbox, WRLD3D, GO Map
- **Specialized Providers**: Cargo, ONION Protocol, Orion Protocol, PLAN, SEEDS, Apollo Server, ARC Membrane

### **Revolutionary Systems**
- **OASIS HyperDrive**: 100% uptime auto-failover system with intelligent routing
  - Legacy (v1): Auto-Replication, Auto-Failover
  - OASIS HyperDrive 2 (v2): Adds Auto-Load Balancing, Intelligent Selection (latency-first), Predictive Failover, Enhanced Replication Rules (provider/data-type/schedule/cost/permissions), Advanced Analytics, Subscription-aware quotas & alerts, Mode switch with v2→v1 fallback
  - Docs: [README HyperDrive section](../../README.md), [HyperDrive Whitepaper](../../Docs/OASIS_HYPERDRIVE_WHITEPAPER.md)
- **ONET (OASIS Network)**: Revolutionary decentralized networking layer with intelligent discovery, routing, consensus, and security
  - Multi-Protocol Discovery: DHT (Kademlia), mDNS, blockchain, and bootstrap discovery
  - Intelligent Routing: Dijkstra, A*, BFS algorithms with adaptive load balancing
  - Distributed Consensus: Dynamic consensus intervals based on network health
  - Advanced Security: End-to-end encryption, quantum key generation, authentication
  - Real-Time Monitoring: Network metrics, system health, performance optimization
  - Docs: [ONET Documentation](../../Docs/ONET_DOCUMENTATION.md)
- **OASIS COSMIC ORM**: Universal data abstraction layer for all Web2/Web3 providers
- **OASIS NFT System**: Revolutionary NFT system with cross-chain support
- **OASIS Universal Wallet System**: Unified digital asset management across 50+ blockchain networks
- **STAR CLI**: Revolutionary Interoperable Low/No Code Generator for all metaverses, games, apps, and platforms
- **Our World**: Groundbreaking AR geo-location game that encourages environmental stewardship and community service
- **One World**: Benevolent MMORPG with optional VR, similar to Minecraft and Pax Dei with cross-platform asset sharing
- **OASIS/STAR Cross-Platform Universal System**: Revolutionary interoperable universal system with universal STARNETHolon sharing
- **HoloNET**: Revolutionary Holochain integration bringing P2P architecture to .NET and Unity ecosystems

### **OASIS Web4 Site & Plans**
- Marketing site scaffold: `oasisweb4.com` (Vite + React + TS)
- Plans: Bronze, Silver, Gold, Enterprise
- Frontend checkout calls WEB4 OASIS WebAPI Subscription API (to be implemented): `/api/subscriptions/checkout`

## 🚀 **Revolutionary Systems Documentation**

### **OASIS HyperDrive**
- **[OASIS HyperDrive Whitepaper](../../Docs/OASIS_HYPERDRIVE_WHITEPAPER.md)** - Complete HyperDrive documentation
- **Features**: 100% uptime, auto-failover, auto-load balancing, auto-replication
- **Benefits**: Impossible to shutdown, intelligent routing, geographic optimization

### **OASIS COSMIC ORM**
- **[OASIS COSMIC ORM Documentation](../../Docs/OASIS_COSMIC_ORM_DOCUMENTATION.md)** - Complete COSMIC ORM guide
- **[OASIS COSMIC ORM Whitepaper](../../Docs/OASIS_COSMIC_ORM_WHITEPAPER.md)** - Comprehensive whitepaper
- **Features**: Universal data abstraction, provider abstraction, easy migration
- **Benefits**: Single API for all providers, 100% uptime, automatic optimization

### **OASIS NFT System**
- **[OASIS NFT System Whitepaper](../../Docs/OASIS_NFT_SYSTEM_WHITEPAPER.md)** - Complete NFT system documentation
- **Features**: Cross-chain NFTs, shared metadata, Geo-NFTs, universal standard
- **Benefits**: Multi-chain collections, instant minting, AR/VR integration

### **OASIS Universal Wallet System**
- **[OASIS Universal Wallet System Whitepaper](../../Docs/OASIS_UNIVERSAL_WALLET_WHITEPAPER.md)** - Complete wallet system documentation
- **Features**: Unified interface, cross-chain support, portfolio aggregation, DeFi integration
- **Benefits**: Single dashboard for all assets, one-click transfers, enhanced security, 100% uptime

### **Grants & Case Studies**
- Radix DLT: Native bridge between Radix and Solana tokens
- Arbitrum (Grant Ships): Endangered Tokens NFTs in AR World treasure hunt
- Arbitrum (Thrive): HoloNET API -> Stellar Gate browser game
- Solana Superteam UK: AR World Phygital game with geo-cached Solana NFTs

### **STAR CLI - Revolutionary Interoperable Low/No Code Generator**
- **[STAR CLI Documentation](../../../Docs/Devs/STAR_CLI_DOCUMENTATION.md)** - Complete STAR CLI guide
- **[STAR CLI Quick Start Guide](../../../Docs/Devs/STAR_CLI_QUICK_START_GUIDE.md)** - Quick start tutorial
- **Features**: Interoperable Low/No Code Generator, OASIS Omniverse unification, asset/app store backend
- **Benefits**: Create entire metaverses with minimal coding, unify all platforms, power Our World game

### **Our World - The Benevolent Pokemon Go and Beyond**
- **Features**: Immersive 3D VR educational platform, geo-location AR game, environmental stewardship, karma system, real-world impact
- **Benefits**: Get people back into nature, connect with real people, earn karma for good deeds, unlock superpowers
- **Impact**: Environmental education, community service, youth engagement, global movement
- **Technical Innovation**: World-first HoloNET integration connecting Holochain to Unity/.NET ecosystems
- **Philosophy**: Inspired by Buckminster Fuller's world peace game vision and The Venus Project resource-based economy
- **Team**: Led by David Ellams with 15+ diverse professionals including developers, designers, strategists, and advisors
- **Partnerships**: Noomap, The S7 Foundation, educational institutions, environmental organizations
- **Documentation**: [Our World Documentation](../../../Docs/Devs/OUR_WORLD_DOCUMENTATION.md) | [Our World Whitepaper](../../../Docs/Devs/OUR_WORLD_WHITEPAPER.md)

### **One World - The Benevolent MMORPG**
- **Features**: MMORPG with optional VR, infinite building like Minecraft and Pax Dei, benevolent focus
- **Benefits**: Cross-platform asset sharing, unified gaming ecosystem, community building
- **Impact**: First benevolent MMORPG with cross-platform interoperability
- **Technical Innovation**: Cross-platform asset sharing, unified gaming ecosystem
- **Market Opportunity**: Massive MMORPG gaming market, first-mover advantage in benevolent gaming
- **Competitive Advantage**: First benevolent MMORPG with cross-platform asset sharing
- **Documentation**: [One World Documentation](../../../Docs/Devs/ONE_WORLD_DOCUMENTATION.md) | [One World Whitepaper](../../../Docs/Devs/ONE_WORLD_WHITEPAPER.md)

### **OASIS/STAR Cross-Platform Universal System**
- **Features**: Universal STARNETHolon sharing, cross-OAPP compatibility, platform agnostic, engine independent
- **Benefits**: Create once, use everywhere, cross-platform progression, unified OAPP experience
- **Impact**: First truly interoperable universal system with universal STARNETHolon sharing
- **Technical Innovation**: Universal STARNETHolon format, cross-platform sync, unified authentication
- **Market Opportunity**: Revolutionary universal ecosystem, massive developer access, infinite use cases
- **Competitive Advantage**: First truly interoperable universal system with universal STARNETHolon sharing across all industries

### **HoloNET - Revolutionary Holochain Integration**
- **Features**: World-first .NET and Unity client for Holochain, HoloNET ORM with one-line commands
- **Benefits**: Simplified Holochain development, enterprise integration, Unity game development
- **Impact**: Brings P2P architecture to mainstream .NET and Unity development
- **Technical Innovation**: First .NET and Unity client for Holochain, revolutionary ORM simplification
- **Market Opportunity**: Massive .NET and Unity developer ecosystems, future of internet architecture
- **Competitive Advantage**: First-mover advantage in Holochain integration, simplified development
- **Documentation**: [HoloNET Documentation](../../../Docs/Devs/HOLONET_DOCUMENTATION.md) | [HoloNET Whitepaper](../../../Docs/Devs/HOLONET_WHITEPAPER.md)

## 📚 **API Documentation**

### **WEB4 OASIS API**
- **[WEB4 OASIS API Documentation](../../../Docs/Devs/API%20Documentation/WEB4_OASIS_API_Documentation.md)** - Complete API reference
- **[WEB4 OASIS API Complete Endpoints](../../../Docs/Devs/API%20Documentation/WEB4_OASIS_API_Complete_Endpoints_Reference.md)** - All endpoints reference
- **[OASIS API Reference](../../../Docs/Devs/OASIS_API_Reference.md)** - Core API documentation

### **WEB5 STAR API**
- **[WEB5 STAR API Documentation](../../../Docs/Devs/API%20Documentation/WEB5_STAR_API_Documentation.md)** - Complete STAR API reference
- **[STAR API Complete Endpoints](../../../Docs/Devs/API%20Documentation/STAR_API_Complete_Endpoints_Reference.md)** - All STAR endpoints
- **[STAR Metadata System](../../../Docs/Devs/API%20Documentation/STAR_Metadata_System_Documentation.md)** - Metadata management
- **[STAR OAPP Builder](../../../Docs/Devs/API%20Documentation/STAR_OAPP_Builder_Documentation.md)** - OAPP Builder guide

### **STAR CLI & DNA System**
- **[STAR CLI Documentation](../../../Docs/Devs/STAR_CLI_DOCUMENTATION.md)** - Complete CLI reference
- **[DNA System Guide](../../../Docs/Devs/DNA_SYSTEM_GUIDE.md)** - STARNETHolon dependency management
- **[Dependency Management Guide](../../../Docs/Devs/DEPENDENCY_MANAGEMENT_GUIDE.md)** - Advanced dependency management

## 🎮 **STARNET Web UI Documentation**

### **User Interface**
- **[STARNET Web UI Overview](../../Docs/STARNET_WEB_UI_OVERVIEW.md)** - Complete UI guide
- **[Dashboard Documentation](../../../Docs/Devs/STARNET_DASHBOARD_GUIDE.md)** - Dashboard features
- **[MetaData Page Guide](../../../Docs/Devs/STARNET_METADATA_PAGE_GUIDE.md)** - Metadata management UI
- **[OAPP Builder UI](../../../Docs/Devs/STARNET_OAPP_BUILDER_UI_GUIDE.md)** - Visual builder interface

### **App Store & Asset Management**
- **[STARNET App Store](../../../Docs/Devs/STARNET_APP_STORE_GUIDE.md)** - Publishing and downloading
- **[Asset Management](../../../Docs/Devs/STARNET_ASSET_MANAGEMENT_GUIDE.md)** - Managing assets
- **[Version Control](../../../Docs/Devs/STARNET_VERSION_CONTROL_GUIDE.md)** - Versioning system

## 🚀 **Getting Started Guides**

### **By platform (installer + manual setup)**
- **STAR CLI:** [Windows](../../../Docs/Devs/STAR_CLI_GettingStarted_Windows.md) · [Linux](../../../Docs/Devs/STAR_CLI_GettingStarted_Linux.md) · [macOS](../../../Docs/Devs/STAR_CLI_GettingStarted_Mac.md) — Install via installer or git clone; run and verify STAR CLI.
- **OASIS development:** [Windows](../../../Docs/Devs/OASIS_Development_GettingStarted_Windows.md) · [Linux](../../../Docs/Devs/OASIS_Development_GettingStarted_Linux.md) · [macOS](../../../Docs/Devs/OASIS_Development_GettingStarted_Mac.md) — Clone, build solution, run tests, project structure.

### **Quick Start**
- **[OASIS Quick Start Guide](../../../Docs/Devs/OASIS_Quick_Start_Guide.md)** - Get started with OASIS
- **[STAR Quick Start Guide](../../../Docs/Devs/STAR_QUICK_START_GUIDE.md)** - Get started with STAR
- **[STARNET Web UI Quick Start](../../../Docs/Devs/STARNET_QUICK_START_GUIDE.md)** - Get started with STARNET Web UI
- **[STAR CLI Quick Start](../../../Docs/Devs/STAR_CLI_QUICK_START_GUIDE.md)** - Get started with STAR CLI

### **Installation & Setup**
- **[Development Environment Setup](../../../Docs/Devs/DEVELOPMENT_ENVIRONMENT_SETUP.md)** - Complete setup guide
- **[Docker Setup Guide](../../../Docs/Devs/DOCKER_SETUP_GUIDE.md)** - Containerized development
- **[Production Deployment](../../../Docs/Devs/PRODUCTION_DEPLOYMENT_GUIDE.md)** - Production setup

## 📖 **Tutorials & Guides**

### **Beginner Tutorials**
- **[Your First OASIS App](../../../Docs/Devs/TUTORIALS/YOUR_FIRST_OASIS_APP.md)** - Step-by-step tutorial
- **[Creating Your First OAPP](../../../Docs/Devs/TUTORIALS/CREATING_YOUR_FIRST_OAPP.md)** - OAPP creation tutorial
- **[STARNET Web UI Basics](../../../Docs/Devs/TUTORIALS/STARNET_WEB_UI_BASICS.md)** - UI navigation tutorial

### **Intermediate Tutorials**
- **[Building a Metaverse Game](../../../Docs/Devs/TUTORIALS/BUILDING_A_METAVERSE_GAME.md)** - Complete game tutorial
- **[Metadata System Tutorial](../../../Docs/Devs/TUTORIALS/METADATA_SYSTEM_TUTORIAL.md)** - Advanced metadata usage
- **[OAPP Builder Advanced](../../../Docs/Devs/TUTORIALS/OAPP_BUILDER_ADVANCED.md)** - Advanced builder features

### **Advanced Tutorials**
- **[Custom Provider Development](../../../Docs/Devs/TUTORIALS/CUSTOM_PROVIDER_DEVELOPMENT.md)** - Creating providers
- **[Enterprise Integration](../../../Docs/Devs/TUTORIALS/ENTERPRISE_INTEGRATION.md)** - Enterprise features
- **[Performance Optimization](../../../Docs/Devs/TUTORIALS/PERFORMANCE_OPTIMIZATION.md)** - Optimization techniques

## 🔧 **Development Guides**

### **Best Practices**
- **[OASIS Best Practices](../../../Docs/Devs/OASIS-BEST-PRACTICES.md)** - Development best practices
- **[Code Standards](../../../Docs/Devs/CODE_STANDARDS.md)** - Coding standards and conventions
- **[Security Guidelines](../../../Docs/Devs/SECURITY_GUIDELINES.md)** - Security best practices

### **Testing & Quality**
- **[Testing Guide](../../../Docs/Devs/OASIS_Test_Harnesses_Guide.md)** - Testing methodologies
- **[Test Coverage Summary](../../../Docs/Devs/TEST-COVERAGE-SUMMARY.md)** - Testing coverage
- **[Quality Assurance](../../../Docs/Devs/QUALITY_ASSURANCE_GUIDE.md)** - QA processes

### **Provider Development**
- **[Provider Development Guide](../../../Docs/Devs/OASIS_Provider_Development_Guide.md)** - Creating providers
- **[Provider Testing](../../../Docs/Devs/PROVIDER_TESTING_GUIDE.md)** - Testing providers
- **[Provider Deployment](../../../Docs/Devs/PROVIDER_DEPLOYMENT_GUIDE.md)** - Deploying providers

## 📱 **SDKs & Libraries**

### **Official SDKs**
- **[JavaScript/Node.js SDK](../../../Docs/Devs/SDKS/JAVASCRIPT_SDK_GUIDE.md)** - JavaScript development
- **[C#/.NET SDK](../../../Docs/Devs/SDKS/DOTNET_SDK_GUIDE.md)** - .NET development
- **[Python SDK](../../../Docs/Devs/SDKS/PYTHON_SDK_GUIDE.md)** - Python development

### **Community Libraries**
- **[React Components](../../../Docs/Devs/LIBRARIES/REACT_COMPONENTS.md)** - React UI components
- **[Unity Integration](../../../Docs/Devs/LIBRARIES/UNITY_INTEGRATION.md)** - Unity game development
- **[Mobile SDKs](../../../Docs/Devs/LIBRARIES/MOBILE_SDKS.md)** - Mobile development

## 🎯 **Use Cases & Examples**

### **Gaming & Metaverse**
- **[Game Development](../../../Docs/Devs/USE_CASES/GAME_DEVELOPMENT.md)** - Game development examples
- **[Metaverse Worlds](../../../Docs/Devs/USE_CASES/METAVERSE_WORLDS.md)** - Virtual world creation
- **[NFT Games](../../../Docs/Devs/USE_CASES/NFT_GAMES.md)** - NFT-based games

### **Enterprise & Business**
- **[Enterprise Applications](../../../Docs/Devs/USE_CASES/ENTERPRISE_APPLICATIONS.md)** - Business applications
- **[Supply Chain Management](../../../Docs/Devs/USE_CASES/SUPPLY_CHAIN.md)** - SCMS integration
- **[Data Analytics](../../../Docs/Devs/USE_CASES/DATA_ANALYTICS.md)** - Analytics applications

### **Social & Community**
- **[Social Platforms](../../../Docs/Devs/USE_CASES/SOCIAL_PLATFORMS.md)** - Social applications
- **[Community Management](../../../Docs/Devs/USE_CASES/COMMUNITY_MANAGEMENT.md)** - Community features
- **[Content Sharing](../../../Docs/Devs/USE_CASES/CONTENT_SHARING.md)** - Content platforms

## 🔐 **Security & Privacy**

### **Security Documentation**
- **[Security Overview](../../../Docs/Devs/SECURITY/SECURITY_OVERVIEW.md)** - Security architecture
- **[Authentication Guide](../../../Docs/Devs/SECURITY/AUTHENTICATION_GUIDE.md)** - Authentication systems
- **[Data Protection](../../../Docs/Devs/SECURITY/DATA_PROTECTION.md)** - Data security

### **Privacy & Compliance**
- **[Privacy Controls](../../../Docs/Devs/PRIVACY/PRIVACY_CONTROLS.md)** - User privacy features
- **[GDPR Compliance](../../../Docs/Devs/PRIVACY/GDPR_COMPLIANCE.md)** - GDPR compliance
- **[Data Governance](../../../Docs/Devs/PRIVACY/DATA_GOVERNANCE.md)** - Data management

## 📊 **Analytics & Monitoring**

### **Performance Monitoring**
- **[Performance Metrics](../../../Docs/Devs/MONITORING/PERFORMANCE_METRICS.md)** - Performance tracking
- **[Error Monitoring](../../../Docs/Devs/MONITORING/ERROR_MONITORING.md)** - Error tracking
- **[Usage Analytics](../../../Docs/Devs/MONITORING/USAGE_ANALYTICS.md)** - Usage statistics

### **Business Intelligence**
- **[Revenue Tracking](../../../Docs/Devs/ANALYTICS/REVENUE_TRACKING.md)** - Revenue analytics
- **[User Behavior](../../../Docs/Devs/ANALYTICS/USER_BEHAVIOR.md)** - User analytics
- **[Market Analysis](../../../Docs/Devs/ANALYTICS/MARKET_ANALYSIS.md)** - Market insights

## 🌍 **Deployment & Operations**

### **Deployment Guides**
- **[Local Development](../../../Docs/Devs/DEPLOYMENT/LOCAL_DEVELOPMENT.md)** - Local setup
- **[Cloud Deployment](../../../Docs/Devs/DEPLOYMENT/CLOUD_DEPLOYMENT.md)** - Cloud deployment
- **[Edge Deployment](../../../Docs/Devs/DEPLOYMENT/EDGE_DEPLOYMENT.md)** - Edge computing

### **Operations**
- **[Monitoring & Alerting](../../../Docs/Devs/OPERATIONS/MONITORING.md)** - System monitoring
- **[Backup & Recovery](../../../Docs/Devs/OPERATIONS/BACKUP_RECOVERY.md)** - Data protection
- **[Scaling Guide](../../../Docs/Devs/OPERATIONS/SCALING.md)** - System scaling

## 🤝 **Community & Support**

### **Community Resources**
- **[Contributing Guide](../../../Docs/Devs/CONTRIBUTING.md)** - How to contribute
- **[Code of Conduct](../../../Docs/Devs/CODE_OF_CONDUCT.md)** - Community guidelines
- **[Alpha Tester Documentation](../../../Docs/Devs/OASIS_Alpha_Tester_Documentation.md)** - Alpha testing

### **Support & Help**
- **[FAQ](../../../Docs/Devs/SUPPORT/FAQ.md)** - Frequently asked questions
- **[Troubleshooting](../../../Docs/Devs/SUPPORT/TROUBLESHOOTING.md)** - Common issues
- **[Contact Support](../../../Docs/Devs/SUPPORT/CONTACT_SUPPORT.md)** - Get help

## 📈 **Roadmap & Updates**

### **Development Roadmap**
- **[Current Roadmap](../../../Docs/Devs/ROADMAP/CURRENT_ROADMAP.md)** - Development timeline
- **[Feature Requests](../../../Docs/Devs/ROADMAP/FEATURE_REQUESTS.md)** - Request features
- **[Release Notes](../../../Docs/Devs/ROADMAP/RELEASE_NOTES.md)** - Version updates

### **Updates & News**
- **[Latest Updates](../../../Docs/Devs/NEWS/LATEST_UPDATES.md)** - Recent changes
- **[Community News](../../../Docs/Devs/NEWS/COMMUNITY_NEWS.md)** - Community updates
- **[Technical Blog](../../../Docs/Devs/NEWS/TECHNICAL_BLOG.md)** - Technical articles

## 🎓 **Learning Resources**

### **Educational Content**
- **[Video Tutorials](../../../Docs/Devs/LEARNING/VIDEO_TUTORIALS.md)** - Video learning
- **[Webinars](../../../Docs/Devs/LEARNING/WEBINARS.md)** - Live sessions
- **[Workshops](../../../Docs/Devs/LEARNING/WORKSHOPS.md)** - Hands-on workshops

### **Certification**
- **[Developer Certification](../../../Docs/Devs/CERTIFICATION/DEVELOPER_CERTIFICATION.md)** - Get certified
- **[Exam Preparation](../../../Docs/Devs/CERTIFICATION/EXAM_PREPARATION.md)** - Study guides
- **[Certification Benefits](../../../Docs/Devs/CERTIFICATION/CERTIFICATION_BENEFITS.md)** - Certification value

---

## 🚀 **Quick Navigation**

### **By Experience Level**
- **Beginner**: Start with [Quick Start Guides](#-getting-started-guides)
- **Intermediate**: Check out [Tutorials & Guides](#-tutorials--guides)
- **Advanced**: Explore [Development Guides](#-development-guides)

### **By Technology**
- **WEB4 OASIS**: [OASIS API Documentation](#-api-documentation)
- **WEB5 STAR**: [STAR API Documentation](#-api-documentation)
- **STARNET Web UI**: [STARNET Web UI Documentation](#-starnet-web-ui-documentation)

### **By Use Case**
- **Gaming**: [Gaming & Metaverse](#gaming--metaverse)
- **Enterprise**: [Enterprise & Business](#enterprise--business)
- **Social**: [Social & Community](#social--community)

---

*This documentation index is regularly updated. For the latest version, visit [docs.oasisplatform.world](https://docs.oasisplatform.world)*
