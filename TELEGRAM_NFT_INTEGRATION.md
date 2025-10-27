# 🎨 Telegram NFT Integration Guide

## Overview

Integrate OASIS NFT minting with your Telegram accountability bot to create achievement badges, proof-of-work tokens, and rewards for your Metamasons community.

---

## 🎯 Use Cases

### 1. **Achievement NFTs** (Auto-Minted)
When a user completes a goal:
- Auto-mint an NFT badge
- Send to their Solana wallet
- Share achievement in group

### 2. **Manual NFT Rewards**
Group admins can:
- Send custom NFTs to members
- Reward top performers
- Create special edition badges

### 3. **Proof of Accountability**
- NFT receipts for completed challenges
- On-chain proof of commitment
- Collectible progress history

### 4. **Membership Tokens**
- NFTs for joining exclusive groups
- Access passes for premium features
- Limited edition group badges

---

## 🚀 Implementation

### API Endpoint (Already Available!)

Your OASIS API already has the NFT minting endpoint:

```
POST https://oasisweb4.one/api/nft/mint-nft
```

**Payload Structure:**
```json
{
  "Title": "Achievement Unlocked: MVP Launch",
  "Description": "Completed building and launching MVP",
  "Symbol": "MMASON",
  "OnChainProvider": { "value": 3, "name": "SolanaOASIS" },
  "OffChainProvider": { "value": 23, "name": "MongoDBOASIS" },
  "NFTOffChainMetaType": { "value": 3, "name": "ExternalJsonURL" },
  "NFTStandardType": { "value": 2, "name": "SPL" },
  "ImageUrl": "https://your-pinata-url.com/image.png",
  "JSONMetaDataURL": "https://your-pinata-url.com/metadata.json",
  "Price": 0,
  "NumberToMint": 1,
  "StoreNFTMetaDataOnChain": false,
  "MintedByAvatarId": "your-avatar-id",
  "SendToAddressAfterMinting": "recipient-solana-wallet",
  "WaitTillNFTSent": true,
  "WaitForNFTToSendInSeconds": 60,
  "AttemptToSendEveryXSeconds": 5
}
```

---

## 📱 Telegram Commands

### User Commands

```
/mintnft <title> <description>
  - Mint a custom NFT
  - Example: /mintnft "My Badge" "Completed 30-day challenge"

/mynfts
  - View all your minted NFTs
  - Shows collection stats

/sendnft <@username> <nft-id>
  - Send an NFT to another user
  - Example: /sendnft @alice nft_abc123
```

### Admin Commands

```
/rewardnft <@username> <achievement-id>
  - Manually reward an NFT for achievement completion
  - Example: /rewardnft @bob ach_xyz789

/groupnft <group-id>
  - Mint membership NFT for all group members
  - Example: /groupnft 0c29ebea-fa93-4a17-90c5-4a5f3d850258
```

---

## 🔧 Code Implementation

### Step 1: Create NFT Service Helper

Create: `/ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI/Services/NFTService.cs`

