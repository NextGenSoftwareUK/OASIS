# DOOM Weapon Images for NFTs: Where to Find Accurate 2D and 3D Assets

You need **accurate** images (ideally **3D**) for each classic DOOM weapon for the weapon NFT catalog. Classic DOOM (1993) used **2D sprites**, not 3D models, so "3D versions" are either **fan-made 3D recreations** based on those sprites or **renders/viewer exports** from the original art. Below are vetted sources.

---

## Community guide (reference)

- **Reddit: Classic DOOM weapon guide**  
  https://www.reddit.com/r/Doom/comments/pe4haq/classic_doom_weapon_guide_for_players/  
  Good player-oriented reference; check the post and comments for any linked images or asset packs.

---

## 1. Accurate 2D sources (sprites and in-game shots)

### DoomWiki (canonical reference, CC BY-SA)

Each weapon has a dedicated page with **in-game screenshots**, pickup sprites, and data. Content is **CC BY-SA**; you can use with attribution and share-alike.

| Weapon           | DoomWiki page |
|------------------|----------------|
| Shotgun          | https://doomwiki.org/wiki/Shotgun |
| Chaingun         | https://doomwiki.org/wiki/Chaingun |
| Rocket launcher  | https://doomwiki.org/wiki/Rocket_launcher |
| Plasma gun       | https://doomwiki.org/wiki/Plasma_gun |
| BFG9000          | https://doomwiki.org/wiki/BFG9000 |
| Chainsaw         | https://doomwiki.org/wiki/Chainsaw |
| Super shotgun    | https://doomwiki.org/wiki/Super_shotgun |
| Pistol           | https://doomwiki.org/wiki/Pistol |

**Image URLs:** DoomWiki image URLs look like `https://doomwiki.org/wiki/File:MAP11_shotgun.png`. Use the "File:" links from each weapon page for the actual image files. **Check license:** CC BY-SA (https://doomwiki.org/wiki/Doom_Wiki:Copyrights).

### The Spriters Resource (original sprite sheets)

- **DOOM / DOOM II – Weapons (all-in-one sheet)**  
  - PC: https://www.spriters-resource.com/pc_computer/doomdoomii/sheet/4111/  
  - MS-DOS: https://www.spriters-resource.com/ms_dos/doomdoomii/sheet/4111/

**What you get:** Full weapon sprite sheets from the original WAD (first-person view, pickup sprites). Accurate to the game; you can crop per weapon. **License:** Typically fair use for fan/game reference; confirm their site policy if you redistribute or sell NFT art.

### ModDB – upscaled classic weapon sprites

- **Enhanced classic weapons sprites for Doom and Doom 2**  
  https://www.moddb.com/addons/upscaled-classic-weapons-sprites-for-doom-and-doom-2  

**What you get:** 2× resolution versions of the classic sprites, manually edited to stay faithful. Good for higher-res 2D NFT images. Check the addon’s license (usually mod-friendly; clarify if commercial/NFT use is allowed).

---

## 2. 3D sources (recreations; classic DOOM had no 3D models)

Classic DOOM did **not** ship 3D weapon models. Any "3D" asset is a **recreation** (often based on the same toy/photos the id artists used, or on the sprites).

### Sketchfab (3D models)

- **The Doom Shotgun (TootsieToy)**  
  https://sketchfab.com/3d-models/the-doom-shotgun-tootsietoy-4a7e015e391640db8f393af0789909b4  
  Free; based on the TootsieToy Dakota (the real toy the shotgun sprite was traced from). Good for an accurate **3D Shotgun**.

- **Search:** https://sketchfab.com/search?q=doom+weapon&type=models  
  Filter by **Downloadable** and **License** (CC BY, CC0, etc.). Many results are from **Doom 2016 / Eternal**; for **classic** look for titles that say "classic", "1993", or "original".

**Note:** Always check each model’s license (CC BY, CC0, or custom). Export stills or turntable videos for NFT images.

### DoomWiki note on the Shotgun

DoomWiki states the shotgun sprites were based on the **"TootsieToy Dakota"** (Strombecker). So 3D models that reference that toy (e.g. the Sketchfab "TootsieToy" shotgun) are **accurate to the original art source**.

### id Software source (sprites only)

- **GitHub – id-Software/DOOM**  
  https://github.com/id-Software/DOOM  

Source code is GPL; game assets (WAD/sprites) are **not** in the repo (still proprietary). So you **cannot** get official 3D weapons from here—only engine/code. Use this for behaviour reference, not art.

---

## 3. Practical pipeline for NFT images

1. **2D (easiest, most accurate to the game)**  
   - Use **DoomWiki** in-game / File images (CC BY-SA, attribute DoomWiki).  
   - Or crop from **Spriters Resource** weapon sheet per weapon.  
   - Or use **ModDB** upscaled sprites for higher-res 2D.

2. **3D (best for “3D version” ask)**  
   - Use **Sketchfab** “Doom Shotgun (TootsieToy)” for Shotgun.  
   - Search Sketchfab (and similar) for **classic** Doom Chaingun, Rocket Launcher, Plasma, BFG, Chainsaw, Super Shotgun; check license and accuracy.  
   - If no good classic 3D exists for a weapon: commission a 3D artist using DoomWiki + Spriters Resource as reference, or use a high-res 2D export for that weapon.

3. **Attribution**  
   - DoomWiki: "Image from DoomWiki.org, CC BY-SA."  
   - Sketchfab: use the license text shown on each model (e.g. "Model by X, CC BY 4.0").  
   - Spriters Resource / ModDB: follow their policies and credit the source.

---

## 4. Per-weapon quick links (2D reference)

| Weapon           | DoomWiki (reference + images)     | Sprite sheet (Spriters Resource) |
|------------------|------------------------------------|-----------------------------------|
| Shotgun          | https://doomwiki.org/wiki/Shotgun  | Sheet 4111 (SHOT, SHTG, SHTF)     |
| Chaingun         | https://doomwiki.org/wiki/Chaingun | Sheet 4111 (CHGG, CHGF)           |
| Rocket launcher  | https://doomwiki.org/wiki/Rocket_launcher | Sheet 4111 (LAUN, MISL)   |
| Plasma gun       | https://doomwiki.org/wiki/Plasma_gun     | Sheet 4111 (PLSG, PLSF)   |
| BFG 9000         | https://doomwiki.org/wiki/BFG9000        | Sheet 4111 (BFGG, BFGF)   |
| Chainsaw         | https://doomwiki.org/wiki/Chainsaw       | Sheet 4111 (CSAW)         |
| Super shotgun    | https://doomwiki.org/wiki/Super_shotgun   | Sheet 4111 (SGN2, SHT2)   |

Once you have final image URLs (hosted on your CDN or IPFS), set `imageUrl` and optionally `thumbnailUrl` in **weapon-nft-catalog.json** (or in the mint request body) for each weapon.
