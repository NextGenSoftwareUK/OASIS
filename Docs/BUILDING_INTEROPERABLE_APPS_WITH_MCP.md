# Building Interoperable Apps Using MCP Prompts with OASIS & STAR APIs

**Date:** January 2026  
**Status:** 📘 Complete Guide  
**Goal:** Demonstrate how to build interoperable applications using MCP (Model Context Protocol) prompts with OASIS and STAR APIs

---

## 🎯 Executive Summary

The OASIS platform provides **three powerful layers** for building interoperable applications:

1. **OASIS API (WEB4)** - 500+ endpoints for data, identity, blockchain operations
2. **STAR API (WEB5)** - 500+ endpoints for gamification, metaverse, OAPP creation
3. **MCP Tools** - Natural language interface that makes both APIs accessible via prompts

**Key Innovation:** MCP transforms complex API interactions into simple natural language prompts, enabling rapid development of interoperable applications (OAPPs) that work across all platforms, blockchains, and networks.

---

## 🏗️ Architecture Overview

```
┌─────────────────────────────────────────────────────────────┐
│                    Your Application                         │
│              (Built with MCP Prompts)                       │
└──────────────────────┬──────────────────────────────────────┘
                       │
                       │ Natural Language Prompts
                       │ (via MCP Protocol)
                       │
┌──────────────────────▼──────────────────────────────────────┐
│              MCP Server (Model Context Protocol)           │
│  ┌──────────────────────────────────────────────────────┐  │
│  │  MCP Tools:                                          │  │
│  │  • oasis_register_avatar                            │  │
│  │  • oasis_mint_nft                                   │  │
│  │  • oasis_create_wallet                              │  │
│  │  • oasis_save_holon                                 │  │
│  │  • oasis_execute_ai_workflow                        │  │
│  │  • scgen_generate_contract                          │  │
│  │  • (100+ more tools)                                │  │
│  └──────────────────────────────────────────────────────┘  │
└──────────────────────┬──────────────────────────────────────┘
                       │
        ┌──────────────┴──────────────┐
        │                            │
┌───────▼────────┐          ┌────────▼────────┐
│  OASIS API    │          │   STAR API      │
│  (WEB4)       │          │   (WEB5)        │
│               │          │                 │
│ • Avatars     │          │ • Missions      │
│ • Karma       │          │ • Quests        │
│ • NFTs        │          │ • OAPPs         │
│ • Wallets     │          │ • Celestial     │
│ • Holons      │          │   Bodies        │
│ • Search      │          │ • Templates     │
│ • A2A/SERV    │          │ • Libraries     │
└───────────────┘          └─────────────────┘
```

---

## 🔑 Core Concepts

### 1. **MCP (Model Context Protocol)**

MCP is a protocol that allows AI assistants (like Cursor) to interact with external services through **tools**. Instead of writing code to call APIs, you describe what you want in natural language, and MCP handles the API calls.

**Example:**
```
❌ Without MCP (Traditional):
const response = await fetch('https://api.oasisweb4.com/api/avatar/register', {
  method: 'POST',
  headers: { 'Authorization': 'Bearer token', 'Content-Type': 'application/json' },
  body: JSON.stringify({ username: 'player1', email: 'player@example.com', password: 'pass' })
});

✅ With MCP (Natural Language):
"Create an avatar with username 'player1' and email 'player@example.com'"
```

### 2. **OAPPs (OASIS Applications)**

OAPPs are interoperable applications that can:
- Work across all blockchains (Solana, Ethereum, etc.)
- Deploy to multiple platforms (Web, Mobile, VR, AR)
- Share data and components with other OAPPs
- Use the universal OASIS identity system

### 3. **Interoperability Through OASIS**

OASIS provides:
- **Universal Identity** - One avatar works everywhere
- **Cross-Chain Operations** - NFTs, wallets, transactions work on any blockchain
- **Data Portability** - Holons (data objects) can be shared between apps
- **Provider Abstraction** - Switch between 50+ providers seamlessly

