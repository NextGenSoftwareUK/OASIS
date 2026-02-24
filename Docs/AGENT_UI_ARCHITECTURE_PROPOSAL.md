# Agent UI Architecture Proposal

**Date:** January 2026  
**Status:** 📋 Proposal

---

## Executive Summary

This document analyzes the architecture and proposes UI strategies for agent building and management in the OASIS ecosystem. We evaluate two approaches: integrating with SERV's existing platform vs. building our own UI in the oportal-repo.

---

## Current Architecture Analysis

### OASIS Agent Infrastructure

**What We Have:**
- ✅ A2A Protocol (Agent-to-Agent communication)
- ✅ SERV Infrastructure (Service Registry & Discovery)
- ✅ UnifiedAgentServiceManager (Service management)
- ✅ Agent-to-User linking
- ✅ Agent-to-NFT trading
- ✅ MCP integration (12 tools)
- ✅ API endpoints for all agent operations

**What SERV (OpenServ) Has:**
- Agent building UI with visual interface
- Integration marketplace (Google Calendar, Drive, etc.)
- Workflow builder
- Credits/payment system
- Agent browsing and discovery UI

### OASIS Portal (oportal-repo)

**Current Tabs:**
- Dashboard
- Avatar
- Wallets
- NFTs
- STAR
- Smart Contracts
- Data
- Bridges
- Trading
- Oracle
- Developer
- Telegram
- Settings

**Missing:** Agents tab (mentioned but not implemented)

---

## Architecture Comparison

### Option 1: Integrate with SERV's Platform

**Approach:** Add OASIS-specific integrations to SERV's existing UI

**What It Would Look Like:**

```
SERV Platform (platform.openserv.ai)
├── Existing Features
│   ├── Browse Agents
│   ├── Build Agent
│   ├── Add Agent
│   └── Your Agents
│
└── NEW: OASIS Integrations Section
    ├── OASIS Wallet Integration
    │   ├── Link Solana Wallet
    │   ├── Link Ethereum Wallet
    │   └── Multi-chain Wallet Management
    │
    ├── OASIS NFT Integration
    │   ├── Mint Agent as NFT
    │   ├── View Agent NFT
    │   ├── Transfer Agent NFT
    │   └── Agent NFT Marketplace
    │
    ├── OASIS A2A Protocol
    │   ├── A2A Messaging
    │   ├── Agent-to-Agent Communication
    │   └── Payment Requests
    │
    ├── OASIS SERV Discovery
    │   ├── Discover OASIS Agents
    │   ├── Register with SERV
    │   └── Cross-platform Agent Discovery
    │
    └── OASIS Ecosystem
        ├── Link to OASIS Avatar
        ├── Karma & Reputation
        └── Cross-OAPP Integration
```

**Integration Points:**
1. **OASIS Wallet Scope** - Add to SERV's integration cards
   - Card: "OASIS Multi-Chain Wallet"
   - Scopes: `wallet:read`, `wallet:write`, `wallet:transfer`
   - Enables agents to interact with OASIS wallets

2. **OASIS NFT Scope** - Add to SERV's integration cards
   - Card: "OASIS NFT System"
   - Scopes: `nft:mint`, `nft:transfer`, `nft:view`
   - Enables agents to mint/transfer NFTs (including agent NFTs)

3. **OASIS A2A Scope** - Add to SERV's integration cards
   - Card: "OASIS A2A Protocol"
   - Scopes: `a2a:send`, `a2a:receive`, `a2a:discover`
   - Enables agents to communicate via A2A Protocol

4. **OASIS SERV Scope** - Add to SERV's integration cards
   - Card: "OASIS SERV Discovery"
   - Scopes: `serv:register`, `serv:discover`, `serv:route`
   - Enables agents to register with and discover via SERV

**Pros:**
- ✅ Leverage existing, polished UI
- ✅ Users already familiar with SERV interface
- ✅ Built-in workflow system
- ✅ Credits/payment infrastructure
- ✅ Integration marketplace already exists
- ✅ Faster time to market

**Cons:**
- ❌ Dependency on external platform
- ❌ Limited customization
- ❌ OASIS features may feel "bolted on"
- ❌ Vendor lock-in concerns
- ❌ Less control over user experience
- ❌ May not align with OASIS branding/vision

---

