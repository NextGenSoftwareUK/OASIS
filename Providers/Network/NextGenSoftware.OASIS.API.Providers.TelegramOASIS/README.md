# TelegramOASIS Provider

## Overview

The TelegramOASIS Provider integrates Telegram messaging platform with the OASIS ecosystem, enabling:

- **Account Linking**: Connect Telegram accounts to OASIS avatars
- **Accountability Groups**: Create and manage group-based achievement tracking
- **Achievement System**: Set goals, track progress, and verify completion
- **Reward Distribution**: Automatically award karma points and Solana tokens
- **Social Features**: Check-ins, leaderboards, and group interactions

## Features

### 1. Telegram Bot Commands

- `/start` - Link Telegram account to OASIS avatar
- `/creategroup <name>` - Create a new accountability group
- `/joingroup <id>` - Join an existing group
- `/setgoal <description>` - Set a new goal/achievement
- `/checkin <message>` - Check in with progress update
- `/mystats` - View karma and achievement statistics
- `/mygroups` - List all groups you're a member of
- `/leaderboard` - View group leaderboard
- `/help` - Show available commands

### 2. API Endpoints

#### Avatar Linking
- `POST /api/telegram/link-avatar` - Link Telegram to OASIS
- `GET /api/telegram/avatar/telegram/{telegramId}` - Get by Telegram ID
- `GET /api/telegram/avatar/oasis/{oasisAvatarId}` - Get by OASIS ID

#### Group Management
- `POST /api/telegram/groups/create` - Create accountability group
- `GET /api/telegram/groups/{groupId}` - Get group details
- `POST /api/telegram/groups/join` - Join a group
- `GET /api/telegram/groups/user/{telegramUserId}` - Get user's groups

#### Achievement Tracking
- `POST /api/telegram/achievements/create` - Create achievement
- `POST /api/telegram/achievements/complete` - Complete achievement
- `GET /api/telegram/achievements/user/{userId}` - Get user achievements
- `GET /api/telegram/achievements/group/{groupId}` - Get group achievements
- `POST /api/telegram/achievements/checkin` - Add progress check-in

## Configuration

### OASIS_DNA.json

Add the following configuration to your `OASIS_DNA.json`:

```json
{
  "StorageProviders": {
    "TelegramOASIS": {
      "BotToken": "YOUR_TELEGRAM_BOT_TOKEN",
      "WebhookUrl": "https://your-domain.com/api/telegram/webhook",
      "MongoConnectionString": "mongodb://...",
      "TreasuryWalletAddress": "Your_Solana_Wallet_Address",
      "RewardTokenMintAddress": "SPL_Token_Mint_Address",
      "RewardTokenSymbol": "EXP",
      "SolanaCluster": "devnet"
    }
  }
}
```

### Environment Variables

Alternatively, you can use environment variables:

- `TELEGRAM_BOT_TOKEN` - Your Telegram bot token from @BotFather
- `TELEGRAM_WEBHOOK_URL` - Public URL for Telegram webhooks
- `TELEGRAM_MONGO_CONNECTION` - MongoDB connection string
- `SOLANA_TREASURY_WALLET` - Treasury wallet for token rewards
- `SOLANA_REWARD_TOKEN_MINT` - SPL token mint address

## Setup Instructions

### 1. Create Telegram Bot

1. Open Telegram and search for @BotFather
2. Send `/newbot` command
3. Follow prompts to name your bot
4. Copy the bot token provided
5. Update `OASIS_DNA.json` with the bot token

### 2. Configure MongoDB

TelegramOASIS uses MongoDB to store:
- Telegram → OASIS avatar mappings
- Group data and membership
- Achievement definitions and progress

The provider creates the following collections:
- `telegram_avatars`
- `telegram_groups`
- `achievements`

### 3. Set Up Solana Rewards (Optional)

For token rewards, you need:
1. Create an SPL token on Solana
2. Set up a treasury wallet with token supply
3. Configure wallet address and token mint in `OASIS_DNA.json`

