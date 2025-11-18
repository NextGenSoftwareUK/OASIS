# OASIS Telegram Bot - Overview & Use Cases

## 📖 Table of Contents
1. [What is the OASIS Telegram Bot?](#what-is-the-oasis-telegram-bot)
2. [How It Works](#how-it-works)
3. [Core Features](#core-features)
4. [Architecture](#architecture)
5. [Use Cases](#use-cases)
6. [Getting Started](#getting-started)
7. [Command Reference](#command-reference)
8. [Example Scenarios](#example-scenarios)
9. [Integration Possibilities](#integration-possibilities)

---

## What is the OASIS Telegram Bot?

The OASIS Telegram Bot is a social accountability and achievement tracking system that connects Telegram users with the OASIS (Open Application Standard for Interoperability and Scalability) platform. It enables users to:

- **Create and join accountability groups** for shared goals
- **Track progress and achievements** through check-ins
- **Earn rewards** (karma points and tokens) for completing goals
- **Mint NFT badges** for achievements on Solana blockchain
- **Build reputation** that's portable across the entire OASIS ecosystem

**Key Innovation**: Unlike traditional bots that operate in silos, this bot creates a unified identity that works across multiple platforms and blockchains through OASIS's provider architecture.

---

## How It Works

### 🔄 User Flow

```
1. User sends /start to bot
   ↓
2. Bot creates OASIS avatar (unified identity)
   ↓
3. Telegram ID ←→ OASIS Avatar mapping stored
   ↓
4. User creates or joins groups
   ↓
5. User sets goals and checks in regularly
   ↓
6. Bot awards karma + tokens for progress
   ↓
7. User mints NFT badges for achievements
   ↓
8. Reputation and data portable to other OASIS apps
```

### 🏗️ Technical Architecture

```
┌─────────────────────────────────────────────────────┐
│              Telegram Users                         │
│         (Mobile/Desktop/Web Telegram)               │
└────────────────────┬────────────────────────────────┘
                     │ Messages & Commands
                     ▼
┌─────────────────────────────────────────────────────┐
│            TelegramBotService                       │
│     • Receives messages (polling/webhook)           │
│     • Routes commands to handlers                   │
│     • Sends responses                               │
└────────────────────┬────────────────────────────────┘
                     │
        ┌────────────┼────────────┐
        ▼            ▼            ▼
┌───────────┐  ┌──────────┐  ┌──────────────┐
│ Telegram  │  │ Avatar   │  │  NFT         │
│ OASIS     │  │ Manager  │  │  Service     │
│ Provider  │  │          │  │              │
└─────┬─────┘  └────┬─────┘  └──────┬───────┘
      │             │                │
      ▼             ▼                ▼
┌──────────┐  ┌──────────┐  ┌──────────────┐
│ MongoDB  │  │  OASIS   │  │   Solana     │
│ (Social  │  │  Core    │  │  Blockchain  │
│  Data)   │  │  (Karma) │  │  (NFTs/Token)│
└──────────┘  └──────────┘  └──────────────┘
```

### 🔐 Data Model

**TelegramAvatar** (Linking Record)
```javascript
{
  id: "unique_id",
  telegramId: 123456789,
  telegramUsername: "@john_doe",
  firstName: "John",
  lastName: "Doe",
  oasisAvatarId: "uuid-of-oasis-avatar",
  groupIds: ["group1", "group2"],
  lastInteractionAt: "2025-10-27T10:00:00Z"
}
```

**TelegramGroup** (Accountability Group)
```javascript
{
  id: "group_id",
  name: "Fitness Warriors",
  description: "30-day fitness challenge",
  createdBy: "uuid",
  memberIds: [123456789, 987654321],
  telegramChatId: -1001234567890,
  createdAt: "2025-10-27T10:00:00Z"
}
```

**Achievement** (Goal/Milestone)
```javascript
{
  id: "achievement_id",
  userId: "uuid",
  telegramUserId: 123456789,
  groupId: "group_id",
  description: "Complete 30 workouts",
  type: "Manual" | "Automated",
  status: "Active" | "Completed" | "Failed",
  karmaReward: 50,
  tokenReward: 10.5,
  deadline: "2025-11-27T10:00:00Z",
  completedAt: null,
  checkins: [
    {
      message: "Completed day 1!",
      karmaAwarded: 10,
      timestamp: "2025-10-27T10:00:00Z"
    }
  ]
}
```

---

## Core Features

### 1. **Identity & Account Linking**

- **One-Time Setup**: Users send `/start` once to create their OASIS avatar
- **Automatic Linking**: Telegram ID permanently linked to OASIS identity
- **Cross-Platform**: Same identity works across all OASIS apps (not just Telegram)
- **Privacy**: Users control what data is shared

**How it works**:
```
/start → Check if user exists → If not, create OASIS avatar → Link Telegram ID → Welcome message
```

### 2. **Accountability Groups**

- **Create Groups**: `/creategroup Fitness Warriors`
- **Public Groups**: Anyone with group ID can join
- **Group Chat Integration**: Bot works in group chats and DMs
- **Member Management**: Track who's in each group
- **Activity Feed**: See group progress and leaderboards

**How it works**:
```
/creategroup Name → Creates group in DB → Returns group ID → Members join with /joingroup ID
```

### 3. **Goal Setting & Tracking**

- **Personal Goals**: `/setgoal Complete 5 workouts this week`
- **Group Goals**: Set collective targets
- **Deadlines**: Auto-set (1 week default) or custom
- **Progress Tracking**: Check in regularly with updates
- **Status Management**: Active, Completed, Failed

**How it works**:
```
/setgoal Description → Create achievement record → Set deadline → Track check-ins → Award rewards on completion
```

### 4. **Check-Ins & Progress Updates**

- **Daily Check-Ins**: `/checkin Completed 30-min workout! 💪`
- **Karma Rewards**: Earn 10 karma per check-in
- **Streak Tracking**: Build consecutive day streaks
- **Group Visibility**: Updates visible to group members
- **Encouragement**: Bot responds with stats and motivation

**How it works**:
```
/checkin Message → Validate user & group → Create check-in record → Award karma → Update stats → Send confirmation
```

### 5. **Rewards System**

**Two-Tier Rewards**:

| Action | Karma | Tokens | Notes |
|--------|-------|--------|-------|
| Check-in | 10 | 0 | Daily progress |
| Set Goal | 0 | 0 | Creates accountability |
| Complete Goal | 50 | 5 | Milestone reward |
| Mint NFT | 50 | 0 | Bonus for on-chain badge |
| Custom Badge NFT | 100 | 0 | Photo + metadata |

**Karma**: OASIS-native reputation points (stored in avatar)
**Tokens**: Solana SPL tokens with real economic value

### 6. **NFT Badge Minting**

**Text-Based NFTs**:
```
/mintnft 7vXZK6SQ4z... | Achievement Badge | Completed 30-day challenge!
```

**Image-Based NFTs**:
```
1. Take/select photo
2. Add caption: wallet | title | description
3. Send to bot
4. Bot uploads to IPFS via Pinata
5. Mints NFT on Solana with custom image
```

**Features**:
- Permanent on-chain record
- Custom images stored on IPFS
- Sent to user's Solana wallet
- Can be traded, displayed, or kept as memorabilia

### 7. **Statistics & Leaderboards**

- **Personal Stats**: `/mystats` shows karma, tokens, achievements, streaks
- **Group Leaderboards**: `/leaderboard` shows rankings
- **Progress Visualization**: See completion rates and activity
- **Comparative Analytics**: Compare your progress to group average

---

## Use Cases

### 🏋️ **1. Fitness & Health Accountability**

**Scenario**: A group of friends wants to work out consistently for 30 days.

**Implementation**:
1. Create group: `/creategroup 30-Day Fitness Challenge`
2. Share group ID with friends
3. Each member sets goal: `/setgoal Complete 30 workouts in 30 days`
4. Daily check-ins: `/checkin Completed leg day! 🦵`
5. Admin verifies milestones using photos/videos
6. Rewards distributed automatically
7. NFT badge minted for completers

**Benefits**:
- Social accountability keeps members motivated
- Visible progress builds momentum
- Token rewards create stakes
- NFT badge provides lasting recognition

### 📚 **2. Learning & Skill Development**

**Scenario**: Language learners practicing daily on Duolingo.

**Implementation**:
1. Create group: `/creategroup Spanish Fluency Squad`
2. Set individual goals: `/setgoal Complete 30 lessons this month`
3. Check in with screenshots: Photo + caption showing daily progress
4. Track streaks and consistency
5. Mint certificate NFT upon course completion

**Benefits**:
- Group learning more engaging than solo
- Streaks prevent falling off
- Progress visible to peers
- Portable credentials (NFTs) prove skill level

### 💼 **3. Professional Development**

**Scenario**: Developers committing to daily coding practice.

**Implementation**:
1. Create group: `/creategroup #100DaysOfCode`
2. Daily check-ins with GitHub commits
3. Weekly goals: `/setgoal Build and deploy 1 project per week`
4. Peer code reviews in group chat
5. Milestone NFTs for 30/60/100 day completion

**Benefits**:
- Builds consistent coding habit
- Portfolio grows steadily
- NFT credentials for job applications
- Network with fellow developers

### 💰 **4. Savings & Financial Goals**

**Scenario**: Friends saving for a group trip.

**Implementation**:
1. Create group: `/creategroup Euro Trip 2026 Savings`
2. Set savings goals: `/setgoal Save $200/month`
3. Weekly check-ins with bank balance screenshots (or self-reported)
4. Group tracks collective progress
5. Token rewards can go toward trip expenses

**Benefits**:
- Social pressure to save consistently
- Gamification makes saving fun
- Tokens have real value
- Group reaches goal faster together

### 🎨 **5. Creative Challenges**

**Scenario**: Artists doing daily drawing practice.

**Implementation**:
1. Create group: `/creategroup Inktober 2025`
2. Daily submissions: Photo of artwork + caption
3. Each submission auto-mints NFT
4. Build portfolio of 31 NFT artworks
5. Karma determines rankings

**Benefits**:
- Daily practice improves skills
- NFT portfolio showcases growth
- Potential to sell NFTs later
- Community feedback and support

### 🌱 **6. Habit Formation**

**Scenario**: Building multiple positive habits (meditation, reading, exercise).

**Implementation**:
1. Create private group (just yourself) or small accountability pod
2. Set multiple goals: `/setgoal Meditate 10 min daily`
3. Check in for each habit: `/checkin Day 5 meditation ✓`
4. Track streak across all habits
5. Milestone NFTs for 30/60/90 day streaks

**Benefits**:
- Multiple habit tracking in one place
- Streaks are motivating
- No app fatigue (uses Telegram you already have)
- Portable data if you switch apps

### 🏢 **7. Corporate Wellness Programs**

**Scenario**: Company wants to improve employee health.

**Implementation**:
1. HR creates department groups
2. Monthly challenges (steps, water intake, sleep)
3. Employees check in daily
4. Top performers get token bonuses
5. Tokens redeemable for PTO or perks

**Benefits**:
- Fun, non-intrusive wellness program
- Data-driven insights for HR
- Rewards tied to real value
- Cross-department engagement

### 🎓 **8. Educational Institutions**

**Scenario**: University course with project milestones.

**Implementation**:
1. Professor creates course group
2. Students join with `/joingroup`
3. Milestones: `/checkin Completed assignment 1`
4. Peer verification in group
5. Completion NFTs serve as micro-credentials

**Benefits**:
- Transparent progress tracking
- Peer accountability reduces procrastination
- NFT credentials supplement traditional grades
- Portable achievements for resumes

### 🌍 **9. Social Impact & Volunteering**

**Scenario**: Community service group tracking volunteer hours.

**Implementation**:
1. Create group: `/creategroup Community Helpers`
2. Set goals: `/setgoal Volunteer 10 hours this month`
3. Check in after each session with photos
4. Admin verifies and approves
5. NFT badges for cumulative hour milestones (50, 100, 500 hours)

**Benefits**:
- Verifiable volunteer history
- Recognition for community service
- NFT badges usable for college apps, resumes
- Encourages continued involvement

### 🎮 **10. Gaming Guilds & eSports**

**Scenario**: Gaming team coordinating practice and tournaments.

**Implementation**:
1. Create guild group: `/creategroup Apex Legends Squad`
2. Daily practice goals: `/setgoal Play 3 ranked matches`
3. Tournament check-ins with results
4. Token rewards for wins
5. Championship NFTs for tournament victories

**Benefits**:
- Team coordination via familiar platform (Telegram)
- Rewards for dedication
- Trophy case of NFT achievements
- Potential to monetize success

---

## Getting Started

### For Users

**Step 1: Start the Bot**
```
Open Telegram → Search for @YourOASISBot → Send /start
```

**Step 2: Create or Join a Group**
```
Create: /creategroup My Goal Group
Join: /joingroup [group_id_from_friend]
```

**Step 3: Set Your Goal**
```
/setgoal Complete 30 workouts in 30 days
```

**Step 4: Check In Daily**
```
/checkin Completed workout #1! Feeling great! 💪
```

**Step 5: Track Progress**
```
/mystats → See your karma, tokens, and achievements
```

**Step 6: Mint Your Badge**
```
/mintnft [your_solana_wallet] | Fitness Champion | Completed 30-day challenge!
```

### For Developers

**Requirements**:
- Telegram Bot Token (from @BotFather)
- MongoDB instance
- OASIS API running
- Solana wallet (for token distribution)
- Pinata API key (for NFT images)

**Configuration** (OASIS_DNA.json):
```json
{
  "TelegramOASIS": {
    "BotToken": "YOUR_BOT_TOKEN",
    "WebhookUrl": "https://your-domain.com/api/telegram/webhook",
    "MongoConnectionString": "mongodb+srv://...",
    "TreasuryWalletAddress": "YOUR_SOLANA_WALLET",
    "RewardTokenMintAddress": "TOKEN_MINT_ADDRESS",
    "RewardTokenSymbol": "REWARD",
    "SolanaCluster": "devnet"
  }
}
```

**Startup**:
```csharp
// In Program.cs or Startup.cs
var telegramProvider = new TelegramOASIS(
    botToken: Configuration["TelegramOASIS:BotToken"],
    webhookUrl: Configuration["TelegramOASIS:WebhookUrl"],
    mongoConnectionString: Configuration["TelegramOASIS:MongoConnectionString"]
);

await telegramProvider.ActivateProviderAsync();
services.AddSingleton(telegramProvider);

var botService = new TelegramBotService(
    Configuration["TelegramOASIS:BotToken"],
    telegramProvider,
    avatarManager,
    logger,
    nftService,
    pinataService
);

botService.StartReceiving();
```

---

## Command Reference

### Account Commands
| Command | Description | Example |
|---------|-------------|---------|
| `/start` | Link Telegram to OASIS | `/start` |
| `/mystats` | View your karma, tokens, achievements | `/mystats` |
| `/help` | Show all commands | `/help` |

### Group Commands
| Command | Description | Example |
|---------|-------------|---------|
| `/creategroup <name>` | Create new accountability group | `/creategroup Fitness Warriors` |
| `/joingroup <id>` | Join existing group | `/joingroup abc123xyz` |
| `/mygroups` | List your groups | `/mygroups` |
| `/leaderboard [groupId]` | View group rankings | `/leaderboard` |

### Achievement Commands
| Command | Description | Example |
|---------|-------------|---------|
| `/setgoal <description>` | Create personal goal | `/setgoal Run 100 miles this month` |
| `/checkin <message>` | Log progress, earn karma | `/checkin Completed day 5! 🎉` |
| `/milestone <@user>` | Mark milestone complete (admin) | `/milestone @john_doe` |

### NFT Commands
| Command | Description | Example |
|---------|-------------|---------|
| `/mintnft <wallet> \| <title> \| <desc>` | Mint text NFT | `/mintnft 7vX... \| Badge \| Completed challenge` |
| Photo + Caption | Mint image NFT | Send photo with caption: `wallet \| title \| description` |

---

## Example Scenarios

### Example 1: Complete Onboarding Flow

```
User: /start

Bot: 🎉 Welcome to OASIS Accountability Bot!
     Your OASIS avatar has been created and linked to @john_doe.
     
     Getting Started:
     • Use /creategroup to start an accountability group
     • Use /joingroup to join an existing group
     • Use /checkin to log your progress and earn karma
     
     Type /help anytime to see all commands.

User: /creategroup Morning Meditation

Bot: ✅ Group created: Morning Meditation
     Group ID: g7x9k2m1
     
     Share this ID with friends so they can join using:
     /joingroup g7x9k2m1

User: /setgoal Meditate 10 minutes every morning for 30 days

Bot: 🎯 Goal Set!
     Meditate 10 minutes every morning for 30 days
     
     📅 Deadline: Nov 26, 2025
     🎁 Reward: 50 karma + 5 REWARD tokens
     
     Use /checkin to track your progress!

User: /checkin Day 1 complete! Feeling centered and calm.

Bot: ✅ Check-in recorded!
     Day 1 complete! Feeling centered and calm.
     
     🎯 +10 karma
     ⭐ Total: 10 karma
     📊 Check-ins: 1
     
     Keep it up! 🚀
```

### Example 2: Minting Achievement NFT

```
User: /mintnft 7vXZK6SQ4zGMc3P... | Meditation Master | Completed 30-day meditation challenge

Bot: 🎨 Minting your NFT...
     📝 Title: Meditation Master
     💭 Description: Completed 30-day meditation challenge
     💰 Wallet: 7vXZK6SQ...kxJs
     
     ⏳ This may take 30-90 seconds...

Bot: ✅ NFT Minted Successfully! 🎉
     🎨 Title: Meditation Master
     📝 Description: Completed 30-day meditation challenge
     💰 Sent to: 7vXZK6SQ...kxJs
     
     🔍 Check your Solana wallet!
     Your achievement is now on-chain! 🎊

Bot: ✨ Bonus: +50 Karma for minting an NFT!
```

### Example 3: Checking Group Stats

```
User: /mygroups

Bot: 📋 Your Groups:
     1. Morning Meditation - g7x9k2m1
     2. Fitness Warriors - h8w2n3p5
     3. Book Club - j9y4m6q7

User: /leaderboard g7x9k2m1

Bot: 🏆 Morning Meditation Leaderboard
     
     1. 👑 @alice - 450 karma | 15 check-ins
     2. 🥈 @bob - 380 karma | 12 check-ins
     3. 🥉 @you - 320 karma | 11 check-ins
     4. @dave - 280 karma | 9 check-ins
     5. @eve - 210 karma | 7 check-ins
     
     Keep pushing! You're doing great! 💪
```

---

## Integration Possibilities

### 🔗 **1. Cross-Platform Identity**

**OASIS enables unified identity across**:
- Telegram (current)
- Discord
- Slack  
- Twitter/X
- Facebook Messenger
- WhatsApp
- Native mobile apps
- Web dashboards

**Benefit**: Users can switch platforms without losing progress or starting over.

### ⛓️ **2. Multi-Chain Support**

**Current**: Solana (SPL tokens, NFTs)

**Future via OASIS Providers**:
- Ethereum (ERC-20, ERC-721)
- Polygon
- Arbitrum
- Base
- Optimism
- Avalanche
- Any blockchain with an OASIS provider

**Benefit**: Rewards and NFTs on user's preferred chain.

### 🔌 **3. External App Integrations**

**Fitness**:
- Strava (auto-verify workout check-ins)
- Apple Health / Google Fit
- MyFitnessPal

**Learning**:
- Duolingo (verify lesson completion)
- Coursera / Udemy
- GitHub (track coding activity)

**Productivity**:
- Todoist / Notion
- RescueTime
- Forest app

**Implementation**: Webhook verification or OAuth integration to auto-confirm achievements.

### 🏦 **4. DeFi Integration**

**Staking**:
- Stake tokens to join premium groups
- Higher stakes = higher rewards
- Slashing for missed commitments

**Yield Farming**:
- Rewards auto-deposited to yield vaults
- Compound earnings while achieving goals

**Governance**:
- Karma = voting power in group decisions
- DAO-style group management

### 🎮 **5. Gamification Layers**

**Experience Levels**:
- Bronze → Silver → Gold → Platinum tiers
- Based on cumulative karma
- Unlock features at higher tiers

**Achievements System**:
- "Early Bird" - 30-day streak
- "Team Player" - Help 10 others reach goals
- "Overachiever" - Complete 100 goals
- "Legend" - 365-day streak

**Quests & Challenges**:
- Time-limited challenges
- Special rewards for completion
- Seasonal events

### 📊 **6. Analytics & Insights**

**Personal Dashboard**:
- Goal completion rates
- Streak analytics
- Peer comparison
- Best performing goal types

**Group Analytics**:
- Member engagement scores
- Retention metrics
- Success predictors
- Optimal group size

### 🤝 **7. Enterprise Solutions**

**Corporate Wellness**:
- Custom branding
- Compliance tracking
- ROI analytics
- Integration with HR systems

**Educational Institutions**:
- Grade book integration
- Attendance tracking
- Parent notifications
- Student progress reports

### 🌐 **8. Web3 Integrations**

**NFT Marketplace**:
- Sell achievement NFTs
- Collections for major milestones
- Rarity tiers based on difficulty

**Token Utilities**:
- Redeem for coaching sessions
- Access premium groups
- Trade on DEXs
- Collateral for loans

**Social Tokens**:
- Create group-specific tokens
- Token-gated communities
- Member-owned economies

---

## Why OASIS Makes This Different

### Traditional Accountability Apps
❌ Siloed data (can't export)  
❌ Single platform only  
❌ No real ownership of achievements  
❌ Centralized control  
❌ Platform risk (app could shut down)  

### OASIS Telegram Bot
✅ **Portable data** - Export and move anytime  
✅ **Cross-platform** - Same identity everywhere  
✅ **True ownership** - NFTs prove achievements  
✅ **Decentralized** - Your data, your control  
✅ **Future-proof** - Built on open standards  

---

## Technical Benefits

### For Developers

**Modular Architecture**:
```
TelegramBotService (messaging layer)
    ↓
TelegramOASIS Provider (data layer)
    ↓
OASIS Core (universal identity & storage)
    ↓
MongoDB / Solana / Any blockchain
```

**Easy to Extend**:
- Add new commands by extending `HandleCommandAsync`
- New reward types via `AchievementManager`
- Additional blockchains through OASIS providers
- New platforms (Discord, etc.) reuse same logic

**Well-Structured Code**:
- Separation of concerns
- Dependency injection
- Async/await throughout
- Comprehensive error handling
- Logging at every step

---

## Conclusion

The OASIS Telegram Bot transforms Telegram into a powerful platform for social accountability, habit formation, and achievement tracking. By leveraging OASIS's unified identity system and multi-chain capabilities, it provides:

1. **Immediate value** - Works in Telegram users already use
2. **Real rewards** - Tokens and NFTs with actual worth
3. **Social proof** - On-chain credentials for achievements
4. **Flexibility** - Adaptable to countless use cases
5. **Future-ready** - Built on open, interoperable standards

Whether you're forming healthy habits, learning new skills, saving money, or building a community, the OASIS Telegram Bot provides the infrastructure for sustainable behavior change backed by social accountability and tangible rewards.

---

## Resources

- **Documentation**: See `TELEGRAM_INTEGRATION_SUMMARY.md` for technical details
- **Code**: `/NextGenSoftware.OASIS.API.Providers.TelegramOASIS/`
- **Bot Service**: `/ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI/Services/TelegramBotService.cs`
- **Controller**: `/ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI/Controllers/TelegramController.cs`

---

**Version**: 1.0  
**Last Updated**: October 27, 2025  
**Status**: Production Ready 🚀







