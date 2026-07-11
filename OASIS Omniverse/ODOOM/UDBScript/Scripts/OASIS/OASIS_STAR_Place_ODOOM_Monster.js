/// <reference path="../../../udbscript.d.ts" />

`#version 4`;

`#name OASIS STAR - Place ODOOM monster (cursor)`;

`#description Places an ODOOM monster at mouse. Edit MONSTER_ID. Imp=3001, Demon=3002, Caco=3005, Baron=3003, Revenant=65, Arch-Vile=64, Spider=7, Cyber=16.`;

var MONSTER_ID = 3001;  // 3001=Imp, 3002=Demon, 3005=Cacodemon, 3003=Baron, 65=Revenant, 64=Arch-Vile, 7=Spider Mastermind, 16=Cyberdemon

var pos = UDB.Map.mousePosition;
if (!pos) { UDB.log("OASIS STAR: Click in map view first."); return; }
var t = UDB.Map.createThing(pos, MONSTER_ID);
if (UDB.Map.isUDMF) { t.flags.skill1 = t.flags.skill2 = t.flags.skill3 = t.flags.skill4 = t.flags.skill5 = true; }
else { t.flags['1'] = t.flags['2'] = t.flags['4'] = true; }
UDB.log("OASIS STAR: Placed monster type " + MONSTER_ID);