---

## 🚀 Building Your First Interoperable App with MCP

### Step 1: Set Up MCP Connection

MCP is already configured in your Cursor IDE. The MCP server exposes tools that you can use via natural language prompts.

**Verify MCP is working:**
```bash
# Check MCP server status
cd /Users/maxgershfield/OASIS_CLEAN/MCP
node dist/index.js
```

### Step 2: Create an Avatar (Identity Foundation)

**MCP Prompt:**
```
"Create an avatar for my app user with username 'myapp_user' and email 'user@myapp.com'"
```

**What happens:**
1. MCP calls `oasis_register_avatar` tool
2. OASIS API creates the avatar
3. Returns avatar ID and authentication token
4. This avatar can now be used across all OASIS/STAR services

**Result:**
```json
{
  "result": {
    "id": "avatar-uuid",
    "username": "myapp_user",
    "email": "user@myapp.com"
  }
}
```

### Step 3: Create a Wallet (Blockchain Access)

**MCP Prompt:**
```
"Create a Solana wallet for avatar {avatar_id}"
```

**What happens:**
1. MCP calls `oasis_create_solana_wallet` tool
2. OASIS generates a Solana keypair
3. Links wallet to avatar
4. Wallet is now available for all blockchain operations

### Step 4: Create Your App Data (Holons)

**MCP Prompt:**
```
"Save a holon representing my app's user profile with name 'UserProfile', 
description 'Main user profile data', and data containing {userData}"
```

**What happens:**
1. MCP calls `oasis_save_holon` tool
2. OASIS stores the data with auto-failover across providers
3. Data is now accessible from any OAPP or service

### Step 5: Create a Mission (Gamification)

**MCP Prompt:**
```
"Create a STAR mission called 'Welcome Mission' that rewards 100 karma 
when users complete their profile"
```

**What happens:**
1. MCP would call STAR API mission creation (if STAR tools were added)
2. Mission is created in the STAR ecosystem
3. Can be linked to your OAPP

---

## 💡 Real-World Examples

### Example 1: NFT Collection App

**Goal:** Create an app that mints NFTs and tracks them across blockchains

**MCP Prompts Sequence:**

1. **Setup:**
```
"Create an avatar for NFT collector 'collector1' with email 'collector@nftapp.com'"
```

2. **Create Wallet:**
```
"Create a Solana wallet for avatar {avatar_id}"
```

3. **Mint NFT:**
```
"Mint an NFT with title 'My First NFT', description 'A special collectible', 
image URL 'https://example.com/image.png', and symbol 'MYNFT'"
```

4. **Track NFT:**
```
"Get all NFTs owned by avatar {avatar_id}"
```

**Result:** A fully functional NFT collection app that works across all blockchains supported by OASIS.

### Example 2: Gamified Learning App

**Goal:** Create an app that awards karma and NFTs for learning achievements

**MCP Prompts Sequence:**

1. **User Onboarding:**
```
"Create an avatar for student 'student1' with email 'student@learnapp.com'"
```

2. **Award Karma:**
```
"Add 50 karma to avatar {avatar_id} for completing a lesson, 
karma type 'SelfHelpImprovement', source type 'App'"
```

3. **Mint Achievement NFT:**
```
"Mint an NFT titled 'Lesson Completed' as a reward for avatar {avatar_id}"
```

4. **Track Progress:**
```
"Get karma stats for avatar {avatar_id}"
```

**Result:** An interoperable learning app where achievements are stored as NFTs and karma, accessible from any OASIS-compatible app.

### Example 3: Multi-Chain DeFi App

**Goal:** Create an app that works with wallets on multiple blockchains

**MCP Prompts Sequence:**

1. **Create Multi-Chain Wallets:**
```
"Create a Solana wallet for avatar {avatar_id}"
"Create an Ethereum wallet for avatar {avatar_id}"
```

