OASIS STAR API – Cross-game assets in ODOOM Editor
============================================================

Where to find STAR
------------------
• Menu bar: "★ STAR" (between Prefabs and Tools).
• View menu: View → OASIS STAR.
• Tools menu: Tools → OASIS STAR.
• Toolbar: ★ button on the File toolbar (next to New / Open / Save). If you don’t see it, turn on View → Toolbars → File.

If you don’t see any of these:
1. Rebuild the UDBScript project (right-click UDBScript → Build).
2. Run Builder.exe from the Build folder (e.g. Build\Builder.exe), not from another copy.
3. Check the log (Help → Show Error Log or the log file): look for "OASIS STAR: Menu and toolbar added" or "OASIS STAR init failed".

What STAR does
--------------
• Place ODOOM / OQUAKE assets (keycards, monsters, weapons, health, ammo) at the cursor.
• Convert OQUAKE .map → Doom thing list.
• Convert current ODOOM map → OQUAKE .map (point entities).

Place asset: click in the 2D map, then use STAR → Place ODOOM/OQUAKE asset at cursor (or the ★ toolbar button). Choose the asset in the script dialog. Same thing types work in both games (e.g. Silver Key in a Doom map opens Silver doors in OQuake).

Next step: modify ODOOM and OQUAKE runtimes so cross-game assets work in-game (e.g. OQUAKE weapons in ODOOM).