```csharp
using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services
{
    public class NFTService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;

        public NFTService(string baseUrl = "https://oasisweb4.one")
        {
            _httpClient = new HttpClient();
            _baseUrl = baseUrl;
        }

        public async Task<OASISResult<string>> MintAchievementNFTAsync(
            string title,
            string description,
            string recipientWallet,
            Guid mintedByAvatarId,
            string imageUrl = null)
        {
            try
            {
                var payload = new
                {
                    Title = title,
                    Description = description,
                    Symbol = "MMASON", // Metamasons symbol
                    OnChainProvider = new { value = 3, name = "SolanaOASIS" },
                    OffChainProvider = new { value = 23, name = "MongoDBOASIS" },
                    NFTOffChainMetaType = new { value = 3, name = "ExternalJsonURL" },
                    NFTStandardType = new { value = 2, name = "SPL" },
                    ImageUrl = imageUrl ?? "https://default-badge-image.com/achievement.png",
                    JSONMetaDataURL = "", // Will be auto-generated
                    Price = 0,
                    NumberToMint = 1,
                    StoreNFTMetaDataOnChain = false,
                    MintedByAvatarId = mintedByAvatarId,
                    SendToAddressAfterMinting = recipientWallet,
                    WaitTillNFTSent = true,
                    WaitForNFTToSendInSeconds = 60,
                    AttemptToSendEveryXSeconds = 5
                };

                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var response = await _httpClient.PostAsync($"{_baseUrl}/api/nft/mint-nft", content);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return new OASISResult<string>
                    {
                        Result = responseBody,
                        Message = "NFT minted successfully",
                        IsError = false
                    };
                }
                else
                {
                    return new OASISResult<string>
                    {
                        Message = $"NFT minting failed: {responseBody}",
                        IsError = true
                    };
                }
            }
            catch (Exception ex)
            {
                return new OASISResult<string>
                {
                    Message = $"Error minting NFT: {ex.Message}",
                    IsError = true
                };
            }
        }

        public async Task<OASISResult<string>> MintGroupBadgeAsync(
            string groupName,
            string memberName,
            string recipientWallet,
            Guid mintedByAvatarId)
        {
            return await MintAchievementNFTAsync(
                title: $"{groupName} Member Badge",
                description: $"Official member of {groupName} accountability group - {memberName}",
                recipientWallet: recipientWallet,
                mintedByAvatarId: mintedByAvatarId,
                imageUrl: "https://your-group-badge-image.com/badge.png"
            );
        }
    }
}
```

### Step 2: Add NFT Commands to TelegramBotService

Add to `/ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI/Services/TelegramBotService.cs`:

```csharp
// Add to class fields:
private readonly NFTService _nftService;

// Add to constructor:
public TelegramBotService(
    string botToken,
    TelegramOASIS telegramProvider,
    AvatarManager avatarManager,
    ILogger<TelegramBotService> logger,
    NFTService nftService)
{
    // ... existing code ...
    _nftService = nftService;
}

// Add to HandleMessageAsync method, in the command switch:

case "/mintnft":
    await HandleMintNFTCommand(message);
    break;

case "/mynfts":
    await HandleMyNFTsCommand(message);
    break;

case "/rewardnft":
    await HandleRewardNFTCommand(message);
    break;

// Add these new methods:

private async Task HandleMintNFTCommand(Message message)
{
    try
    {
        var chatId = message.Chat.Id;
        var userId = message.From?.Id ?? 0;
        
        // Parse command: /mintnft title | description
        var parts = message.Text.Split('|', 2);
        if (parts.Length < 2)
        {
            await _botClient.SendTextMessageAsync(
                chatId,
                "❌ Usage: /mintnft Title | Description\nExample: /mintnft My Achievement | Completed 30-day challenge"
            );
            return;
        }
        
        var title = parts[0].Replace("/mintnft", "").Trim();
        var description = parts[1].Trim();
        
        // Get user's OASIS avatar and wallet
        var avatar = await _telegramProvider.GetTelegramAvatarByTelegramIdAsync(userId);
        if (avatar == null || avatar.IsError)
        {
            await _botClient.SendTextMessageAsync(
                chatId,
                "❌ You need to /start first to create your OASIS avatar!"
            );
            return;
        }
        
        // TODO: Get user's actual Solana wallet from avatar
        var solanaWallet = "PLACEHOLDER_WALLET"; // Get from avatar.Result
        
        await _botClient.SendTextMessageAsync(
            chatId,
            "🎨 Minting your NFT... This may take a moment..."
        );
        
        // Mint the NFT
        var result = await _nftService.MintAchievementNFTAsync(
            title: title,
            description: description,
            recipientWallet: solanaWallet,
            mintedByAvatarId: avatar.Result.Id
        );
        
        if (result.IsError)
        {
            await _botClient.SendTextMessageAsync(
                chatId,
                $"❌ Failed to mint NFT: {result.Message}"
            );
        }
        else
        {
            await _botClient.SendTextMessageAsync(
                chatId,
                $"✅ NFT Minted Successfully!\n\n" +
                $"🎨 Title: {title}\n" +
                $"📝 Description: {description}\n\n" +
                $"Check your Solana wallet!"
            );
        }
    }
    catch (Exception ex)
    {
        _logger?.LogError(ex, "Error handling /mintnft command");
        await _botClient.SendTextMessageAsync(
            message.Chat.Id,
            "❌ An error occurred while minting your NFT."
        );
    }
}

private async Task HandleMyNFTsCommand(Message message)
{
    // TODO: Query user's NFT collection from blockchain/MongoDB
    await _botClient.SendTextMessageAsync(
        message.Chat.Id,
        "🎨 Your NFT Collection:\n\n" +
        "Coming soon! This will show all your achievement badges and tokens."
    );
}

private async Task HandleRewardNFTCommand(Message message)
{
    // TODO: Admin-only command to reward NFTs
    // Verify user is group admin
    // Parse recipient and achievement
    // Mint and send NFT
    
    await _botClient.SendTextMessageAsync(
        message.Chat.Id,
        "🎁 Reward NFT feature coming soon!"
    );
}
```