2. **Check Balances:**
```
"Get wallet information for avatar {avatar_id}"
```

3. **Send Transaction:**
```
"Send 0.1 SOL from avatar {avatar_id} to address {recipient_address}"
```

**Result:** A DeFi app that works seamlessly across Solana, Ethereum, and other supported blockchains.

---

## 🔧 Advanced Patterns

### Pattern 1: OAPP Creation with MCP

**Goal:** Create a complete OAPP (OASIS Application) using MCP prompts

**Workflow:**

1. **Create App Structure (Holons):**
```
"Save a holon representing my OAPP configuration with:
- name: 'MyAwesomeApp'
- description: 'A revolutionary interoperable app'
- data: { appConfig, features, metadata }
"
```

2. **Create Avatar for App:**
```
"Create an avatar with type 'Agent' for my OAPP service"
```

3. **Register App Capabilities:**
```
"Register agent capabilities for avatar {app_avatar_id} with services:
['user-management', 'nft-minting', 'wallet-operations']"
```

4. **Register as SERV Service:**
```
"Register the agent as a SERV service so other apps can discover it"
```

**Result:** Your app is now discoverable and can be used by other OAPPs in the ecosystem.

### Pattern 2: Cross-App Data Sharing

**Goal:** Share data between multiple OAPPs using holons

**Workflow:**

1. **App A creates shared data:**
```
"Save a holon with name 'SharedUserData', parentId null, 
so it can be accessed by other apps"
```

2. **App B accesses shared data:**
```
"Search for holons with name 'SharedUserData'"
```

3. **App B updates shared data:**
```
"Update holon {holon_id} with new data {updatedData}"
```

**Result:** Multiple apps can share and update the same data, creating a truly interoperable ecosystem.

### Pattern 3: AI-Powered App Features

**Goal:** Add AI capabilities to your app using A2A Protocol

**Workflow:**

1. **Discover AI Agents:**
```
"Discover agents that provide 'data-analysis' service"
```

2. **Execute AI Workflow:**
```
"Execute an AI workflow with agent {agent_id} to analyze user data: {userData}"
```

3. **Process Results:**
```
"Save the AI analysis results as a holon for the user"
```

**Result:** Your app now has AI capabilities without building AI infrastructure yourself.

---

## 📋 MCP Tools Reference

### OASIS Identity & Authentication
- `oasis_register_avatar` - Create new avatar
- `oasis_authenticate_avatar` - Login and get JWT token
- `oasis_get_avatar_detail` - Get avatar information
- `oasis_update_avatar` - Update avatar profile

### OASIS Wallet Operations
- `oasis_create_solana_wallet` - Create Solana wallet
- `oasis_create_ethereum_wallet` - Create Ethereum wallet
- `oasis_get_wallet` - Get wallet information
- `oasis_send_transaction` - Send tokens between wallets

### OASIS NFT Operations
- `oasis_mint_nft` - Mint new NFT
- `oasis_get_nfts` - Get NFTs for avatar
- `oasis_send_nft` - Transfer NFT
- `oasis_place_geo_nft` - Place NFT at real-world location

### OASIS Data Management
- `oasis_save_holon` - Save data object
- `oasis_get_holon` - Get data object
- `oasis_search_holons` - Search data objects
- `oasis_update_holon` - Update data object

### OASIS Karma System
- `oasis_get_karma` - Get karma score
- `oasis_add_karma` - Award karma
- `oasis_get_karma_stats` - Get karma statistics
- `oasis_get_karma_history` - Get karma history

### A2A Protocol & AI
- `oasis_register_agent_capabilities` - Register AI agent
- `oasis_discover_agents_via_serv` - Find AI agents
- `oasis_execute_ai_workflow` - Run AI workflow
- `oasis_send_a2a_jsonrpc_request` - Send A2A message

