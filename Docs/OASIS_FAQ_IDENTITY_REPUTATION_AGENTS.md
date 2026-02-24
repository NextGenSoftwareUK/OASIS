# OASIS FAQ: Identity, Reputation & Agents

## OpenSERV Agents

### What is an OpenSERV agent from a practical, community-facing perspective?

**Answer:**

OpenServ has their own platform which we don't control. However, OASIS allows SERV agents to exist on our platform, access our APIs, and we will be creating characters and services using SERV agents that live in our world.

**Key Points:**
- **SERV agents can register** with OASIS as `AvatarType.Agent` avatars
- **Full API access** - SERV agents can use all OASIS APIs (Avatar, Karma, NFT, Wallet, etc.)
- **Integration with Our World** - SERV agents can be used to create characters and services within OASIS applications
- **Holonic Architecture Integration** - We are keen to add our holonic architecture to BRAID (SERV's agentic infrastructure) to demonstrate persistent, shared memory

**Technical Implementation:**
- SERV agents are registered as OASIS avatars with `AvatarType.Agent`
- Agents can register capabilities and services via the A2A Protocol
- Agents can be discovered via SERV infrastructure through OASIS APIs
- Agents can earn karma, mint NFTs, and participate in the full OASIS ecosystem

---

## Reputation (Karma) System

### What is reputation used for inside OASIS?

Reputation in OASIS is measured through the **Karma System**, which serves multiple purposes:

1. **Level Progression**: Karma determines your avatar's level (Level 1-99, with max level requiring ~1.2 trillion karma)
2. **Content Unlocking**: Various quests, special powers, abilities, items, and locations unlock based on karma thresholds
3. **Real-World Benefits**: Higher karma can entitle you to free upgrades, discounts, and rewards in real life
4. **Token Multipliers**: Higher karma = higher token rewards (karma acts as a reputation layer for token distribution)
5. **Community Recognition**: Karma is visible to other users, helping build trust and reputation
6. **Leaderboards**: Global karma leaderboards for community recognition

### Does reputation affect discovery?

**Yes.** While not explicitly implemented in all discovery mechanisms yet, karma/reputation can affect:
- **Agent Discovery**: Higher karma agents may be prioritized in SERV agent discovery
- **Content Discovery**: Higher karma users may see more relevant content
- **Social Discovery**: Users can view karma levels of others to find trustworthy partners for quests

### Does reputation affect rewards?

**Yes, significantly:**
- **Token Rewards**: Higher karma = higher token multipliers (up to 2.0x for high-value karma actions)
- **Karma-Weighted Rewards**: Builder rewards are weighted by karma score
- **Real-World Rewards**: Free upgrades, discounts, holidays based on karma level
- **In-Game Benefits**: New abilities, special items, unlock content based on karma thresholds

**Karma-Token Integration Examples:**
- `ContributingTowardsAGoodCauseCreatorOrganiser`: 10x karma → 2.0x token multiplier
- `BeASuperHero`: 8x karma → 1.8x token multiplier
- `BeAHero`: 7x karma → 1.5x token multiplier
- `HelpingTheEnvironment`: 5x karma → 1.2x token multiplier

### Does reputation affect permissions?

**Yes:**
- **Content Access**: Special locations (e.g., mystic temples, animal sanctuaries) require minimum karma levels
- **Quest Access**: Some quests require specific karma thresholds or karma in specific categories
- **Feature Unlocking**: Abilities, powers, and items unlock based on karma levels
- **Governance Rights**: Higher karma may grant governance rights in certain systems

**Example:** To enter a special mystic temple in Our World, you may need:
- Total karma level of 1000
- Karma level of 500 in Self Help/Improvement
- Karma level of 500 for Our World

### Does reputation affect creation rights?

**Yes:**
- **Holon Creation**: Higher karma may grant rights to create certain types of holons
- **NFT Creation**: Karma affects NFT minting capabilities and rewards
- **Content Creation**: Some content creation features may require minimum karma levels
- **Agent Registration**: Higher karma may be required for certain agent capabilities

### Can reputation be lost or reduced?

**Yes, absolutely.** Karma can be lost through negative actions:

**Negative Karma Types:**
- `DropLitter` (-9 karma)
- `AttackPhysciallyOtherPersonOrPeople` (-10 karma)
- `AttackVerballyOtherPersonOrPeople` (-5 karma)
- `HarmingAnimals` (-10 karma)
- `HarmingChildren` (-9 karma)
- `HarmingNature` (-10 karma)
- `BeingSelfish` (-3 karma)
- `NotTeamPlayer` (-3 karma)
- `DisrespectPersonOrPeople` (-4 karma)
- `NutritionEatMeat` (-7 karma)
- `NutritionEatDiary` (-6 karma)
- And more...

**Karma Decay:**
- Karma decreases over time if not maintained (1% decay per month)
- Formula: `Karma(t) = Karma(t-1) × 0.99 + NewKarma`
- Prevents "set and forget" karma accumulation
- Encourages sustained positive contribution

**Consequences of Losing Karma:**
- If you fall below a karma threshold, previously unlocked content becomes locked again
- Level may decrease if karma drops below level thresholds
- Token multipliers decrease with lower karma

### Is reputation portable across OASIS products?

**Yes, absolutely.** Karma/reputation is fully portable:

1. **Universal Avatar System**: Your avatar and karma are stored in the OASIS core, accessible across all OASIS products
2. **Multi-Provider Storage**: Karma is stored across multiple providers (blockchains, databases, etc.) ensuring portability
3. **Single Sign-On (SSO)**: One avatar/karma profile works across all OASIS applications
4. **Cross-Platform**: Karma earned in one OASIS app (game, dApp, website) is available in all others
5. **Interoperability**: The OASIS API enables karma to be earned anywhere that integrates with OASIS

**From the documentation:**
> "Our World will automatically support all of the platforms/networks/protocols listed above so your profile/avatar/karma will be available to any apps that use their platforms/networks/protocols. This will also make it easier to earn karma in a wider range of apps by supporting as many platforms/networks/protocols as possible."

---

## Avatar / Identity System

### What is an Avatar / Identity inside of OASIS?

An **Avatar** in OASIS is your digital identity - a comprehensive representation of you across the entire OASIS ecosystem. It's much more than just a username or profile.

### Core Avatar Properties

**Basic Identity:**
- **Globally Unique ID**: Each avatar has a unique GUID that works across all platforms
- **Username, Email, Password**: Standard authentication credentials
- **Full Name**: Title, First Name, Last Name
- **Avatar Type**: User, Wizard, Agent, or System

**Extended Identity (AvatarDetail):**
- **Karma**: Your reputation score (determines level)
- **XP**: Experience points
- **Level**: Calculated from karma (Level 1-99)
- **Stats**: HP, Mana, Stamina (for gamification)
- **Portrait/Model3D**: Visual representation
- **Location Data**: Address, country, postcode
- **DOB**: Date of birth

**Advanced Properties:**
- **Chakras**: Energy system representation
- **Aura**: Spiritual/metaphysical attributes
- **GeneKeys**: Personal development system
- **Human Design**: Astrological/design system
- **Skills, Attributes, SuperPowers**: Gamification elements
- **Spells, Achievements, Inventory**: Game items
- **Omniverse**: "We have all of creation inside of us" - nested reality representation

### How Levelling Up Works

**Level Calculation:**
- Level is **automatically calculated** from total karma
- Formula: `Level = LevelManager.GetLevelFromKarma(Karma)`
- Levels range from 1 to 99 (max level requires ~1.2 trillion karma)
- Level thresholds increase exponentially (configurable via `LevelThresholdWeighting`)

**Level Progression:**
- **Level 1**: Default starting level
- **Level 2+**: Requires increasing karma thresholds
- Each level requires: `currentKarma + 100 + (currentKarma / LevelThresholdWeighting)`
- Default `LevelThresholdWeighting` = 4 (can be adjusted to make leveling easier/harder)

**What Unlocks with Levels:**
- New abilities and powers
- Special items and inventory slots
- Access to restricted locations
- Quest availability
- Token reward multipliers
- Governance rights

### How Interoperability Across Platforms Works

**Universal API Architecture:**
- **Single Avatar, Multiple Providers**: Your avatar is stored across multiple providers simultaneously
- **Provider Abstraction**: OASIS abstracts away provider differences - you interact with one API
- **Hot-Swappable Providers**: Can switch providers without losing data
- **Auto-Replication**: Avatar data is automatically replicated across providers for redundancy

**Cross-Platform Identity:**
- **SSO (Single Sign-On)**: One login works across all OASIS applications
- **Universal Wallet System**: Wallets for multiple blockchains managed through one avatar
- **Cross-Provider Data Sync**: Avatar data syncs across all providers automatically
- **Provider-Specific Keys**: Each provider can have unique keys/usernames, but avatar ID is universal

**Supported Platforms:**
- **Blockchains**: Ethereum, Solana, Polygon, Arbitrum, Avalanche, BNB Chain, Cardano, NEAR, Polkadot, Cosmos, Fantom, Optimism, Rootstock, TRON, EOSIO, Telos, Aptos, Alephium, Radix, and more
- **Storage**: MongoDB, Neo4j, SQLite, LocalFile, Azure Cosmos DB
- **Networks**: Holochain, IPFS, ThreeFold, SOLID, ActivityPub, Scuttlebutt, Telegram, Pinata
- **50+ Total Providers**: All accessible through one unified API

### How It's Designed to Work with NFTs

**NFT Integration:**
- **Avatar NFTs**: Avatars can be represented as NFTs on any supported blockchain
- **Reputation NFTs**: Karma/reputation can be minted as NFTs (agent reputation NFTs)
- **Achievement NFTs**: Achievements can be minted as NFTs
- **Credential NFTs**: Certificates and credentials can be stored as NFTs

**NFT Capabilities:**
- **Multi-Chain NFTs**: Mint NFTs on any supported blockchain (Ethereum, Solana, Polygon, etc.)
- **Cross-Chain Transfer**: Send NFTs between blockchains via OASIS
- **GeoNFTs**: Location-based NFTs for real-world integration
- **NFT Metadata**: Rich metadata stored on-chain or off-chain (IPFS)
- **NFT Collections**: Organize NFTs into collections
- **NFT Gifting**: Send NFTs to other avatars

**Example Use Cases:**
- Agent reputation scores minted as NFTs
- Service completion certificates as NFTs
- Achievement badges as NFTs
- Avatar portraits as NFTs
- Credential verification via NFTs

### How It's Designed to Work with Credentials

**Credential System:**
- **Verifiable Credentials**: Credentials can be stored as NFTs or holons
- **Credential Verification**: Credentials are verifiable across platforms
- **Credential Portability**: Credentials travel with your avatar
- **Credential Categories**: Skills, achievements, certifications, licenses, etc.

**Integration Points:**
- Credentials stored in avatar's `Achievements` list
- Credentials can be minted as NFTs for blockchain verification
- Credentials linked to karma/level requirements
- Credentials visible in avatar profile across all platforms

### How Its Holonic System Works

**Holonic Architecture:**
- **Avatar as Holon**: Every avatar IS a holon (inherits from `Holon` base class)
- **Holon Definition**: "A part that is also a whole" - functions as standalone entity while being part of larger system
- **Parent-Child Relationships**: Avatars can have parent holons and child holons (infinite nesting)
- **Universal Format**: Works across all platforms, blockchains, and databases

**Holon Properties:**
- **Globally Unique ID**: `Guid Id` - works everywhere
- **Parent Holon ID**: Links to parent holon
- **Children**: List of child holons (infinite depth)
- **Holon Type**: Classification (Avatar, AvatarDetail, etc.)
- **Metadata**: Flexible metadata dictionary
- **Provider Keys**: Unique storage keys per provider
- **Version Control**: Full history tracking

**Holon Hierarchy Example:**
```
Omniverse
  └─ Multiverse
      └─ Universe
          └─ SolarSystem
              └─ Planet
                  └─ Moon
                      └─ Zome
                          └─ Holon (Avatar)
                              └─ Child Holon (Avatar Memory)
                                  └─ Child Holon (Avatar Knowledge)
                                      └─ ... (infinite depth)
```

**Benefits:**
- **Identity Independence**: Avatar identity not derived from any single external system
- **Persistence**: Identity survives movement/replication across providers
- **Interoperability**: Identity invariants remain intact across platforms
- **Observability**: State, dependencies, and conflicts are inspectable
- **Shared Memory**: Holons enable persistent, shared memory for agents

### How It Connects to SERV Agents

**SERV Agent Integration:**
- **Agent Avatars**: SERV agents are registered as `AvatarType.Agent` avatars
- **A2A Protocol**: Agents communicate via Agent-to-Agent (A2A) Protocol
- **SERV Infrastructure**: Agents registered in SERV can be discovered via OASIS APIs
- **Capability Registration**: Agents register capabilities (services, skills) with OASIS
- **Holonic Memory**: Agent memories/knowledge stored as child holons of agent avatar

**Integration Flow:**
1. **Register Agent**: SERV agent registers as OASIS avatar (`AvatarType.Agent`)
2. **Register Capabilities**: Agent registers services/skills via A2A Protocol
3. **Register with SERV**: Agent registered in SERV infrastructure for discovery
4. **Discover Agents**: Other agents/users can discover via SERV through OASIS APIs
5. **Agent Communication**: Agents communicate via A2A JSON-RPC 2.0 protocol
6. **Shared Memory**: Agent memories/knowledge stored as holons for persistence

**Code Example:**
```csharp
// Register OpenSERV agent as OASIS avatar
var avatarResult = await AvatarManager.Instance.RegisterAsync(
    avatarTitle: "Agent",
    firstName: "OpenSERV",
    lastName: openServAgentId,
    email: avatarEmail,
    password: avatarPassword,
    username: avatarUsername,
    avatarType: AvatarType.Agent,
    createdOASISType: OASISType.OASIS
);

// Register agent capabilities
await AgentManager.Instance.RegisterAgentCapabilitiesAsync(
    avatarId: avatarResult.Result.Id,
    capabilities: new AgentCapabilities {
        Services = ["data-analysis", "nlp"],
        Skills = ["Python", "Machine Learning"],
        Status = AgentStatus.Available
    }
);

// Register as SERV service
await ONETUnifiedArchitecture.Instance.RegisterUnifiedServiceAsync(
    unifiedService: new UnifiedService {
        ServiceId = avatarResult.Result.Id.ToString(),
        ServiceName = $"OpenSERV Agent: {openServAgentId}",
        Endpoint = openServEndpoint,
        Protocol = "OpenSERV_HTTP"
    }
);
```

### How Avatars Integrate into Our World

**Our World Integration:**
- **Digital Twin**: Avatar is your "digital twin" - represents you in the virtual world
- **Real-World Connection**: Avatar connects to real-world location via GPS/geolocation
- **AR/VR Presence**: Avatar appears in AR/VR experiences based on location
- **Quest System**: Avatars participate in quests, missions, and chapters
- **Social Interaction**: Avatars interact with other avatars in shared spaces
- **Karma-Based Access**: Avatar karma determines access to locations, quests, and content

**Integration Points:**
- **3D Map**: Avatar placed on 3D map using GPS location
- **AR Mode**: Avatar appears in AR when in parks, locations
- **Quest Participation**: Avatar joins quests based on location and karma
- **Social Features**: Avatar interacts with other avatars in real-time
- **Content Creation**: Avatar creates content (holons) in the world
- **NFT Collection**: Avatar collects GeoNFTs at real-world locations

**From the Vision:**
> "We started off building the digital twin of you (the Avatar Karma System back in 2019) along with the digital twin of our planet called Our World (we started doing R&D, research, designing, prototyping back in 2011). It then expanded into the digital twin of our Solar System when STAR was born last year on Lions Gate (8th Aug). Since then it has continued to expand and what we are now building is the digital twin for all of creation..."

---

## Summary

**Avatar/Identity in OASIS:**
- Universal digital identity that works across all platforms
- Holonic architecture enables infinite nesting and shared memory
- Fully interoperable across 50+ providers
- Integrated with NFTs, credentials, and SERV agents
- Karma-based progression system
- Real-world integration through Our World

**Reputation (Karma):**
- Affects discovery, rewards, permissions, and creation rights
- Can be lost through negative actions
- Fully portable across all OASIS products
- Integrated with token economics
- Real-world benefits and rewards

**SERV Agents:**
- Register as OASIS avatars
- Full API access
- Holonic memory for persistence
- Integrated with Our World for characters and services

---

*Last Updated: January 2025*
