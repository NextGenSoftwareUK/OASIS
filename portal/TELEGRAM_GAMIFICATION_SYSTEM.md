# Telegram Gamification System - Integration with Portal

## Executive Summary

This document outlines how to gamify Telegram usage by rewarding users for marketing OASIS, engaging in discussions, and promoting the platform. Rewards (karma, tokens, NFTs) are automatically distributed and displayed in the portal.

---

## 1. System Architecture

### 1.1 Components

```
┌─────────────────────┐
│  Telegram Group     │
│  (OASIS Community)  │
└──────────┬──────────┘
           │
           │ User Actions
           ▼
┌─────────────────────────────────┐
│  Telegram Bot (Message Monitor)│
│  - Detects keywords/actions     │
│  - Tracks engagement            │
│  - Records achievements         │
└──────────┬──────────────────────┘
           │
           ▼
┌─────────────────────────────────┐
│  TelegramOASIS Provider         │
│  - Links Telegram → OASIS Avatar│
│  - Creates achievements         │
│  - Awards karma/tokens           │
└──────────┬──────────────────────┘
           │
           ├──────────────────────┐
           │                      │
           ▼                      ▼
┌─────────────────────┐  ┌──────────────────────┐
│  AchievementManager │  │  NFT Minting Service │
│  - Karma rewards    │  │  - Mint NFTs         │
│  - Token drops      │  │  - Transfer to user  │
└──────────┬──────────┘  └──────────┬───────────┘
           │                        │
           └──────────┬──────────────┘
                      │
                      ▼
           ┌──────────────────────┐
           │  Portal Dashboard    │
           │  - Display rewards   │
           │  - Show achievements  │
           │  - Activity feed     │
           └──────────────────────┘
```

---

## 2. Gamified Actions & Rewards

### 2.1 Action Categories

#### **Category 1: Content Creation & Sharing**

| Action | Detection Method | Karma Reward | Token Reward | NFT Reward |
|--------|-----------------|--------------|--------------|------------|
| **Mention OASIS** | Keyword detection: "OASIS", "OASIS Platform", "OASIS API" | +5 per mention | - | - |
| **Share OASIS Link** | URL detection: `oasisplatform.world`, `oasisweb4.one` | +10 per share | 0.1 tokens | - |
| **Create Quality Post** | AI analysis (length, engagement, keywords) | +20-50 | 0.5-2 tokens | - |
| **Answer Questions** | Reply to questions with helpful content | +15 per answer | 0.3 tokens | - |
| **Create Tutorial/Guide** | Long-form content with code examples | +100 | 5 tokens | Tutorial NFT |
| **Share Success Story** | User testimonial/case study | +75 | 3 tokens | Story NFT |

#### **Category 2: Community Engagement**

| Action | Detection Method | Karma Reward | Token Reward | NFT Reward |
|--------|-----------------|--------------|--------------|------------|
| **Welcome New Members** | First to greet new joiners | +5 per welcome | - | - |
| **Daily Active** | Messages on 7+ consecutive days | +50 weekly bonus | 1 token | - |
| **Weekly Active** | Messages on 5+ days in week | +25 weekly bonus | 0.5 tokens | - |
| **Helpful Moderator** | Admin-verified helpful actions | +30 per action | 1 token | Moderator Badge NFT |
| **Event Participation** | Join/participate in AMAs, events | +50 per event | 2 tokens | Event NFT |

#### **Category 3: Marketing & Growth**

| Action | Detection Method | Karma Reward | Token Reward | NFT Reward |
|--------|-----------------|--------------|--------------|------------|
| **Invite New Member** | Referral link tracking | +25 per invite | 1 token | - |
| **New Member Joins** | Invited member joins group | +50 bonus | 2 tokens | Referral NFT |
| **Share on Social Media** | Proof of cross-platform share | +30 per share | 1.5 tokens | - |
| **Create Marketing Content** | Videos, graphics, articles | +100 | 5 tokens | Creator NFT |
| **Viral Content** | Post gets 100+ reactions | +200 | 10 tokens | Viral NFT |

#### **Category 4: Technical Contributions**

| Action | Detection Method | Karma Reward | Token Reward | NFT Reward |
|--------|-----------------|--------------|--------------|------------|
| **Report Bug** | Bug report format | +20 per bug | 0.5 tokens | - |
| **Suggest Feature** | Feature request format | +15 per suggestion | 0.3 tokens | - |
| **Code Contribution** | Share GitHub link/PR | +150 | 10 tokens | Contributor NFT |
| **Documentation** | Create/improve docs | +75 | 3 tokens | Doc NFT |
| **Integration Example** | Share working integration | +100 | 5 tokens | Integration NFT |