### Smart Contracts
- `scgen_generate_contract` - Generate contract from spec
- `scgen_compile_contract` - Compile contract
- `scgen_deploy_contract` - Deploy to blockchain

### Media Generation
- `glif_generate_image` - Generate image with Glif
- `nanobanana_generate_image` - Generate image with Nano Banana
- `ltx_generate_video` - Generate video with LTX

---

## 🎨 Building Interoperable UI Components

### Pattern: Universal Avatar Display

**MCP Prompt:**
```
"Get avatar details for username 'player1' and return the portrait URL"
```

**Use in your app:**
```javascript
// Your app can display avatars from any OASIS app
const avatar = await getAvatar('player1');
<img src={avatar.portrait} alt={avatar.username} />
```

### Pattern: Cross-App NFT Gallery

**MCP Prompt:**
```
"Get all NFTs for avatar {avatar_id} from all blockchains"
```

**Use in your app:**
```javascript
// Display NFTs from Solana, Ethereum, etc. in one gallery
const nfts = await getNFTs(avatarId);
nfts.forEach(nft => {
  // Render NFT from any blockchain
  renderNFT(nft);
});
```

### Pattern: Universal Wallet Balance

**MCP Prompt:**
```
"Get portfolio value for avatar {avatar_id} across all chains"
```

**Use in your app:**
```javascript
// Show total value across all blockchains
const portfolio = await getPortfolioValue(avatarId);
console.log(`Total: $${portfolio.totalValue}`);
```

---

## 🔄 Interoperability Patterns

### 1. **Identity Interoperability**

**Problem:** Users have different accounts on different apps  
**Solution:** OASIS provides universal avatar system

**MCP Approach:**
```
"Create avatar that works across all my apps"
"Authenticate user once, use everywhere"
```

**Result:** One identity works across all OAPPs.

### 2. **Data Interoperability**

**Problem:** Apps store data in silos  
**Solution:** Holons provide universal data containers

**MCP Approach:**
```
"Save data as holon that other apps can access"
"Search for holons shared by other apps"
```

**Result:** Data can be shared between apps while maintaining user control.

### 3. **Asset Interoperability**

**Problem:** NFTs and tokens locked to one blockchain  
**Solution:** OASIS abstracts blockchain differences

**MCP Approach:**
```
"Mint NFT that works across all blockchains"
"Send tokens between any blockchains"
```

**Result:** Assets work across all supported blockchains.

### 4. **Service Interoperability**

**Problem:** Apps can't discover or use each other's services  
**Solution:** A2A Protocol and SERV infrastructure

**MCP Approach:**
```
"Register my app as a discoverable service"
"Find and use services from other apps"
```

**Result:** Apps can discover and use each other's capabilities.

---

## 🛠️ Development Workflow

### Phase 1: Design Your App

1. **Define App Purpose:**
   - What problem does it solve?
   - What OASIS/STAR features does it need?

2. **Map to MCP Tools:**
   - Which MCP tools provide the features you need?
   - What natural language prompts will you use?

3. **Design Data Model:**
   - What holons will store your app data?
   - How will data be shared with other apps?

### Phase 2: Build Core Features

1. **Identity System:**
   ```
   "Create avatar system for my app users"
   ```

2. **Data Storage:**
   ```
   "Save app data as holons"
   ```

3. **Blockchain Integration:**
   ```
   "Create wallets and enable NFT/token operations"
   ```

### Phase 3: Add Interoperability

1. **Register as Service:**
   ```
   "Register my app capabilities so others can discover it"
   ```

2. **Enable Data Sharing:**
   ```
   "Make my app data accessible to other OAPPs"
   ```

3. **Add Cross-App Features:**
   ```
   "Integrate with other OAPPs via A2A Protocol"
   ```

### Phase 4: Deploy & Scale

1. **Test Interoperability:**
   - Test with other OAPPs
   - Verify cross-chain operations
   - Test data sharing

2. **Deploy:**
   - Deploy to OASIS ecosystem
   - Make discoverable via SERV
   - Publish to STARNET

