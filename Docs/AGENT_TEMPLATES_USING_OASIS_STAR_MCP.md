# Agent Templates Using OASIS, STAR, and MCP APIs

**Date:** January 11, 2026  
**Status:** 💡 Strategic Proposal  
**Goal:** Create powerful agent templates leveraging OASIS, STAR, and MCP capabilities

---

## Executive Summary

We have **three powerful APIs** available:
1. **OASIS API** - 500+ endpoints (avatars, karma, NFTs, wallets, data, search)
2. **STAR API** - 500+ endpoints (missions, quests, celestial bodies, OAPPs, templates)
3. **MCP Tools** - Natural language interface to OASIS/STAR via Model Context Protocol

**Opportunity:** Create agent templates that leverage these APIs to build powerful, production-ready agents in minutes.

---

## API Capabilities Overview

### OASIS API (500+ Endpoints)

#### Core Features:
- **Avatar Management** (80+ endpoints)
  - Create, authenticate, update avatars
  - Avatar search, portraits, details
  - Avatar karma management
  
- **Karma System** (20+ endpoints)
  - Reputation tracking
  - Karma voting and weighting
  - Karma history and stats
  
- **NFT Management** (20+ endpoints)
  - Mint NFTs (Solana, Ethereum, etc.)
  - Cross-chain NFT operations
  - GeoNFTs (location-based NFTs)
  - NFT search and discovery
  
- **Wallet Management** (25+ endpoints)
  - Multi-chain wallets (Solana, Ethereum, etc.)
  - Create wallets for avatars
  - Send transactions
  - Balance queries
  
- **Data Management** (30+ endpoints)
  - Holons (universal data containers)
  - File storage
  - Universal search
  - Data aggregation
  
- **HyperDrive** (50+ endpoints)
  - Intelligent routing
  - Auto-failover
  - Performance analytics
  - Cost optimization

### STAR API (500+ Endpoints)

#### Core Features:
- **Missions System** (27+ endpoints)
  - Create, publish, clone missions
  - Mission status and completion
  - Leaderboards and rewards
  - Mission search and filtering
  
- **Quests System** (25+ endpoints)
  - Quest creation and management
  - Quest progression
  - Quest rewards
  
- **Celestial Bodies** (25+ endpoints)
  - Stars, planets, moons, asteroids
  - 3D world objects
  - Celestial body metadata
  
- **OAPPs** (25+ endpoints)
  - OASIS Application management
  - Publish, clone, download OAPPs
  - OAPP versioning
  - OAPP templates
  
- **Templates & Libraries** (50+ endpoints)
  - Reusable templates
  - Code libraries
  - Runtimes and plugins
  
- **NFTs & GeoNFTs** (50+ endpoints)
  - STAR-specific NFT operations
  - Location-based NFTs
  - GeoHotSpots

### MCP Tools (Natural Language Interface)

#### Available Tools:
- **OASIS Operations:**
  - Avatar: register, authenticate, get, update
  - Karma: get, add, remove, stats
  - NFTs: mint, get, send, search
  - Wallets: create, get, send transactions
  - Holons: save, get (data objects)
  
- **A2A/SERV Operations:**
  - Agent registration and discovery
  - Capability registration
  - SERV service registration
  - JSON-RPC 2.0 communication
  
- **Smart Contract Operations:**
  - Generate contracts (Ethereum, Solana)
  - Compile contracts
  - Deploy contracts
  

---

## Agent Template Categories

### 1. **OASIS Integration Agents** 🔐

Agents that leverage OASIS API for identity, reputation, and data management.

#### A. **Avatar Management Agent**
**Use Cases:**
- Automated user onboarding
- Profile management
- Avatar search and discovery
- Bulk avatar operations

**Capabilities:**
- Create avatars programmatically
- Authenticate and manage sessions
- Update avatar profiles
- Search and filter avatars
- Manage avatar karma