---

## 3. Detection & Tracking System

### 3.1 Message Analysis Service

**Location**: Extend `TelegramBotService.cs` with message monitoring

```csharp
public class TelegramGamificationService
{
    private readonly TelegramOASIS _telegramProvider;
    private readonly AchievementManager _achievementManager;
    private readonly NFTMintingService _nftService;
    
    // Keyword patterns for detection
    private readonly string[] OASIS_KEYWORDS = {
        "OASIS", "OASIS Platform", "OASIS API", 
        "OASIS Portal", "OASIS Avatar", "OASIS NFT"
    };
    
    private readonly string[] OASIS_URLS = {
        "oasisplatform.world", "oasisweb4.one", 
        "api.oasisplatform.world"
    };
    
    /// <summary>
    /// Analyze message for gamification opportunities
    /// </summary>
    public async Task AnalyzeMessageAsync(Message message)
    {
        var telegramId = message.From.Id;
        var text = message.Text?.ToLower() ?? "";
        var chatId = message.Chat.Id;
        
        // Get linked OASIS avatar
        var telegramAvatar = await _telegramProvider
            .GetTelegramAvatarByTelegramIdAsync(telegramId);
        
        if (telegramAvatar.Result == null)
            return; // User not linked
        
        var avatarId = telegramAvatar.Result.OasisAvatarId;
        var rewards = new List<RewardAction>();
        
        // 1. Check for OASIS mentions
        if (ContainsOASISKeywords(text))
        {
            rewards.Add(new RewardAction {
                Type = "mention",
                Karma = 5,
                Description = "Mentioned OASIS"
            });
        }
        
        // 2. Check for OASIS links
        if (ContainsOASISLinks(text))
        {
            rewards.Add(new RewardAction {
                Type = "link_share",
                Karma = 10,
                Tokens = 0.1m,
                Description = "Shared OASIS link"
            });
        }
        
        // 3. Check for quality content (length + keywords)
        if (IsQualityContent(text))
        {
            rewards.Add(new RewardAction {
                Type = "quality_post",
                Karma = 30,
                Tokens = 1.0m,
                Description = "Created quality content"
            });
        }
        
        // 4. Check if answering question
        if (IsAnsweringQuestion(message))
        {
            rewards.Add(new RewardAction {
                Type = "helpful_answer",
                Karma = 15,
                Tokens = 0.3m,
                Description = "Provided helpful answer"
            });
        }
        
        // 5. Check for code examples
        if (ContainsCode(text))
        {
            rewards.Add(new RewardAction {
                Type = "code_example",
                Karma = 50,
                Tokens = 2.0m,
                Description = "Shared code example"
            });
        }
        
        // Apply rewards
        foreach (var reward in rewards)
        {
            await ApplyRewardAsync(avatarId, telegramId, reward, message);
        }
    }
    
    private bool ContainsOASISKeywords(string text)
    {
        return OASIS_KEYWORDS.Any(keyword => 
            text.Contains(keyword.ToLower()));
    }
    
    private bool ContainsOASISLinks(string text)
    {
        return OASIS_URLS.Any(url => text.Contains(url));
    }
    
    private bool IsQualityContent(string text)
    {
        // Quality indicators:
        // - Length > 200 characters
        // - Contains multiple OASIS keywords
        // - Has structured content (numbered lists, etc.)
        return text.Length > 200 && 
               ContainsOASISKeywords(text) &&
               (text.Contains("1.") || text.Contains("-") || 
                text.Contains("•"));
    }
    
    private bool IsAnsweringQuestion(Message message)
    {
        // Check if message is a reply to a question
        if (message.ReplyToMessage == null)
            return false;
            
        var questionText = message.ReplyToMessage.Text?.ToLower() ?? "";
        var questionWords = new[] { "how", "what", "why", "where", "when", "?" };
        
        return questionWords.Any(word => questionText.Contains(word));
    }
    
    private bool ContainsCode(string text)
    {
        // Check for code blocks or code-like patterns
        return text.Contains("```") || 
               text.Contains("function") || 
               text.Contains("const ") || 
               text.Contains("async ") ||
               text.Contains("api/");
    }
}
```

### 3.2 Daily/Weekly Tracking

```csharp
public class EngagementTracker
{
    private readonly Dictionary<long, UserEngagement> _dailyEngagement = new();
    
