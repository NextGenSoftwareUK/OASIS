# OASIS Omniverse — Solana Graveyard Hackathon: Sponsor Integrations

## Introduction

We are entered in the **Solana Graveyard Hackathon** (Feb 12–27, 2026 — submission deadline **Feb 27**). The hackathon theme is "resurrect dead categories" and prizes are awarded per sponsor track.

Our project is the **OASIS Omniverse**: a cross-game metaverse layer built on top of the OASIS STAR API that connects DOOM (ODOOM) and Quake (OQuake) through a shared, persistent, on-chain identity and inventory system on Solana. Items collected in one game unlock things in the other. The browser-based DOOM build (`doom-browser/`) already runs DOOM in the browser with an OASIS avatar bar, weapon NFT minting on pickup, and XP tracking.

**The hackathon prize money is directly tied to these three integrations being live and demoable.** Without them, we cannot claim any sponsor bounties. The integrations below are scoped to be achievable in the remaining time before the deadline. They are all frontend/JS additions to the existing `doom-browser/` project — no new Rust programs or blockchain deployments required.

---

## Prize Summary

| Track | Sponsor | Prize (1st) | Total Available |
|---|---|---|---|
| Gaming | MagicBlock | $2,500 | $5,000 across 3 places |
| Onchain Social | Tapestry | $2,500 | $5,000 across 3 places |
| Metaverse | Portals | $2,500 | $3,500 across 2 places |

**Combined maximum: $10,000+** across the three tracks.

---

## Codebase Context

All three integrations are additions to the `doom-browser/` directory:

```
OASIS_CLEAN/doom-browser/
├── index.html          ← Main launcher + game UI (all JS is inline in this file)
├── server.js           ← Express server (Node.js), currently just serves static files + proxies doom bundle
├── package.json        ← Currently only has "express" as a dependency
├── weapon-nft-catalog.json  ← Defines 7 DOOM weapons with rarity tiers (Shotgun → BFG 9000 legendary)
└── doom-wasm-src/
    └── src/doom/p_inter.c   ← Patched C source: calls Module.onWeaponPickedUp(weapon) on pickup
```

**Key existing hooks in `index.html`:**

- `doMintWeapon(weapon)` — already calls `POST /api/nft/mint-nft` on the OASIS API when a weapon is picked up
- `showWeaponNftNotification(weapon)` — shows a DOOM-style overlay notification
- `addXpForGameAction(amount)` — already updates OASIS avatar XP via the API
- `window.awardXpMission()` — triggered by the MISSION button in the game HUD
- `currentProfile` — populated on login, contains `username`, `level`, `karma`, `xp`, `portraitUrl`
- `gameStartTime` — set when the game starts (can be used to calculate a session score)
- `sessionStorage.getItem('oasis_doom_jwt')` — the player's auth JWT
- `sessionStorage.getItem('oasis_doom_avatar_id')` — the player's OASIS avatar ID

The game already calls `Module.onWeaponPickedUp(weapon)` from the patched WASM C source when a weapon is picked up in-game. The JS listens for this and calls `doMintWeapon`.

---

---

# Integration 1: MagicBlock — Gaming Track ($2,500–$5,000)

## What It Is

MagicBlock is a Solana gaming infrastructure provider. They offer two products relevant to us:

1. **SOAR** (Solana On-Chain Achievement & Ranking) — a TypeScript SDK for onchain leaderboards, player profiles, and achievements. This is the primary integration target.
2. **Session Keys** — a SDK that lets players sign a session token once, then the app sends transactions on their behalf without requiring a wallet popup on every action.

## What to Build

### Part A: SOAR Leaderboard (Required)

Integrate `@magicblock-labs/soar-sdk` to submit the player's kill/score count to an onchain SOAR leaderboard when they exit DOOM.

**The flow:**
1. When the game starts, the `gameStartTime` timestamp is set in `index.html`
2. When the player clicks EXIT, calculate `sessionScore` (e.g. time survived in seconds, or a mock kill count for demo purposes)
3. Submit that score to the SOAR leaderboard via `client.submitScoreToLeaderBoard(...)`
4. Show a confirmation in the existing `showXpToast()` function (e.g. "Score submitted to leaderboard!")