### Option 2: Build Our Own UI in oportal-repo

**Approach:** Create comprehensive Agents tab in OASIS Portal

**What It Would Look Like:**

```
OASIS Portal (oportal-repo)
├── Existing Tabs
│   ├── Dashboard
│   ├── Avatar
│   ├── Wallets
│   ├── NFTs
│   └── ...
│
└── NEW: Agents Tab
    ├── Overview Dashboard
    │   ├── My Agents (count, status)
    │   ├── Available Agents (SERV discovery)
    │   ├── Recent Activity
    │   └── Quick Actions
    │
    ├── Browse Agents
    │   ├── SERV Agents (from SERV infrastructure)
    │   ├── A2A Agents (from A2A Protocol)
    │   ├── OpenSERV Agents (via bridge)
    │   ├── Filter by Service/Capability
    │   ├── Search Agents
    │   └── Agent Cards (with capabilities, pricing)
    │
    ├── Build Agent
    │   ├── Basic Info
    │   │   ├── Name
    │   │   ├── Description
    │   │   └── Avatar Selection
    │   │
    │   ├── Capabilities
    │   │   ├── Services (list)
    │   │   ├── Skills (list)
    │   │   └── Pricing (per service)
    │   │
    │   ├── OASIS Integrations
    │   │   ├── Wallet Integration
    │   │   │   ├── Link Solana Wallet
    │   │   │   ├── Link Ethereum Wallet
    │   │   │   └── Multi-chain Support
    │   │   │
    │   │   ├── NFT Integration
    │   │   │   ├── Mint Agent as NFT
    │   │   │   ├── NFT Marketplace
    │   │   │   └── Trading Options
    │   │   │
    │   │   ├── A2A Protocol
    │   │   │   ├── Enable A2A Messaging
    │   │   │   ├── Configure Endpoints
    │   │   │   └── Payment Settings
    │   │   │
    │   │   └── SERV Registration
    │   │       ├── Register with SERV
    │   │       ├── Discovery Settings
    │   │       └── Health Monitoring
    │   │
    │   ├── External Integrations (Optional)
    │   │   ├── OpenSERV Bridge
    │   │   ├── Google Calendar (via OASIS)
    │   │   ├── Google Drive (via OASIS)
    │   │   └── Other Services
    │   │
    │   └── Preview & Deploy
    │       ├── Agent Card Preview
    │       ├── Test Agent
    │       └── Deploy Agent
    │
    ├── My Agents
    │   ├── Agent List
    │   │   ├── Name, Status, Capabilities
    │   │   ├── NFT Status (if minted)
    │   │   ├── SERV Status (if registered)
    │   │   └── Actions (Edit, Delete, View NFT, etc.)
    │   │
    │   ├── Agent Details
    │   │   ├── Agent Card
    │   │   ├── Capabilities
    │   │   ├── Usage Stats
    │   │   ├── NFT Information
    │   │   ├── SERV Registration
    │   │   └── Activity Log
    │   │
    │   └── Agent Management
    │       ├── Update Capabilities
    │       ├── Link/Unlink User
    │       ├── Mint/Transfer NFT
    │       └── SERV Registration
    │
    ├── Agent Marketplace
    │   ├── Browse Tradable Agents (NFT-backed)
    │   ├── Filter by Price, Capability, Chain
    │   ├── Agent Details
    │   │   ├── Agent Card
    │   │   ├── NFT Information
    │   │   ├── Owner Information
    │   │   ├── Pricing
    │   │   └── Purchase/Transfer
    │   │
    │   └── My Agent Sales
    │       ├── Listed Agents
    │       ├── Sales History
    │       └── Revenue
    │
    └── Agent Communication
        ├── A2A Messages
        │   ├── Inbox
        │   ├── Sent
        │   ├── Compose Message
        │   └── Payment Requests
        │
        └── Agent-to-Agent Calls
            ├── Service Requests
            ├── Task Delegation
            └── Capability Queries
```

**Pros:**
- ✅ Full control over UI/UX
- ✅ Native OASIS branding and experience
- ✅ Deep integration with OASIS ecosystem
- ✅ Can leverage all OASIS features (NFTs, wallets, SERV, etc.)
- ✅ No external dependencies
- ✅ Consistent with existing portal design
- ✅ Can showcase OASIS unique features (NFT trading, multi-chain, etc.)