    public async Task TrackDailyEngagementAsync(long telegramId, Guid avatarId)
    {
        var today = DateTime.UtcNow.Date;
        var key = $"{telegramId}_{today:yyyyMMdd}";
        
        if (!_dailyEngagement.ContainsKey(telegramId))
        {
            _dailyEngagement[telegramId] = new UserEngagement {
                TelegramId = telegramId,
                AvatarId = avatarId,
                Date = today,
                MessageCount = 0,
                KarmaEarned = 0
            };
        }
        
        _dailyEngagement[telegramId].MessageCount++;
        
        // Check for daily active bonus
        if (_dailyEngagement[telegramId].MessageCount >= 3)
        {
            await AwardDailyActiveBonusAsync(avatarId);
        }
    }
    
    public async Task CheckWeeklyBonusAsync(long telegramId, Guid avatarId)
    {
        // Check if user was active 5+ days this week
        var weekStart = DateTime.UtcNow.AddDays(-(int)DateTime.UtcNow.DayOfWeek);
        var activeDays = await GetActiveDaysCountAsync(telegramId, weekStart);
        
        if (activeDays >= 5)
        {
            await AwardWeeklyBonusAsync(avatarId, activeDays);
        }
    }
    
    private async Task AwardDailyActiveBonusAsync(Guid avatarId)
    {
        // Award karma for daily activity
        await _achievementManager.AwardKarmaAsync(avatarId, 10);
    }
    
    private async Task AwardWeeklyBonusAsync(Guid avatarId, int activeDays)
    {
        // Award weekly bonus
        var karma = activeDays * 5; // 5 karma per active day
        var tokens = activeDays >= 7 ? 1.0m : 0.5m;
        
        await _achievementManager.AwardKarmaAsync(avatarId, karma);
        await AwardTokensAsync(avatarId, tokens);
    }
}
```

---

## 4. Reward Distribution System

### 4.1 Karma Rewards

**Implementation**: Use existing `AchievementManager.AwardKarmaAsync()`

```csharp
// In TelegramBotService or GamificationService
await _achievementManager.AwardKarmaAsync(avatarId, karmaAmount);

// Update avatar karma in real-time
var avatarResult = await _avatarManager.LoadAvatarAsync(avatarId);
if (avatarResult.Result != null)
{
    avatarResult.Result.Karma += karmaAmount;
    await _avatarManager.SaveAvatarAsync(avatarResult.Result);
}
```

### 4.2 Token Drops

**Implementation**: Use SolanaOASIS provider for token transfers

```csharp
public async Task AwardTokensAsync(Guid avatarId, decimal tokenAmount)
{
    // Get user's Solana wallet
    var walletResult = await _walletManager.GetDefaultWalletAsync(
        avatarId, ProviderType.SolanaOASIS);
    
    if (walletResult.Result == null)
    {
        // User doesn't have Solana wallet yet
        // Could create one automatically or notify user
        return;
    }
    
    var userWallet = walletResult.Result.WalletAddress;
    
    // Transfer tokens from treasury wallet
    var transferRequest = new WalletTransactionRequest
    {
        FromWalletAddress = TREASURY_WALLET_ADDRESS,
        ToWalletAddress = userWallet,
        Amount = tokenAmount,
        Token = "OASIS", // Or your reward token symbol
        FromProviderType = ProviderType.SolanaOASIS,
        ToProviderType = ProviderType.SolanaOASIS,
        MemoText = "Telegram gamification reward"
    };
    
    await _walletManager.SendTokenAsync(transferRequest);
}
```

### 4.3 NFT Rewards

**Implementation**: Mint and transfer NFTs for special achievements

```csharp
public async Task AwardNFTRewardAsync(
    Guid avatarId, 
    string achievementType, 
    string description)
{
    // Get user's Solana wallet
    var walletResult = await _walletManager.GetDefaultWalletAsync(
        avatarId, ProviderType.SolanaOASIS);
    
    if (walletResult.Result == null)
        return;
    
    var userWallet = walletResult.Result.WalletAddress;
    
    // Create NFT metadata
    var nftMetadata = new NFTMetadata
    {
        Title = GetNFTTitle(achievementType),
        Description = description,
        ImageUrl = GetNFTImageUrl(achievementType),
        Attributes = new Dictionary<string, string>
        {
            { "Achievement Type", achievementType },
            { "Earned Date", DateTime.UtcNow.ToString("yyyy-MM-dd") },
            { "Source", "Telegram Gamification" }
        }
    };
    
    // Upload metadata to IPFS
    var metadataUrl = await UploadToIPFSAsync(nftMetadata);
    
    // Mint NFT
    var mintRequest = new MintNFTRequest
    {
        MintedByAvatarId = SITE_AVATAR_ID,
        Title = nftMetadata.Title,
        Symbol = "OASIS",
        JSONMetaDataURL = metadataUrl,
        ImageUrl = nftMetadata.ImageUrl,
        SendToAddressAfterMinting = userWallet,
        WaitTillNFTSent = true,
        ProviderType = ProviderType.SolanaOASIS
    };
    
    var mintResult = await _nftService.MintNFTAsync(mintRequest);
    
    if (!mintResult.IsError)
    {
        // Create achievement record
        await CreateAchievementRecordAsync(avatarId, achievementType, mintResult);
    }
}

