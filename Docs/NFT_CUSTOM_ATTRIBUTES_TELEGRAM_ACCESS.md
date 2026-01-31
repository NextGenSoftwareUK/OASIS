# NFT Custom Attributes & Telegram Private Chat Access

**Summary:** Yes — you can add custom attributes to NFTs (including for access control), and use them to grant access to private Telegram chats. OASIS already supports custom metadata on mint; the rest is attribute schema + a Telegram bot that verifies ownership and grants access.

---

## 1. Custom attributes on NFTs (already supported)

### In OASIS

- **Mint request** accepts **`MetaData`**: a dictionary of custom key/value pairs (`Dictionary<string, object>` in C#, `MetaData` object in MCP/API).
- This is written into the NFT’s **off-chain JSON** as **`attributes`** (Metaplex on Solana, same idea on ERC721/ERC1155).
- So any custom data you put in `MetaData` becomes part of the NFT and can be read later (by your backend, marketplaces, or a Telegram bot).

### How to send custom attributes

**API (mint-nft):**

```json
{
  "Title": "VIP Pass",
  "Symbol": "VIP",
  "ImageUrl": "https://...",
  "JSONMetaDataURL": "https://...",
  "MetaData": {
    "telegram_chat_id": "-1001234567890",
    "access_type": "private_chat",
    "tier": "vip"
  }
}
```

**MCP (`oasis_mint_nft`):**  
Pass the same structure in the **`MetaData`** argument (e.g. `attributes` / traits plus access fields as above).

**Metaplex-style traits (optional):**  
If you want compatibility with OpenSea/etc., you can also send attributes as an array of `{ "trait_type": "Access", "value": "Private Chat" }`. The backend currently serialises `MetaData` as the `attributes` object; if you pass an array, it will be stored as-is. So you can do either:

- **Custom keys** for your app (e.g. `telegram_chat_id`, `access_type`) — good for access control logic.
- **trait_type / value** — good for display and marketplaces.

You can mix both in the same `MetaData` (e.g. custom keys for the bot + trait_type/value for marketplaces).

---

## 2. Using attributes for “allow access to private chats” (ideally Telegram)

### Idea

- When **minting** the NFT, you store in **MetaData**:
  - **Which Telegram chat** the holder is allowed into (e.g. `telegram_chat_id` or a **tier/id** that your backend maps to a chat).
  - Optionally: `access_type`, `tier`, `expiry`, etc.
- When a **user** wants to join:
  - They interact with **your Telegram bot** (e.g. “Join private chat” or use a deep link).
  - Bot asks them to **prove they hold the NFT** (e.g. connect wallet, or link OASIS avatar and you check that avatar’s NFTs).
  - Your **backend** checks:
    - That the wallet/avatar **owns** the right NFT (e.g. by mint address or collection).
    - Optionally reads the NFT’s **attributes** (from chain or from OASIS/API) to see **which** chat(s) or tier they get.
  - If the check passes, the bot **grants access** to the private chat (e.g. by sending a one-time invite link, or by adding the user if your bot is admin and Telegram allows it).

So: **custom attributes on the NFT** (e.g. `telegram_chat_id`, `access_type`) **+ Telegram bot that verifies ownership and grants access** = “NFT allows access to private Telegram chat”.

---

## 3. Suggested attribute schema for Telegram access

You can keep it minimal and extensible:

| Attribute (key)       | Example value        | Use |
|-----------------------|----------------------|-----|
| `telegram_chat_id`   | `-1001234567890`     | Telegram supergroup/channel ID the NFT grants access to. |
| `access_type`        | `private_chat`       | So your bot knows this is “private chat” access (vs e.g. “DM only”). |
| `tier`               | `vip`, `alpha`       | Optional: map one NFT type to several chats or rules. |
| `telegram_invite_link` | (optional)         | Pre-generated invite; bot can send this after verification instead of creating a new one. |

- **Who sets these:** You (or your minting flow) when creating the “access pass” NFTs. The meme coin creator could have one “VIP” NFT that has `telegram_chat_id` = their private group.
- **Who reads these:** Your backend / Telegram bot when a user asks to join. Bot fetches the user’s NFT (from OASIS or chain), reads `telegram_chat_id` / `tier`, then grants access to the right chat.

---

## 4. Telegram-side flow (high level)

1. **Setup**
   - Create a **Telegram bot** (BotFather).
   - Add the bot to the **private group** as admin (with permission to invite users or create invite links).
   - Your backend stores: which NFT mint (or collection) = which chat/tier; optionally store `telegram_chat_id` only in NFT attributes and resolve chat from that.

2. **User wants access**
   - User opens the bot (e.g. “Join private chat” button or link).
   - Bot asks: “Connect wallet” or “Sign in with OASIS” (or “Send your wallet address” for a simple flow).
   - User connects wallet or links OASIS avatar.

3. **Verify**
   - Backend checks that the wallet/avatar **holds** the required NFT (OASIS API: e.g. NFTs by avatar; or Solana/ETH RPC by wallet).
   - Backend optionally reads the NFT’s **attributes** (from OASIS or from the NFT’s metadata URI) to get `telegram_chat_id` or `tier`.
   - If the NFT grants access to the chat the user asked for (or to a default chat), proceed.

4. **Grant access**
   - **Option A:** Bot creates a **one-time invite link** for that Telegram chat and sends it to the user in DM (Telegram API: `createChatInviteLink`).
   - **Option B:** If your group allows “add by username/phone”, bot can add the user by their Telegram user id (you’d need to link Telegram user ↔ wallet or OASIS avatar once, e.g. via the bot).

So: **ideally on Telegram** = the whole “prove NFT → get access” flow happens inside Telegram (user talks to the bot, connects wallet or OASIS, bot sends invite or adds them to the private chat).

---

## 5. What you already have in-repo

- **Custom attributes:** Supported in **mint-nft** (API and MCP) via **`MetaData`**; stored as **`attributes`** in the minted JSON.
- **Telegram:**  
  - **TelegramOASIS** provider and **TelegramBotService** (currently commented out) exist; TimoRides/telegram gamification stubs are present.  
  - So the **integration point** (Telegram bot + backend) is there; you’d add the “verify NFT → read attributes → grant Telegram access” logic and expose it via the bot.

---

## 6. Practical next steps

1. **Mint with access attributes**  
   When minting “access pass” NFTs (e.g. for the meme coin creator), include **`MetaData`** with e.g. `telegram_chat_id`, `access_type`, and optionally `tier`. Use existing mint-nft API/MCP; no backend change required for storage.

2. **Backend: resolve NFT → chat**  
   Small API or bot logic: given a wallet or OASIS avatar, load their NFTs (and optionally NFT attributes). Decide which Telegram chat(s) they can join from attributes (and/or from a config map: mint/collection → chat).

3. **Telegram bot: verify + grant**  
   Re-enable or extend **TelegramBotService** (or a new bot):  
   - Handle “Join private chat”.  
   - Call your backend to verify NFT ownership (+ read attributes).  
   - If allowed, create invite link and send to user (or add user to group if you have their Telegram id and permissions).

4. **Optional: OASIS avatar ↔ Telegram**  
   If you want “Sign in with OASIS” in Telegram: bot links Telegram user id to OASIS avatar (e.g. after wallet connect or login); then “verify NFT” uses avatar’s NFTs in OASIS. That way access is tied to identity you already manage.

---

**Bottom line:** You can add custom attributes to NFTs today (e.g. `telegram_chat_id`, `access_type`) and use them to allow access to private Telegram chats by having a Telegram bot verify NFT ownership (and optionally attributes) and then grant access (invite link or add to group). OASIS already supports the attributes; the remaining work is the bot + verification + access-grant flow on Telegram.
