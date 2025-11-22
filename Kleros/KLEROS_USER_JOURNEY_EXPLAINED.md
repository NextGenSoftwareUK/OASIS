# KLEROS USER JOURNEY - WHERE THE PROCESS ACTUALLY OCCURS

**Understanding the three interfaces in Kleros dispute resolution**

---

## 🎯 THE KEY INSIGHT

**Question**: Where does the Kleros process actually occur for users?

**Answer**: **Three separate places**, but users only see ONE of them:

1. **Partner dApp** (e.g., OpenSea, Uniswap) - Where USERS interact
2. **court.kleros.io** - Where JURORS interact  
3. **Smart Contracts** (on-chain) - Where EXECUTION happens

**Critical Point**: **End users never visit court.kleros.io** - they stay on the partner dApp (like OpenSea) the entire time!

---

## 👥 THREE DIFFERENT USER TYPES

### 1. Dispute Parties (Buyer/Seller)

**Where they are**: Partner dApp (OpenSea, Uniswap, etc.)  
**What they do**:
- File dispute through partner's UI
- Upload evidence through partner's UI
- Receive ruling notification on partner's platform
- See automatic execution (refund/release)

**What they DON'T do**:
- ❌ Never visit court.kleros.io
- ❌ Never interact with Kleros directly
- ❌ Never see jurors or voting

**Their experience**: "I had a problem on OpenSea, clicked 'Dispute', uploaded proof, got my refund 2 weeks later. Felt seamless."

### 2. Jurors (PNK Stakers)

**Where they are**: court.kleros.io  
**What they do**:
- Stake PNK to become eligible
- Get randomly selected for cases
- Review evidence
- Cast votes
- Earn rewards for coherent votes

**What they DON'T do**:
- ❌ Never see the partner dApp's dispute UI
- ❌ Don't interact with disputants directly
- ❌ Just vote based on evidence

**Their experience**: "I staked PNK, got selected for an NFT dispute, reviewed evidence, voted 'Buyer wins', earned 0.05 ETH + 50 PNK."

### 3. Smart Contracts (Automatic)

**Where**: On-chain (Ethereum, Polygon, etc.)  
**What**: Kleros arbitrator contract + Partner's escrow contract  
**How**: Automatic execution based on ruling

---

## 🗺️ THE COMPLETE 7-STEP JOURNEY

### Step 1: Problem Occurs ⚠️
**Location**: Partner dApp (OpenSea)  
**Actor**: User (Buyer)

**Example**:
- Bob buys an NFT from Alice for 1 ETH on OpenSea
- NFT metadata doesn't match listing
- Bob feels scammed

**Interface**: OpenSea's product page

---

### Step 2: Create Dispute 🔨
**Location**: Partner dApp UI → Kleros Smart Contract  
**Actor**: User clicks button

**What happens**:
1. Bob clicks "Dispute Transaction" on OpenSea
2. OpenSea's frontend calls Kleros contract
3. Transaction sent: `createDispute(3, "ipfs://...")`
4. Bob pays arbitration fee (0.1 ETH)
5. Dispute ID returned: #12345