### Step 3: Auto-Mint NFTs on Achievement Completion

Update `/OASIS Architecture/NextGenSoftware.OASIS.API.Core/Managers/AchievementManager.cs`:

```csharp
// Add to CompleteAchievementAsync method:

public async Task<OASISResult<AchievementRewardResult>> CompleteAchievementAsync(
    Guid avatarId,
    string achievementTitle,
    string achievementDescription,
    int karmaReward = 50,
    decimal tokenReward = 1.0m,
    bool mintNFT = false,  // NEW parameter
    string nftImageUrl = null,  // NEW parameter
    ProviderType providerType = ProviderType.Default)
{
    try
    {
        var result = new AchievementRewardResult
        {
            AvatarId = avatarId,
            AchievementTitle = achievementTitle,
            Rewards = new List<string>()
        };

        // Existing karma and token rewards...
        
        // NEW: Mint NFT if requested
        if (mintNFT)
        {
            // Get avatar's Solana wallet
            var avatar = await _avatarManager.LoadAvatarAsync(avatarId);
            if (avatar != null && !avatar.IsError && avatar.Result != null)
            {
                var nftService = new NFTService();
                var nftResult = await nftService.MintAchievementNFTAsync(
                    title: $"Achievement: {achievementTitle}",
                    description: achievementDescription,
                    recipientWallet: "SOLANA_WALLET", // Get from avatar
                    mintedByAvatarId: avatarId,
                    imageUrl: nftImageUrl
                );
                
                if (!nftResult.IsError)
                {
                    result.Rewards.Add($"NFT Badge: {achievementTitle}");
                    result.Message += " | NFT minted and sent!";
                }
            }
        }

        return new OASISResult<AchievementRewardResult>
        {
            Result = result,
            Message = result.Message
        };
    }
    catch (Exception ex)
    {
        // ...
    }
}
```

### Step 4: Register NFT Service in Startup.cs

```csharp
// In ConfigureServices:
services.AddSingleton<NFTService>();

// Update TelegramBotService registration:
services.AddSingleton<TelegramBotService>(provider =>
{
    var telegramProvider = provider.GetRequiredService<TelegramOASIS>();
    var avatarManager = AvatarManager.Instance;
    var logger = provider.GetService<ILogger<TelegramBotService>>();
    var nftService = provider.GetRequiredService<NFTService>();  // NEW
    var botToken = "7927576561:AAEFHa3k1t6kj0t6wOu6QtU61KRsNxOoeMo";
    
    return new TelegramBotService(
        botToken, 
        telegramProvider, 
        avatarManager, 
        logger,
        nftService  // NEW
    );
});
```

---

## 🎨 Achievement Badge Templates

### Badge Image Guidelines

