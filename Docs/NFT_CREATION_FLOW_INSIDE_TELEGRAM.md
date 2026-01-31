# NFT Creation Flow Inside Telegram

**Short answer:** Yes. You can put the full NFT creation flow inside Telegram so users never leave the app: they talk to a bot, send an image or describe one, fill in details, and the bot mints via OASIS.

You already have the building blocks in-repo (Telegram bot that handles photos, uploads to Pinata, mints NFT; Telegram ↔ OASIS avatar link). The rest is turning that into a clear conversational flow and wiring it to the same OASIS/Glif APIs you use elsewhere.

---

## 1. What “NFT creation flow inside Telegram” means

- User opens **your Telegram bot** (e.g. “OASIS NFT Bot” or “Meme Coin → NFT”).
- Entire flow happens in chat:
  1. **Auth** – one-time link Telegram user to OASIS avatar (or “mint as” avatar).
  2. **Image** – user sends a **photo** or a **text description** (bot uses Glif for the latter).
  3. **Details** – bot asks for title, symbol, description (and optionally custom attributes, e.g. Telegram chat access).
  4. **Optional** – “Paste pump.fun token address” to pre-fill from memecoin metadata.
  5. **Mint** – bot calls OASIS mint-nft and replies with tx link + NFT link.

No web app, no Cursor; everything inside Telegram.

---

## 2. What you already have

### Telegram bot (TelegramBotService)

- **Commented out** but implemented:
  - Handles **photos**: `HandlePhotoMessageAsync` → download from Telegram → upload to **Pinata** → mint NFT.
  - **Caption format** today: `wallet | title | description` (single message).
  - Resolves **Telegram user → OASIS avatar** via `GetTelegramAvatarByTelegramIdAsync` (TelegramOASIS provider).
  - Uses **PinataService** for upload and **NFTService.MintAchievementNFTAsync** for minting.

So: **photo + caption → upload → mint** and **Telegram ↔ OASIS avatar** are already there; they just need to be re-enabled and generalized.

### OASIS / ONODE

- **Auth:** Avatar login; you can keep “mint as this avatar” once Telegram user is linked.
- **Mint:** `POST /api/nft/mint-nft` with Title, Symbol, ImageUrl, Description, MetaData (custom attributes).
- **Upload:** e.g. `FilesController` upload to IPFS (Pinata); or reuse PinataService from the bot.
- **Glif:** Your backend can call Glif (or existing Glif integration) when the user sends a **text description** instead of a photo.

So the **backend** for “image URL + metadata → mint” and “text prompt → image URL” already exists; the bot just needs to drive it step by step.

---

## 3. How the flow could work in Telegram

### Option A: Simple (minimal change)

- Keep **single-message** style:
  - User sends **photo + caption** in one go.
  - Caption format: e.g. `title | symbol | description` (or `wallet | title | description` if you still send to a wallet).
- Bot: resolve Telegram user → OASIS avatar, download photo, upload to Pinata, call mint-nft with ImageUrl + metadata.
- Optional: **Custom attributes** in caption, e.g. `title | symbol | description | telegram_chat_id:-1001234567890` and parse that into `MetaData`.

**Pros:** Reuse existing HandlePhotoMessageAsync almost as-is.  
**Cons:** Less friendly for non-technical users; no “describe image” (Glif) in-app.

### Option B: Conversational (recommended)

- **Commands:** e.g. `/mint` or “Create NFT” (inline/menu button).
- Bot runs a **short wizard** in chat:

| Step | Bot asks | User can send |
|------|----------|----------------|
| 1 | “Send a **photo** for your NFT, or **describe** the image (e.g. ‘a frog on a skateboard’).” | Photo **or** text |
| 2 | If text → “Generating image…” → (Glif) → “Here’s your image. Use it? (Yes / No / Describe again)” | Yes / No / new description |
| 3 | “**Title** for the NFT?” | Text |
| 4 | “**Symbol** (e.g. FROG)?” | Text |
| 5 | “**Description**? (optional)” | Text or “skip” |
| 6 | “Add **private Telegram chat access**? Send chat ID or ‘skip’.” | `-1001234567890` or skip |
| 7 | “Mint now? (Yes / No)” | Yes / No |

- On **Yes**: bot uploads image if it was a photo (Pinata), or uses Glif image URL; builds mint payload (Title, Symbol, Description, ImageUrl, MetaData with e.g. `telegram_chat_id`, `access_type`); calls OASIS **mint-nft**; replies with Solana explorer link + NFT link.