**Template Features:**
```python
from oasis_agent import OASISAgent
from oasis_api import AvatarAPI, KarmaAPI

class AvatarManagementAgent(OASISAgent):
    def __init__(self):
        super().__init__(
            name="Avatar Manager",
            services=["avatar-creation", "profile-management", "avatar-search"],
            skills=["Python", "OASIS API"]
        )
        self.avatar_api = AvatarAPI()
        self.karma_api = KarmaAPI()
    
    async def handle_service_request(self, service, params):
        if service == "avatar-creation":
            return await self.create_avatar(params)
        elif service == "profile-management":
            return await self.update_profile(params)
        elif service == "avatar-search":
            return await self.search_avatars(params)
    
    async def create_avatar(self, params):
        # Use OASIS API to create avatar
        avatar = await self.avatar_api.register(
            username=params["username"],
            email=params["email"],
            password=params["password"]
        )
        return {"status": "success", "avatar_id": avatar.id}
```

**OASIS Endpoints Used:**
- `POST /api/avatar/register`
- `POST /api/avatar/authenticate`
- `PUT /api/avatar/{id}`
- `GET /api/avatar/search`

#### B. **Karma & Reputation Agent**
**Use Cases:**
- Automated karma awards
- Reputation monitoring
- Karma analytics
- Reputation-based filtering

**Capabilities:**
- Award/remove karma
- Track karma history
- Generate karma reports
- Monitor reputation trends

**Template:**
```python
class KarmaAgent(OASISAgent):
    def __init__(self):
        super().__init__(
            name="Karma Manager",
            services=["award-karma", "karma-analytics", "reputation-tracking"],
            skills=["Python", "OASIS API", "Analytics"]
        )
        self.karma_api = KarmaAPI()
    
    async def award_karma(self, params):
        result = await self.karma_api.add_karma(
            avatar_id=params["avatar_id"],
            amount=params["amount"],
            reason=params.get("reason")
        )
        return {"status": "success", "new_karma": result.karma}
```

**OASIS Endpoints Used:**
- `POST /api/karma/add-karma-to-avatar`
- `GET /api/karma/get-karma-stats`
- `GET /api/karma/get-karma-history`

#### C. **NFT Minting Agent**
**Use Cases:**
- Automated NFT creation
- Bulk NFT minting
- NFT collection management
- Cross-chain NFT operations

**Capabilities:**
- Mint NFTs on multiple blockchains
- Create NFT collections
- Manage NFT metadata
- Transfer NFTs between wallets

**Template Features:**
```python
class NFTMintingAgent(OASISAgent):
    def __init__(self):
        super().__init__(
            name="NFT Minter",
            services=["mint-nft", "create-collection", "transfer-nft"],
            skills=["Python", "OASIS API", "Blockchain"]
        )
        self.nft_api = NFTAPI()
    
    async def mint_nft(self, params):
        nft = await self.nft_api.mint(
            title=params["title"],
            description=params["description"],
            metadata_url=params["metadata_url"],
            blockchain=params.get("blockchain", "Solana"),
            avatar_id=params["avatar_id"]
        )
        return {"status": "success", "nft_id": nft.id, "mint_address": nft.mint_address}
```

**OASIS Endpoints Used:**
- `POST /api/nft/mint-nft`
- `POST /api/nft/send-nft`
- `GET /api/nft/get-nfts-for-avatar`
- `GET /api/geonft/get-geonfts-for-avatar`

#### D. **Wallet Management Agent**
**Use Cases:**
- Automated wallet creation
- Transaction management
- Multi-chain wallet operations
- Balance monitoring

**Capabilities:**
- Create wallets for avatars
- Send transactions
- Query balances
- Manage multi-chain wallets

**Template Features:**
```python
class WalletAgent(OASISAgent):
    def __init__(self):
        super().__init__(
            name="Wallet Manager",
            services=["create-wallet", "send-transaction", "get-balance"],
            skills=["Python", "OASIS API", "Blockchain"]
        )
        self.wallet_api = WalletAPI()
    
    async def create_wallet(self, params):
        wallet = await self.wallet_api.create_wallet(
            avatar_id=params["avatar_id"],
            provider=params.get("provider", "Solana"),
            name=params.get("name", "Default Wallet")
        )
        return {"status": "success", "wallet_address": wallet.address}
    
    async def send_transaction(self, params):
        tx = await self.wallet_api.send_transaction(
            from_avatar_id=params["from_avatar_id"],
            to_address=params["to_address"],
            amount=params["amount"],
            token=params.get("token", "SOL")
        )
        return {"status": "success", "transaction_id": tx.id}
```