**What needs to be initialized once (server-side or in a setup script):**
- Create a SOAR game account using `client.initializeNewGame(...)` — this only needs to be done once; store the resulting game public key
- Create a leaderboard for that game using `client.addNewGameLeaderBoard(...)` — store the leaderboard PDA

Both of these can be done via a one-time setup script in `doom-browser/soar-setup.js`.

### Part B: BFG 9000 Achievement (Nice to Have)

When the player picks up the BFG 9000 (`doomednum: 2005`, `rarity: "legendary"` in `weapon-nft-catalog.json`), register a SOAR achievement. The existing `Module.onWeaponPickedUp` callback already fires for this.

## SDK and Docs

- **SDK:** `@magicblock-labs/soar-sdk`
  - Install: `npm install @magicblock-labs/soar-sdk`
  - Full docs: https://docs.magicblock.gg/pages/tools/open-source-programs/SOAR.md
- **MagicBlock docs index:** https://docs.magicblock.gg/llms.txt
- **Hackathon track info:** https://magicblock.xyz

## Implementation Notes

The SOAR SDK is TypeScript — since `server.js` is plain Node.js and `index.html` uses vanilla JS, the cleanest approach is to add a new server-side route in `server.js` that the frontend calls via `fetch`. This keeps all SOAR keypair/authority logic server-side and out of the browser.

**Add to `server.js`:**

```js
// New route: POST /api/soar/submit-score
// Body: { playerWallet: string, score: number }
app.post('/api/soar/submit-score', express.json(), async (req, res) => {
  // Use SOAR SDK here with the game authority keypair (loaded from env)
  // client.submitScoreToLeaderBoard(playerWallet, authWallet, leaderboardPda, score)
  res.json({ success: true });
});
```

**Add to `index.html` (inside `exitGame()` function):**

```js
function exitGame() {
  var score = Math.floor((Date.now() - gameStartTime) / 1000); // seconds survived
  var walletAddress = currentProfile && currentProfile.solanaWallet; // if available
  if (walletAddress && score > 0) {
    fetch('/api/soar/submit-score', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ playerWallet: walletAddress, score: score })
    }).then(function(r) { return r.json(); })
      .then(function() { showXpToast('Score submitted to leaderboard!'); });
  }
  // ... existing exit logic
}
```

**Environment variables needed in `.env`:**
```
SOAR_AUTH_KEYPAIR=<base58 keypair of the game authority wallet>
SOAR_GAME_ADDRESS=<public key of the SOAR game account>
SOAR_LEADERBOARD_PDA=<public key of the leaderboard>
```

## Files to Modify

| File | Change |
|---|---|
| `doom-browser/package.json` | Add `@magicblock-labs/soar-sdk`, `@solana/web3.js`, `dotenv` |
| `doom-browser/server.js` | Add `POST /api/soar/submit-score` route |
| `doom-browser/soar-setup.js` | New one-time setup script to create game + leaderboard |
| `doom-browser/index.html` | Call `/api/soar/submit-score` inside `exitGame()` function; show toast |

---

---

# Integration 2: Tapestry — Onchain Social Track ($2,500–$5,000)

## What It Is

Tapestry is Solana's leading onchain social protocol. It provides a REST API (and `socialfi` npm package) for:

- **Profiles** — create or find an onchain social profile linked to a wallet address
- **Content** — post content nodes (text, links) associated with a profile
- **Follows** — follow other profiles
- **Likes/Comments** — engage with content

They explicitly want apps that bring social experiences onchain. Our integration: after a DOOM session ends, auto-post the player's result (score, weapons collected) as an onchain Tapestry content node.

## What to Build

### Part A: Profile Creation on Login (Required)

When the player logs in with their OASIS credentials and their Solana wallet address is available, call Tapestry's `findOrCreate` endpoint to create (or retrieve) their Tapestry profile linked to their wallet.

**Flow:**
1. After successful OASIS login (inside `handleLogin` callback in `index.html`), extract the player's Solana wallet address from `currentProfile`
2. Call `POST https://api.usetapestry.dev/v1/profiles/findOrCreate?apiKey=YOUR_KEY` with the wallet address and OASIS username
3. Store the returned Tapestry profile ID in `sessionStorage`

### Part B: Post-Game Social Post (Required)

When the player exits DOOM (inside or after `exitGame()`), auto-post their session result as a Tapestry content node.