private string GetNFTTitle(string achievementType)
{
    return achievementType switch
    {
        "tutorial" => "OASIS Tutorial Creator",
        "viral" => "OASIS Viral Content Creator",
        "contributor" => "OASIS Code Contributor",
        "moderator" => "OASIS Community Moderator",
        "referral" => "OASIS Community Builder",
        _ => "OASIS Achievement"
    };
}
```

---

## 5. Portal Integration

### 5.1 Display Telegram Activity in Portal

**Update `avatar-dashboard.js`** to load Telegram achievements:

```javascript
async function loadTelegramActivity(avatarId) {
    try {
        // Get Telegram achievements
        const achievementsResponse = await oasisAPI.request(
            `/api/telegram/achievements/user/${avatarId}`
        );
        
        const achievements = achievementsResponse.result || [];
        
        // Filter recent achievements
        const recentAchievements = achievements
            .filter(a => {
                const created = new Date(a.createdAt);
                const daysAgo = (Date.now() - created) / (1000 * 60 * 60 * 24);
                return daysAgo <= 30; // Last 30 days
            })
            .sort((a, b) => new Date(b.createdAt) - new Date(a.createdAt))
            .slice(0, 10); // Top 10 most recent
        
        // Convert to activity feed format
        const activities = recentAchievements.map(achievement => ({
            id: `telegram-${achievement.id}`,
            source: 'Telegram',
            title: getAchievementTitle(achievement),
            description: achievement.description || 'Telegram activity',
            timestamp: formatTimeAgo(achievement.createdAt),
            valueChange: getRewardDisplay(achievement),
            providerType: 'EthereumOASIS' // Or SolanaOASIS
        }));
        
        return activities;
    } catch (error) {
        console.error('Error loading Telegram activity:', error);
        return [];
    }
}

function getAchievementTitle(achievement) {
    const type = achievement.type || 'activity';
    const titles = {
        'mention': 'Mentioned OASIS',
        'link_share': 'Shared OASIS link',
        'quality_post': 'Created quality content',
        'helpful_answer': 'Helped community member',
        'code_example': 'Shared code example',
        'daily_active': 'Daily active bonus',
        'weekly_active': 'Weekly active bonus',
        'invite': 'Invited new member',
        'viral': 'Created viral content',
        'tutorial': 'Created tutorial'
    };
    return titles[type] || 'Telegram activity';
}

function getRewardDisplay(achievement) {
    const parts = [];
    if (achievement.karmaReward > 0) {
        parts.push(`+${achievement.karmaReward} karma`);
    }
    if (achievement.tokenReward > 0) {
        parts.push(`+${achievement.tokenReward} tokens`);
    }
    if (achievement.nftReward) {
        parts.push('+1 NFT');
    }
    return parts.join(', ') || '+5 karma';
}
```

### 5.2 Update Portal Stats

**Add Telegram-specific stats to dashboard**:

```javascript
async function loadTelegramStats(avatarId) {
    try {
        // Get Telegram avatar link
        const telegramLink = await oasisAPI.request(
            `/api/telegram/avatar/oasis/${avatarId}`
        );
        
        if (telegramLink.isError || !telegramLink.result) {
            return null; // User not linked to Telegram
        }
        
        // Get achievements
        const achievements = await oasisAPI.request(
            `/api/telegram/achievements/user/${avatarId}`
        );
        
        const allAchievements = achievements.result || [];
        const completed = allAchievements.filter(a => 
            a.status === 'Completed').length;
        const active = allAchievements.filter(a => 
            a.status === 'Active').length;
        
        // Calculate total rewards
        const totalKarma = allAchievements.reduce((sum, a) => 
            sum + (a.karmaReward || 0), 0);
        const totalTokens = allAchievements.reduce((sum, a) => 
            sum + (a.tokenReward || 0), 0);
        const nftCount = allAchievements.filter(a => 
            a.nftReward).length;
        
        return {
            telegramLinked: true,
            telegramUsername: telegramLink.result.telegramUsername,
            totalAchievements: allAchievements.length,
            completedAchievements: completed,
            activeAchievements: active,
            totalKarmaEarned: totalKarma,
            totalTokensEarned: totalTokens,
            nftsEarned: nftCount,
            groupsJoined: telegramLink.result.groupIds?.length || 0
        };
    } catch (error) {
        console.error('Error loading Telegram stats:', error);
        return null;
    }
}
```

### 5.3 Add Telegram Tab to Portal

**Update `portal.html`** to add Telegram-specific section:

```html
<!-- Add to portal tabs -->
<button class="portal-tab" data-tab="telegram" onclick="switchTab('telegram')">
    Telegram
