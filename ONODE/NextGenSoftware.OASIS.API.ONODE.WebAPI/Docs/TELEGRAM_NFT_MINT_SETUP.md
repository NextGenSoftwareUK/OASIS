# Telegram NFT Mint Flow – Setup

The NFT creation flow runs **inside Telegram**: user sends `/mint`, then photo (or `skip`), title, symbol, description, Solana wallet, and confirms; the bot mints via OASIS and replies with the tx link.

---

## 1. Configuration

Add to **appsettings.json** (or User Secrets / env):

```json
{
  "TelegramNftMint": {
    "BotToken": "YOUR_TELEGRAM_BOT_TOKEN",
    "BotAvatarId": "GUID_OF_OASIS_AVATAR_USED_FOR_MINTING",
    "PinataJwt": "YOUR_PINATA_JWT",
    "WebhookSecret": "",
    "MintingGifUrl": "https://example.com/your-minting.gif",
    "MintingGifUrls": ["https://example.com/one.gif", "https://example.com/two.gif"]
  }
}
```

| Key | Required | Description |
|-----|----------|-------------|
| **BotToken** | Yes | From [@BotFather](https://t.me/BotFather). Used to receive updates and send replies. |
| **BotAvatarId** | Yes | OASIS avatar ID (GUID) that will mint. This avatar must have a Solana wallet and be able to mint (e.g. OASIS_ADMIN or a dedicated “bot” avatar). |
| **PinataJwt** | Yes | Pinata JWT for uploading photos to IPFS. Get it from [Pinata](https://app.pinata.cloud/). |
| **WebhookSecret** | No | Optional secret to validate webhook payloads. |
| **MintingGifUrl** | No | GIF/video URL shown while minting ("Minting your NFT..."). If not set, the built-in video is used when **OasisApiBaseUrl** is set. Use for a single custom branded GIF/video. |
| **MintingGifUrls** | No | List of GIF/video URLs; one is chosen at random each mint. Overrides **MintingGifUrl** when set. Use for multiple themed GIFs (e.g. SAINT or OASIS branding). |
| **OasisApiBaseUrl** | No | Base URL of this API (e.g. `https://api.oasisweb4.com`). When set, the default minting animation is the built-in video at `{OasisApiBaseUrl}/minting/witness-the-jpeg-miracle.mp4`. |

---

## 2. Set Telegram webhook

After deploying, tell Telegram to send updates to your API:

```bash
curl -X POST "https://api.telegram.org/bot<YOUR_BOT_TOKEN>/setWebhook" \
  -H "Content-Type: application/json" \
  -d '{"url": "https://YOUR_DOMAIN/api/telegram/webhook"}'
```

- Replace `<YOUR_BOT_TOKEN>` and `YOUR_DOMAIN` with your bot token and public base URL (e.g. `https://api.oasisweb4.com`).
- For local testing, use a tunnel (e.g. ngrok): `https://abc123.ngrok.io/api/telegram/webhook`.

---

## 3. User flow in Telegram

1. User sends **/mint** (or “Create NFT”).
2. Bot: “Send a **photo** for your NFT (or type `skip` for placeholder).”
3. User sends photo **or** `skip` → image uploaded to Pinata (or placeholder used).
4. Bot asks: **Title** → **Symbol** → **Description** (or `skip`) → **Solana wallet address**.
5. Bot shows summary and: “Type **YES** to mint or /cancel to cancel.”
6. User types **YES** → backend mints via OASIS (Solana), NFT sent to the given wallet.
7. Bot replies with tx hash and NFT address.

---

## 4. Endpoints

| Method | URL | Description |
|--------|-----|-------------|
| POST | `/api/telegram/webhook` | Receives Telegram `Update` JSON. No auth; Telegram only knows the URL + secret (if you validate it yourself). |

---

## 5. Optional: custom attributes (e.g. Telegram chat access)

The flow can be extended to ask for “Telegram chat ID for access” and pass it in `MetaData` when minting (see [NFT_CUSTOM_ATTRIBUTES_TELEGRAM_ACCESS.md](../../Docs/NFT_CUSTOM_ATTRIBUTES_TELEGRAM_ACCESS.md)).

---

## 6. Local development (localhost)

When `OasisApiBaseUrl` is set to a localhost URL (e.g. `https://localhost:5004`), the minting animation automatically uses the fallback GIF instead of the built-in video, because Telegram’s servers cannot fetch localhost. Use ngrok (see [LOCAL_TELEGRAM_TESTING.md](LOCAL_TELEGRAM_TESTING.md)) so Telegram can reach your webhook; the “Minting…” message will still show the fallback GIF until you deploy with a public `OasisApiBaseUrl`.

**To use your own video when testing locally:** upload the file (e.g. `wwwroot/minting/witness-the-jpeg-miracle.mp4`) to a **public** URL (e.g. [Pinata](https://app.pinata.cloud/) → upload → copy URL), then set `MintingGifUrl` in `appsettings.Development.json` to that URL.

**To use a specific GIF you found in Telegram:** the bot needs a **public URL** to the GIF (Telegram file links are not directly usable). Save or forward the GIF from Telegram to your machine, upload it to a public host (e.g. [Pinata](https://app.pinata.cloud/) → upload → copy “Gateway URL”), then set `MintingGifUrl` in `TelegramNftMint` (e.g. in `appsettings.Development.json`) to that URL. That GIF will be shown when “Minting your NFT…” is sent.

---

## 7. Using the bot in a group

When the bot is in a **group**, Telegram's **Group Privacy** (default: on) means the bot only receives:

- Messages that start with a **command** (e.g. `/mint`),
- **Replies** to the bot's messages,
- Messages that **@mention** the bot.

So if someone sends a photo or GIF without replying to the bot, Telegram does **not** deliver that update, so the bot never sees the asset and the flow appears to hang.

**Options:**

1. **Reply to the bot:** When the bot says "Send a photo or GIF…", send the photo or GIF as a **reply to that message**. The bot now adds a short hint in groups: "In groups: send your photo or GIF as a **reply to this message**".
2. **Disable Group Privacy:** In [@BotFather](https://t.me/BotFather), choose your bot, then **Bot Settings** → **Group Privacy** → **Turn off**. The bot will then receive all messages in the group (no need to reply).

---

## 8. Troubleshooting

- **“Mint failed: bot avatar not configured”** – Set `BotAvatarId` to a valid OASIS avatar GUID that can mint on Solana.
- **“Failed to upload image”** – Check `PinataJwt` and that the bot can download the photo from Telegram (valid `BotToken`).
- **No reply in Telegram** – Ensure the webhook URL is HTTPS and returns 200 quickly; check logs for errors when processing the update.
- **Command hung in a group after sending photo/GIF** – Telegram did not deliver the photo (Group Privacy). Send the photo as a **reply to the bot's message**, or turn off Group Privacy in @BotFather (see §7).