**OASIS Endpoints Used:**
- `POST /api/wallet/avatar/{id}/create-wallet`
- `GET /api/wallet/avatar/{id}/wallets`
- `POST /api/wallet/send-transaction`

---

### 2. **STAR Integration Agents** ⭐

Agents that leverage STAR API for missions, quests, and metaverse experiences.

#### A. **Mission Creation Agent**
**Use Cases:**
- Automated mission generation
- Mission templates
- Mission publishing
- Mission analytics

**Capabilities:**
- Create missions from templates
- Publish missions
- Clone existing missions
- Track mission completion

**Template Features:**
```python
from star_api import MissionsAPI

class MissionAgent(OASISAgent):
    def __init__(self):
        super().__init__(
            name="Mission Creator",
            services=["create-mission", "publish-mission", "mission-analytics"],
            skills=["Python", "STAR API", "Game Design"]
        )
        self.missions_api = MissionsAPI()
    
    async def create_mission(self, params):
        mission = await self.missions_api.create(
            name=params["name"],
            description=params["description"],
            mission_type=params.get("type", "Quest"),
            karma_reward=params.get("karma_reward", 100),
            objectives=params.get("objectives", [])
        )
        return {"status": "success", "mission_id": mission.id}
    
    async def publish_mission(self, params):
        result = await self.missions_api.publish(mission_id=params["mission_id"])
        return {"status": "success", "published": True}
```

**STAR Endpoints Used:**
- `POST /api/missions`
- `POST /api/missions/{id}/publish`
- `POST /api/missions/{id}/clone`
- `GET /api/missions/{id}/leaderboard`

#### B. **Quest Orchestration Agent**
**Use Cases:**
- Dynamic quest generation
- Quest progression tracking
- Quest reward distribution
- Quest analytics

**Capabilities:**
- Create quests
- Track quest progress
- Award quest rewards
- Generate quest reports

**Template Features:**
```python
from star_api import QuestsAPI

class QuestAgent(OASISAgent):
    def __init__(self):
        super().__init__(
            name="Quest Orchestrator",
            services=["create-quest", "track-progress", "award-rewards"],
            skills=["Python", "STAR API", "Game Design"]
        )
        self.quests_api = QuestsAPI()
    
    async def create_quest(self, params):
        quest = await self.quests_api.create(
            name=params["name"],
            description=params["description"],
            quest_type=params.get("type", "Adventure"),
            rewards=params.get("rewards", [])
        )
        return {"status": "success", "quest_id": quest.id}
```

**STAR Endpoints Used:**
- `POST /api/quests`
- `GET /api/quests/{id}/progress`
- `POST /api/quests/{id}/complete`

#### C. **OAPP Builder Agent**
**Use Cases:**
- Automated OAPP creation
- OAPP template generation
- OAPP publishing
- OAPP versioning

**Capabilities:**
- Create OAPPs from templates
- Publish OAPPs
- Clone OAPPs
- Manage OAPP versions

**Template Features:**
```python
from star_api import OAPPsAPI

class OAPPBuilderAgent(OASISAgent):
    def __init__(self):
        super().__init__(
            name="OAPP Builder",
            services=["create-oapp", "publish-oapp", "clone-oapp"],
            skills=["Python", "STAR API", "Low-Code Development"]
        )
        self.oapps_api = OAPPsAPI()
    
    async def create_oapp(self, params):
        oapp = await self.oapps_api.create(
            name=params["name"],
            description=params["description"],
            template_id=params.get("template_id"),
            config=params.get("config", {})
        )
        return {"status": "success", "oapp_id": oapp.id}
```