</button>

<!-- Add Telegram tab content -->
<div id="tab-telegram" class="portal-tab-content" style="display: none;">
    <div class="portal-section">
        <div class="portal-section-header">
            <div>
                <h2 class="portal-section-title">Telegram Rewards</h2>
                <p class="portal-section-subtitle">Your contributions to the OASIS Telegram community</p>
            </div>
        </div>
        
        <!-- Telegram Stats -->
        <div class="stats-grid" id="telegramStats">
            <!-- Loaded dynamically -->
        </div>
        
        <!-- Recent Achievements -->
        <div class="portal-card">
            <div class="portal-card-header">
                <div class="portal-card-title">Recent Achievements</div>
                <a href="#" class="portal-card-link">View all</a>
            </div>
            <div id="telegramAchievements">
                <!-- Loaded dynamically -->
            </div>
        </div>
        
        <!-- Link Telegram Account -->
        <div class="portal-card" id="telegramLinkCard" style="display: none;">
            <div class="portal-card-header">
                <div class="portal-card-title">Link Telegram Account</div>
            </div>
            <div class="portal-card-content">
                <p>Connect your Telegram account to start earning rewards for promoting OASIS!</p>
                <button class="btn-primary" onclick="linkTelegramAccount()">
                    Link Telegram Account
                </button>
            </div>
        </div>
    </div>
</div>
```

---

## 6. Real-Time Notification System

### 6.1 Telegram Bot Notifications

**Notify users when they earn rewards**:

```csharp
public async Task NotifyRewardAsync(
    long telegramId, 
    RewardAction reward)
{
    var message = $"🎉 Reward Earned!\n\n" +
                  $"{reward.Description}\n" +
                  $"Karma: +{reward.Karma}\n";
    
    if (reward.Tokens > 0)
    {
        message += $"Tokens: +{reward.Tokens}\n";
    }
    
    if (reward.NFT)
    {
        message += $"NFT: {reward.NFTTitle}\n";
    }
    
    message += $"\nView in portal: https://portal.oasisplatform.world";
    
    await _telegramProvider.SendMessageAsync(telegramId, message);
}
```

### 6.2 Portal Notifications

**Show real-time notifications in portal**:

```javascript
// WebSocket or polling for real-time updates
async function checkForNewRewards(avatarId) {
    const lastCheck = localStorage.getItem('lastRewardCheck') || 0;
    
    const response = await oasisAPI.request(
        `/api/telegram/achievements/user/${avatarId}?since=${lastCheck}`
    );
    
    const newAchievements = response.result || [];
    
    if (newAchievements.length > 0) {
        // Show notification
        showRewardNotification(newAchievements);
        
        // Update last check time
        localStorage.setItem('lastRewardCheck', Date.now());
        
        // Refresh dashboard
        await loadAvatarDashboard();
    }
}

