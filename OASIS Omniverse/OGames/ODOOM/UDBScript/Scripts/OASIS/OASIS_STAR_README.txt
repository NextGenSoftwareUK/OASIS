OASIS STAR API - UDBScript scripts for ODOOM/OQUAKE cross-game assets
=======================================================================

These scripts let you place ODOOM things (and OQUAKE-equivalent assets) in your
ODOOM maps from Ultimate Doom Builder. Run them from the Scripts docker or assign
hotkeys (Tools → Preferences → Controls).

SCRIPTS
-------
  OASIS_STAR_Place_Selected.js      Pick asset from dialog, then click map to place (requires UDB.setPendingStarPlacement).
  OASIS_STAR_Place_At_Cursor.js     Place any Doom thing at mouse (edit THING_TYPE).
  OASIS_STAR_Place_ODOOM_Key.js     Place keycards/skulls (edit WHICH: red, blue, yellow, skull_*).
  OASIS_STAR_Place_ODOOM_Monster.js Place monsters (edit MONSTER_ID).
  OASIS_STAR_Place_ODOOM_Weapon_Health_Ammo.js  Place weapons/health/ammo (edit CATEGORY and ITEM).

USAGE
-----
1. Open your map in Ultimate Doom Builder.
2. Open the Scripts docker (View → Scripts or similar).
3. Expand OASIS and double-click a script, or select and click Run.
4. For "Place at cursor" scripts: click in the 2D map view to set mouse position, then run the script.

OQUAKE ASSETS IN ODOOM MAPS
----------------------------
To place an OQUAKE-style asset in an ODOOM map we use the Doom equivalent thing type:
  - OQUAKE Silver Key  → Doom Red Keycard   (type 13)
  - OQUAKE Gold Key   → Doom Blue Keycard   (type 5)
Use OASIS_STAR_Place_ODOOM_Key.js with WHICH = "red" or "blue" for those.

FULL ASSET LIST
---------------
See Config/oasis_star_assets.json in the OASIS Omniverse repo for all thing type IDs
and OQUAKE entity classnames. Map conversion (OQUAKE .map ↔ ODOOM WAD) is available
via the OASIS STAR menu in the ODOOM launcher (Editor tab).

STAR MENU / TOOLBAR IN UDB
---------------------------
To add a STAR menu and star icon to Ultimate Doom Builder's toolbar you need to
build UDB from source and add a menu item that runs these scripts or opens the
Scripts docker on the OASIS folder. See OASIS Omniverse docs for a UDB patch.
