# Babelfish 🌉

**Babelfish** is a real-time chat bridge built on OASIS that connects Discord and Telegram into a single unified conversation, with OASIS Avatars as the shared identity layer across both platforms.

A message sent in Discord appears in Telegram. A message sent in Telegram appears in Discord. Users link their platform accounts to their OASIS Avatar once, and from then on their avatar name travels with them across both platforms.

---

## Architecture

```
Discord (#bridge-channel)          Telegram (bridged group)
        │                                    │
        ▼                                    ▼
 DiscordBridgeService          TelegramWebhookController
  (IHostedService gateway)      (extended with bridge routing)
        │                                    │
        └──────────► ChatBridgeRouter ◄──────┘
                           │
                    Avatar Identity Lookup
               (ChatBridgeAvatarLinkStore → MongoDB)
                           │
              ┌────────────┴────────────┐
              ▼                         ▼
  TelegramBridgeSender         DiscordWebhookSender
  (Bot API HTTP POST)          (Incoming Webhook HTTP POST)
```

---

## Source Code

All Babelfish code lives inside the ONODE WebAPI project:

```
ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI/
├── Models/ChatBridge/
│   ├── ChatBridgeOptions.cs          # Typed config (appsettings "ChatBridge" section)
│   └── BridgedAvatarLinkDocument.cs  # MongoDB doc: platform user → OASIS avatar
├── Interfaces/
│   └── IChatBridgeAvatarLinkStore.cs
├── Services/
│   ├── ChatBridgeAvatarLinkStore.cs  # MongoDB-backed avatar link store
│   ├── ChatBridgeRouter.cs           # Core routing + avatar display name logic
│   ├── DiscordBridgeService.cs       # IHostedService: Discord gateway bot
│   ├── DiscordWebhookSender.cs       # Sends to Discord via Incoming Webhook
│   └── TelegramBridgeSender.cs       # Sends to Telegram via Bot API
└── Controllers/
    └── ChatBridgeController.cs       # REST API for avatar linking + status
```

---

## Configuration

Add to `appsettings.json` (use .NET User Secrets or environment variables for secrets):

```json
"ChatBridge": {
  "Enabled": true,
  "DiscordBotToken": "your-discord-bot-token",
  "DiscordBridgeChannelId": 1234567890123456789,
  "DiscordWebhookUrl": "https://discord.com/api/webhooks/...",
  "TelegramBotToken": "your-telegram-bot-token",
  "TelegramBridgeChatId": -1001234567890
}
```

### Using .NET User Secrets (recommended for local dev)

```bash
cd ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI
dotnet user-secrets set "ChatBridge:DiscordBotToken" "..."
dotnet user-secrets set "ChatBridge:DiscordBridgeChannelId" "..."
dotnet user-secrets set "ChatBridge:DiscordWebhookUrl" "..."
dotnet user-secrets set "ChatBridge:TelegramBotToken" "..."
dotnet user-secrets set "ChatBridge:TelegramBridgeChatId" "..."
dotnet user-secrets set "ChatBridge:Enabled" "true"
```

---

## Discord Setup

1. [Discord Developer Portal](https://discord.com/developers/applications) → New Application → Bot
2. Enable **Message Content Intent** under Bot → Privileged Gateway Intents
3. Copy bot token → `ChatBridge:DiscordBotToken`
4. Invite bot: OAuth2 → URL Generator → scopes: `bot` → permissions: `View Channels`, `Send Messages`
5. Create Incoming Webhook: Channel Settings → Integrations → Webhooks → New Webhook → Copy URL → `ChatBridge:DiscordWebhookUrl`
6. Right-click bridge channel → Copy Channel ID → `ChatBridge:DiscordBridgeChannelId`

## Telegram Setup

1. Message [@BotFather](https://t.me/BotFather) → `/newbot` → copy token → `ChatBridge:TelegramBotToken`
2. **Disable privacy mode**: BotFather → `/mybots` → select bot → Bot Settings → Group Privacy → Turn off
3. Add the bot to your Telegram group
4. Get the group chat ID (use `getUpdates` API or @RawDataBot) → `ChatBridge:TelegramBridgeChatId`
5. Register webhook (replace with your public URL):
   ```
   https://api.telegram.org/bot<TOKEN>/setWebhook?url=https://your-domain.com/api/telegram/webhook
   ```

### Local dev with ngrok
```bash
ngrok http 5003
curl "https://api.telegram.org/bot<TOKEN>/setWebhook?url=https://<ngrok-url>/api/telegram/webhook"
```

---

## Avatar Linking

Users link their platform account to their OASIS Avatar once. After linking, their avatar username appears in relayed messages instead of their platform handle.

**From Discord** (in any channel or DM with the bot):
```
/link your-oasis-username
/unlink
```

**From Telegram** (in the bridged group or DM with the bot):
```
/link your-oasis-username
/unlink
```

**Via REST API** (admin/programmatic):
```
POST /api/chatbridge/link/discord   { "platformUserId": 123, "oasisUsername": "..." }
POST /api/chatbridge/link/telegram  { "platformUserId": 456, "oasisUsername": "..." }
GET  /api/chatbridge/status
```

---

## Message Format

**Discord → Telegram:**
```
🎮 *AvatarName* (Discord)
message text here
```

**Telegram → Discord:**
```
📱 **AvatarName** (Telegram)
message text here
```

Unlinked users appear as their platform username until they run `/link`.

---

## Roadmap

- [ ] Signal bridge (via signal-cli REST API)
- [ ] Avatar profile photos shown alongside relayed messages  
- [ ] Karma points awarded for cross-platform participation
- [ ] OASIS Avatar lookup by username (v1 stores username string only)
- [ ] Media/image relay (currently text only)
- [ ] Multi-group / multi-channel support
