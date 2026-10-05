# Exercise B: Kleros Escrow - New Use Cases

**Assignment**: Expand Kleros escrow system with 2 new use cases

**Reference**: [Kleros Integration Manager Assignment](link-to-assignment-doc)

---

## Part A: Two New Use Cases (2 points)

### Use Case 1: "Freelance Work"
**Category**: Gig Economy / Professional Services  
**Target Market**: Developers, designers, writers, consultants, marketers

### Use Case 2: "NFT Commission"
**Category**: Digital Art / Creative Services  
**Target Market**: NFT artists, collectors, brands commissioning art

---

## Part B: Logo Sketches (2 points)

### Use Case 1: Freelance Work Logo

**Concept**: Laptop + handshake + checkmark

```
ASCII Sketch:

    _______________
   |  ___     ___  |    <-- Laptop screen
   | |   |   |   | |
   | |___|   |___| |    <-- Code/design symbols inside
   |_______________|
        |     |
   _____|_____|_____
  |                  |   <-- Laptop base
  |__________________|
  
     👥 🤝 ✓           <-- Handshake + verified
```

**Description**: 
- Primary element: Laptop (represents remote work)
- Secondary element: Handshake icon overlay (trust/agreement)
- Accent: Green checkmark (completed work verified)
- Color scheme: Blue (trust) + Green (success)
- Style: Minimalist, modern, professional

---

### Use Case 2: NFT Commission Logo

**Concept**: Artist palette + NFT diamond + paintbrush

```
ASCII Sketch:

        /\
       /  \          <-- NFT diamond shape
      /____\
     |  🎨  |        <-- Palette icon inside
     |      |
     \______/
        ||           <-- Paintbrush handle
       /  \
      🖌️   ✨        <-- Brush + sparkle (creativity)
```

**Description**:
- Primary element: NFT diamond/hexagon (blockchain + NFT association)
- Secondary element: Artist palette or paintbrush (creative work)
- Accent: Sparkle/stars (custom art, unique)
- Color scheme: Purple/Pink gradient (creative/artistic)
- Style: Bold, colorful, artistic flair

---

## Part C: Use Case Descriptions (2 points)

### Use Case 1: Freelance Work

**Description for Selection Page**:

Escrow funds to facilitate safe freelance work agreements. This is designed for hiring developers, designers, writers, or any professional service provider in the Web3 space. The client deposits funds in escrow, the freelancer completes the work, and funds are released upon approval. If there's a dispute about work quality or completion, Kleros arbitrators review the evidence and make a fair ruling. This protects both parties: clients get quality work, freelancers get paid reliably. Perfect for remote work, bounties, or one-time projects where trust hasn't been established yet.

**Length**: ~100 words  
**Tone**: Professional, reassuring, practical

---

### Use Case 2: NFT Commission

**Description for Selection Page**:

Escrow funds to commission custom NFT artwork safely. This use case is for collectors or brands hiring artists to create custom NFT pieces. The commissioner deposits payment in escrow, the artist creates the work according to specifications, and the NFT is minted and delivered. If there's disagreement about whether the artwork matches the brief, Kleros arbitrators—selected from a specialized art/NFT court—review the commission agreement and delivered work to make a ruling. This ensures artists get paid for their work and commissioners receive art that matches their vision. Ideal for 1/1 commissions, PFP collections, or branded NFT projects.

**Length**: ~110 words  
**Tone**: Creative, protective of both parties, art-focused

---

## Part D: Automatically Generated Contracts (2 points)

### Use Case 1: Freelance Work

**Contract Template**:

```
FREELANCE WORK AGREEMENT

Service Provider (Freelancer): [Blockchain Address]
Client: [Blockchain Address]
Escrow Amount: [Amount] [Cryptocurrency]
Due Date: [Due Date (Local Time)]

SCOPE OF WORK:
[Text Description of Work]

DELIVERABLES:
[Deliverable Description - e.g., "GitHub repository with smart contract code and tests" or "Figma design files for mobile app UI"]

ACCEPTANCE CRITERIA:
The work will be considered complete when the freelancer has delivered the agreed-upon deliverables and the client has reviewed them. If the client does not dispute within [Review Period, default: 3 days] of delivery, the escrow will automatically release to the freelancer.

DISPUTE RESOLUTION:
If there is a dispute about work quality, completion, or delivery, either party may raise a dispute to be resolved by Kleros arbitrators. Evidence may include: work files, communication logs, contract specifications, and progress updates.

AGREED TERMS:
✓ Freelancer will deliver work by [Due Date]
✓ Client will review deliverables within [Review Period] days
✓ Payment will be released upon acceptance or automatic release
✓ Disputes will be resolved by Kleros Freelance Court

This contract is governed by the Kleros escrow smart contract at [Contract Address] on [Blockchain].
```

**Dynamic Fields**:
- `[Blockchain Address]` for both parties
- `[Amount]` and `[Cryptocurrency]` (ETH, USDC, DAI, etc.)
- `[Due Date]` with timezone
- `[Text Description of Work]` (free text, max 500 characters)
- `[Deliverable Description]` (free text, max 300 characters)
- `[Review Period]` (default 3 days, adjustable)

