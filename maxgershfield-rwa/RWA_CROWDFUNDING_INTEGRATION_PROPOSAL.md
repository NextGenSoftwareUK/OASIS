# RWA Crowdfunding Integration Proposal
## Adapting MetaBricks Crowdfunding Logic for Fractional RWA Purchases

---

## Executive Summary

This proposal outlines how to integrate the MetaBricks crowdfunding mechanism into the RWA Exchange platform, allowing users to purchase fractional shares of properties through an escrow-based crowdfunding system. Funds are held in escrow until the full property price is reached, at which point fractional ownership NFTs are minted and distributed to investors.

---

## Current State Analysis

### MetaBricks System
- **Architecture**: Angular-based frontend with Solana/Arbitrum blockchain integration
- **Core Concept**: Users purchase individual "bricks" (NFTs) from a fixed array (432 bricks)
- **Payment Methods**: 
  - Solana (SOL) via Phantom wallet
  - Stripe (fiat) with backend processing
- **NFT Minting**: Uses OASIS NFT API via backend service
- **Key Components**:
  - `brick-details.component.ts` - Individual brick purchase flow
  - `mint.component.ts` - Minting logic
  - `nft-minting.service.ts` - NFT minting service
  - Backend API handles OASIS authentication and NFT transfers
- **Escrow Logic**: Currently direct purchase (no escrow), but payment is held until NFT transfer completes

### RWA Exchange System
- **Architecture**: Next.js/React frontend with .NET backend
- **Current Purchase Flow**: Direct purchase via `PurchaseFlow.tsx` component
- **Property Details**: `RwaData.tsx` displays property information
- **Blockchain**: Solana integration for tokenization
- **Missing**: Crowdfunding/escrow mechanism for partial funding

---

## Proposed Integration Architecture

### 1. **Crowdfunding Component Structure**

```
RWA Property Details Page
├── Hero Section (existing)
├── Property Information (existing)
├── Crowdfunding Widget (NEW)
│   ├── Funding Progress Bar
│   ├── Share Purchase Interface
│   ├── Investor List
│   └── Escrow Status
└── Purchase Flow (existing - for full purchase)
```

### 2. **Core Concepts**

#### **Shares/Fractions Instead of Bricks**
- Each property can be divided into a configurable number of shares (e.g., 1000 shares)
- Each share represents a fractional ownership percentage
- Shares are priced: `sharePrice = propertyPrice / totalShares`
- Example: $1,890,000 property ÷ 1000 shares = $1,890 per share

#### **Escrow Mechanism**
- All payments go to an escrow account (smart contract or multi-sig wallet)
- Funds are locked until funding goal is reached
- If goal is reached: Funds released, property purchased, fractional NFTs minted
- If goal not reached by deadline: Funds returned to investors

#### **Fractional NFT Distribution**
- Once fully funded, fractional ownership NFTs are minted via OASIS API
- Each investor receives NFTs proportional to their share purchase
- NFTs represent fractional ownership rights

---

## Technical Implementation Plan

### Phase 1: Database Schema Updates

#### New Tables Required

```sql
-- Crowdfunding Campaigns
CREATE TABLE CrowdfundingCampaigns (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    RwaTokenId NVARCHAR(255) NOT NULL,
    TotalShares INT NOT NULL,
    SharePrice DECIMAL(18,2) NOT NULL,
    FundingGoal DECIMAL(18,2) NOT NULL,
    CurrentFunding DECIMAL(18,2) DEFAULT 0,
    StartDate DATETIME2 NOT NULL,
    EndDate DATETIME2 NOT NULL,
    Status NVARCHAR(50) NOT NULL, -- 'Active', 'Funded', 'Expired', 'Cancelled'
    EscrowWalletAddress NVARCHAR(255),
    CreatedAt DATETIME2 NOT NULL,
    UpdatedAt DATETIME2
);

-- Share Purchases
CREATE TABLE SharePurchases (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    CampaignId UNIQUEIDENTIFIER NOT NULL,
    UserId UNIQUEIDENTIFIER NOT NULL,
    WalletAddress NVARCHAR(255) NOT NULL,
    SharesPurchased INT NOT NULL,
    AmountPaid DECIMAL(18,2) NOT NULL,
    TransactionHash NVARCHAR(255),
    Status NVARCHAR(50) NOT NULL, -- 'Pending', 'Confirmed', 'Refunded'
    PurchasedAt DATETIME2 NOT NULL,
    FOREIGN KEY (CampaignId) REFERENCES CrowdfundingCampaigns(Id)
);

-- Fractional NFTs (after funding completes)
CREATE TABLE FractionalNFTs (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    CampaignId UNIQUEIDENTIFIER NOT NULL,
    SharePurchaseId UNIQUEIDENTIFIER NOT NULL,
    NFTMintAddress NVARCHAR(255),
    TokenId NVARCHAR(255),
    SharePercentage DECIMAL(5,2) NOT NULL,
    MintedAt DATETIME2,
    FOREIGN KEY (CampaignId) REFERENCES CrowdfundingCampaigns(Id),
    FOREIGN KEY (SharePurchaseId) REFERENCES SharePurchases(Id)
);
```

