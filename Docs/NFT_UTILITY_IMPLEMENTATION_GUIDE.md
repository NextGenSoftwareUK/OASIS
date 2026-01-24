# NFT Utility Implementation Guide - How NFTs Connect to Other Tools

## Overview

This guide explains **how NFT utilities actually work in practice** - the technical implementation patterns, API calls, and integration mechanisms that connect NFTs to other systems.

---

## 1. NFT Ownership Verification (The Foundation)

### How It Works

**Step 1: User Makes Request**
```javascript
// User wants to access premium feature
const userAvatarId = "0df19747-fa32-4c2f-a6b8-b55ed76d04af";
const feature = "premium_access";
```

**Step 2: System Checks NFT Ownership**
```javascript
// Frontend: nft-gate.js
async function hasNFTRequiredFor(featureKey, avatarId) {
  // 1. Load all NFTs for avatar from OASIS API
  const response = await fetch(
    `https://localhost:5004/api/nft/load-all-nfts-for_avatar/${avatarId}`,
    {
      headers: {
        'Authorization': `Bearer ${jwtToken}`,
        'Content-Type': 'application/json'
      }
    }
  );
  
  const data = await response.json();
  const nfts = data.result || [];
  
  // 2. Check if any NFT matches requirements
  const feature = FEATURE_NFTS[featureKey];
  for (const nft of nfts) {
    // Match by symbol, mint address, or trait
    if (nft.symbol === 'PREMIUM_PASS') {
      return true; // User has access!
    }
  }
  
  return false; // No access
}
```

**Step 3: Backend Verification (Optional - More Secure)**
```csharp
// Backend: C# Controller
[HttpPost("api/premium/access")]
[Authorize]
public async Task<IActionResult> AccessPremiumFeature()
{
    var avatarId = GetAvatarIdFromToken(); // From JWT
    
    // Load NFTs for avatar
    var nftResult = await _nftManager.LoadAllNFTsForAvatarAsync(avatarId);
    
    // Check if user owns required NFT
    var hasAccess = nftResult.Result.Any(nft => 
        nft.Symbol == "PREMIUM_PASS" || 
        nft.MetaData?.ContainsKey("AccessLevel") == true
    );
    
    if (!hasAccess) {
        return Unauthorized("Premium Pass NFT required");
    }
    
    // Grant access to premium feature
    return Ok(await _premiumService.GetContent());
}
```

**Step 4: On-Chain Verification (Most Secure)**
```csharp
// Verify ownership on blockchain
public async Task<bool> VerifyNFTOnChain(string mintAddress, string walletAddress)
{
    // Query Solana blockchain directly
    var rpcRequest = new {
        jsonrpc = "2.0",
        method = "getTokenAccountsByOwner",
        @params = new object[] {
            walletAddress,
            new { mint = mintAddress }
        }
    };
    
    var response = await _httpClient.PostAsync(
        "https://api.mainnet-beta.solana.com",
        new StringContent(JsonSerializer.Serialize(rpcRequest))
    );
    
    // Check if wallet owns the NFT
    var result = await response.Content.ReadFromJsonAsync<SolanaResponse>();
    return result.result.value.Count > 0; // Has token account = owns NFT
}
```

---

## 2. x402 Revenue Sharing (Payment Distribution)

### How It Works

**Step 1: NFT Minted with x402 Config**
```javascript
// When minting NFT
{
  Symbol: "REVENUE_NFT",
  X402Enabled: true,
  X402PaymentEndpoint: "https://api.yourservice.com/x402/revenue",
  X402RevenueModel: "equal"
}

