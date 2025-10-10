# Telegram-OASIS Quick Start Guide

## For Naali (experiences.fun)

This guide will help you get the Telegram bot running in under 30 minutes.

## Prerequisites

- .NET 8.0 SDK installed
- MongoDB connection (already configured in OASIS)
- Solana wallet for token treasury
- Telegram account

## Step 1: Create Telegram Bot (5 minutes)

1. Open Telegram and search for `@BotFather`
2. Send: `/newbot`
3. Choose a name: `OASIS Accountability Bot`
4. Choose username: `oasis_accountability_bot` (must end in 'bot')
5. Copy the token you receive (looks like: `123456789:ABCdefGHIjklMNOpqrsTUVwxyz`)

## Step 2: Configure OASIS (2 minutes)

Edit `/NextGenSoftware.OASIS.API.ONODE.WebAPI/OASIS_DNA.json`:

```json
{
  "TelegramOASIS": {
    "BotToken": "PASTE_YOUR_BOT_TOKEN_HERE",
    "WebhookUrl": "https://oasisweb4.one/api/telegram/webhook",
    "MongoConnectionString": "mongodb+srv://OASISWEB4:Uppermall1!@oasisweb4.ifxnugb.mongodb.net/?retryWrites=true&w=majority&appName=OASISWeb4",
    "TreasuryWalletAddress": "Be51B1n3m1MCtZYvH8JEX3LnZZwoREyH4rYoyhMrkxJs",
    "RewardTokenMintAddress": "YOUR_SPL_TOKEN_MINT_ADDRESS",
    "RewardTokenSymbol": "EXP",
    "SolanaCluster": "devnet"
  }
}
```

**Required Changes**:
- Replace `PASTE_YOUR_BOT_TOKEN_HERE` with your bot token from Step 1
- Replace `YOUR_SPL_TOKEN_MINT_ADDRESS` with your SPL token mint (or leave empty for now)

## Step 3: Build the Project (3 minutes)

```bash
cd /Volumes/Storage/OASIS_CLEAN

# Build TelegramOASIS provider
cd NextGenSoftware.OASIS.API.Providers.TelegramOASIS
dotnet build

# Build and run the API
cd ../NextGenSoftware.OASIS.API.ONODE.WebAPI
dotnet build
dotnet run
```

## Step 4: Test the Bot (5 minutes)

1. Open Telegram
2. Search for your bot username (e.g., `@oasis_accountability_bot`)
3. Send: `/start`
4. You should receive a welcome message
5. Try: `/help` to see all commands

### Test Flow:

```
You: /start
Bot: 🎉 Welcome to OASIS! Your Telegram account has been linked...

You: /creategroup Test Accountability
Bot: ✅ Group 'Test Accountability' created successfully!

You: /setgoal Complete first milestone
Bot: 🎯 Goal set successfully!

You: /checkin Made progress today!
Bot: ✅ Check-in recorded! Karma awarded: +10

You: /mystats
Bot: 📊 Your OASIS Stats
     Username: tg_yourname
     Karma: 10
     ...
```

## Step 5: Create SPL Token (Optional - 15 minutes)

If you want to distribute actual Solana tokens:

```bash
# Install Solana CLI if not already installed
sh -c "$(curl -sSfL https://release.solana.com/stable/install)"

# Generate keypair for treasury
solana-keygen new -o ~/treasury.json

# Switch to devnet
solana config set --url https://api.devnet.solana.com

# Get devnet SOL for testing
solana airdrop 2

# Create token
spl-token create-token --decimals 9

# Copy the token address and update OASIS_DNA.json
# Example output: "Creating token ABC123..."

# Create token account
spl-token create-account <TOKEN_ADDRESS>

# Mint initial supply (1 million tokens)
spl-token mint <TOKEN_ADDRESS> 1000000
```

## Common Issues & Solutions

### Issue 1: Bot doesn't respond
**Solution**: Check the bot token is correct in OASIS_DNA.json and the API is running

### Issue 2: MongoDB connection error
**Solution**: Verify MongoDB connection string is correct

### Issue 3: "Provider not activated"
**Solution**: Make sure TelegramOASIS provider is being activated in startup code

### Issue 4: Karma not updating
**Solution**: Check that MongoDBOASIS is connected and avatar is being saved

## Bot Commands Reference

| Command | Description | Example |
|---------|-------------|---------|
| `/start` | Link account to OASIS | `/start` |
| `/creategroup <name>` | Create accountability group | `/creategroup Fitness Challenge` |
| `/joingroup <id>` | Join existing group | `/joingroup abc-123-def` |
| `/setgoal <description>` | Set a new goal | `/setgoal Exercise 30 min daily` |
| `/checkin <update>` | Post progress | `/checkin Completed workout!` |
| `/mystats` | View your stats | `/mystats` |
| `/mygroups` | List your groups | `/mygroups` |
| `/help` | Show help message | `/help` |

