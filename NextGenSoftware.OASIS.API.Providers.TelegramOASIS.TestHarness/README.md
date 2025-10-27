# TelegramOASIS Test Harness

Test harness for the TelegramOASIS provider, which integrates Telegram messaging platform with OASIS for accountability tracking and rewards.

## Features Tested

1. **Provider Activation/Deactivation**
   - Initialize MongoDB connection
   - Connect to Telegram Bot API
   - Graceful shutdown

2. **Avatar Linking**
   - Link Telegram chat ID to OASIS avatar
   - Retrieve avatar by chat ID
   - Store username mappings

3. **Group Operations**
   - Create accountability groups
   - Retrieve group details
   - List groups for an avatar

4. **Achievement Tracking**
   - Create achievements with karma/token rewards
   - Retrieve achievements by avatar
   - Retrieve achievements by group

5. **Telegram Messaging**
   - Send messages via bot (requires real chat ID)

## Running the Test Harness

### Prerequisites

1. **MongoDB Connection**: Ensure MongoDB is accessible
2. **Telegram Bot Token**: Valid bot token from @BotFather
3. **Network Access**: Bot needs internet to reach Telegram API

### Build and Run

```bash
cd /Volumes/Storage/OASIS_CLEAN/NextGenSoftware.OASIS.API.Providers.TelegramOASIS.TestHarness
dotnet build
dotnet run
```

### Configuration

Edit `OASIS_DNA.json` or `Program.cs` to configure:

- `BotToken`: Your Telegram bot token
- `WebhookUrl`: Webhook endpoint (optional for test harness)
- `MongoConnectionString`: MongoDB connection string

## Expected Output

```
TELEGRAM OASIS TEST HARNESS V1.0
=================================

Initializing TelegramOASIS provider...
Testing provider activation...
✅ Provider activated successfully
Provider Name: TelegramOASIS
Provider Description: Telegram Provider for social accountability and achievement tracking

--- Testing Avatar Linking ---
Linking Telegram user @test_user_xxx (chatId: 123456789) to avatar xxx...
✅ Avatar linked successfully
✅ Avatar retrieved: ID=xxx, Username=test_user_xxx

--- Testing Group Operations ---
Creating group 'Test Accountability Group xxx'...
✅ Group created: ID=xxx
✅ Group retrieved: Name=Test Accountability Group xxx, Members=1
✅ Found 1 groups for avatar

--- Testing Achievement Operations ---
Creating test achievement...
✅ Achievement created: First Milestone
✅ Found 1 achievements for avatar
✅ Found 1 achievements for group

--- Testing Telegram Messaging ---
⚠️  Messaging test skipped - requires real Telegram chat ID

Testing provider deactivation...
✅ Provider deactivated successfully

Test harness completed. Press any key to exit...
```

## MongoDB Collections Created

After running, check your MongoDB database for these collections:

- `telegram_avatars` - User mappings
- `telegram_groups` - Accountability groups  
- `achievements` - Achievement records

## Testing with Real Telegram Users

To test messaging:

1. Start a chat with your bot on Telegram
2. Send `/start` to get your chat ID (implement in bot service)
3. Update `Program.cs` with your real chat ID in `TestTelegramMessaging()`
4. Run the test harness again

## Troubleshooting

### MongoDB Connection Errors
- Check connection string format
- Ensure network access to MongoDB cluster
- Verify credentials

### Telegram Bot Errors
- Verify bot token is valid
- Check bot is not blocked
- Ensure internet connectivity

### Build Errors
- Run `dotnet restore` first
- Check all project references exist
- Ensure .NET 8.0 SDK is installed

## Next Steps

After successful test harness run:

1. Build the TelegramBotService for command handling
2. Create API endpoints for webhook integration
3. Implement AchievementManager for rewards
4. Deploy to production with proper configuration management