// Stored in NFT metadata:
{
  "x402Config": {
    "enabled": true,
    "paymentEndpoint": "https://api.yourservice.com/x402/revenue",
    "revenueModel": "equal"
  }
}
```

**Step 2: Payment Received**
```csharp
// Backend detects payment on blockchain
public async Task OnPaymentReceived(string transactionSignature)
{
    // 1. Verify transaction on Solana
    var transaction = await VerifyTransactionOnChain(transactionSignature);
    
    // 2. Identify which NFT received payment
    var nft = await FindNFTByTreasuryWallet(transaction.Recipient);
    
    // 3. Read x402 config from NFT metadata
    var x402Config = nft.MetaData["x402Config"];
    
    if (x402Config.enabled) {
        // 4. Trigger webhook to payment endpoint
        await TriggerDistributionWebhook(nft, transaction);
    }
}
```

**Step 3: Webhook Distribution**
```csharp
// Your payment endpoint receives webhook
[HttpPost("api/x402/revenue")]
public async Task<IActionResult> DistributeRevenue([FromBody] PaymentWebhook payload)
{
    // 1. Get all NFT holders from blockchain
    var holders = await GetNFTHolders(payload.NftTokenAddress);
    
    // 2. Calculate distribution based on revenue model
    decimal amountPerHolder;
    if (payload.RevenueModel == "equal") {
        amountPerHolder = payload.Amount / holders.Count;
    } else if (payload.RevenueModel == "weighted") {
        // Calculate based on ownership percentage
        amountPerHolder = CalculateWeightedDistribution(payload.Amount, holders);
    }
    
    // 3. Send payments to each holder
    foreach (var holder in holders) {
        await SendPayment(holder.WalletAddress, amountPerHolder);
    }
    
    return Ok(new { distributed = true, recipients = holders.Count });
}
```

**Step 4: Blockchain Verification**
```csharp
// Verify transaction on Solana
private async Task<bool> VerifyTransactionOnChain(string signature, decimal amount)
{
    var rpcRequest = new {
        jsonrpc = "2.0",
        method = "getTransaction",
        @params = new object[] {
            signature,
            new { encoding = "json", commitment = "confirmed" }
        }
    };
    
    var response = await _httpClient.PostAsync(
        "https://api.mainnet-beta.solana.com",
        new StringContent(JsonSerializer.Serialize(rpcRequest))
    );
    
    var result = await response.Content.ReadFromJsonAsync<SolanaTransaction>();
    
    // Verify transaction succeeded and amount matches
    return result.meta.err == null && 
           result.transaction.message.instructions
               .Any(i => i.parsed.info.lamports == amount * 1_000_000_000);
}
```

---

## 3. NFT-Gated Features (Access Control)

### Frontend Implementation

```javascript
// nft-gate.js - Real implementation
async function isTabAccessible(avatarId, tabName) {
    // 1. Load configuration
    const config = window.FEATURE_NFTS || {};
    const feature = Object.values(config).find(f => f.unlocks?.tab === tabName);
    
    if (!feature) return true; // No gate
    
    // 2. Load user's NFTs
    const nfts = await loadNFTsForAvatar(avatarId);
    
    // 3. Check if any NFT matches requirements
    return nftsSatisfyRequire(nfts, feature.require);
}

// Usage in portal
async function switchTab(tabName) {
    const avatarId = getAvatarId();
    
    // Check access before switching
    const hasAccess = await isTabAccessible(avatarId, tabName);
    
    if (!hasAccess) {
        // Show locked UI
        showLockedMessage(feature.name, feature.description);
        return;
    }
    
    // User has access - show tab
    showTabContent(tabName);
}
```

### Backend API Integration

```csharp
// Backend middleware for NFT gating
public class NFTGateMiddleware
{
    public async Task InvokeAsync(HttpContext context)
    {
        var endpoint = context.GetEndpoint();
        var nftRequirement = endpoint?.Metadata.GetMetadata<NFTRequirementAttribute>();
        
        if (nftRequirement != null) {
            var avatarId = GetAvatarIdFromToken(context);
            
            // Check NFT ownership
            var hasAccess = await CheckNFTOwnership(
                avatarId, 
                nftRequirement.Symbol,
                nftRequirement.Trait
            );
            
            if (!hasAccess) {
                context.Response.StatusCode = 403;
                await context.Response.WriteAsync("NFT required for access");
                return;
            }
        }
        
        await _next(context);
    }
}