**STAR Endpoints Used:**
- `POST /api/oapps`
- `POST /api/oapps/{id}/publish`
- `POST /api/oapps/{id}/clone`
- `GET /api/oapps/{id}/versions`

#### D. **Celestial World Builder Agent**
**Use Cases:**
- Automated world creation
- Celestial body generation
- 3D environment setup
- World metadata management

**Capabilities:**
- Create celestial bodies (stars, planets, moons)
- Manage celestial spaces
- Set up world metadata
- Publish worlds

**Template Features:**
```python
from star_api import CelestialBodiesAPI

class WorldBuilderAgent(OASISAgent):
    def __init__(self):
        super().__init__(
            name="World Builder",
            services=["create-world", "create-planet", "setup-metadata"],
            skills=["Python", "STAR API", "3D World Design"]
        )
        self.celestial_api = CelestialBodiesAPI()
    
    async def create_planet(self, params):
        planet = await self.celestial_api.create(
            name=params["name"],
            celestial_body_type="Planet",
            description=params.get("description"),
            metadata=params.get("metadata", {})
        )
        return {"status": "success", "planet_id": planet.id}
```

**STAR Endpoints Used:**
- `POST /api/celestialbodies`
- `POST /api/celestialspaces`
- `GET /api/celestialbodiesmetadata`

---

### 3. **MCP-Powered Agents** 🤖

Agents that use MCP tools for natural language interaction with OASIS/STAR.

#### A. **Natural Language OASIS Agent**
**Use Cases:**
- Conversational OASIS operations
- Voice-activated commands
- AI assistant for OASIS
- Natural language queries

**Capabilities:**
- Interpret natural language requests
- Execute OASIS operations via MCP
- Provide conversational responses
- Handle complex multi-step operations

**Template Features:**
```python
from mcp_client import MCPClient

class NaturalLanguageOASISAgent(OASISAgent):
    def __init__(self):
        super().__init__(
            name="OASIS Assistant",
            services=["natural-language-query", "conversational-ops"],
            skills=["Python", "MCP", "NLP"]
        )
        self.mcp = MCPClient()
    
    async def handle_natural_language(self, params):
        query = params["query"]
        
        # Use MCP to interpret and execute
        if "create avatar" in query.lower():
            # Extract parameters from natural language
            username = self.extract_username(query)
            email = self.extract_email(query)
            
            result = await self.mcp.call_tool(
                "oasis_register_avatar",
                {"username": username, "email": email, "password": "auto-generated"}
            )
            return {"status": "success", "message": f"Created avatar: {username}", "result": result}
        
        elif "mint nft" in query.lower():
            title = self.extract_title(query)
            result = await self.mcp.call_tool(
                "oasis_mint_nft",
                {"title": title, "description": self.extract_description(query)}
            )
            return {"status": "success", "message": f"Minted NFT: {title}", "result": result}
        
        return {"status": "error", "message": "Could not understand request"}
```

**MCP Tools Used:**
- `oasis_register_avatar`
- `oasis_mint_nft`
- `oasis_create_wallet`
- `oasis_get_avatar`
- `oasis_get_karma`

#### B. **Smart Contract Agent (MCP-Powered)**
**Use Cases:**
- Natural language contract generation
- Automated contract deployment
- Contract templates
- Multi-chain contract management

**Capabilities:**
- Generate contracts from descriptions
- Compile contracts
- Deploy to multiple blockchains
- Manage contract versions

**Template Features:**
```python
class SmartContractAgent(OASISAgent):
    def __init__(self):
        super().__init__(
            name="Smart Contract Generator",
            services=["generate-contract", "deploy-contract", "compile-contract"],
            skills=["Python", "MCP", "Smart Contracts", "Blockchain"]
        )
        self.mcp = MCPClient()
    
    async def generate_contract(self, params):
        description = params["description"]
        blockchain = params.get("blockchain", "Solana")
        
        # Use MCP to generate contract
        result = await self.mcp.call_tool(
            "scgen_generate_contract",
            {
                "spec": self.description_to_spec(description),
                "blockchain": blockchain
            }
        )
        return {"status": "success", "contract_code": result.code}
    
    async def deploy_contract(self, params):
        result = await self.mcp.call_tool(
            "scgen_deploy_contract",
            {
                "contract_id": params["contract_id"],
                "blockchain": params["blockchain"]
            }
        )
        return {"status": "success", "deployment_address": result.address}
```