function showRewardNotification(achievements) {
    achievements.forEach(achievement => {
        const notification = document.createElement('div');
        notification.className = 'reward-notification';
        notification.innerHTML = `
            <div class="reward-notification-content">
                <div class="reward-notification-icon">🎉</div>
                <div class="reward-notification-text">
                    <strong>Reward Earned!</strong><br>
                    ${getAchievementTitle(achievement)}<br>
                    ${getRewardDisplay(achievement)}
                </div>
            </div>
        `;
        
        document.body.appendChild(notification);
        
        // Auto-remove after 5 seconds
        setTimeout(() => {
            notification.remove();
        }, 5000);
    });
}
```

---

## 7. Achievement Types & NFT Designs

### 7.1 Achievement Categories

#### **Bronze Tier Achievements**
- **OASIS Mentioner**: Mention OASIS 10 times → Bronze Badge NFT
- **Link Sharer**: Share OASIS links 5 times → Link Master NFT
- **Daily Active**: 7 consecutive days → Daily Streak NFT

#### **Silver Tier Achievements**
- **Quality Contributor**: 10 quality posts → Quality Creator NFT
- **Community Helper**: Answer 20 questions → Helper NFT
- **Weekly Champion**: 4 weeks active → Weekly Champion NFT

#### **Gold Tier Achievements**
- **Tutorial Creator**: Create tutorial → Tutorial Master NFT
- **Viral Creator**: Post gets 100+ reactions → Viral Creator NFT
- **Code Contributor**: Share code examples → Developer NFT

#### **Platinum Tier Achievements**
- **Community Builder**: Invite 10 members → Builder NFT
- **Moderator**: Become group moderator → Moderator NFT
- **OASIS Ambassador**: 100+ total achievements → Ambassador NFT

### 7.2 NFT Metadata Structure

```json
{
  "name": "OASIS Tutorial Creator",
  "description": "Awarded for creating a quality tutorial about OASIS",
  "image": "https://ipfs.io/ipfs/QmTutorialNFTImage",
  "attributes": [
    {
      "trait_type": "Achievement Type",
      "value": "Tutorial Creator"
    },
    {
      "trait_type": "Tier",
      "value": "Gold"
    },
    {
      "trait_type": "Source",
      "value": "Telegram Gamification"
    },
    {
      "trait_type": "Earned Date",
      "value": "2025-01-15"
    },
    {
      "trait_type": "Karma Earned",
      "value": 100
    }
  ],
  "external_url": "https://portal.oasisplatform.world/telegram"
}
```

---

## 8. Implementation Phases

### Phase 1: Basic Detection (Week 1-2)
- [ ] Extend `TelegramBotService` with message monitoring
- [ ] Implement keyword detection (OASIS mentions)
- [ ] Implement link detection
- [ ] Basic karma rewards
- [ ] Display in portal

### Phase 2: Engagement Tracking (Week 3-4)
- [ ] Daily active tracking
- [ ] Weekly bonus system
- [ ] Quality content detection
- [ ] Token reward distribution
- [ ] Portal stats integration

### Phase 3: Advanced Features (Week 5-6)
- [ ] NFT minting for achievements
- [ ] Referral tracking
- [ ] Viral content detection
- [ ] Leaderboard system
- [ ] Real-time notifications

### Phase 4: AI Enhancement (Week 7-8)
- [ ] AI-powered content quality analysis
- [ ] Sentiment analysis for positive mentions
- [ ] Automatic achievement verification
- [ ] Smart reward scaling

---

## 9. API Endpoints Needed

### 9.1 New Telegram Gamification Endpoints

```csharp
// Get user's Telegram gamification stats
GET /api/telegram/gamification/stats/{avatarId}

Response: {
    telegramLinked: bool,
    telegramUsername: string,
    totalKarmaEarned: int,
    totalTokensEarned: decimal,
    nftsEarned: int,
    achievementsCompleted: int,
    achievementsActive: int,
    groupsJoined: int,
    dailyStreak: int,
    weeklyActive: bool
}

// Get recent rewards
GET /api/telegram/gamification/rewards/{avatarId}?limit=20

Response: {
    rewards: [
        {
            id: string,
            type: string,
            description: string,
            karmaAwarded: int,
            tokensAwarded: decimal,
            nftAwarded: bool,
            timestamp: DateTime,
            source: "Telegram"
        }
    ]
}

// Get leaderboard
GET /api/telegram/gamification/leaderboard?group={groupId}&period=weekly

Response: {
    leaderboard: [
        {
            rank: int,
            avatarId: Guid,
            username: string,
            karma: int,
            tokens: decimal,
            achievements: int
        }
    ]
}

// Link Telegram account from portal
POST /api/telegram/link-from-portal

Body: {
    avatarId: Guid,
    telegramVerificationCode: string  // Sent via bot
}

// Get link code for portal
GET /api/telegram/link-code/{avatarId}

Response: {
    verificationCode: string,
    instructions: string
}
```

---

## 10. Portal UI Components

### 10.1 Telegram Stats Card

```html
<div class="telegram-stats-card">
    <div class="telegram-stats-header">
        <h3>Telegram Activity</h3>
        <span class="telegram-status-badge" id="telegramStatus">
            Not Linked
        </span>
    </div>
    
    <div class="telegram-stats-grid">
        <div class="telegram-stat">
            <div class="stat-label">Karma Earned</div>
            <div class="stat-value" id="telegramKarma">0</div>
        </div>
        <div class="telegram-stat">
            <div class="stat-label">Tokens Earned</div>
            <div class="stat-value" id="telegramTokens">0</div>
        </div>
        <div class="telegram-stat">
            <div class="stat-label">NFTs Earned</div>
            <div class="stat-value" id="telegramNFTs">0</div>
        </div>
        <div class="telegram-stat">
            <div class="stat-label">Daily Streak</div>
            <div class="stat-value" id="telegramStreak">0 days</div>
        </div>
    </div>