### Phase 2: Backend API Endpoints

#### New Endpoints Needed

```csharp
// Campaign Management
POST /api/rwa/{tokenId}/crowdfunding/create
GET /api/rwa/{tokenId}/crowdfunding
GET /api/rwa/crowdfunding/active
GET /api/rwa/crowdfunding/{campaignId}

// Share Purchases
POST /api/rwa/crowdfunding/{campaignId}/purchase
GET /api/rwa/crowdfunding/{campaignId}/purchases
GET /api/rwa/crowdfunding/{campaignId}/investors

// Escrow Management
POST /api/rwa/crowdfunding/{campaignId}/release-funds
POST /api/rwa/crowdfunding/{campaignId}/refund
GET /api/rwa/crowdfunding/{campaignId}/escrow-status

// NFT Distribution
POST /api/rwa/crowdfunding/{campaignId}/mint-fractional-nfts
GET /api/rwa/crowdfunding/{campaignId}/nfts
```

### Phase 3: Frontend Components

#### New Components to Create

1. **CrowdfundingWidget.tsx**
   - Displays funding progress
   - Shows share price and available shares
   - Purchase interface
   - Investor list

2. **SharePurchaseForm.tsx**
   - Input for number of shares
   - Payment method selection (Solana/Stripe)
   - Transaction confirmation

3. **FundingProgressBar.tsx**
   - Visual progress indicator
   - Time remaining
   - Funding percentage

4. **InvestorList.tsx**
   - Shows recent investors
   - Total investors count
   - Largest investments

5. **EscrowStatusBadge.tsx**
   - Shows escrow status
   - Funding goal progress
   - Time remaining

### Phase 4: Smart Contract / Escrow Logic

#### Option A: Solana Program (Recommended)
```rust
// Solana Escrow Program
pub struct CrowdfundingEscrow {
    pub campaign_id: u64,
    pub property_token_id: String,
    pub funding_goal: u64,
    pub current_funding: u64,
    pub total_shares: u64,
    pub share_price: u64,
    pub end_date: i64,
    pub escrow_wallet: Pubkey,
    pub status: EscrowStatus,
}

pub enum EscrowStatus {
    Active,
    Funded,
    Expired,
    Cancelled,
}

// Instructions
pub fn purchase_shares(
    ctx: Context<PurchaseShares>,
    shares: u64,
) -> Result<()> {
    // Transfer funds to escrow
    // Record purchase
    // Check if goal reached
}

pub fn release_funds(ctx: Context<ReleaseFunds>) -> Result<()> {
    // Verify funding goal reached
    // Release funds to property owner
    // Trigger NFT minting
}

pub fn refund_investors(ctx: Context<RefundInvestors>) -> Result<()> {
    // Verify deadline passed and goal not met
    // Return funds to investors
}
```

#### Option B: Multi-Sig Wallet (Simpler, but less automated)
- Use a multi-signature wallet (e.g., Squads Protocol on Solana)
- Requires manual intervention to release funds
- Less trustless but easier to implement

### Phase 5: Integration with OASIS NFT API

#### Fractional NFT Minting Flow

```typescript
// After funding goal is reached
async function mintFractionalNFTs(campaignId: string) {
  const campaign = await getCampaign(campaignId);
  const purchases = await getSharePurchases(campaignId);
  
  for (const purchase of purchases) {
    const sharePercentage = (purchase.sharesPurchased / campaign.totalShares) * 100;
    
    // Mint NFT via OASIS API
    const nftMetadata = {
      name: `${campaign.propertyTitle} - Share ${purchase.sharesPurchased}`,
      description: `Fractional ownership: ${sharePercentage}%`,
      image: campaign.propertyImage,
      attributes: [
        { trait_type: "Property Token ID", value: campaign.rwaTokenId },
        { trait_type: "Share Percentage", value: sharePercentage },
        { trait_type: "Shares Owned", value: purchase.sharesPurchased },
        { trait_type: "Total Shares", value: campaign.totalShares }
      ]
    };
    
    // Use OASIS NFT API (similar to MetaBricks)
    const nftResult = await oasisNFTService.mintNFT({
      walletAddress: purchase.walletAddress,
      metadata: nftMetadata,
      network: 'solana'
    });
    
    // Store NFT info in database
    await createFractionalNFT({
      campaignId,
      sharePurchaseId: purchase.id,
      nftMintAddress: nftResult.mintAddress,
      sharePercentage
    });
  }
}
```

---