### 4. Deploy and Test

```bash
# Start OASIS API
cd NextGenSoftware.OASIS.API.ONODE.WebAPI
dotnet run

# In another terminal, test the bot
# Open Telegram and search for your bot
# Send /start to link your account
```

## Architecture

```
┌─────────────────┐
│  Telegram Users │
└────────┬────────┘
         │ Bot Commands
         ▼
┌─────────────────────────┐
│   TelegramBotService    │
│   (Command Handler)     │
└────────┬────────────────┘
         │
         ▼
┌─────────────────────────┐     ┌──────────────────┐
│   TelegramOASIS         │────▶│   MongoDB        │
│   Provider              │     │   (Social Data)  │
└────────┬────────────────┘     └──────────────────┘
         │
         ├─────────────────┐
         │                 │
         ▼                 ▼
┌──────────────────┐  ┌────────────────────┐
│  AvatarManager   │  │ AchievementManager │
│  (OASIS Core)    │  │  (Rewards)         │
└──────────────────┘  └─────────┬──────────┘
                                │
                                ▼
                      ┌────────────────────┐
                      │   SolanaOASIS      │
                      │   (Token Rewards)  │
                      └────────────────────┘
```

## Data Models

### TelegramAvatar
Links Telegram users to OASIS avatars:
```csharp
{
  TelegramId: long,
  TelegramUsername: string,
  OasisAvatarId: Guid,
  LinkedAt: DateTime,
  GroupIds: List<string>
}
```

### TelegramGroup
Accountability group definition:
```csharp
{
  Id: string,
  Name: string,
  TelegramChatId: long,
  MemberIds: List<long>,
  AdminIds: List<long>,
  Rules: {
    KarmaPerCheckin: int,
    TokenPerMilestone: decimal,
    RequiredCheckinsPerWeek: int
  }
}
```

### Achievement
Goal/milestone tracking:
```csharp
{
  Id: string,
  GroupId: string,
  UserId: Guid,
  Description: string,
  Status: "Active" | "Completed" | "Failed",
  KarmaReward: int,
  TokenReward: decimal,
  Checkins: List<CheckIn>
}
```

## Use Cases

### 1. Fitness Accountability Group
```
1. Create group: /creategroup "30 Day Fitness Challenge"
2. Members join: /joingroup <id>
3. Set goal: /setgoal "Complete 30 workouts in 30 days"
4. Daily check-in: /checkin "Day 5: 45 min cardio ✅"
5. Auto-reward: +10 karma per check-in
6. Completion: +100 karma + 5 tokens
```

### 2. Learning Groups
```
1. Create group for course completion
2. Members set learning goals
3. Share progress via check-ins
4. Admins verify milestone completion
5. Rewards distributed automatically
```

### 3. Habit Tracking
```
1. Join accountability group
2. Set daily/weekly habits
3. Check in with evidence/updates
4. Track karma accumulation
5. Compete on leaderboard
```

## Extending the Integration

### Adding New Commands

Edit `TelegramBotService.cs`:

```csharp
case "/yourcommand":
    await HandleYourCommandAsync(chatId, userId, cancellationToken);
    break;
```

### Custom Reward Logic

Edit `AchievementManager.cs`:

```csharp
public async Task<OASISResult<int>> CalculateCustomReward(Achievement achievement)
{
    // Your custom logic here
}
```

### Integration with Other Platforms

The provider abstraction allows easy extension to:
- Discord
- Slack
- WhatsApp
- Any messaging platform with API

## Security Considerations

1. **Bot Token**: Keep secret, never commit to version control
2. **Treasury Wallet**: Use multi-sig for production
3. **Admin Verification**: Implement proper authentication for admin actions
4. **Rate Limiting**: Add rate limits to prevent spam
5. **Input Validation**: Sanitize all user inputs

## Support

For questions or issues:
- GitHub Issues: [Link to repo]
- Discord: [Community server]
- Email: support@oasis.dev

## License

MIT License - See LICENSE file for details