**Cons:**
- ❌ More development effort
- ❌ Need to build workflow system (or integrate)
- ❌ Need to build payment/credits system (or integrate)
- ❌ Longer time to market
- ❌ Need to maintain UI ourselves

---

## Recommended Approach: Hybrid Strategy

### Phase 1: Build OASIS Native UI (Short Term)

**Rationale:**
1. **Control & Branding**: OASIS has unique features (NFT trading, multi-chain, SERV) that deserve native UI
2. **Ecosystem Integration**: Deep integration with existing portal (wallets, NFTs, STAR, etc.)
3. **Differentiation**: Showcase OASIS-specific capabilities
4. **Foundation**: Build the foundation for future expansion

**Implementation:**
- Add "Agents" tab to oportal-repo
- Implement core agent management UI
- Integrate with existing OASIS APIs
- Focus on OASIS-native features (NFT trading, SERV discovery, A2A messaging)

### Phase 2: SERV Integration Bridge (Medium Term)

**Rationale:**
1. **Marketplace Access**: SERV has existing user base and agent marketplace
2. **Workflow System**: Leverage SERV's workflow builder
3. **Credits System**: Use SERV's payment infrastructure
4. **Best of Both Worlds**: OASIS UI for OASIS features, SERV for workflows

**Implementation:**
- Add OASIS integration cards to SERV platform
- Enable OASIS agents to be discovered via SERV
- Bridge OASIS agents to SERV workflows
- Allow SERV agents to access OASIS features (wallets, NFTs)

### Phase 3: Unified Experience (Long Term)

**Rationale:**
1. **Seamless Experience**: Users can use either UI seamlessly
2. **Cross-Platform**: Agents work in both ecosystems
3. **Marketplace**: Unified agent marketplace across both platforms

**Implementation:**
- Sync agent data between OASIS and SERV
- Unified agent discovery
- Cross-platform agent execution
- Shared agent marketplace

---

## Detailed UI Design: OASIS Agents Tab

### 1. Overview Dashboard

```
┌─────────────────────────────────────────────────────────┐
│  Agents Dashboard                                       │
├─────────────────────────────────────────────────────────┤
│                                                         │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐│
│  │ My Agents│  │Available │  │NFT Agents│  │SERV     ││
│  │    5     │  │   52     │  │    3     │  │Active: 2││
│  └──────────┘  └──────────┘  └──────────┘  └──────────┘│
│                                                         │
│  Quick Actions:                                         │
│  [Build New Agent]  [Browse Agents]  [My Agents]      │
│                                                         │
│  Recent Activity:                                       │
│  • Agent "Data Analyst" NFT minted                      │
│  • Agent "Report Writer" registered with SERV           │
│  • Received payment request from Agent "Helper"         │
└─────────────────────────────────────────────────────────┘
```

### 2. Browse Agents

```
┌─────────────────────────────────────────────────────────┐
│  Browse Agents                    [Search...] [Filter ▼] │
├─────────────────────────────────────────────────────────┤
│                                                         │
│  Tabs: [All] [SERV Agents] [A2A Agents] [NFT Agents]   │
│                                                         │
│  Categories:                                            │
│  [Data Analysis (12)] [Content Creation (8)]           │
│  [Finance (5)] [Research (10)]                          │
│                                                         │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐ │
│  │ Data Analyst │  │ Report Writer│  │ Code Helper  │ │
│  │              │  │              │  │              │ │
│  │ Services:    │  │ Services:    │  │ Services:    │ │
│  │ • analysis   │  │ • reports    │  │ • coding     │ │
│  │              │  │              │  │              │ │
│  │ Skills:      │  │ Skills:      │  │ Skills:      │ │
│  │ • Python     │  │ • Writing    │  │ • JavaScript │ │
│  │ • ML         │  │ • Design    │  │ • Python     │ │
│  │              │  │              │  │              │ │
│  │ [NFT] [SERV] │  │ [SERV]       │  │ [NFT] [SERV] │ │
│  │ Price: 0.5 SOL│ │ Free         │  │ Price: 1 SOL │ │
│  │              │  │              │  │              │ │
│  │ [View] [Use] │  │ [View] [Use] │  │ [View] [Buy] │ │
│  └──────────────┘  └──────────────┘  └──────────────┘ │
└─────────────────────────────────────────────────────────┘
```