**MCP Tools Used:**
- `scgen_generate_contract`
- `scgen_compile_contract`
- `scgen_deploy_contract`
- `scgen_generate_and_compile`

---

### 4. **Hybrid Agents** 🔄

Agents that combine OASIS, STAR, and MCP capabilities.

#### A. **Complete Game Agent**
**Use Cases:**
- Full game lifecycle management
- Automated game creation
- Player onboarding
- Mission and quest automation

**Capabilities:**
- Create avatars (OASIS)
- Create missions (STAR)
- Award karma (OASIS)
- Mint NFTs as rewards (OASIS)
- Manage quests (STAR)

**Template Features:**
```python
class GameAgent(OASISAgent):
    def __init__(self):
        super().__init__(
            name="Game Manager",
            services=["onboard-player", "create-mission", "award-rewards"],
            skills=["Python", "OASIS API", "STAR API", "Game Design"]
        )
        self.oasis = OASISClient()
        self.star = STARClient()
    
    async def onboard_player(self, params):
        # Create avatar
        avatar = await self.oasis.avatar.register(
            username=params["username"],
            email=params["email"]
        )
        
        # Create wallet
        wallet = await self.oasis.wallet.create(avatar_id=avatar.id)
        
        # Create initial mission
        mission = await self.star.missions.create(
            name="Welcome Mission",
            description="Complete your first mission",
            karma_reward=50
        )
        
        return {
            "status": "success",
            "avatar_id": avatar.id,
            "wallet_address": wallet.address,
            "mission_id": mission.id
        }
    
    async def award_rewards(self, params):
        # Award karma
        await self.oasis.karma.add(avatar_id=params["avatar_id"], amount=params["karma"])
        
        # Mint NFT reward if specified
        if params.get("nft_reward"):
            nft = await self.oasis.nft.mint(
                title=params["nft_reward"]["title"],
                avatar_id=params["avatar_id"]
            )
            return {"status": "success", "karma_awarded": params["karma"], "nft_id": nft.id}
        
        return {"status": "success", "karma_awarded": params["karma"]}
```

#### B. **NFT Marketplace Agent**
**Use Cases:**
- Automated NFT marketplace
- Collection management
- NFT trading
- Revenue tracking

**Capabilities:**
- Mint NFTs (OASIS)
- Create collections (OASIS)
- Manage listings (OASIS)
- Process transactions (OASIS)
- Track sales (OASIS)

**Template Features:**
```python
class NFTMarketplaceAgent(OASISAgent):
    def __init__(self):
        super().__init__(
            name="NFT Marketplace",
            services=["list-nft", "purchase-nft", "create-collection"],
            skills=["Python", "OASIS API", "Blockchain", "E-commerce"]
        )
        self.oasis = OASISClient()
    
    async def list_nft(self, params):
        # Mint NFT
        nft = await self.oasis.nft.mint(
            title=params["title"],
            description=params["description"],
            price=params["price"],
            avatar_id=params["seller_id"]
        )
        
        # Create listing
        listing = await self.oasis.nft.create_listing(
            nft_id=nft.id,
            price=params["price"],
            seller_id=params["seller_id"]
        )
        
        return {"status": "success", "nft_id": nft.id, "listing_id": listing.id}
    
    async def purchase_nft(self, params):
        # Get listing
        listing = await self.oasis.nft.get_listing(params["listing_id"])
        
        # Transfer payment
        tx = await self.oasis.wallet.send_transaction(
            from_avatar_id=params["buyer_id"],
            to_address=listing.seller_wallet,
            amount=listing.price
        )
        
        # Transfer NFT
        await self.oasis.nft.send(
            nft_id=listing.nft_id,
            to_avatar_id=params["buyer_id"]
        )
        
        return {"status": "success", "transaction_id": tx.id}
```

