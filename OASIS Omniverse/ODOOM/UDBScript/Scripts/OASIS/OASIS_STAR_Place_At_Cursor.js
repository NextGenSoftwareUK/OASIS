/// <reference path="../../../udbscript.d.ts" />

`#version 4`;

`#name OASIS STAR - Place at cursor`;

`#description Places an OASIS/ODOOM thing at the current mouse position. Edit THING_TYPE below to place different assets (see OASIS STAR - README). Use for keycards, monsters, weapons, health, ammo.`;

// --- OASIS STAR: change this to place a different asset (Doom thing type) ---
// Keys: 5=blue, 13=red, 6=yellow, 38/39/40=skulls | Weapons: 2001-2006 | Ammo: 2007,2008,2010,2047-2049 | Health: 2011-2016 | Monsters: 3001 imp, 3002 demon, 3003 baron, 3004 zombieman, 3005 caco, 9 sergeant, 64 arch-vile, 65 revenant, 66 mancubus, 67 arach, 68 pain elemental, 69 hell knight, 7 spider, 16 cyber
var THING_TYPE = 13;  // Red keycard (OQUAKE silver key equivalent)

var pos = UDB.Map.mousePosition;
if (!pos) {
    UDB.log("OASIS STAR: No mouse position (click in map view first).");
} else {
    var t = UDB.Map.createThing(pos, THING_TYPE);
    if (UDB.Map.isUDMF) {
        t.flags.skill1 = t.flags.skill2 = t.flags.skill3 = t.flags.skill4 = t.flags.skill5 = true;
    } else {
        t.flags['1'] = t.flags['2'] = t.flags['4'] = true;
    }
    UDB.log("OASIS STAR: Placed thing type " + THING_TYPE + " at " + pos.x + ", " + pos.y);
}