### 3. Build Agent

```
┌─────────────────────────────────────────────────────────┐
│  Build Agent                                            │
├─────────────────────────────────────────────────────────┤
│                                                         │
│  Step 1: Basic Information                              │
│  ┌───────────────────────────────────────────────────┐ │
│  │ Name: [Data Analysis Agent        ]              │ │
│  │ Description: [Analyzes data and...]              │ │
│  │ Avatar: [Select Avatar ▼]                        │ │
│  └───────────────────────────────────────────────────┘ │
│                                                         │
│  Step 2: Capabilities                                   │
│  ┌───────────────────────────────────────────────────┐ │
│  │ Services:                                         │ │
│  │ [+ Add Service]                                   │ │
│  │   • data-analysis                                 │ │
│  │   • report-generation                             │ │
│  │                                                   │ │
│  │ Skills:                                           │ │
│  │ [+ Add Skill]                                     │ │
│  │   • Python                                        │ │
│  │   • Machine Learning                              │ │
│  │                                                   │ │
│  │ Pricing (per service):                            │ │
│  │   data-analysis: [0.01 SOL]                       │ │
│  │   report-generation: [0.02 SOL]                   │ │
│  └───────────────────────────────────────────────────┘ │
│                                                         │
│  Step 3: OASIS Integrations                            │
│  ┌───────────────────────────────────────────────────┐ │
│  │ ☑ Wallet Integration                             │ │
│  │   [Link Solana Wallet] [Link Ethereum Wallet]    │ │
│  │                                                   │ │
│  │ ☑ NFT Integration                                │ │
│  │   ☐ Mint Agent as NFT                             │ │
│  │   ☐ Enable Trading                                │ │
│  │                                                   │ │
│  │ ☑ A2A Protocol                                   │ │
│  │   ☐ Enable A2A Messaging                         │ │
│  │   Endpoint: [https://api.oasis...]               │ │
│  │                                                   │ │
│  │ ☑ SERV Registration                              │ │
│  │   ☐ Register with SERV                           │ │
│  │   ☐ Enable Discovery                             │ │
│  └───────────────────────────────────────────────────┘ │
│                                                         │
│  [Preview] [Save Draft] [Deploy Agent]                 │
└─────────────────────────────────────────────────────────┘
```

### 4. My Agents

```
┌─────────────────────────────────────────────────────────┐
│  My Agents                                              │
├─────────────────────────────────────────────────────────┤
│                                                         │
│  ┌─────────────────────────────────────────────────────┐│
│  │ Data Analysis Agent                    [Edit] [⋯]  ││
│  │ Status: Available | SERV: Registered | NFT: Minted ││
│  │ Services: data-analysis, report-generation         ││
│  │ Skills: Python, Machine Learning                  ││
│  │                                                    ││
│  │ [View Agent Card] [View NFT] [SERV Dashboard]     ││
│  └─────────────────────────────────────────────────────┘│
│                                                         │
│  ┌─────────────────────────────────────────────────────┐│
│  │ Report Writer                          [Edit] [⋯]  ││
│  │ Status: Busy | SERV: Registered | NFT: Not Minted ││
│  │ Services: report-generation                        ││
│  │ Skills: Writing, Design                           ││
│  │                                                    ││
│  │ [View Agent Card] [Mint NFT] [SERV Dashboard]      ││
│  └─────────────────────────────────────────────────────┘│
└─────────────────────────────────────────────────────────┘
```

### 5. Agent Marketplace (NFT Trading)

```
┌─────────────────────────────────────────────────────────┐
│  Agent Marketplace                                      │
├─────────────────────────────────────────────────────────┤
│                                                         │
│  Filter: [All Chains ▼] [Price Range] [Capabilities]   │
│                                                         │
│  ┌─────────────────────────────────────────────────────┐│
│  │ Data Analyst Agent (NFT)                           ││
│  │ [Agent Image]                                      ││
│  │                                                    ││
│  │ Services: data-analysis, ML                        ││
│  │ Skills: Python, TensorFlow                        ││
│  │                                                    ││
│  │ NFT: Solana | Token ID: 12345                     ││
│  │ Price: 0.5 SOL                                    ││
│  │ Owner: @username                                  ││
│  │                                                    ││
│  │ [View Details] [Purchase]                         ││
│  └─────────────────────────────────────────────────────┘│
└─────────────────────────────────────────────────────────┘
```

