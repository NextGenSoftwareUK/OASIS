# DOOM in Browser — OASIS Avatar in the Game

Play DOOM in the browser with your **OASIS avatar in the game**: a bar at the top shows your portrait, name, level, and karma while you play.

---

## WASM build status (weapon NFT notifications)

**The WASM build with in-game weapon NFT notifications is not working yet** unless you rebuild it yourself.

- **Source is patched:** `doom-wasm-src/src/doom/p_inter.c` calls `Module.onWeaponPickedUp(weapon)` when you pick up a weapon. The launcher listens for that and mints the NFT + shows the notification.
- **Current `doom-wasm-build/` files:** The existing `doom.js` and `doom.wasm` in `doom-wasm-build/` were **not** built from that patched source (they don’t contain the callback). So the game may load, but weapon pickup will not trigger NFT minting or notifications.
- **To get NFT notifications working:** Rebuild from the patched source and copy the output into `doom-wasm-build/`:

  **Option A – Docker (recommended):**
  ```bash
  cd doom-browser
  chmod +x build-docker.sh
  ./build-docker.sh
  ```

  **Option B – Local Emscripten:**
  ```bash
  cd doom-browser
  chmod +x doom-wasm/build-and-copy.sh
  ./doom-wasm/build-and-copy.sh
  ```
  (Requires `brew install emscripten` and `doom1.wad` in `doom-wasm-src/src/`.)

  Then run `npm start` and click **Play DOOM**. Picking up a weapon in-game should show the notification and mint the NFT.

---

## DOOM only (no portal)

To play DOOM with no OASIS/portal: start the server (`npm start`), then open:

**http://localhost:8765/doom-only.html** (or 8766 if 8765 is in use)

That page loads the js-dos emulator and runs the DOOM bundle from this server. No login, no launcher—just the game.

---

## Where is DOOM hosted?

**Doom is not hosted anywhere by default.** You run it locally from this folder.

- **Location:** `OASIS_CLEAN/doom-browser/`
- **You must start the server yourself** (see below).
- **URL:** After `npm start`, the terminal prints e.g. `http://localhost:8765` or `http://localhost:8766`. **Open that exact URL in your browser.**

The portal’s “Play” link only opens that URL with your token; it does not start the Doom server. If the server is not running, the page will not load.

---

## What actually runs when you click "Play DOOM"

**Play DOOM** loads **doom-jsdos-embed.html**, which runs the **js-dos** emulator with the DOOM bundle from `/doom.jsdos` (proxied from cdn.dos.zone). This is the version that works in the browser. The local WASM build (`doom-wasm-embed.html`) has an IWAD lookup issue and is not used by default.

## Run it (avatar in the game)

1. **Install and start the Node server** (this proxies the DOOM bundle so it loads on the same page):

   ```bash
   cd doom-browser
   npm install
   npm start
   ```

2. **Run ONODE** so you can log in (e.g. from `ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI`):

   ```bash
   dotnet run
   ```

3. Open **http://localhost:8765**. Log in with your OASIS username and password.

4. Click **“Play DOOM (avatar in game)”**. The game runs on the same page with your avatar bar at the top (portrait, name, level, karma). Use **Exit game** to return to the launcher.

**Why Node?** The DOOM bundle is fetched by the server from a CDN (same-origin proxy). The browser only talks to your local server; it never connects to dos.zone or any external game site. The server serves `/doom.jsdos` so js-dos can run the game on this page with your avatar visible.

## Fallback (no Node)

If you only run a static server (e.g. `python3 -m http.server 8765 --directory doom-browser`), the “Play DOOM (avatar in game)” button will fail to load the bundle. You must run `npm start` in this folder so the server can proxy the bundle.

## Custom API URL

Open `http://localhost:8765?api=https://api.oasisweb4.com` to use the production OASIS API. Ensure CORS allows `http://localhost:8765`.

## Controls

Arrow keys to move, Ctrl to fire. First load may take a moment while the WASM and IWAD load.

---

## Weapon pickup → NFT (notification when you collect)

When you **pick up a weapon in DOOM** (shotgun, chaingun, etc.), the game calls out to the page, the weapon NFT is minted and sent to your avatar, and a **notification** appears with the weapon image and “You received: [weapon] / NFT sent to your avatar.”

This requires the **WASM build to be compiled from doom-wasm-src** with the weapon-pickup patch (in `doom-wasm-src/src/doom/p_inter.c`). If you use a pre-built doom.wasm that wasn’t compiled with that patch, the callback never runs. To get notification-on-pickup:

1. From `doom-browser/doom-wasm-src`, build the Emscripten target (see that folder’s README / build scripts).
2. Copy the built `doom.js` and `doom.wasm` into `doom-browser/doom-wasm-build/`.
3. Ensure `doom1.wad` is in `doom-wasm-build/` (it is often included or downloadable).
4. Run `npm start` in doom-browser and click **Play DOOM**; when you pick up a weapon, the notification and NFT mint will run.