## User Flow

### 1. **Property Owner Creates Campaign**
```
Property Owner → RWA Details Page → "Start Crowdfunding" Button
→ Configure: Total Shares, Share Price, Deadline
→ Campaign Created → Status: Active
```

### 2. **Investor Purchases Shares**
```
Investor → RWA Details Page → See Crowdfunding Widget
→ Enter number of shares → Select payment method
→ Connect wallet (Phantom) or use Stripe
→ Confirm transaction → Payment sent to escrow
→ Purchase recorded → Progress bar updates
```

### 3. **Funding Goal Reached**
```
System detects funding goal reached
→ Escrow releases funds to property owner
→ Property purchase transaction executed
→ Fractional NFTs minted via OASIS API
→ NFTs distributed to investors' wallets
→ Campaign status: Funded
→ Investors can view their fractional NFTs
```

### 4. **Funding Goal Not Met (Deadline Passed)**
```
Deadline passes without reaching goal
→ Escrow refunds all investors
→ Campaign status: Expired
→ Funds returned to investors' wallets
```

---

## Key Adaptations from MetaBricks

### Similarities
1. **NFT Minting**: Both use OASIS NFT API
2. **Payment Processing**: Solana wallet integration
3. **Backend API**: Similar structure for handling minting
4. **User Experience**: Purchase flow similar to brick purchase

### Differences
1. **Escrow vs Direct Purchase**: MetaBricks is direct purchase; RWA needs escrow
2. **Fixed Array vs Configurable Shares**: MetaBricks has 432 fixed bricks; RWA shares are configurable per property
3. **Goal-Based vs Individual**: MetaBricks sells individual items; RWA needs collective goal
4. **Fractional Ownership**: RWA NFTs represent fractional ownership; MetaBricks are collectibles

---

## Implementation Steps

### Step 1: Database & Backend (Week 1-2)
- [ ] Create database schema
- [ ] Implement campaign CRUD endpoints
- [ ] Implement share purchase endpoints
- [ ] Implement escrow status tracking
- [ ] Add escrow wallet management

### Step 2: Smart Contract / Escrow (Week 2-3)
- [ ] Design Solana escrow program (or set up multi-sig)
- [ ] Implement purchase shares instruction
- [ ] Implement release funds instruction
- [ ] Implement refund instruction
- [ ] Test escrow logic

### Step 3: Frontend Components (Week 3-4)
- [ ] Create CrowdfundingWidget component
- [ ] Create SharePurchaseForm component
- [ ] Create FundingProgressBar component
- [ ] Create InvestorList component
- [ ] Integrate into RwaData.tsx

### Step 4: OASIS NFT Integration (Week 4-5)
- [ ] Adapt MetaBricks NFT minting service for RWA
- [ ] Create fractional NFT metadata structure
- [ ] Implement batch NFT minting
- [ ] Implement NFT distribution logic

### Step 5: Testing & Deployment (Week 5-6)
- [ ] End-to-end testing
- [ ] Security audit
- [ ] User acceptance testing
- [ ] Deploy to staging
- [ ] Deploy to production

---

## Security Considerations

1. **Escrow Security**
   - Use multi-sig wallet or audited smart contract
   - Time-locked releases
   - Clear refund mechanisms

2. **Payment Verification**
   - Verify all transactions on-chain
   - Prevent double-spending
   - Handle failed transactions gracefully

3. **Access Control**
   - Only property owner can create campaign
   - Only verified users can purchase shares
   - Admin controls for emergency actions

4. **Data Integrity**
   - All purchases recorded on-chain
   - Database as secondary record
   - Regular reconciliation

---

## Success Metrics

1. **Adoption**
   - Number of campaigns created
   - Number of investors participating
   - Average shares per investor

2. **Funding**
   - Campaign success rate (% reaching goal)
   - Average time to fund
   - Total capital raised

3. **User Experience**
   - Purchase completion rate
   - Time to complete purchase
   - User satisfaction scores

---

## Future Enhancements

1. **Secondary Market**: Allow trading of fractional NFTs
2. **Dividend Distribution**: Automatically distribute rental income
3. **Governance**: Voting rights for major decisions
4. **Multiple Properties**: Bundle multiple properties in one campaign
5. **Tiered Shares**: Different share classes with different rights

---

## Conclusion

This integration would transform the RWA Exchange from a direct purchase platform into a crowdfunding platform, making property investment more accessible. By adapting the proven MetaBricks crowdfunding logic and adding escrow functionality, we can create a secure, user-friendly system for fractional property ownership.

The key is maintaining the simplicity of the MetaBricks purchase flow while adding the complexity of escrow and goal-based funding. The OASIS NFT API integration provides a solid foundation for fractional ownership representation.

---

**Next Steps**: Review this proposal, prioritize features, and begin Phase 1 implementation.


