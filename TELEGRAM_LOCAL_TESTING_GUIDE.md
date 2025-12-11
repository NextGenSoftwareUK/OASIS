# Telegram OASIS Integration - Local Testing Guide

## 🎯 What's Configured

All services are now configured and ready to test:

✅ **TelegramOASIS Provider** - Integrated and auto-activates on startup  
✅ **TelegramBotService** - Registers all bot commands  
✅ **TelegramController** - REST API endpoints active  
✅ **AchievementManager** - Reward distribution ready  
✅ **Dependencies** - Telegram.Bot & MongoDB.Driver added  

## 🚀 Quick Start - Local Testing

### Option 1: Use the Test Script

```bash
cd /Volumes/Storage/OASIS_CLEAN
./test-telegram-bot-local.sh
```

### Option 2: Manual Steps

```bash
cd /Volumes/Storage/OASIS_CLEAN/ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI
dotnet restore
dotnet build
dotnet run
```

## 📱 Testing the Bot

### 1. Start the API Server

Run the test script or manual steps above. You should see:

```
Starting up The OASIS... (REST API)
Activating TelegramOASIS provider...
TelegramOASIS provider activated successfully
Starting Telegram bot service...
Telegram bot service started successfully
```

### 2. Open Telegram

- Search for your bot (the name you gave it when creating with @BotFather)
- Or use the link: `t.me/YOUR_BOT_USERNAME`

### 3. Test Commands

Send these commands to test:

```
/start
```
**Expected:** Bot creates OASIS avatar and links your Telegram account
```
🎉 Welcome to Experiences.fun, @yourname!

Your OASIS avatar has been created and linked to your Telegram account.

**Getting Started:**
• Use /creategroup to start an accountability group
• Use /joingroup to join an existing group
• Use /checkin to log your progress and earn karma
• Use /mystats to see your achievements
```

```
/help
```
**Expected:** List of all available commands

```
/creategroup My Fitness Crew
```
**Expected:** Creates a group and returns the group ID

```
/checkin Just completed my morning workout! 💪
```
**Expected:** Records check-in and awards karma
```
✅ Check-in recorded!

_Just completed my morning workout! 💪_

🎯 +10 karma
⭐ Total: 10 karma
📊 Check-ins: 1

Keep it up! 🚀
```

```
/mystats
```
**Expected:** Shows your karma, tokens, and achievements

## 🔍 Debugging

### Check Logs

The API logs show bot activity:

```bash
# In another terminal while the API is running
tail -f /path/to/logs
```

### Check MongoDB

Verify data is being stored:

```javascript
// Connect to MongoDB
mongodb+srv://OASISWEB4:Uppermall1!@oasisweb4.ifxnugb.mongodb.net/

// Check collections
db.telegram_avatars.find()
db.telegram_groups.find()
db.achievements.find()
```

### Common Issues

**Bot doesn't respond:**
- Check the bot token is correct in Startup.cs
- Verify MongoDB connection string is valid
- Check console logs for errors

**"Provider not configured" warning:**
- The TelegramOASIS provider may not be initializing
- Check the logs during startup

**MongoDB connection errors:**
- Verify network access to MongoDB Atlas
- Check connection string format
- Ensure IP whitelist includes your location

## 🧪 API Endpoint Testing

You can also test via REST API:

### Link Avatar
```bash
curl -X POST http://localhost:5000/api/telegram/link-avatar \
  -H "Content-Type: application/json" \
  -d '{
    "telegramUserId": 123456789,
    "telegramUsername": "testuser",
    "firstName": "Test",
    "lastName": "User"
  }'
```

### Create Group
```bash
curl -X POST http://localhost:5000/api/telegram/groups/create \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Test Group",
    "description": "Testing group creation",
    "createdBy": "00000000-0000-0000-0000-000000000000",
    "telegramChatId": -1001234567890
  }'
```

### Get User Stats
```bash
curl http://localhost:5000/api/telegram/stats/user/{avatarId}
```

## 📊 What's Happening Behind the Scenes

When you send `/start`:
1. **TelegramBotService** receives the message
2. Creates new OASIS avatar via **TelegramOASIS provider**
3. Links Telegram ID ↔ Avatar ID
4. Stores in **MongoDB**: `telegram_avatars` collection
5. Returns welcome message

When you send `/checkin`:
1. **TelegramBotService** parses the message
2. Creates **Achievement** record
3. Awards karma via **AchievementManager**
4. Updates avatar karma in OASIS Core
5. Stores check-in in **MongoDB**: `achievements` collection
6. Returns confirmation with karma total

## 🎯 Next Steps After Local Testing

Once local testing works:

### 1. Deploy to Production Server
```bash
# Build for release
dotnet publish -c Release

# Copy to server and run
./NextGenSoftware.OASIS.API.ONODE.WebAPI
```

### 2. Set Telegram Webhook
```bash
curl -X POST "https://api.telegram.org/bot7927576561:AAEFHa3k1t6kj0t6wOu6QtU61KRsNxOoeMo/setWebhook" \
  -H "Content-Type: application/json" \
  -d '{"url":"https://oasisweb4.one/api/telegram/webhook"}'
```

### 3. Create Solana SPL Token
```bash
# On Solana CLI
spl-token create-token --decimals 9
spl-token create-account <TOKEN_MINT_ADDRESS>
spl-token mint <TOKEN_MINT_ADDRESS> 1000000
```

### 4. Update Token Config
Add the token mint address to OASIS_DNA.json:
```json
"RewardTokenMintAddress": "YOUR_TOKEN_MINT_ADDRESS"
```

### 5. Test Token Distribution
Complete an achievement and verify tokens are transferred

## 🐛 Troubleshooting Guide

| Issue | Solution |
|-------|----------|
| Bot not starting | Check bot token validity |
| No MongoDB connection | Verify connection string and network access |
| Commands not working | Check TelegramBotService logs |
| Karma not updating | Verify AchievementManager is registered |
| Tokens not sending | Implement SolanaOASIS integration |

## 💡 Tips

- **Test with multiple users** - Have friends test the bot
- **Monitor MongoDB** - Watch data being created in real-time
- **Check logs constantly** - They show exactly what's happening
- **Start simple** - Test `/start` first, then add complexity
- **Use Postman** - Test API endpoints independently

## 📞 Support

- Check `/plan.md` for architecture details
- Review `TELEGRAM_INTEGRATION_SUMMARY.md` for overview
- See `TELEGRAM_QUICKSTART.md` for bot setup

## ✅ Success Checklist

- [ ] API starts without errors
- [ ] Telegram bot responds to `/start`
- [ ] Avatar created in MongoDB
- [ ] `/checkin` awards karma
- [ ] `/mystats` shows correct data
- [ ] Groups can be created
- [ ] Multiple users can interact
- [ ] Data persists after restart

Happy testing! 🚀