---

## SERV Integration Design (If We Integrate)

### OASIS Integration Cards in SERV

**Card 1: OASIS Multi-Chain Wallet**
```
┌─────────────────────────┐
│ [OASIS Logo]            │
│ OASIS Multi-Chain Wallet│
│                         │
│ Supports:               │
│ • Solana                │
│ • Ethereum              │
│ • Polygon               │
│ • + 47 more chains      │
│                         │
│ [Add Scope]             │
└─────────────────────────┘
```

**Scopes:**
- `oasis:wallet:read` - Read wallet balances
- `oasis:wallet:transfer` - Send transactions
- `oasis:wallet:create` - Create new wallets

**Card 2: OASIS NFT System**
```
┌─────────────────────────┐
│ [OASIS Logo]            │
│ OASIS NFT System        │
│                         │
│ Features:               │
│ • Mint NFTs             │
│ • Transfer NFTs         │
│ • Agent NFTs            │
│ • Cross-chain NFTs      │
│                         │
│ [Add Scope]             │
└─────────────────────────┘
```

**Scopes:**
- `oasis:nft:mint` - Mint NFTs
- `oasis:nft:transfer` - Transfer NFTs
- `oasis:nft:agent` - Mint agent as NFT
- `oasis:nft:view` - View NFT details

**Card 3: OASIS A2A Protocol**
```
┌─────────────────────────┐
│ [OASIS Logo]            │
│ OASIS A2A Protocol      │
│                         │
│ Features:               │
│ • Agent Messaging       │
│ • Payment Requests      │
│ • Service Discovery     │
│ • Task Delegation       │
│                         │
│ [Add Scope]             │
└─────────────────────────┘
```

**Scopes:**
- `oasis:a2a:send` - Send A2A messages
- `oasis:a2a:receive` - Receive A2A messages
- `oasis:a2a:discover` - Discover agents
- `oasis:a2a:payment` - Send/receive payments

**Card 4: OASIS SERV Discovery**
```
┌─────────────────────────┐
│ [OASIS Logo]            │
│ OASIS SERV Discovery    │
│                         │
│ Features:               │
│ • Register with SERV    │
│ • Discover Agents       │
│ • Service Routing       │
│ • Health Monitoring     │
│                         │
│ [Add Scope]             │
└─────────────────────────┘
```

**Scopes:**
- `oasis:serv:register` - Register agent with SERV
- `oasis:serv:discover` - Discover agents via SERV
- `oasis:serv:route` - Route service requests

---

## Technical Implementation

### OASIS Portal Agents Tab

**File Structure:**
```
oportal-repo/
├── agents-dashboard.js      # Main agents tab logic
├── agent-builder.js         # Agent building UI
├── agent-marketplace.js     # NFT trading UI
├── agent-communication.js   # A2A messaging UI
└── styles/
    └── agents.css          # Agent-specific styles
```

**API Integration:**
```javascript
// agents-dashboard.js
const OASIS_API = 'https://api.oasisweb4.com';

// Get my agents
async function getMyAgents() {
    const response = await fetch(`${OASIS_API}/api/a2a/agents`, {
        headers: { 'Authorization': `Bearer ${token}` }
    });
    return response.json();
}

// Discover SERV agents
async function discoverServAgents(serviceName = null) {
    const url = serviceName 
        ? `${OASIS_API}/api/a2a/agents/discover-serv?service=${serviceName}`
        : `${OASIS_API}/api/a2a/agents/discover-serv`;
    const response = await fetch(url);
    return response.json();
}

// Register agent capabilities
async function registerAgentCapabilities(agentId, capabilities) {
    const response = await fetch(`${OASIS_API}/api/a2a/agent/capabilities`, {
        method: 'POST',
        headers: {
            'Authorization': `Bearer ${token}`,
            'Content-Type': 'application/json'
        },
        body: JSON.stringify(capabilities)
    });
    return response.json();
}

// Register with SERV
async function registerWithSERV(agentId) {
    const response = await fetch(`${OASIS_API}/api/a2a/agent/register-service`, {
        method: 'POST',
        headers: { 'Authorization': `Bearer ${token}` }
    });
    return response.json();
}

// Mint agent NFT
async function mintAgentNFT(agentId, nftRequest) {
    const response = await fetch(`${OASIS_API}/api/a2a/agent/${agentId}/mint-nft`, {
        method: 'POST',
        headers: {
            'Authorization': `Bearer ${token}`,
            'Content-Type': 'application/json'
        },
        body: JSON.stringify(nftRequest)
    });
    return response.json();
}

// Get agent NFT
async function getAgentNFT(agentId) {
    const response = await fetch(`${OASIS_API}/api/a2a/agent/${agentId}/nft`);
    return response.json();
}
```

