# KLEROS DEEP DIVE - HOW IT ACTUALLY WORKS

**Based on official Kleros documentation**

---

## 🎯 KLEROS IS NOT JUST ARBITRATION - IT'S 5 PRODUCTS

Based on the [official docs](https://docs.kleros.io/integrations/overview), Kleros offers:

### 1. Kleros Court - Arbitration-as-a-Service ⚖️

**What it is**: Decentralized dispute resolution via crowdsourced jurors  
**Use cases**: Escrow disputes, NFT authenticity, work quality, DAO governance  
**Integration**: Partner implements `IArbitrable` interface, calls Kleros `IArbitrator` contract

### 2. Kleros Oracle - Truth-as-a-Service 🔮

**What it is**: Answer ANY subjective question with a publicly verifiable answer  
**How it works**: Reality.eth (bonding mechanism) + Kleros (final arbitration)  
**Use cases**: "Who won the election?", "Did this event happen?", "Is this photo authentic?"  
**Integration**: Connect to Reality.eth + Kleros Arbitrator Proxy

### 3. Kleros Curate - Data-Curation-as-a-Service 📊

**What it is**: Community-curated lists with dispute resolution  
**Use cases**: Token lists, verified addresses, content moderation  
**Integration**: Use Kleros Curate API to build/manage lists

### 4. Kleros Escrow - Escrow-as-a-Service 💰

**What it is**: Trustless escrow with built-in dispute resolution  
**Use cases**: Freelancing, sales, service payments  
**Integration**: Use Kleros Escrow contracts or build custom with arbitration

### 5. Kleros Governor - Supreme-Court-as-a-Service 🏛️

**What it is**: Enforce DAO proposal votes via dispute resolution  
**Use cases**: DAO governance disputes, contested proposals  
**Integration**: Integrate with DAO voting systems

---

## 🔧 THE ERC-792 ARBITRATION STANDARD

**Source**: [ERC-792 Documentation](https://docs.kleros.io/developer/arbitration-development/erc-792-arbitration-standard)

### Why Two Contracts?

**Key Innovation**: Separation of ruling (Kleros) from enforcement (Partner)

**Benefits**:
1. **Modularity**: Partners can switch arbitrators without changing their contract
2. **Flexibility**: Kleros doesn't need to know partner's business logic
3. **Reusability**: One arbitrator serves many arbitrable contracts

### IArbitrable Interface (What Partners Implement)

```solidity
// Partner's contract implements this
interface IArbitrable {
    // Kleros calls this function to deliver the ruling
    function rule(uint256 _disputeID, uint256 _ruling) external;
    
    // Event emitted when ruling received
    event Ruling(
        IArbitrator indexed _arbitrator,
        uint256 indexed _disputeID,
        uint256 _ruling
    );
}
```

**Partner's responsibilities**:
- Implement `rule()` function to handle rulings
- Call `arbitrator.createDispute()` when dispute arises
- Execute the ruling (refund, release, etc.)

### IArbitrator Interface (What Kleros Implements)

```solidity
// Kleros contract implements this
interface IArbitrator {
    // Partner calls this to create dispute
    function createDispute(uint256 _choices, bytes calldata _extraData) 
        external 
        payable 
        returns (uint256 disputeID);
    
    // Get arbitration cost
    function arbitrationCost(bytes calldata _extraData) 
        external 
        view 
        returns (uint256 cost);
    
    // Get current ruling
    function currentRuling(uint256 _disputeID) 
        external 
        view 
        returns (uint256 ruling);
    
    // Get dispute status
    function disputeStatus(uint256 _disputeID) 
        external 
        view 
        returns (DisputeStatus status);
    
    // Appeal a ruling
    function appeal(uint256 _disputeID, bytes calldata _extraData) 
        external 
        payable;
    
    // Check appeal period
    function appealPeriod(uint256 _disputeID) 
        external 
        view 
        returns (uint256 start, uint256 end);
}
```

**Kleros's responsibilities**:
- Create disputes when requested
- Manage juror selection (VRF)
- Handle voting and appeals
- Call partner's `rule()` when final

---

## 📝 THE extraData PARAMETER

**Critical for integration**: The `extraData` parameter specifies:

**Structure** (64 bytes total):
- **First 32 bytes**: Subcourt ID (which specialized court?)
- **Next 32 bytes**: Minimum number of jurors (usually 3-5)

**Example**:
```solidity
// Create dispute in "NFT" subcourt with 3 jurors
uint96 subcourtID = 5; // NFT court
uint minJurors = 3;

bytes memory extraData = abi.encodePacked(
    subcourtID,
    minJurors
);

uint fee = arbitrator.arbitrationCost(extraData);
uint disputeID = arbitrator.createDispute{value: fee}(
    2, // number of ruling options (0=refuse, 1=option1, 2=option2)
    extraData
);
```

**Available Subcourts**:
- General Court (ID: 0)
- Blockchain (ID: 1)
- Currency (ID: 2)
- English Language (ID: 3)
- Marketing Services (ID: 4)
- **NFT** (ID: 5) ← Important for marketplaces!
- And more...

---

## 🏗️ INTEGRATION PATTERNS

### Pattern 1: Simple Escrow (Most Common)

**Use case**: Buyer/seller transactions with dispute resolution

**Architecture**:
```
┌──────────────────────────────────────────┐
│         Partner's Escrow Contract         │
│          (Implements IArbitrable)         │
│                                            │
│  • Holds funds                            │
│  • Creates dispute if problem             │
│  • Executes ruling when received          │
└──────────────┬───────────────────────────┘
               │
               ↓ (createDispute call)
┌──────────────────────────────────────────┐
│       Kleros Arbitrator Contract          │
│                                            │
│  • Selects jurors                         │
│  • Manages voting                         │
│  • Calls rule() on escrow                 │
└───────────────────────────────────────────┘
```

**Code Example**:
```solidity
contract SimpleEscrow is IArbitrable {
    address public buyer;
    address public seller;
    uint public amount;
    IArbitrator public arbitrator;
    uint public disputeID;
    
    function payBuyer() external {
        // Buyer deposits funds
        amount = msg.value;
        buyer = msg.sender;
    }
    
    function releaseFunds() external {
        // Seller can release if buyer is happy
        require(msg.sender == buyer || msg.sender == seller);
        payable(seller).transfer(amount);
    }
    
    function raiseDispute() external payable {
        // Either party can create dispute
        require(msg.sender == buyer || msg.sender == seller);
        
        bytes memory extraData = abi.encodePacked(
            uint96(0), // General court
            uint(3)    // 3 jurors
        );
        
        disputeID = arbitrator.createDispute{value: msg.value}(
            2, // 2 ruling options
            extraData
        );
    }
    
    // Kleros calls this when ruling is final
    function rule(uint _disputeID, uint _ruling) external override {
        require(msg.sender == address(arbitrator));
        require(_disputeID == disputeID);
        
        if (_ruling == 1) {
            // Buyer wins - refund
            payable(buyer).transfer(amount);
        } else if (_ruling == 2) {
            // Seller wins - release
            payable(seller).transfer(amount);
        } else {
            // Refuse to arbitrate - split
            payable(buyer).transfer(amount / 2);
            payable(seller).transfer(amount / 2);
        }
    }
}
```

---

### Pattern 2: Kleros Oracle (Subjective Truth)

**Use case**: Get decentralized answer to subjective questions

**Architecture**:
```
Question asker → Reality.eth → Bond escalation → Kleros arbitration
                                                         ↓
                                                   Final answer
```

**How it works** ([source](https://docs.kleros.io/products/oracle)):

1. **Ask question on Reality.eth**:
   - "Who won the 2024 US election?"
   - "Is this NFT authentic?"
   - Post initial bond (e.g., 0.1 ETH)

2. **Someone answers**:
   - "Donald Trump won"
   - Must match the bond (0.1 ETH)
   - Starts countdown (e.g., 24 hours)

3. **Challenge (optional)**:
   - If answer seems wrong, someone can challenge
   - Must DOUBLE the bond (0.2 ETH)
   - Provide different answer
   - Resets countdown

4. **Escalation**:
   - Bond can escalate: 0.1 → 0.2 → 0.4 → 0.8 → 1.6 ETH
   - At any point, anyone can "apply for arbitration"

5. **Kleros arbitration**:
   - Case goes to Kleros Court
   - Jurors review evidence and vote
   - Kleros ruling is FINAL
   - Winner gets all bonds

**Code Example**:
```solidity
// Ask question on Reality.eth with Kleros as arbitrator
bytes32 questionID = realityETH.askQuestion(
    templateID,
    "Who won the 2024 US election?",
    klerosArbitratorProxy, // Kleros as final arbitrator
    timeout,
    openingTimestamp,
    nonce
);

// Submit answer
realityETH.submitAnswer{value: bond}(
    questionID,
    bytes32("Donald Trump"),
    previousAnswer
);

// Challenge answer (double the bond)
realityETH.submitAnswer{value: bond * 2}(
    questionID,
    bytes32("Joe Biden"),
    previousAnswer
);

// Request arbitration (goes to Kleros)
realityETH.requestArbitration{value: arbitrationFee}(
    questionID,
    maxPrevious
);
```

---

### Pattern 3: Kleros Curate (Token Lists, Registries)

**Use case**: Community-curated lists with dispute resolution

**Examples**:
- Token lists (like Uniswap's token list, but decentralized)
- Verified addresses
- Content moderation decisions

**How it works**:
1. Someone submits item to list
2. Pays submission deposit
3. Challenge period (e.g., 3 days)
4. If challenged, goes to Kleros arbitration
5. Jurors decide if item belongs in list
6. Winner gets deposits

---

## 🌐 LIVE INTEGRATIONS

### Real Partners Using Kleros

Based on research and documentation:

1. **Proof of Humanity** - Sybil-resistant registry
   - Status: Live on Ethereum
   - Use: Human verification with dispute resolution

2. **Omen** - Prediction markets
   - Kleros Oracle for market resolution
   - Live on Gnosis Chain

3. **Reality.eth** - Subjective oracle
   - Partnership integration
   - Kleros as final arbitrator

4. **Token Lists** - DeFi token curation
   - Token2CRT on Reality.eth
   - Used by some DEXes

5. **SafeSnap** (Gnosis Safe + Snapshot)
   - DAO governance execution
   - Kleros Reality Module for validation

---

## 💡 KEY INSIGHTS FOR INTEGRATION MANAGER

### Integration is Easier Than You Think

**Minimum viable integration**:
```solidity
// 1. Inherit IArbitrable
contract MyDapp is IArbitrable {
    IArbitrator public arbitrator;
    
    // 2. Create dispute when needed
    function createMyDispute() external payable {
        uint disputeID = arbitrator.createDispute{value: msg.value}(
            2, // ruling options
            "" // extraData
        );
    }
    
    // 3. Handle ruling
    function rule(uint _disputeID, uint _ruling) external override {
        require(msg.sender == address(arbitrator));
        // Execute based on ruling
    }
}
```

**That's it!** 3 parts, ~20 lines of code.

### What Partners Actually Need

**Smart Contract Level**:
1. Implement `IArbitrable` interface
2. Call `createDispute()` when problem occurs
3. Implement `rule()` to execute ruling

**Frontend Level**:
1. Add "File Dispute" button
2. Add evidence upload (to IPFS)
3. Show dispute status
4. Notify when ruling received

**Backend**:
- Optional: Monitor dispute events
- Optional: Send email notifications
- Optional: Cache dispute data

### Integration Tools Kleros Provides

**1. Centralized Arbitrator** ([docs](https://docs.kleros.io/integrations/types-of-integrations/1.-dispute-resolution-integration-plan/integration-tools/centralized-arbitrator))
- For testing before going fully decentralized
- Same interface as Kleros, but centralized backend
- Useful for prototyping

**2. Dispute Resolver** ([docs](https://docs.kleros.io/integrations/types-of-integrations/1.-dispute-resolution-integration-plan/integration-tools/dispute-resolver))
- Helper contract for common dispute patterns
- Pre-built escrow with Kleros integration
- Copy-paste and customize

**3. Arbitrable Proxy**
- Upgradeable arbitration
- Change arbitrator without redeploying main contract

---

## 🎯 THE COMPLETE USER FLOW (Detailed)

### Example: OpenSea NFT Marketplace Integration

**Scenario**: Bob buys NFT from Alice for 1 ETH

#### Phase 1: Setup (One-time, Developer Work)

**OpenSea developers**:
1. Deploy escrow contract implementing `IArbitrable`
2. Set Kleros as arbitrator: `0x988b3a5...` (Ethereum)
3. Add "Dispute" button to frontend
4. Add evidence upload (to Pinata/IPFS)
5. Listen for `Ruling` event

**Time**: 2-3 days development

#### Phase 2: Normal Transaction (Happy Path)

1. Bob clicks "Buy NFT" on OpenSea
2. OpenSea creates escrow contract instance
3. Bob sends 1 ETH to escrow
4. NFT transferred to escrow
5. Seller ships/reveals NFT
6. Bob confirms receipt
7. Escrow releases 1 ETH to Alice
8. NFT transferred to Bob

**Kleros involvement**: ZERO (no dispute)

#### Phase 3: Dispute Arises

**Day 0 - File Dispute**:
```
Bob (on OpenSea): "This NFT is fake!"
  ↓
Clicks "File Dispute" button
  ↓
OpenSea frontend calls:
  escrow.raiseDispute{value: 0.1 ETH}()
  ↓
Escrow contract calls:
  klerosArbitrator.createDispute{value: 0.1 ETH}(2, extraData)
  ↓
Kleros emits:
  DisputeCreation(disputeID: 12345, arbitrable: escrowAddress)
  ↓
OpenSea listens for event, shows:
  "Dispute #12345 created. Status: Evidence Period"
```

**Day 0-7 - Evidence Submission**:
```
Bob (on OpenSea):
  Uploads screenshots → Pinata → gets IPFS hash
  Clicks "Submit Evidence"
  ↓
OpenSea frontend calls:
  metaEvidence.submitEvidence(12345, "ipfs://QmBob...")
  ↓
Emits event that Kleros indexes

Alice (on OpenSea):
  Uploads authenticity proof → IPFS
  Submits evidence same way
  ↓
Both can see each other's evidence on OpenSea UI
```

**Day 7 - Juror Selection** (Automatic):
```
Kleros contract:
  Uses VRF (Chainlink or similar) for randomness
  Selects 3 jurors from "NFT Court" PNK stakers
  Emits JurorDrawn events
  ↓
Jurors see notification on court.kleros.io
OpenSea shows: "Status: Jurors Reviewing"
```

**Days 7-12 - Voting**:
```
Juror1 (on court.kleros.io):
  Clicks case #12345
  Reviews IPFS evidence links
  Casts vote: "1" (Buyer wins)
  Pays gas to submit encrypted vote

Juror2: Votes "1"
Juror3: Votes "2" (Seller wins)
  ↓
After voting deadline, votes revealed
Majority: "1" (Buyer wins)
```

**Day 12 - Ruling Announced**:
```
Kleros contract:
  Tallies votes: 2 for "1", 1 for "2"
  Sets currentRuling(12345) = 1
  Emits AppealPossible event
  Starts appeal period (3 days)
  ↓
OpenSea listens for event, shows:
  "Ruling: Buyer Wins. Appeal period: 3 days"
```

**Days 12-15 - Appeal Period**:
```
Alice can appeal by:
  arbitrator.appeal{value: 0.2 ETH}(12345, extraData)
  ↓
If appealed:
  More jurors drawn (e.g., 5 → 9)
  Process repeats
  ↓
If NOT appealed:
  After 3 days, ruling becomes final
```

**Day 15 - Execution**:
```
Anyone (usually OpenSea bot) calls:
  escrow.executeRuling(12345)
  ↓
Escrow checks:
  ruling = arbitrator.currentRuling(12345) // Returns: 1
  isFinal = arbitrator.disputeStatus(12345) // Returns: Solved
  ↓
Escrow executes:
  if (ruling == 1):
    buyer.transfer(1 ETH) // Refund Bob
    nft.transfer(seller)  // Return NFT to Alice
  ↓
OpenSea shows:
  "Case closed. Refund issued to your wallet."
```

---

## 🔑 KEY TECHNICAL DETAILS

### Ruling Numbers

**Standard across all Kleros disputes**:
- `0` = Refuse to arbitrate (invalid case, can't decide, etc.)
- `1` = First option (usually "Yes", "Buyer wins", "Accept", etc.)
- `2` = Second option (usually "No", "Seller wins", "Reject", etc.)
- `3+` = Additional options if needed

**Partner defines what each number means** when creating dispute.

### Dispute Lifecycle States

```solidity
enum DisputeStatus {
    Waiting,    // Waiting for jurors to be drawn or vote
    Appealable, // Ruling given, appeal period active
    Solved      // Ruling is final (no more appeals)
}
```

### Events to Listen For

**When creating dispute**:
```solidity
event DisputeCreation(
    uint256 indexed _disputeID,
    IArbitrable indexed _arbitrable
);
```

**When ruling can be appealed**:
```solidity
event AppealPossible(
    uint256 indexed _disputeID,
    IArbitrable indexed _arbitrable
);
```

**When ruling is final**:
```solidity
// Partner's IArbitrable emits this when rule() is called
event Ruling(
    IArbitrator indexed _arbitrator,
    uint256 indexed _disputeID,
    uint256 _ruling
);
```

---

## 🎨 REAL INTEGRATION EXAMPLE: Kleros Escrow

**Source**: [Kleros Escrow Tutorial](https://docs.kleros.io/products/escrow/kleros-escrow-tutorial)

**What it is**: Pre-built escrow service with Kleros arbitration

**How users use it**:
1. Visit https://escrow.kleros.io
2. Create escrow transaction
3. Set recipient, amount, timeout
4. Share link with counterparty
5. Counterparty pays
6. Either party can file dispute
7. Automatic execution based on ruling

**Key insight**: This is a **standalone product**, but partners can:
- White-label it
- Embed it in iframe
- Use the smart contracts as reference
- Build their own version

---

## 📊 INTEGRATION PATHS FOR PARTNERS

Based on [Kleros docs](https://docs.kleros.io/integrations/types-of-integrations), partners have options:

### Option 1: Full Smart Contract Integration (Most Control)

**What**: Partner implements IArbitrable in their contracts  
**Pros**: Complete control, white-label, custom UX  
**Cons**: Development required  
**Time**: 1-2 weeks  
**Examples**: Custom marketplaces, custom escrow, DAO governance

### Option 2: Use Kleros Escrow (Fastest)

**What**: Use Kleros's pre-built escrow  
**Pros**: Zero development, immediate use  
**Cons**: Less customization  
**Time**: 1 day (just iframe embedding)  
**Examples**: Freelance platforms, simple sales

### Option 3: Kleros Oracle Integration (For Truth)

**What**: Use Reality.eth + Kleros for subjective questions  
**Pros**: Get truth/data with dispute resolution  
**Cons**: Bonding mechanism adds complexity  
**Time**: 1 week  
**Examples**: Prediction markets, verification systems, content moderation

### Option 4: Kleros Curate (For Lists)

**What**: Use Kleros Curate for token lists, registries  
**Pros**: Community curation + dispute resolution  
**Cons**: Specific to list/registry use cases  
**Time**: 1 week  
**Examples**: DEX token lists, address registries

---

## 💼 FOR INTEGRATION MANAGER: WHICH PARTNERS NEED WHAT?

### NFT Marketplaces (OpenSea, Magic Eden, Blur)
→ **Option 1: Full Integration**
- Need custom escrow for NFT sales
- Implement IArbitrable in marketplace contract
- Create dispute on "fake NFT", "not as described"
- Execute ruling: refund or release

### DeFi Protocols (Uniswap, Aave, Curve)
→ **Option 1: Full Integration** OR **Option 3: Oracle**
- Escrow for OTC trades (Option 1)
- Oracle for price disputes (Option 3)
- Governance disputes (Option 1)

### Freelance/Gig Platforms (Gitcoin, Braintrust)
→ **Option 2: Use Kleros Escrow** (fast) OR **Option 1**
- Quick: Embed Kleros Escrow iframe
- Custom: Build own with IArbitrable

### DAOs (Arbitrum DAO, MakerDAO)
→ **Option 1: Full Integration** with **Kleros Governor**
- Integrate with governance contracts
- Dispute contested proposals
- Enforce execution based on ruling

### Prediction Markets (Polymarket, Omen)
→ **Option 3: Kleros Oracle**
- Use Reality.eth + Kleros
- Resolve market outcomes
- Handle disputes on subjective events

---

## 🎯 CRITICAL INSIGHTS

### 1. The Two-Contract Pattern is Genius

**Why it matters**:
- Partners can switch arbitrators WITHOUT redeploying
- Kleros can upgrade WITHOUT breaking integrations
- One Kleros contract serves 1000s of partners

**Code**:
```solidity
// Partner can change arbitrator anytime
function setArbitrator(address _newArbitrator) external onlyOwner {
    arbitrator = IArbitrator(_newArbitrator);
}
// No redeployment needed!
```

### 2. Evidence is Off-Chain (IPFS)

**Why**:
- Too expensive to store evidence on-chain
- Evidence can be large (PDFs, images, videos)

**How it works**:
- Partners upload to IPFS (Pinata, Infura, etc.)
- Submit IPFS hash on-chain
- Jurors click IPFS link on court.kleros.io
- View evidence off-chain

**Code**:
```solidity
// ERC-1497: Evidence Standard
event Evidence(
    IArbitrator indexed _arbitrator,
    uint indexed _evidenceGroupID,
    address indexed _party,
    string _evidence // IPFS URI: "ipfs://Qm..."
);
```

### 3. Appeals Increase Juror Count

**Mechanism**:
- Round 1: 3 jurors
- If appealed → Round 2: 9 jurors (3x)
- If appealed again → Round 3: 27 jurors (3x)
- Continues until no appeal or max reached

**Why**: More jurors = more expensive to manipulate, higher confidence

### 4. Jurors are Incentivized to be Honest

**Game theory**:
- Jurors who vote with majority: Get PNK + share arbitration fee
- Jurors who vote against majority: Lose PNK stake
- Attacking costs more than honest voting

---

## 🚀 HOW OASIS + ASSETRAIL HELPS

### For Standard Integrations (ERC-792)

**Without OASIS**:
```solidity
// Partner deploys on each chain manually
// Ethereum
IArbitrator ethArbitrator = 0x988b3a5...;
// Polygon  
IArbitrator polyArbitrator = 0x9C1dA9A...;
// Arbitrum
IArbitrator arbArbitrator = 0xArbXXX...;

// Different code for each chain
```

**With OASIS**:
```csharp
// Generate arbitrable contract from template
var scGen = new AssetRailSCGen();
await scGen.GenerateArbitrableContract(
    template: "escrow-with-kleros.sol.hbs",
    chains: ["ethereum", "polygon", "arbitrum"],
    klerosAddresses: config.KlerosAddresses
);

// Deploy to all chains at once
var deployer = new OASISDeployer();
await deployer.DeployToAllChains(artifact);

// Result: Same contract on 3 chains, took 1 day vs 1 week
```

### For Oracle Integrations

**Without OASIS**:
- Manually integrate with Reality.eth on each chain
- Different configurations per chain
- Separate monitoring

**With OASIS**:
- Unified API to query oracles across chains
- Auto-route to cheapest chain for question
- Aggregate answers from multiple chains

### For Monitoring

**Without OASIS**:
```javascript
// Monitor Ethereum
const ethProvider = new ethers.providers.JsonRpcProvider(ETH_RPC);
const ethDisputes = await ethArbitrator.queryFilter('DisputeCreation');

// Monitor Polygon
const polyProvider = new ethers.providers.JsonRpcProvider(POLY_RPC);
const polyDisputes = await polyArbitrator.queryFilter('DisputeCreation');

// Separate dashboards, separate code
```

**With OASIS**:
```csharp
// Monitor all chains at once
var monitor = new KlerosMultiChainMonitor();
var allDisputes = await monitor.GetDisputesAcrossAllChains();

// Returns unified dataset from Ethereum, Polygon, Arbitrum, etc.
// One dashboard, one codebase
```

---

## 📋 INTEGRATION CHECKLIST

### For Partners Integrating Kleros

**Smart Contract** ☑️:
- [ ] Implement `IArbitrable` interface
- [ ] Import Kleros arbitrator address for your chain
- [ ] Add `createDispute()` call in relevant function
- [ ] Implement `rule()` function to execute rulings
- [ ] Handle all ruling options (0, 1, 2, etc.)
- [ ] Test on testnet first

**Frontend** ☑️:
- [ ] Add "File Dispute" button/link
- [ ] Add evidence upload (to IPFS)
- [ ] Display dispute status
- [ ] Show evidence submitted
- [ ] Notify when ruling received
- [ ] Explain what ruling means

**Documentation** ☑️:
- [ ] Explain to users when they can dispute
- [ ] Clarify arbitration costs
- [ ] Set expectations (timeline: ~2 weeks)
- [ ] Link to Kleros Court for transparency

**Optional** ☑️:
- [ ] Email notifications for dispute events
- [ ] Dashboard for dispute history
- [ ] Appeal functionality (if applicable)
- [ ] Multi-language support

---

## 🎯 FOR YOUR KLEROS PITCH

### Updated Value Proposition

**You're not just bringing integration skills** - you're bringing:

1. **Product Understanding**:
   - Know all 5 Kleros products (Court, Oracle, Curate, Escrow, Governor)
   - Understand ERC-792 standard deeply
   - Can recommend right product for each partner

2. **Integration Methodology**:
   - Simple escrow: Use Kleros Escrow (1 day)
   - Custom marketplace: Full ERC-792 integration (1-2 weeks)
   - DAO governance: Kleros Governor (1 week)
   - Oracle needs: Reality.eth + Kleros (1 week)

3. **Technical Infrastructure**:
   - AssetRail SC-Gen: Generate IArbitrable contracts from templates
   - OASIS: Deploy to 15+ chains simultaneously
   - Monitoring: Track all disputes across all chains

4. **Partner-Specific Recommendations**:
   - **OpenSea**: Custom ERC-792 integration for NFT disputes (highest control)
   - **Uniswap**: Kleros Escrow for OTC + Oracle for pricing disputes
   - **Gitcoin**: Kleros Escrow (fastest path to market)
   - **Magic Eden** (Solana): Port Kleros contracts via AssetRail + OASIS
   - **Arbitrum DAO**: Kleros Governor integration

---

## 📚 UPDATED POC DELIVERABLES

### What to Add to Frontend

**New tab idea**: "Integration Patterns"

Show:
1. **Pattern 1**: Simple Escrow (code example)
2. **Pattern 2**: NFT Marketplace (code example)
3. **Pattern 3**: Oracle Integration (code example)
4. **Pattern 4**: DAO Governance (code example)

Each with:
- When to use
- Code snippet
- Timeline estimate
- Cost estimate

### Updated Documentation

Create:
- `KLEROS_INTEGRATION_PATTERNS.md` - Deep dive on all 5 products
- Update user journey with ERC-792 details
- Add subcourt selection guide
- Add evidence submission best practices

---

## 🏆 THE COMPLETE PICTURE

### How Kleros Actually Works (Summary)

**ERC-792 Standard**:
- Two contracts: IArbitrable (partner) + IArbitrator (Kleros)
- Partner calls `createDispute()`, Kleros calls `rule()`
- Clean separation: ruling vs enforcement

**Five Products**:
- Court (arbitration), Oracle (truth), Curate (lists), Escrow (transactions), Governor (DAOs)

**Three Interfaces**:
- Partner dApp (where users are)
- court.kleros.io (where jurors are)  
- Smart contracts (where execution happens)

**Timeline**:
- Evidence: 3-7 days
- Voting: 3-5 days
- Appeal: 3 days
- **Total**: ~2 weeks

**Integration Time**:
- Use Kleros Escrow: 1 day (iframe)
- Custom ERC-792: 1-2 weeks
- Oracle integration: 1 week

---

## 💡 WHY THIS MATTERS FOR YOUR ROLE

### As Integration Manager, You Need to Know

**For Each Prospect**:
1. **Which product** fits their need? (Court vs Oracle vs Curate vs Escrow vs Governor)
2. **Which integration pattern**? (Full custom vs pre-built)
3. **Which subcourt**? (NFT, Blockchain, Marketing, etc.)
4. **What timeline**? (1 day vs 1-2 weeks)
5. **What cost**? (Development + arbitration fees)

### Example Pitch (OpenSea)

> "OpenSea, you're spending $X million on customer support for disputes. Here's how Kleros works:
>
> **Technical**: Implement IArbitrable in your marketplace contract. Takes 1-2 weeks, ~500 lines of code. We'll help.
>
> **UX**: Add 'File Dispute' button. Users never leave OpenSea - they file, upload proof, get ruling all on your site.
>
> **Process**: Decentralized jurors vote in ~2 weeks. Your contract auto-executes ruling (refund or release).
>
> **Cost**: $50-100 per dispute (vs $500+ for traditional arbitration). Users pay, not you.
>
> **Differentiation**: 'Guaranteed Fair Disputes' becomes your marketing advantage. No other marketplace has this.
>
> Let me show you our OASIS integration - we can deploy this on Polygon for you in 2 days, saving gas costs vs Ethereum."

---

**This deep understanding is what makes you the perfect Integration Manager.**

Sources:
- [Kleros Documentation](https://docs.kleros.io)
- [ERC-792 Standard](https://docs.kleros.io/developer/arbitration-development/erc-792-arbitration-standard)
- [Smart Contract Integration](https://docs.kleros.io/integrations/types-of-integrations/1.-dispute-resolution-integration-plan/smart-contract-integration)
- [Kleros Oracle](https://docs.kleros.io/products/oracle)
- [Integration Overview](https://docs.kleros.io/integrations/overview)