// Usage on endpoint
[NFTRequirement(Symbol = "PREMIUM_PASS")]
[HttpGet("api/premium/content")]
public async Task<IActionResult> GetPremiumContent()
{
    return Ok(await _premiumService.GetContent());
}
```

---

## 4. Quest/Mission Rewards (Gamification)

### How Quests Reward NFTs

```csharp
// STAR API: Quest completion
[HttpPost("api/quests/{questId}/complete")]
public async Task<IActionResult> CompleteQuest(Guid questId)
{
    var avatarId = GetAvatarIdFromToken();
    
    // 1. Verify quest requirements met
    var quest = await _questService.GetQuestAsync(questId);
    var progress = await _questService.GetProgressAsync(questId, avatarId);
    
    if (!progress.IsComplete) {
        return BadRequest("Quest requirements not met");
    }
    
    // 2. Award rewards
    foreach (var reward in quest.Rewards) {
        if (reward.Type == "NFT") {
            // Mint NFT and send to avatar
            await _nftManager.MintNFTAsync(new MintNFTRequest {
                Symbol = reward.NFTSymbol,
                Title = reward.NFTTitle,
                SendToAvatarAfterMintingId = avatarId,
                // ... other NFT properties
            });
        }
        
        if (reward.Type == "Karma") {
            await _karmaService.AddKarmaAsync(avatarId, reward.KarmaAmount);
        }
    }
    
    // 3. Mark quest as completed
    await _questService.MarkCompletedAsync(questId, avatarId);
    
    return Ok(new { completed = true, rewards = quest.Rewards });
}
```

---

## 5. Agent-NFT Linking (A2A Protocol)

### How Agents Connect to NFTs

```csharp
// Mint NFT for agent
[HttpPost("api/a2a/agent/{agentId}/mint-nft")]
public async Task<IActionResult> MintAgentNFT(Guid agentId)
{
    // 1. Verify caller owns agent
    var agent = await _agentManager.GetAgentAsync(agentId);
    if (agent.OwnerAvatarId != GetAvatarIdFromToken()) {
        return Unauthorized();
    }
    
    // 2. Mint NFT with agent metadata
    var nftResult = await _nftManager.MintNFTAsync(new MintNFTRequest {
        Symbol = $"AGENT_{agentId}",
        Title = agent.Name,
        MetaData = new Dictionary<string, object> {
            ["AgentId"] = agentId.ToString(),
            ["AgentCard"] = JsonSerializer.Serialize(agent.Card),
            ["AgentType"] = "Agent"
        },
        SendToAvatarAfterMintingId = agent.OwnerAvatarId
    });
    
    // 3. Link NFT to agent (bidirectional)
    agent.MetaData["NFTId"] = nftResult.Result.Id.ToString();
    agent.MetaData["NFTMintAddress"] = nftResult.Result.NftTokenAddress;
    await _agentManager.UpdateAgentAsync(agent);
    
    return Ok(nftResult.Result);
}

// Sync ownership when NFT transferred
public async Task SyncAgentOwnershipFromNFT(Guid nftId)
{
    // 1. Load NFT
    var nft = await _nftManager.LoadNFTAsync(nftId);
    
    // 2. Get agent ID from NFT metadata
    var agentId = Guid.Parse(nft.MetaData["AgentId"].ToString());
    
    // 3. Update agent ownership to match NFT
    var agent = await _agentManager.GetAgentAsync(agentId);
    agent.MetaData["OwnerAvatarId"] = nft.CurrentOwnerAvatarId.ToString();
    await _agentManager.UpdateAgentAsync(agent);
}
```

---

## 6. GeoNFT Integration (Location-Based)

### How GeoNFTs Work

```csharp
// Place NFT at location
[HttpPost("api/nft/place-geo-nft")]
public async Task<IActionResult> PlaceGeoNFT([FromBody] PlaceGeoNFTRequest request)
{
    // 1. Verify user owns NFT
    var nft = await _nftManager.LoadNFTAsync(request.OriginalOASISNFTId);
    if (nft.CurrentOwnerAvatarId != GetAvatarIdFromToken()) {
        return Unauthorized("You don't own this NFT");
    }
    
    // 2. Create GeoNFT record
    var geoNFT = new GeoNFT {
        OriginalNFTId = request.OriginalOASISNFTId,
        Latitude = request.Latitude,
        Longitude = request.Longitude,
        PlacedByAvatarId = GetAvatarIdFromToken(),
        AllowOtherPlayersToCollect = request.AllowOtherPlayersToCollect
    };
    
    // 3. Store in database
    await _geoNFTService.CreateGeoNFTAsync(geoNFT);
    
    return Ok(geoNFT);
}