**Recommended specs:**
- **Size:** 1000x1000px
- **Format:** PNG with transparency
- **Style:** Clean, professional, branded

**Badge Types:**
1. **Bronze** - First milestone
2. **Silver** - Intermediate achievement
3. **Gold** - Major milestone
4. **Diamond** - Ultimate achievement
5. **Custom** - Group-specific badges

### Storage Options

**Option 1: Pinata (IPFS)**
```bash
# Upload to Pinata
curl -X POST "https://api.pinata.cloud/pinning/pinFileToIPFS" \
  -H "Authorization: Bearer YOUR_PINATA_JWT" \
  -F "file=@badge.png"

# Use returned IPFS URL in NFT mint
```

**Option 2: Your Own CDN**
- Host badge images on your server
- Use URLs directly in mint payload

---

## 🔄 Complete Flow Example

### Scenario: User Completes 30-Day Challenge

1. **User checks in** (Day 30)
```
User: /checkin "Day 30! Completed the challenge! 🎉"
```

2. **System detects completion**
```csharp
// In TelegramBotService
if (checkInCount >= 30)
{
    // Complete achievement with NFT
    await AchievementManager.Instance.CompleteAchievementAsync(
        avatarId: userAvatarId,
        achievementTitle: "30-Day Consistency Champion",
        achievementDescription: "Completed 30 consecutive days of check-ins",
        karmaReward: 500,
        tokenReward: 10.0m,
        mintNFT: true,
        nftImageUrl: "https://yourdomain.com/badges/30day-champion.png"
    );
}
```

3. **Bot responds**
```
Bot: 🎉 AMAZING! You completed the 30-Day Challenge!

Rewards:
✨ 500 Karma Points
💎 10 EXP Tokens
🎨 NFT Badge: "30-Day Consistency Champion"

Check your Solana wallet for your achievement NFT! 🏆
```

4. **NFT appears in wallet**
- User sees NFT in Phantom/Solflare
- On-chain proof of achievement
- Can share on social media

---

## 📊 NFT Analytics & Tracking

### Track NFT Distribution

Add to TelegramController:

```csharp
[HttpGet("nft-stats")]
public async Task<IActionResult> GetNFTStats()
{
    // Query MongoDB for NFT mint history
    // Return statistics:
    // - Total NFTs minted
    // - NFTs by type
    // - Top collectors
    // - Recent mints
    
    return Ok(new {
        totalMinted = 150,
        uniqueCollectors = 45,
        topBadge = "30-Day Champion",
        recentMints = new[] { /* ... */ }
    });
}
```

---

## 🎯 Next Steps

1. **Create Badge Designs**
   - Design 5-10 achievement badges
   - Upload to Pinata or your CDN
   - Get IPFS/CDN URLs

2. **Set Up Solana Wallets**
   - Ensure users can link Solana wallets to OASIS avatars
   - Or auto-create wallets for new users

3. **Configure NFT Templates**
   - Define achievement → badge mappings
   - Set karma/token/NFT reward tiers

4. **Test Flow**
   - Complete test achievement
   - Verify NFT minting
   - Check wallet receipt

5. **Launch** 🚀
   - Announce NFT badges to Metamasons
   - Share example badges
   - Start rewarding achievements!

---

## 🔐 Security Notes

- **Wallet Verification:** Ensure users own the wallets they claim
- **Admin Controls:** Restrict `/rewardnft` to verified admins
- **Rate Limiting:** Prevent NFT spam
- **Cost Management:** Monitor Solana transaction fees

---

## 📚 Resources

- [OASIS NFT API Docs](../nft-mint-frontend/docs/OASIS_NFT_Minting_Value_Proposition.md)
- [Solana SPL Token Standard](https://spl.solana.com/token)
- [Pinata IPFS Documentation](https://docs.pinata.cloud/)
- [Telegram Bot API](https://core.telegram.org/bots/api)

---

**Built with ❤️ for the Metamasons community**