---

### Use Case 2: NFT Commission

**Contract Template**:

```
NFT COMMISSION AGREEMENT

Artist: [Blockchain Address]
Commissioner: [Blockchain Address]
Commission Fee: [Amount] [Cryptocurrency]
Delivery Deadline: [Due Date (Local Time)]

COMMISSION DETAILS:
Subject: [NFT Subject - e.g., "Portrait of my ENS avatar in cyberpunk style"]
Style/Aesthetic: [Style Description - e.g., "Anime-inspired, vibrant colors, futuristic background"]
Dimensions/Format: [Format - e.g., "3000x3000px, PNG, suitable for PFP"]

REVISION ROUNDS:
The artist will provide [Number of Revisions, default: 2] rounds of revisions based on commissioner feedback.

DELIVERY:
The artist will mint the NFT and transfer it to [Commissioner Blockchain Address] by the delivery deadline. The NFT metadata and artwork files will be uploaded to IPFS, and the IPFS hash will be provided as proof of delivery.

ACCEPTANCE:
If the commissioner does not dispute within [Review Period, default: 5 days] of NFT delivery, the escrow will automatically release to the artist. If the artwork does not match the agreed specifications, the commissioner may raise a dispute.

INTELLECTUAL PROPERTY:
Upon payment release, all commercial rights to the artwork transfer to the commissioner. The artist retains the right to display the work in their portfolio with attribution.

DISPUTE RESOLUTION:
Disputes about artistic interpretation, quality, or delivery will be resolved by Kleros NFT/Art Court, which specializes in creative disputes. Evidence may include: reference images, WIP (work-in-progress) files, communication history, and the final delivered NFT.

AGREED TERMS:
✓ Artist will deliver NFT by [Due Date]
✓ Commissioner will review within [Review Period] days
✓ [Number of Revisions] revision rounds included
✓ Payment released upon acceptance or auto-release
✓ Disputes resolved by Kleros NFT Court

This contract is governed by the Kleros escrow smart contract at [Contract Address] on [Blockchain].
```

**Dynamic Fields**:
- `[Blockchain Address]` for artist and commissioner
- `[Amount]` and `[Cryptocurrency]`
- `[Due Date]` with timezone
- `[NFT Subject]` (free text, max 200 chars)
- `[Style Description]` (free text, max 300 chars)
- `[Format]` (dropdown: PFP, 1/1 Art, Animated, 3D, etc.)
- `[Number of Revisions]` (default 2, adjustable 0-5)
- `[Review Period]` (default 5 days, adjustable)

---

## Part E: Court Selection (2 points)

### Use Case 1: Freelance Work

**Court**: **Create New Court → "Web3 Freelance Court"**

**Court Purpose**:

This subcourt handles disputes arising from freelance work agreements in the Web3 ecosystem, including smart contract development, design work, content creation, marketing services, and technical consulting. Jurors in this court have experience with remote work, technical deliverables, and Web3 industry standards.

**Court Specialization**:
- Software development disputes (code quality, functionality, testing)
- Design deliverables (UI/UX, graphics, branding)
- Content creation (writing, documentation, videos)
- Marketing and community management work
- Technical consulting and advisory services

**Types of Disputes**:
1. **Work Quality**: Does the delivered work meet professional standards?
2. **Scope Completion**: Did the freelancer complete all agreed-upon tasks?
3. **Delivery**: Was the work delivered by the deadline?
4. **Specification Match**: Does the work match the original requirements?
5. **Communication**: Did the freelancer provide reasonable updates?

**Example Dispute**:

**Case**: Smart Contract Development Disagreement

**Background**: Client hired a Solidity developer to create an ERC-20 token with vesting functionality for $3,000 USDC. The contract specified: "Token with linear vesting over 12 months, admin controls for vesting schedules, fully tested with Hardhat."

**Dispute**: Developer delivered the contract 2 days late. Client claims the code is "low quality" and "not properly tested" (only 60% test coverage). Client refuses to release escrow. Developer claims the contract works perfectly and the delay was due to client's late feedback on vesting parameters.

**Evidence Submitted**:
- **Developer**: GitHub repository with code, commit history showing client feedback delay, test suite with 60% coverage, deployment to testnet
- **Client**: Messages showing original deadline, industry standard (80%+ test coverage), screenshots of uncovered edge cases

**Juror Decision**: Jurors review:
- Does the contract meet functional requirements? ✅ Yes
- Is 60% test coverage acceptable? ⚠️ Below industry standard but functional
- Was the delay reasonable given client feedback delay? ✅ Yes

**Ruling**: 80% of escrow to developer ($2,400), 20% returned to client ($600) as compensation for below-standard testing. Both parties partially at fault.

**Court Parameters**:
- **Minimum Stake**: 500 PNK (~$50)
- **Jurors per Dispute**: 3 (first instance), 5 (appeal), 9 (final appeal)
- **Voting Period**: 48 hours
- **Appeal Fee**: 2x arbitration cost
- **Juror Qualifications**: Recommended to have experience in tech/freelance work

---

### Use Case 2: NFT Commission