**Auth:** Before step 1 (or once per user), bot checks Telegram user → OASIS avatar. If not linked: “Link your OASIS account first: [link]” or “Send wallet address to mint to” (and optionally create/link avatar by wallet). So “NFT creation flow inside Telegram” includes this linking step inside the same bot.

**Optional steps:** “Paste **pump.fun token address** to use its image and name” → bot fetches metadata + image URL, pre-fills title/symbol/description/image and asks for confirmation.

---

## 4. Image handling

- **Photo:** Already implemented: `GetFileAsync` → download → Pinata upload → use returned URL as `ImageUrl` in mint-nft. No change except wiring to the chosen flow (A or B).
- **Text description (Glif):** Backend (or bot service) calls your existing Glif integration with the user’s message; get back image URL; use that as `ImageUrl` in mint-nft. No need for the user to leave Telegram.

So yes: **both** “send a photo” and “describe the image” can live inside Telegram.

---

## 5. Auth (who mints)

- **Telegram user → OASIS avatar:** You already have `GetTelegramAvatarByTelegramIdAsync`. So “mint as this avatar” = use that avatar’s JWT (or server-side avatar id) when calling mint-nft. Avatar must exist and be linked (e.g. via `/start` or “Link account” flow).
- **If not linked:** Bot can either:
  - Ask user to link (e.g. open a small web page: connect wallet or log in with OASIS → store Telegram id ↔ avatar id), or
  - For a “quick mint” mode: “Send Solana wallet address; we’ll mint and send the NFT there” and use a **service avatar** to mint (NFT still sent to user’s wallet). That avoids OASIS login in Telegram but doesn’t attribute the mint to the user’s OASIS identity.

So the flow can stay **inside Telegram** as long as you consider “link once” or “mint to wallet” as part of the flow.

---

## 6. Custom attributes (e.g. Telegram chat access)

- In the conversational flow, one step can be: “**Private chat access:** send the Telegram chat/group ID (e.g. -1001234567890) or type **skip**.”
- Bot puts that into **MetaData** (e.g. `telegram_chat_id`, `access_type: "private_chat"`) when calling mint-nft. No API change; same mint-nft `MetaData` you already use.

So **yes**: custom attributes (including “allow access to private Telegram chat”) can be collected entirely inside Telegram and sent in the same creation flow.

---

## 7. What to implement

1. **Re-enable and generalize** `HandlePhotoMessageAsync` (and related handlers) in `TelegramBotService`.
2. **Add a conversational mint flow** (Option B):
   - State machine or simple “step” store per Telegram user (e.g. in TelegramOASIS or Redis): current step, collected fields (image URL, title, symbol, description, MetaData).
   - Handlers: on `/mint`, start flow; on message, depending on step, collect photo or text (and call Glif if text for image); then ask for title, symbol, description, optional attributes; then confirm and call mint-nft.
3. **Glif in backend:** One endpoint or internal call that takes a text prompt and returns image URL (reuse existing Glif integration); bot calls it when user sends a description instead of a photo.
4. **Optional:** “Import from pump.fun” step: user sends token address → backend resolves metadata + image URL → bot pre-fills and asks for confirmation.
5. **Linking:** Ensure `/start` (or dedicated “Link OASIS” command) creates/updates Telegram user ↔ OASIS avatar so mint-nft is called with the right avatar.

Everything above uses **existing** OASIS APIs (mint-nft, upload, auth/avatar) and Glif; the new work is **Telegram-side**: flow, state, and prompts.

---

## 8. Summary

| Question | Answer |
|----------|--------|
| Can we put the NFT creation flow inside Telegram? | **Yes.** |
| Image: photo or text? | **Both.** Photo → download → Pinata → URL. Text → Glif → URL. |
| Who mints? | Telegram user linked to OASIS avatar (already supported), or “mint to wallet” with service avatar. |
| Custom attributes (e.g. Telegram chat access)? | **Yes.** Collect in chat, send in `MetaData` on mint-nft. |
| Pump.fun import? | **Optional.** User sends token address in Telegram; backend fetches metadata + image; bot pre-fills. |
| What’s already there? | Telegram bot (commented) with photo → Pinata → mint; Telegram ↔ OASIS avatar; mint-nft and upload APIs. |
| What’s to build? | Conversational flow (steps, state), Glif call from backend for “describe image”, optional pump.fun step, and re-enabling/wiring the bot. |

So: **yes, you can put the full NFT creation flow inside Telegram** — auth, image (photo or Glif), details, custom attributes, and mint — using your current OASIS and Glif stack and the Telegram bot you already have in-repo.