// Check nearby GeoNFTs (for AR/VR apps)
[HttpGet("api/geonft/nearby")]
public async Task<IActionResult> GetNearbyGeoNFTs(
    double latitude, 
    double longitude, 
    double radiusKm = 1.0)
{
    var nearbyNFTs = await _geoNFTService.FindNearbyAsync(
        latitude, 
        longitude, 
        radiusKm
    );
    
    return Ok(nearbyNFTs);
}
```

---

## 7. Integration Patterns Summary

### Pattern 1: Frontend Check → Backend Verify
```
User Action → Frontend checks NFT → API call → Backend verifies → Grant/Deny
```

### Pattern 2: Webhook → Event → Action
```
Blockchain Event → Webhook → Backend processes → Update database → Notify users
```

### Pattern 3: Polling → Sync → Update
```
Background job → Poll blockchain → Check ownership → Sync database → Update state
```

### Pattern 4: Metadata → Config → Behavior
```
NFT metadata → Read config → Apply rules → Execute action
```

---

## 8. Real-World Example: Premium Access System

### Complete Flow

```javascript
// 1. User clicks "Premium Content" button
async function accessPremiumContent() {
    const avatarId = getAvatarId();
    
    // 2. Frontend checks NFT ownership (cached)
    const hasAccess = await nftGate.hasNFTRequiredFor('premium', avatarId);
    
    if (!hasAccess) {
        showUpgradeModal();
        return;
    }
    
    // 3. API call to backend
    const response = await fetch('/api/premium/content', {
        headers: {
            'Authorization': `Bearer ${getToken()}`
        }
    });
    
    if (response.status === 403) {
        // Backend also verified - no NFT
        showUpgradeModal();
        return;
    }
    
    // 4. Show premium content
    const content = await response.json();
    displayContent(content);
}
```

```csharp
// Backend endpoint
[NFTRequirement(Symbol = "PREMIUM_PASS")]
[HttpGet("api/premium/content")]
[Authorize]
public async Task<IActionResult> GetPremiumContent()
{
    // Middleware already verified NFT ownership
    // Just return content
    return Ok(await _premiumService.GetContent());
}
```

---

## 9. Key Technical Points

### Ownership Verification Methods

1. **Database Check** (Fast, but can be stale)
   - Query OASIS database for NFT ownership
   - Cached for 5 minutes
   - Used for UI gating

2. **Blockchain Query** (Slower, but authoritative)
   - Direct RPC call to Solana/Ethereum
   - Real-time ownership verification
   - Used for critical operations

3. **Hybrid Approach** (Recommended)
   - Database check for UI
   - Blockchain verification for transactions
   - Periodic sync job to keep database updated

### Caching Strategy

```javascript
// Frontend caching (5 minutes)
const CACHE_TTL = 5 * 60 * 1000;
const cache = {
    [`${avatarId}:${feature}`]: {
        ok: true,
        ts: Date.now()
    }
};

// Backend caching (Redis/Memory)
var cache = _memoryCache.Get($"nft:{avatarId}:{symbol}");
if (cache == null) {
    cache = await VerifyOnChain(avatarId, symbol);
    _memoryCache.Set($"nft:{avatarId}:{symbol}", cache, TimeSpan.FromMinutes(5));
}
```

---

## 10. Security Considerations

### Always Verify on Backend

```csharp
// ❌ BAD: Trusting frontend
[HttpGet("api/premium")]
public IActionResult GetPremium() {
    // No verification - can be bypassed!
    return Ok(premiumContent);
}

// ✅ GOOD: Backend verification
[NFTRequirement(Symbol = "PREMIUM_PASS")]
[HttpGet("api/premium")]
[Authorize]
public async Task<IActionResult> GetPremium() {
    // Middleware verifies NFT ownership
    return Ok(premiumContent);
}
```

### Blockchain Verification for Critical Operations

```csharp
// For payments, transfers, etc.
public async Task<bool> VerifyCriticalOperation(string avatarId, string nftSymbol) {
    // Always verify on-chain for critical operations
    var wallet = await GetAvatarWallet(avatarId);
    return await VerifyNFTOnChain(nftSymbol, wallet.Address);
}
```

---

## Summary

NFT utilities work through:

1. **Ownership Verification**: API calls check NFT ownership (database or blockchain)
2. **Metadata Configuration**: NFT metadata stores utility config (x402, access rules, etc.)
3. **Webhook Events**: Blockchain events trigger webhooks for revenue distribution
4. **Middleware/Guards**: Backend middleware enforces NFT requirements
5. **Caching**: Frontend and backend cache ownership checks for performance
6. **Sync Jobs**: Background jobs keep database in sync with blockchain

The key is: **NFTs are just data structures** - the utility comes from **how your code reads and acts on that data**.