**Code** (Partner's frontend):
```typescript
// OpenSea's code
const tx = await klerosArbitrator.createDispute(
  3, // jurors
  ipfsHash, // evidence URI
  { value: ethers.utils.parseEther("0.1") }
);
await tx.wait();
// Dispute ID: 12345
```

**User sees**: "Dispute #12345 created. Arbitration fee: 0.1 ETH. Evidence period: 7 days."

---

### Step 3: Evidence Submission 📄
**Location**: Partner dApp or Kleros Interface  
**Actor**: Both parties (+ anyone)

**What happens**:
- Evidence period: 7 days
- Alice uploads: Original NFT listing, creation proof
- Bob uploads: Screenshots showing mismatch, expert analysis
- Evidence stored on IPFS, hashes submitted on-chain

**Code**:
```typescript
// Alice submits evidence via OpenSea
await klerosArbitrator.submitEvidence(
  12345,
  "ipfs://QmAlicesProof..."
);

// Bob submits evidence
await klerosArbitrator.submitEvidence(
  12345,
  "ipfs://QmBobsScreenshots..."
);
```

**User sees**: "Evidence submitted. Waiting for juror selection."

---

### Step 4: Juror Selection 🎲
**Location**: Kleros Smart Contract (Automatic)  
**Actor**: Kleros Protocol (VRF - Verifiable Random Function)

**What happens**:
- System randomly selects 3 jurors from "NFT Court" PNK stakers
- Uses cryptographic randomness (VRF)
- Jurors get notification: "You've been selected for case #12345"

**User sees**: "Jurors selected. Voting period started."

**Jurors see** (on court.kleros.io): "New case assigned: NFT Authenticity Dispute #12345"

---

### Step 5: Jurors Vote 🗳️
**Location**: court.kleros.io (Kleros Court dApp)  
**Actor**: Selected Jurors

**What happens**:
- Jurors visit court.kleros.io
- Review all submitted evidence
- Vote: Option 0 (refuse), 1 (buyer wins), or 2 (seller wins)
- Voting period: 3-5 days
- Votes are encrypted until reveal phase

**Juror interface**:
```
Case #12345: NFT Authenticity Dispute
Court: NFT Court
Evidence:
  - Alice's proof (IPFS link)
  - Bob's screenshots (IPFS link)
  - Community report (IPFS link)

Your vote:
[ ] Refuse to arbitrate
[•] Buyer wins (refund)
[ ] Seller wins (release payment)

Submit Vote (Costs gas)
```

**User sees** (on OpenSea): "Jurors are reviewing your case. Expected ruling: 3-5 days."

---

### Step 6: Ruling Announced ⚖️
**Location**: Kleros Smart Contract  
**Actor**: Kleros Protocol

**What happens**:
- Voting period ends
- Votes revealed
- Majority wins: 2 jurors voted "Buyer wins", 1 voted "Seller wins"
- Ruling: Option 1 (Buyer wins)
- Appeal period starts: 3 days
- Appeal cost: 0.2 ETH (higher than initial fee)

**Code**:
```typescript
// Anyone can check ruling
const ruling = await klerosArbitrator.currentRuling(12345);
// Returns: 1 (Buyer wins)

const isAppealable = await checkAppealPeriod(12345);
// Returns: true (still in appeal window)
```

**User sees** (on OpenSea): 
- "Ruling: Buyer wins (refund). Appeal period: 3 days remaining."
- Alice can appeal by paying 0.2 ETH

---

### Step 7: Execute Ruling ✅
**Location**: Partner's Smart Contract  
**Actor**: Anyone (usually automated)

**What happens**:
- Appeal period expires (or no appeal filed)
- Ruling becomes final
- OpenSea's escrow contract executes ruling
- If "Buyer wins": Refund 1 ETH to Bob, return NFT to Alice
- If "Seller wins": Release 1 ETH to Alice, transfer NFT to Bob

**Code** (OpenSea's escrow):
```solidity
function executeRuling(uint256 disputeID) public {
  uint ruling = arbitrator.currentRuling(disputeID);
  require(arbitrator.disputeStatus(disputeID) == Resolved);
  
  if (ruling == 1) {
    // Buyer wins - refund
    payable(buyer).transfer(amount);
    nft.transferFrom(address(this), seller, tokenId);
  } else if (ruling == 2) {
    // Seller wins - release
    payable(seller).transfer(amount);
    nft.transferFrom(address(this), buyer, tokenId);
  }
  
  emit RulingExecuted(disputeID, ruling);
}
```

**User sees** (on OpenSea): "Ruling executed. 1 ETH refunded to your wallet. Case closed."

---

## 📍 WHERE DOES EACH ACTOR GO?

### End Users (Buyer/Seller)

**Interface**: **Partner dApp only** (e.g., OpenSea)

**Never leave the partner platform**:
- ✅ File dispute: OpenSea button
- ✅ Upload evidence: OpenSea interface
- ✅ Check status: OpenSea dashboard
- ✅ See ruling: OpenSea notification
- ✅ Get refund: Automatic (wallet notification)

**Touchpoints with Kleros**: **ZERO** (all happens behind the scenes)

---

### Jurors

**Interface**: **court.kleros.io** (Kleros Court dApp)

**What they see**:
- Dashboard: Staked PNK, cases assigned, earnings
- Case details: Evidence, dispute description, voting options
- Vote interface: Cast vote, see other jurors' votes (after reveal)
- Rewards: PNK redistributed, ETH earned

**Touchpoints with Partner dApp**: **ZERO** (they never see OpenSea's UI)

---

### Developers (Integration)

**Interfaces**: **Partner dApp code + Kleros docs**

**What they do**:
1. Read https://docs.kleros.io/integrations
2. Install SDK or use Ethers.js
3. Add "File Dispute" button to their UI
4. Call Kleros contract when button clicked
5. Display dispute status in their UI
6. Execute ruling in their escrow contract

**Touchpoints**: Code integration only

---

## 🎨 VISUAL FLOW

```
┌─────────────────────────────────────────────────────────────┐
│                    PARTNER dAPP (OpenSea)                    │
│                                                               │
│  User Journey (Buyer/Seller):                                │
│  ┌──────────┐   ┌──────────┐   ┌──────────┐   ┌──────────┐ │
│  │  Browse  │→│  Problem  │→│  Click    │→│  Upload  │ │
│  │   NFT    │   │  Occurs  │   │ "Dispute"│   │ Evidence │ │
│  └──────────┘   └──────────┘   └──────────┘   └──────────┘ │
│                                      ↓                        │
│                          (Transaction to Kleros)              │
└───────────────────────────┬───────────────────────────────────┘
                            │
                            ↓
        ┌───────────────────────────────────┐
        │    KLEROS SMART CONTRACT          │
        │    (On Ethereum/Polygon/etc.)     │
        │                                    │
        │  • Create dispute                 │
        │  • Store evidence hashes          │
        │  • Select jurors (VRF)            │
        │  • Tally votes                    │
        │  • Announce ruling                │
        └───────────────────────────────────┘
                            │
            ┌───────────────┴──────────────┐
            │                              │
            ↓                              ↓
┌─────────────────────┐        ┌────────────────────────┐
│   court.kleros.io   │        │  Partner Smart Contract│
│                     │        │        (Escrow)        │
│  Juror Interface:   │        │                        │
│  ┌──────────────┐  │        │  • Receives ruling     │
│  │ Review       │  │        │  • Executes action     │
│  │ Evidence     │  │        │  • Refund or release   │
│  └──────────────┘  │        │  • Transfer NFT        │
│  ┌──────────────┐  │        └────────────────────────┘
│  │ Cast Vote    │  │                    │
│  └──────────────┘  │                    │
│  ┌──────────────┐  │                    ↓
│  │ Earn Rewards │  │        ┌────────────────────────┐
│  └──────────────┘  │        │   USER'S WALLET        │
└─────────────────────┘        │                        │
                               │  Funds received ✅     │
                               │  (automatic)           │
                               └────────────────────────┘
```

---

## ⏱️ TYPICAL TIMELINE

| Day | Event | Location | Actor |
|-----|-------|----------|-------|
| **Day 0** | Dispute created | OpenSea → Kleros contract | Buyer clicks button |
| **Days 0-7** | Evidence submission | OpenSea UI + IPFS | Both parties upload |
| **Day 7** | Jurors selected | Kleros contract (VRF) | Automatic |
| **Days 7-12** | Voting period | court.kleros.io | Jurors vote |
| **Day 12** | Ruling announced | Kleros contract | Automatic |
| **Days 12-15** | Appeal period | court.kleros.io (optional) | Either party |
| **Day 15** | Ruling executed | OpenSea escrow contract | Automatic |
| **Total** | **~2 weeks** | Multiple interfaces | Multiple actors |

---

## 💡 WHY THIS MATTERS FOR INTEGRATION MANAGER ROLE

### Understanding the UX is Critical

When pitching to partners, you need to explain:

**To Business Stakeholders**:
> "Your users never leave your platform. They file disputes on YOUR site, get results on YOUR site. Kleros just handles the arbitration logic in the background. It's invisible to them."

**To Product Managers**:
> "You add a 'File Dispute' button and a status dashboard. That's it. The rest happens automatically. Your UX stays yours."

**To Developers**:
> "Three function calls: createDispute(), submitEvidence(), executeRuling(). Standard Ethers.js. Takes 2-3 hours to integrate."

**To End Users** (if asked):
> "Fair, decentralized dispute resolution. File your case, upload proof, get a ruling in ~2 weeks. All handled on [Partner Platform], no extra accounts needed."

---

## 📊 THE THREE INTERFACES EXPLAINED

### Interface 1: Partner dApp (Where Users Are)

**Examples**: OpenSea, Uniswap, Magic Eden, Gitcoin

**UI Elements Partner Adds**:
1. **"File Dispute" button** - Triggers `createDispute()` call
2. **Evidence upload form** - Uploads to IPFS, calls `submitEvidence()`
3. **Dispute status widget** - Shows "Evidence Period", "Voting", "Ruling Announced"
4. **Ruling notification** - "You won!" or "You lost. Appeal?"
5. **Automatic execution** - "Funds refunded" or "Payment released"

**User Flow on OpenSea**:
```
Browse NFT → Buy → Problem → "Dispute" button → Upload screenshots →
  ↓
Wait (notification: "Jurors reviewing") →
  ↓
Ruling: "Buyer wins" → Funds automatically refunded → Done
```

**Key**: User never leaves OpenSea!

---

### Interface 2: Kleros Court (Where Jurors Are)

**URL**: https://court.kleros.io

**UI Elements**:
1. **Dashboard**: Staked PNK, assigned cases, earnings
2. **Case list**: "Vote Pending", "Active", "Closed"
3. **Case details**: Evidence links, dispute description, court type
4. **Voting interface**: Radio buttons (Refuse / Option 1 / Option 2), Submit button
5. **Rewards tracker**: Coherent votes %, PNK earned, ETH earned

**Juror Flow**:
```
Stake PNK → Get selected → See notification → Click case →
  ↓
Review evidence (IPFS links) → Cast vote → Wait for reveal →
  ↓
See result (coherent? incoherent?) → Receive rewards → Done
```

**Key**: Jurors never see OpenSea, never interact with Bob or Alice directly!

---

### Interface 3: Smart Contracts (Where Execution Happens)

**Components**:
1. **Kleros Arbitrator** (`0x988b3a5...` on Ethereum)
   - Stores disputes
   - Manages juror selection
   - Tallies votes
   - Announces rulings

2. **Partner's Escrow** (OpenSea's contract)
   - Holds funds during transaction
   - Queries Kleros for ruling
   - Executes based on ruling
   - Transfers NFT + funds

**Execution Flow**:
```solidity
// Automatic on OpenSea's contract
function checkAndExecute(uint256 disputeID) {
  // Query Kleros
  uint ruling = klerosArbitrator.currentRuling(disputeID);
  bool isFinal = !klerosArbitrator.appealPeriodActive(disputeID);
  
  if (isFinal) {
    if (ruling == 1) {
      // Buyer wins
      buyer.transfer(1 ether);
      nft.transferFrom(escrow, seller, tokenId);
    } else if (ruling == 2) {
      // Seller wins
      seller.transfer(1 ether);
      nft.transferFrom(escrow, buyer, tokenId);
    }
  }
}
```

**Key**: All automatic, no manual intervention!

---

## 🎯 FOR THE INTEGRATION MANAGER ROLE

### When Proposing to Partners

**Address Their Concerns**:

**Partner asks**: "Will our users have to learn a new platform?"  
**You answer**: "No! They never leave your site. You add a button, we handle arbitration behind the scenes."

**Partner asks**: "Where do disputes happen?"  
**You answer**: "On YOUR platform. Users file disputes on YOUR UI, see results on YOUR dashboard. Kleros is invisible to them."

**Partner asks**: "What about jurors?"  
**You answer**: "Completely separate. Jurors are on court.kleros.io. Your users never interact with them. Think of it like eBay + PayPal disputes - users stay on eBay, PayPal handles the process."

**Partner asks**: "How complex is the integration?"  
**You answer**: "3 function calls, ~2-3 hours. I can show you a live demo right now."

### Demo Script (Using Frontend POC)

**Show User Journey Tab**:
1. Click through steps 1-7
2. Point out three different locations
3. Emphasize: "Users stay on YOUR platform"
4. Show code examples (simple Ethers.js)
5. Show timeline (2 weeks end-to-end)

**Key Talking Point**:
> "Kleros is like Stripe for payments - your users never visit Stripe.com, but you use Stripe's backend. Same here: users never visit court.kleros.io, but you use Kleros's arbitration backend."

---

## 📱 EXAMPLE: OpenSea NFT Dispute (Real User Experience)

### Bob's Experience (End User)

**Day 0**: 
- Buys NFT on OpenSea
- Realizes it's fake
- Clicks "Dispute Transaction" on OpenSea
- Pays 0.1 ETH arbitration fee
- Uploads screenshots as evidence

**Days 1-7**:
- Checks OpenSea dashboard: "Evidence Period (5 days left)"
- Uploads more proof
- Waits

**Days 7-12**:
- OpenSea shows: "Jurors Reviewing (3 days left)"
- Bob doesn't do anything, just waits

**Day 12**:
- OpenSea notification: "Ruling Announced: You Won!"
- Shows: "Refund: 1 ETH. Appeal period: 3 days."

**Day 15**:
- OpenSea notification: "Case Closed. Ruling Executed."
- Wallet notification: "Received 1 ETH"
- NFT returned to seller automatically

**Total OpenSea visits**: 3-4 times  
**court.kleros.io visits**: ZERO  
**Smart contract interactions**: ZERO (OpenSea handles it)

---

### Juror's Experience (PNK Staker)

**Pre-Day 0**:
- Staked 10,000 PNK in "NFT Court" on court.kleros.io

**Day 7**:
- Notification on court.kleros.io: "You've been drawn for case #12345"
- Clicks case, reviews evidence (IPFS links)
- Reads Alice's listing proof, Bob's screenshots

**Day 10**:
- Casts vote: "Buyer wins (Option 1)"
- Pays gas fee to submit vote

**Day 12**:
- Votes revealed: 2 voted "Buyer wins", 1 voted "Seller wins"
- Juror was coherent (voted with majority)
- Rewards: 50 PNK + 0.033 ETH (share of arbitration fee)

**Total OpenSea visits**: ZERO  
**court.kleros.io visits**: 2-3 times  
**Knowledge of Bob/Alice**: ZERO (just saw case number + evidence)

---

## 🔑 KEY TAKEAWAYS

### For Partner Integration

1. **Seamless UX**: Users never leave partner platform
2. **White-label**: Partner controls the UI completely
3. **Simple Integration**: 3 function calls, standard Web3
4. **No training needed**: Users just click buttons

### For End Users

1. **One platform**: Stay on OpenSea (or Uniswap, Magic Eden, etc.)
2. **Familiar flow**: Like eBay disputes or PayPal chargebacks
3. **Transparent**: Can see evidence, ruling, timeline
4. **Automatic**: Execution happens without manual steps

### For Jurors

1. **Separate platform**: court.kleros.io
2. **Earn money**: PNK + ETH rewards for coherent votes
3. **No bias**: Don't know disputants' identities
4. **Fair process**: Verifiable randomness, encrypted votes

---

## 💼 FOR YOUR KLEROS PITCH

### The Analogy (Use This!)

**Stripe Payments**:
- Merchants integrate Stripe (backend)
- Users checkout on merchant's site (frontend)
- Users never visit Stripe.com
- Stripe handles processing invisibly

**Kleros Arbitration**:
- Partners integrate Kleros (backend)
- Users dispute on partner's site (frontend)
- Users never visit court.kleros.io
- Kleros handles arbitration invisibly

### The Demo (Show User Journey Tab)

**Walk through**:
1. "Here's where the user is - on OpenSea"
2. "They click dispute, upload proof - all on OpenSea"
3. "Meanwhile, jurors vote on court.kleros.io - separate platform"
4. "Ruling comes back, OpenSea executes it automatically"
5. "User gets refund, never left OpenSea"

**Emphasize**:
- Three separate interfaces
- Seamless user experience
- Simple integration
- Powerful arbitration

---

## 🚀 INTEGRATION MANAGER'S ROLE

### Understanding This Journey Helps You

**Identify Targets**:
- Look for platforms with escrow (funds need dispute resolution)
- Look for quality issues (NFTs, deliverables, content)
- Look for subjective conflicts (smart contracts can't handle)

**Propose Solutions**:
- Show how Kleros fits into THEIR UX
- Emphasize users stay on THEIR platform
- Demonstrate simple integration
- Highlight automatic execution

**Close Deals**:
- Address UX concerns (seamless)
- Address technical concerns (simple)
- Address cost concerns (cheap vs traditional arbitration)
- Address trust concerns (transparent, decentralized)

---

**This understanding is what separates a good Integration Manager from a great one.**

You're not just selling arbitration - you're selling **invisible, seamless, automatic dispute resolution** that fits into any platform's existing UX.

---

**See the interactive demo**: `User Journey` tab in kleros-frontend-poc