#### C. **Analytics & Reporting Agent**
**Use Cases:**
- Platform analytics
- User behavior tracking
- Revenue reporting
- Performance monitoring

**Capabilities:**
- Query OASIS data
- Query STAR data
- Generate reports
- Track metrics

**Template Features:**
```python
class AnalyticsAgent(OASISAgent):
    def __init__(self):
        super().__init__(
            name="Analytics Engine",
            services=["user-analytics", "revenue-report", "mission-stats"],
            skills=["Python", "OASIS API", "STAR API", "Data Analytics"]
        )
        self.oasis = OASISClient()
        self.star = STARClient()
    
    async def generate_report(self, params):
        report_type = params["type"]
        
        if report_type == "user":
            # Get all avatars
            avatars = await self.oasis.avatar.get_all()
            
            # Get karma stats
            karma_stats = await self.oasis.karma.get_stats()
            
            return {
                "total_users": len(avatars),
                "total_karma": karma_stats.total,
                "average_karma": karma_stats.average
            }
        
        elif report_type == "missions":
            # Get mission stats
            missions = await self.star.missions.get_all()
            completed = await self.star.missions.get_completed()
            
            return {
                "total_missions": len(missions),
                "completed_missions": len(completed),
                "completion_rate": len(completed) / len(missions) * 100
            }
```

---

## Template Implementation Strategy

### Phase 1: Core Templates (Week 1-2)
1. **Avatar Management Agent** - Basic OASIS integration
2. **NFT Minting Agent** - NFT operations
3. **Mission Creation Agent** - STAR integration

### Phase 2: Advanced Templates (Week 3-4)
1. **Natural Language OASIS Agent** - MCP integration
2. **Smart Contract Agent** - MCP + contract generation
3. **Game Agent** - Hybrid OASIS + STAR

### Phase 3: Specialized Templates (Week 5-6)
1. **NFT Marketplace Agent** - E-commerce
2. **Analytics Agent** - Reporting
3. **World Builder Agent** - 3D worlds

---

## Template Structure

Each template includes:

1. **Agent Code** (`agent.py`)
   - Full implementation
   - API client setup
   - Service handlers

2. **Configuration** (`.oasis-agent.json`)
   - Agent metadata
   - Service definitions
   - API credentials

3. **Dockerfile**
   - Containerized deployment
   - Dependencies included

4. **README.md**
   - Setup instructions
   - API documentation
   - Usage examples

5. **Tests** (`test_agent.py`)
   - Unit tests
   - Integration tests
   - Example requests

6. **Examples** (`examples/`)
   - Sample requests
   - Use case scenarios

---

## Quick Start Example

```bash
# Create agent from template
oasis-agent create my-avatar-manager --template avatar-management

# Configure
cd my-avatar-manager
# Edit .oasis-agent.json with your API keys

# Run
python agent.py

# Agent auto-registers with OASIS
# Ready to use!
```

---

## Benefits

1. **Rapid Development** - Start with working code
2. **Best Practices** - Templates follow OASIS patterns
3. **Full Integration** - Leverage all OASIS/STAR/MCP capabilities
4. **Production Ready** - Templates are battle-tested
5. **Extensible** - Easy to customize and extend

---

## Next Steps

1. **Prioritize Templates** - Which templates are most valuable?
2. **Build SDK** - Create Python/JS SDKs for easy integration
3. **Create Templates** - Implement first 3 templates
4. **Documentation** - Comprehensive guides for each template
5. **Examples** - Real-world use case examples

---

## Conclusion

By leveraging OASIS (500+ endpoints), STAR (500+ endpoints), and MCP (natural language interface), we can create powerful agent templates that enable developers to build production-ready agents in minutes rather than days.

The key is providing:
- ✅ Working code out of the box
- ✅ Full API integration
- ✅ Best practices built-in
- ✅ Easy customization
- ✅ Production-ready deployment

This transforms OASIS from "powerful but complex" to "powerful and accessible."