</div>
```

### 10.2 Achievement Badges Display

```html
<div class="telegram-achievements-grid">
    <div class="achievement-badge bronze">
        <div class="badge-icon">🥉</div>
        <div class="badge-title">OASIS Mentioner</div>
        <div class="badge-progress">8/10 mentions</div>
    </div>
    <div class="achievement-badge silver">
        <div class="badge-icon">🥈</div>
        <div class="badge-title">Quality Contributor</div>
        <div class="badge-progress">7/10 posts</div>
    </div>
    <!-- More badges -->
</div>
```

---

## 11. Security & Anti-Abuse

### 11.1 Rate Limiting

```csharp
// Prevent spam rewards
private readonly Dictionary<long, DateTime> _lastRewardTime = new();
private const int MIN_REWARD_INTERVAL_SECONDS = 60; // 1 minute between rewards

public async Task<bool> CanAwardRewardAsync(long telegramId)
{
    if (_lastRewardTime.ContainsKey(telegramId))
    {
        var timeSinceLastReward = DateTime.UtcNow - _lastRewardTime[telegramId];
        if (timeSinceLastReward.TotalSeconds < MIN_REWARD_INTERVAL_SECONDS)
        {
            return false; // Too soon
        }
    }
    
    _lastRewardTime[telegramId] = DateTime.UtcNow;
    return true;
}
```

### 11.2 Content Validation

```csharp
// Validate content quality
private bool IsValidContent(string text)
{
    // Minimum length
    if (text.Length < 20)
        return false;
    
    // Check for spam patterns
    if (IsSpamPattern(text))
        return false;
    
    // Check for meaningful content
    if (!HasMeaningfulContent(text))
        return false;
    
    return true;
}

private bool IsSpamPattern(string text)
{
    // Check for repeated characters
    if (text.Count(c => c == text[0]) > text.Length * 0.5)
        return true;
    
    // Check for excessive caps
    if (text.Count(char.IsUpper) > text.Length * 0.7)
        return true;
    
    return false;
}
```

### 11.3 Admin Verification

```csharp
// For high-value rewards, require admin verification
public async Task<bool> RequiresVerification(RewardAction reward)
{
    return reward.Tokens > 5.0m || reward.NFT || reward.Karma > 100;
}

// Admin can verify achievements
[HttpPost("admin/verify-achievement")]
public async Task VerifyAchievementAsync(string achievementId, bool approved)
{
    // Only admins can call this
    if (!IsAdmin(Request))
        return Unauthorized();
    
    await _telegramProvider.UpdateAchievementStatusAsync(
        achievementId, 
        approved ? AchievementStatus.Completed : AchievementStatus.Failed
    );
}
```

---

## 12. Analytics & Reporting

### 12.1 Gamification Metrics

Track:
- Total rewards distributed
- Most active users
- Most effective content types
- Conversion rates (Telegram → Portal)
- Token/NFT distribution

### 12.2 Dashboard for Admins

```html
<!-- Admin Telegram Gamification Dashboard -->
<div class="admin-telegram-dashboard">
    <h2>Telegram Gamification Analytics</h2>
    
    <div class="metrics-grid">
        <div class="metric-card">
            <div class="metric-label">Total Rewards Distributed</div>
            <div class="metric-value" id="totalRewards">0</div>
        </div>
        <div class="metric-card">
            <div class="metric-label">Active Users</div>
            <div class="metric-value" id="activeUsers">0</div>
        </div>
        <div class="metric-card">
            <div class="metric-label">Tokens Distributed</div>
            <div class="metric-value" id="tokensDistributed">0</div>
        </div>
        <div class="metric-card">
            <div class="metric-label">NFTs Minted</div>
            <div class="metric-value" id="nftsMinted">0</div>
        </div>
    </div>
    
    <!-- Top performers -->
    <div class="leaderboard-section">
        <h3>Top Performers (This Week)</h3>
        <div id="topPerformers">
            <!-- Loaded dynamically -->
        </div>
    </div>