---

## Recommendation

### **Build OASIS Native UI First (Recommended)**

**Why:**
1. **Unique Value Proposition**: OASIS has unique features (NFT trading, multi-chain, SERV) that need native UI
2. **Ecosystem Integration**: Deep integration with existing portal features
3. **Brand Control**: Maintain OASIS branding and user experience
4. **Foundation**: Build foundation for future SERV integration

**Implementation Priority:**
1. **Phase 1 (MVP)**: Basic agent management
   - Browse agents (SERV discovery)
   - Build agent (capabilities, basic info)
   - My agents (list, view, edit)
   - Link to existing OASIS features (wallets, NFTs)

2. **Phase 2**: Advanced features
   - Agent NFT minting/trading
   - A2A messaging UI
   - SERV registration UI
   - Agent marketplace

3. **Phase 3**: SERV integration bridge
   - Add OASIS integration cards to SERV
   - Cross-platform agent discovery
   - Unified marketplace

**Then Add SERV Integration:**
- Add OASIS integration cards to SERV platform
- Enable OASIS agents to appear in SERV marketplace
- Allow SERV agents to access OASIS features
- Bridge workflows between platforms

---

## SERV Discovery Explained

### How Portal Connects to SERV

**Important:** SERV is OASIS's own infrastructure, not an external platform. The portal connects to SERV through the OASIS API.

**Flow:**
```
OASIS Portal (oportal-repo)
    ↓ HTTP Request
    GET /api/a2a/agents/discover-serv
    ↓
OASIS API (A2AController)
    ↓ Calls
A2AManager.DiscoverAgentsViaSERVAsync()
    ↓ Queries
UnifiedAgentServiceManager (SERV Infrastructure)
    ↓ Returns
List<IAgentCard>
    ↓ JSON Response
OASIS Portal displays agents
```

**Key Points:**
- SERV = UnifiedAgentServiceManager (in-memory service registry)
- Services are registered when agents call `RegisterAgentAsServiceAsync()`
- Discovery queries the in-memory cache for matching services
- No external API calls needed - it's all within OASIS

**See:** `Docs/SERV_DISCOVERY_EXPLAINED.md` for detailed explanation.

## OpenSERV Partnership

### Partnership Benefits

OASIS has a strategic partnership with **OpenSERV** (openserv.ai). This partnership creates mutual value:

**For OpenSERV:**
- Access to OASIS's SERV infrastructure (service discovery, routing)
- Network effects (access to OASIS user base)
- A2A Protocol integration (standardized agent communication)
- Marketplace visibility

**For OASIS:**
- AI agent capabilities (OpenSERV's AI infrastructure)
- Ecosystem growth (more services in SERV registry)
- User attraction (AI capabilities)

### Payment Model

**SERV Discovery is FREE:**
- ✅ Service discovery: **FREE**
- ✅ Agent registration: **FREE**
- ✅ A2A messaging: **FREE**

**What May Cost:**
- ⚠️ OpenSERV API usage (their platform may charge)
- ⚠️ Individual agent services (agents may charge via A2A payments)

**See:** `Docs/OPENSERV_PARTNERSHIP_BENEFITS.md` for detailed partnership analysis.

---

## Next Steps

1. **Design Mockups**: Create detailed UI mockups for Agents tab
2. **Implement MVP**: Build basic agent management UI
3. **Integrate APIs**: Connect to existing OASIS agent APIs
4. **Test & Iterate**: Test with real agents and workflows
5. **SERV Integration**: Plan SERV integration bridge

---

**Status:** 📋 Proposal - Awaiting Decision  
**Last Updated:** January 2026