## Configuration Options

In `OASIS_DNA.json` → `TelegramOASIS`:

| Setting | Purpose | Example |
|---------|---------|---------|
| `BotToken` | Telegram bot API token | `123456789:ABC...` |
| `WebhookUrl` | URL for webhook (optional) | `https://...` |
| `MongoConnectionString` | MongoDB for social data | `mongodb+srv://...` |
| `TreasuryWalletAddress` | Solana wallet for rewards | `Be51B1n...` |
| `RewardTokenMintAddress` | SPL token mint address | `Token123...` |
| `RewardTokenSymbol` | Token symbol (display) | `EXP` |
| `SolanaCluster` | Solana network | `devnet` or `mainnet-beta` |

## Customizing Rewards

Edit group rules in database or modify defaults in `Models/TelegramGroup.cs`:

```csharp
public class GroupRules
{
    public int KarmaPerCheckin { get; set; } = 10;        // Change this
    public decimal TokenPerMilestone { get; set; } = 1.0m; // Change this
    public int RequiredCheckinsPerWeek { get; set; } = 3;  // Change this
}
```

## Architecture Flow

```
User sends /checkin
    ↓
TelegramBotService receives update
    ↓
Looks up user's OASIS avatar
    ↓
Adds check-in to achievement
    ↓
AchievementManager awards karma
    ↓
Avatar.Karma updated in MongoDB
    ↓
Bot sends confirmation message
```

## Database Collections

TelegramOASIS creates these MongoDB collections:

1. **telegram_avatars**
   - Maps Telegram ID ↔ OASIS Avatar ID
   - Stores group memberships

2. **telegram_groups**
   - Group metadata and rules
   - Member and admin lists

3. **achievements**
   - Goals and milestones
   - Check-in history
   - Reward amounts

## API Endpoints

Test with curl or Postman:

```bash
# Link Telegram account
curl -X POST http://localhost:5002/api/telegram/link-avatar \
  -H "Content-Type: application/json" \
  -d '{
    "telegramId": 123456789,
    "telegramUsername": "naali",
    "firstName": "Naali"
  }'

# Create group
curl -X POST http://localhost:5002/api/telegram/groups/create \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Fitness Group",
    "createdBy": "avatar-guid-here",
    "telegramChatId": 123456789
  }'

# Get user stats
curl http://localhost:5002/api/telegram/achievements/user/{avatarId}
```

## Going to Production

When ready to launch:

1. **Switch to Mainnet**:
   - Change `SolanaCluster` to `mainnet-beta`
   - Update `ConnectionString` in `SolanaOASIS` config
   - Ensure treasury wallet has sufficient tokens

2. **Set Up Webhooks**:
   - Get a public domain with HTTPS
   - Configure webhook URL
   - Bot will receive updates instantly instead of polling

3. **Scale MongoDB**:
   - Consider dedicated cluster
   - Set up indexes for performance
   - Enable backups

4. **Monitor & Analytics**:
   - Track active users
   - Monitor token distribution
   - Log achievement completions

5. **Security**:
   - Never commit bot token to git
   - Use environment variables for secrets
   - Set up admin authentication
   - Rate limit API endpoints

## Support & Resources

- **Full Documentation**: `NextGenSoftware.OASIS.API.Providers.TelegramOASIS/README.md`
- **Implementation Details**: `TELEGRAM_INTEGRATION_SUMMARY.md`
- **OASIS Docs**: Check `/Docs` directory
- **Telegram Bot API**: https://core.telegram.org/bots/api

## Example Use Case: 30-Day Fitness Challenge

```
1. Admin creates group:
   /creategroup 30 Day Fitness Challenge
   
2. Members join:
   /joingroup abc-123-def
   
3. Each member sets goal:
   /setgoal Complete 30 workouts in 30 days
   
4. Daily check-ins:
   /checkin Day 1: 30 min cardio ✅
   (Earns +10 karma each time)
   
5. After 30 days:
   Admin marks complete or auto-complete
   User receives: +100 karma + 5 EXP tokens
   
6. Leaderboard:
   /leaderboard
   (Shows top performers)
```

## Next Steps

1. ✅ Get bot running locally
2. ✅ Test all commands
3. ⏳ Create SPL token on devnet
4. ⏳ Test token distribution
5. ⏳ Invite test users
6. ⏳ Gather feedback
7. ⏳ Launch on mainnet

## Questions?

- GitHub Issues: [Your repo]
- Email: naali@experiences.fun
- Telegram: @naali

---

**Ready to build accountability groups on OASIS!** 🚀