---

## 📚 Best Practices

### 1. **Use Natural Language Prompts**

✅ **Good:**
```
"Create a Solana wallet for my user"
```

❌ **Avoid:**
```
"Call POST /api/wallet/avatar/{id}/create-wallet with providerType=Solana"
```

### 2. **Leverage OASIS Abstraction**

✅ **Good:**
```
"Mint an NFT" (OASIS handles blockchain selection)
```

❌ **Avoid:**
```
"Call Solana RPC directly to mint NFT"
```

### 3. **Design for Interoperability**

✅ **Good:**
```
"Save data as holon with public access for sharing"
```

❌ **Avoid:**
```
"Store data in my own database only"
```

### 4. **Use A2A for AI Features**

✅ **Good:**
```
"Discover AI agents and use their services"
```

❌ **Avoid:**
```
"Build my own AI infrastructure"
```

---

## 🎯 Use Cases

### 1. **Social Gaming Platform**

**Features:**
- User avatars (OASIS)
- Missions and quests (STAR)
- NFT rewards (OASIS)
- Karma system (OASIS)
- Cross-platform play

**MCP Prompts:**
```
"Create gaming platform with avatars, missions, and NFT rewards"
"Set up karma system for player achievements"
"Enable cross-platform multiplayer"
```

### 2. **NFT Marketplace**

**Features:**
- Multi-chain NFT support
- Universal wallet
- Cross-chain transactions
- Reputation system

**MCP Prompts:**
```
"Create marketplace that supports NFTs from all blockchains"
"Enable wallet operations across chains"
"Implement seller reputation with karma"
```

### 3. **Educational Platform**

**Features:**
- Learning achievements as NFTs
- Karma for progress
- Cross-app progress sharing
- Certificate NFTs

**MCP Prompts:**
```
"Create learning platform with NFT certificates"
"Award karma for course completion"
"Share progress with other educational apps"
```

### 4. **DeFi Application**

**Features:**
- Multi-chain wallet support
- Cross-chain swaps
- Portfolio tracking
- Transaction history

**MCP Prompts:**
```
"Create DeFi app with multi-chain wallet support"
"Enable cross-chain token transfers"
"Track portfolio across all chains"
```

---

## 🚀 Getting Started Checklist

- [ ] Verify MCP server is running
- [ ] Get OASIS API credentials
- [ ] Understand your app's interoperability needs
- [ ] Map features to MCP tools
- [ ] Create first avatar via MCP prompt
- [ ] Create wallet via MCP prompt
- [ ] Save first holon via MCP prompt
- [ ] Test cross-chain operations
- [ ] Register app as A2A service
- [ ] Test with other OAPPs

---

## 📖 Additional Resources

- **OASIS API Docs:** `/Docs/Devs/API Documentation/WEB4_OASIS_API_Documentation.md`
- **STAR API Docs:** `/Docs/Devs/API Documentation/WEB5_STAR_API_Documentation.md`
- **MCP Tools:** `/MCP/src/tools/oasisTools.ts`
- **Agent Templates:** `/Docs/AGENT_TEMPLATES_USING_OASIS_STAR_MCP.md`
- **OAPP Creation:** `/Docs/Devs/STAR_CLI_DOCUMENTATION.md`

---

## 🎉 Conclusion

MCP prompts transform the complexity of OASIS and STAR APIs into simple natural language interactions. This enables:

1. **Rapid Development** - Build apps in hours, not weeks
2. **True Interoperability** - Apps work together seamlessly
3. **Cross-Chain Support** - One codebase, all blockchains
4. **Universal Identity** - One avatar, all apps
5. **Shared Data** - Apps can share data while maintaining control

By leveraging MCP prompts with OASIS and STAR APIs, you're not just building an app—you're building a piece of the interoperable OASIS ecosystem.

---

*Start building your interoperable app today with a simple MCP prompt!*