**Content to post:** e.g. `"I just played DOOM in the OASIS Omniverse! Survived 142 seconds. Weapons collected: Shotgun, Chaingun. Play at: https://oasisweb4.com 🔥 #OASISMetaverse #Solana"`

**Flow:**
1. On exit, build the post text string from `currentProfile.username`, session duration, and list of weapons collected during the session
2. Call `POST https://api.usetapestry.dev/v1/content?apiKey=YOUR_KEY` with the wallet address and text
3. Show a notification: "Your game result was posted to your onchain feed!"

### Part C: "Challenge a Friend" Follow Button (Nice to Have)

Add a small UI element in the launcher (post-game screen) with a text input for "enter a friend's wallet to follow them on Tapestry". Calls the Tapestry follow endpoint. This demonstrates the social graph feature.

## SDK and Docs

- **npm package:** `socialfi`
  - Install: `npm install socialfi`
  - Source: https://github.com/Primitives-xyz/tapestry-template
- **REST API base:** `https://api.usetapestry.dev/v1/`
- **Quickstart:** https://docs.usetapestry.dev/
- **Get API key:** https://app.usetapestry.dev/
- **Hackathon track info:** https://usetapestry.dev

## Implementation Notes

Tapestry is a pure REST API — no blockchain transactions, no wallet required for posting. All calls can be made directly from the browser (`index.html`) with the API key. No server-side routes needed.

**Add to `index.html` (after successful login):**

```js
async function createOrFindTapestryProfile(walletAddress, username) {
  var TAPESTRY_API_KEY = 'YOUR_TAPESTRY_API_KEY'; // store this safely
  var resp = await fetch(
    'https://api.usetapestry.dev/v1/profiles/findOrCreate?apiKey=' + TAPESTRY_API_KEY,
    {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        walletAddress: walletAddress,
        username: username,
        bio: 'Playing DOOM in the OASIS Omniverse',
        blockchain: 'SOLANA',
        execution: 'FAST_UNCONFIRMED'
      })
    }
  );
  var profile = await resp.json();
  sessionStorage.setItem('tapestry_profile_id', profile.id || '');
  return profile;
}
```

**Add to `index.html` (inside or after `exitGame()`):**

```js
async function postGameResultToTapestry(walletAddress, score, weaponsCollected) {
  var TAPESTRY_API_KEY = 'YOUR_TAPESTRY_API_KEY';
  var weapons = weaponsCollected.length > 0 ? weaponsCollected.join(', ') : 'none';
  var text = 'I just played DOOM in the OASIS Omniverse! '
    + 'Survived ' + score + ' seconds. '
    + 'Weapons collected: ' + weapons + '. '
    + 'Play at oasisweb4.com #OASISMetaverse #Solana';
  await fetch(
    'https://api.usetapestry.dev/v1/content?apiKey=' + TAPESTRY_API_KEY,
    {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        walletAddress: walletAddress,
        text: text,
        execution: 'FAST_UNCONFIRMED'
      })
    }
  );
}
```

**Track weapons collected during a session:**
Add a `var weaponsCollectedThisSession = []` variable at the top of the script block. Push to it inside the existing `doMintWeapon()` success callback. Reset it when a new game starts.

**Environment/config needed:**
```
TAPESTRY_API_KEY=<from https://app.usetapestry.dev/>
```

## Files to Modify

| File | Change |
|---|---|
| `doom-browser/index.html` | Add `createOrFindTapestryProfile()` called after login; add `weaponsCollectedThisSession` array; call `postGameResultToTapestry()` inside `exitGame()`; optional "Challenge a friend" UI |
| `doom-browser/package.json` | Optionally add `socialfi` if moving any logic server-side |

---

---

# Integration 3: Portals — Metaverse Track ($2,500–$3,500)

## What It Is

Portals (theportal.to) is a browser-based 3D metaverse platform on Solana. Players can build and visit spatial 3D worlds in the browser — no download required. Portals Studio is a drag-and-drop + scripting world builder.

The metaverse track explicitly asks to "build virtual worlds, 3D spaces, or spatial experiences on Portals." Our integration: build the **OASIS Omniverse Lobby** — a Portals space that serves as the spatial hub connecting ODOOM, OQuake, and the SOAR leaderboard.

## What to Build

### The OASIS Omniverse Lobby Space

A Portals world that contains:

1. **A portal object to DOOM** — clicking it opens the `doom-browser` URL (e.g. `https://oasisweb4.com/doom`) in a new tab or iframe
2. **A portal object to Quake** — clicking it opens the OQuake/ODOOM download or web page
3. **A leaderboard display** — a scoreboard object in the world that fetches and displays the top SOAR scores (can be a screen with a URL embed, or text rendered via Portals scripting)
4. **OASIS branding** — use the OASIS logo (`oasis-logo.png`), dark/doom aesthetic

### How Portals Works

Portals worlds are built visually in the **Portals Studio** at theportal.to (web-based editor). You:
1. Create a Portals account at https://theportal.to
2. Click "Create World" to open the Studio editor
3. Use drag-and-drop to place 3D objects, set their appearance, and attach **Function Effects** (Portals' scripting system) to them
4. Publish the world — it gets a public URL immediately accessible in any browser

**Function Effects** (Portals scripting) allow objects to:
- Open external URLs when clicked (`openURL` function)
- Display text content
- Fetch external data and render it

### Step-by-Step Build Plan

1. **Create account** at https://theportal.to
2. **Create a new space** — choose a dark/sci-fi template that fits the DOOM/Quake aesthetic
3. **Add a portal object for DOOM:**
   - Place any object (door, archway, glowing panel) in the world
   - Attach a Function Effect: on click → `openURL("https://oasisweb4.com/doom")` (or localhost for demo)
   - Label it "ODOOM — Play DOOM"
4. **Add a portal object for Quake:**
   - Same as above but pointing to the OQuake page/download
   - Label it "OQuake — Play Quake"
5. **Add a leaderboard screen:**
   - Place a flat panel/screen object
   - Use a Function Effect to embed or display the top scores fetched from the SOAR leaderboard endpoint
   - For the demo, a static "Top Players" display is acceptable
6. **Add OASIS branding:**
   - Upload `doom-browser/oasis-logo.png` as a texture on a wall or sign
   - Name the world "OASIS Omniverse"
7. **Publish** — copy the public URL for the submission video

### Demo Script

Record a 60-second clip of:
1. Walking around the OASIS Omniverse Portals lobby
2. Approaching the DOOM portal and clicking it
3. The browser DOOM game opening with the OASIS avatar bar
4. Picking up a weapon → NFT notification fires
5. Exiting DOOM → game result posted to Tapestry feed

## SDK and Docs

- **Portals web app (create your world here):** https://theportal.to
- **Building guide:** https://prtls.gitbook.io/portals-building-guide
- **Advanced tooling / Function Effects:** https://prtls.gitbook.io/portals-building-guide/advanced-tooling/function-effect
- **How to create a space:** https://prtls.gitbook.io/portals-building-guide/building-basics/readme/creating-a-space
- **Hackathon track info:** https://theportal.to

## Files to Modify / Create

| Item | Change |
|---|---|
| Portals Studio (web editor) | Build the OASIS Omniverse Lobby world |
| `doom-browser/doom-oasis-bg.png` or `oasis-logo.png` | Upload as textures into Portals Studio |
| (Optional) New `doom-browser/leaderboard.html` | A simple HTML page showing SOAR top scores, embeddable in the Portals world via URL |

---

---

## Recommended Build Order

Given the submission deadline, tackle these in this order:

| Priority | Task | Estimated Time |
|---|---|---|
| 1 | Get a Tapestry API key and add profile creation + post-game posting to `index.html` | 2–3 hours |
| 2 | Set up SOAR: run one-time setup script, add `/api/soar/submit-score` route to `server.js`, call it from `exitGame()` | 3–4 hours |
| 3 | Build the Portals lobby world in their Studio editor | 2–3 hours |
| 4 | Record the demo video showing all three integrations in action | 1 hour |
| 5 | Submit at https://solanafoundation.typeform.com/graveyard-hack | 30 min |

**Total estimated: ~1.5 days of focused work.**

---

## Questions / Contacts

- MagicBlock Discord: https://discord.com/invite/MBkdC3gxcv
- Tapestry Telegram: https://t.me/+E1ngIfYLaB1kOGY5
- Portals: https://theportal.to (create an account to access the builder)
- Solana Graveyard Hackathon signup: https://solanafoundation.typeform.com/graveyard-hack