</div>
```

---

## 13. Integration Checklist

### Backend (TelegramOASIS Provider)
- [ ] Extend `TelegramBotService` with message monitoring
- [ ] Create `TelegramGamificationService`
- [ ] Implement reward detection logic
- [ ] Add token distribution service
- [ ] Add NFT minting integration
- [ ] Create new API endpoints
- [ ] Add rate limiting
- [ ] Add content validation
- [ ] Implement daily/weekly tracking
- [ ] Add admin verification system

### Portal Integration
- [ ] Add Telegram tab to portal
- [ ] Create Telegram stats display
- [ ] Load Telegram achievements
- [ ] Show reward notifications
- [ ] Display achievement badges
- [ ] Add Telegram account linking UI
- [ ] Show leaderboard
- [ ] Real-time updates (polling/WebSocket)

### Testing
- [ ] Test keyword detection
- [ ] Test link detection
- [ ] Test karma rewards
- [ ] Test token distribution
- [ ] Test NFT minting
- [ ] Test portal display
- [ ] Test rate limiting
- [ ] Test anti-abuse measures

---

## 14. Example User Flow

### 14.1 New User Journey

1. **User joins Telegram group**
   - Bot sends welcome message
   - User runs `/start` command
   - Bot creates OASIS avatar and links accounts

2. **User mentions OASIS**
   - Bot detects keyword: "OASIS"
   - Awards +5 karma immediately
   - Sends notification: "🎉 +5 karma for mentioning OASIS!"

3. **User shares OASIS link**
   - Bot detects URL: `oasisplatform.world`
   - Awards +10 karma + 0.1 tokens
   - Sends notification with reward details

4. **User creates quality post**
   - Bot analyzes content (length, keywords, structure)
   - Awards +30 karma + 1 token
   - Sends notification

5. **User checks portal**
   - Opens portal → Telegram tab
   - Sees all rewards displayed
   - Views achievement progress
   - Sees NFTs earned

6. **User earns NFT**
   - Completes achievement (e.g., 10 quality posts)
   - Bot mints NFT automatically
   - Transfers to user's Solana wallet
   - Sends notification with NFT details
   - NFT appears in portal immediately

---

## 15. Configuration

### 15.1 Reward Configuration

```json
{
  "TelegramGamification": {
    "Rewards": {
      "MentionOASIS": {
        "Karma": 5,
        "Tokens": 0,
        "CooldownSeconds": 60
      },
      "ShareLink": {
        "Karma": 10,
        "Tokens": 0.1,
        "CooldownSeconds": 300
      },
      "QualityPost": {
        "Karma": 30,
        "Tokens": 1.0,
        "MinLength": 200,
        "RequiredKeywords": 2
      },
      "HelpfulAnswer": {
        "Karma": 15,
        "Tokens": 0.3,
        "CooldownSeconds": 300
      },
      "CodeExample": {
        "Karma": 50,
        "Tokens": 2.0,
        "NFT": "CodeContributor"
      },
      "DailyActive": {
        "Karma": 10,
        "Tokens": 0,
        "RequiredMessages": 3
      },
      "WeeklyActive": {
        "Karma": 25,
        "Tokens": 0.5,
        "RequiredDays": 5
      },
      "ViralContent": {
        "Karma": 200,
        "Tokens": 10.0,
        "NFT": "ViralCreator",
        "RequiredReactions": 100
      }
    },
    "NFTs": {
      "TutorialCreator": {
        "Title": "OASIS Tutorial Creator",
        "ImageUrl": "https://ipfs.io/ipfs/...",
        "Requirements": {
          "Type": "tutorial",
          "MinLength": 1000,
          "RequiresCode": true
        }
      },
      "ViralCreator": {
        "Title": "OASIS Viral Content Creator",
        "ImageUrl": "https://ipfs.io/ipfs/...",
        "Requirements": {
          "Reactions": 100
        }
      }
    },
    "Treasury": {
      "WalletAddress": "YOUR_SOLANA_WALLET",
      "TokenMint": "YOUR_SPL_TOKEN_MINT",
      "TokenSymbol": "OASIS"
    }
  }
}
```

---

## 16. Next Steps

1. **Implement Message Monitoring**
   - Extend `TelegramBotService` to analyze all messages
   - Add keyword/link detection
   - Implement reward logic

2. **Create Reward Service**
   - Build `TelegramGamificationService`
   - Integrate with `AchievementManager`
   - Connect to token distribution
   - Connect to NFT minting

3. **Portal Integration**
   - Add Telegram tab
   - Display rewards and achievements
   - Show real-time updates
   - Add account linking UI

4. **Testing & Refinement**
   - Test with real Telegram group
   - Refine reward amounts
   - Adjust detection algorithms
   - Optimize performance

5. **Launch & Monitor**
   - Deploy to production
   - Monitor reward distribution
   - Track user engagement
   - Iterate based on data

---

## 17. Success Metrics

Track these KPIs:
- **Engagement**: Messages per user per day
- **Rewards**: Total karma/tokens/NFTs distributed
- **Portal Visits**: Telegram users visiting portal
- **Retention**: Users active 7+ days
- **Growth**: New members from referrals
- **Content Quality**: Average karma per message

---

This system creates a complete gamification loop: Telegram activity → Rewards → Portal display → More engagement → More rewards!