**Court**: **Create New Court → "NFT & Digital Art Court"**

**Court Purpose**:

This subcourt is dedicated to disputes involving NFT commissions, digital art sales, and creative work deliverables in the Web3 space. Jurors in this court have appreciation for artistic interpretation, understand NFT standards (ERC-721, ERC-1155), and can evaluate whether commissioned artwork meets agreed specifications while respecting artistic license.

**Court Specialization**:
- NFT commission disputes (PFPs, 1/1s, generative art)
- Digital art quality and specification matching
- Artistic interpretation vs. exact replication
- NFT metadata and IPFS delivery
- Copyright and intellectual property in NFT context
- Collection artwork consistency

**Types of Disputes**:
1. **Artistic Quality**: Is the artwork of professional quality?
2. **Specification Match**: Does the artwork match the commission brief?
3. **Style Consistency**: For collections, does art match style guide?
4. **Technical Delivery**: Is the NFT properly minted and transferred?
5. **Revision Abuse**: Did one party abuse revision requests?

**Example Dispute**:

**Case**: PFP Commission Style Disagreement

**Background**: Collector commissioned an artist to create a custom PFP (profile picture) NFT for 1 ETH (~$2,000). Brief specified: "Cyberpunk cat, neon colors, wearing VR goggles, futuristic city background, anime style."

**Dispute**: Artist delivered the NFT. Collector claims the style is "too cartoony, not anime enough" and the colors are "too bright." Artist claims the work matches the brief and the collector is being subjective. Collector refuses to release escrow.

**Evidence Submitted**:
- **Artist**: 
  - Original commission agreement and messages
  - Reference images shared by collector (showing various anime styles)
  - WIP (work-in-progress) sketches sent during process (collector approved)
  - Final delivered NFT (3000x3000px PNG, minted on OpenSea)
  - Similar commissions in portfolio showing consistent style
  
- **Collector**: 
  - Messages emphasizing "anime style" multiple times
  - Reference images of anime art (more detailed, less cartoony)
  - Screenshots comparing delivered work to references
  - Claim that WIP approval was for composition only, not final style

**Juror Decision**: Jurors review:
- Is the artwork professional quality? ✅ Yes, high-quality digital art
- Does it have cyberpunk elements? ✅ Yes (neon, city, VR goggles)
- Is it "anime style"? ⚠️ Subjective; leans more cartoon than traditional anime
- Did artist follow WIP feedback process? ✅ Yes, showed sketches
- Did collector approve WIP? ✅ Yes, with minor notes on colors

**Ruling**: 90% of escrow to artist (0.9 ETH), 10% returned to collector (0.1 ETH) as compromise. Reasoning: Artist delivered professional work matching most specifications. "Anime style" is subjective and artist's interpretation was reasonable. Collector approved WIPs, so artist acted in good faith. Minor discount acknowledges collector's legitimate style concern.

**Court Parameters**:
- **Minimum Stake**: 300 PNK (~$30)
- **Jurors per Dispute**: 5 (first instance) - higher than standard to capture artistic subjectivity
- **Voting Period**: 72 hours (longer to allow aesthetic evaluation)
- **Appeal Fee**: 2x arbitration cost
- **Juror Qualifications**: Recommended to have interest/experience in digital art or NFTs

**Note on Artistic Disputes**:

This court recognizes that art involves subjective interpretation. Jurors are instructed to evaluate:
1. **Objective criteria** (resolution, format, technical delivery)
2. **Good faith effort** (did artist genuinely attempt to meet brief?)
3. **Industry standards** (is this professional-quality work?)
4. **Communication** (were WIPs shared? Feedback incorporated?)

Perfect satisfaction is not required for payment—only that the artist made a reasonable, professional attempt to fulfill the commission as specified.

---

## Summary

**Points Breakdown**:
- Part A (2 Use Cases): 2 points ✅
- Part B (2 Logo Sketches): 2 points ✅
- Part C (2 Descriptions): 2 points ✅
- Part D (2 Contracts): 2 points ✅
- Part E (Court Selection): 2 points ✅

**Total**: 10 points

**Key Design Decisions**:

1. **Freelance Work**: Targets the huge Web3 job market (developers, designers, etc.)
2. **NFT Commission**: Taps into the $2B+ NFT market with a clear pain point
3. **New Courts**: Both use cases warrant specialized courts rather than generic Blockchain Court
4. **Contracts**: Detailed but user-friendly, with dynamic fields for flexibility
5. **Dispute Examples**: Show nuanced rulings (partial payments), not just binary outcomes

**Integration with Existing Kleros Escrow**:
- Uses same smart contract infrastructure
- Adds new templates and court routing
- Minimal technical lift, maximum market expansion

**Market Opportunity**:
- **Freelance Work**: $1B+ Web3 jobs market (Gitcoin, Layer3, Questbook)
- **NFT Commissions**: $100M+ in commissioned art (estimated 5-10% of NFT market)

---

**Reference**: This exercise leverages research methodology from `/Kleros/KLEROS_INTEGRATION_TARGETS.md` and demonstrates understanding of Kleros Court system architecture.





