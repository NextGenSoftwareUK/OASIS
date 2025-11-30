# How to Test Telegcrm

## 🎯 Quick Test Options

### 1. **Swagger UI** (Easiest)
Open in browser:
```
http://localhost:5001/swagger
```

This gives you an interactive API explorer where you can:
- See all endpoints
- Test them directly
- See request/response examples

### 2. **Web Dashboard**
Open in browser:
```
http://localhost:5001
```

Simple dashboard to:
- View conversations
- See stats
- Search conversations
- Test contacts

### 3. **Command Line (curl)**

**Get all contacts:**
```bash
curl http://localhost:5001/api/telegram-crm/contacts
```

**Get conversations (need Avatar ID):**
```bash
curl "http://localhost:5001/api/telegram-crm/conversations?oasisAvatarId=YOUR-GUID-HERE"
```

**Search conversations:**
```bash
curl "http://localhost:5001/api/telegram-crm/conversations/search?oasisAvatarId=YOUR-GUID&keyword=payment"
```

## 📝 Getting Your OASIS Avatar ID

You need an OASIS Avatar ID to test conversations. Get it:

**Option 1: From OASIS API**
```bash
curl -X POST "http://localhost:5000/api/avatar/authenticate" \
  -H "Content-Type: application/json" \
  -d '{"username": "your_username", "password": "your_password"}'
```

The response includes `avatarId`.

**Option 2: Check MongoDB**
```bash
mongosh "your-connection-string"
use OASISAPI_DEV
db.avatars.find({username: "your_username"}).pretty()
```

## 🧪 Test Scenarios

### Test 1: Check Server is Running
```bash
curl http://localhost:5001/api/telegram-crm/contacts
```
Should return: `[]` (empty array) or list of contacts

### Test 2: View Swagger
Open: http://localhost:5001/swagger
- Should show all API endpoints
- Can test directly from browser

### Test 3: View Dashboard
Open: http://localhost:5001
- Should show dashboard interface
- Enter Avatar ID to load conversations

## 🚀 Next: Connect to Telegram Bot

To actually track messages, you need to:
1. Integrate with your Telegram bot (see `QUICK_START.md`)
2. Send messages to your bot
3. Messages will appear in the CRM

For now, the server is running and ready to receive data!

