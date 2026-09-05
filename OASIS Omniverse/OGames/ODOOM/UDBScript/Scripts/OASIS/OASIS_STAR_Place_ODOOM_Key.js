/// <reference path="../../../udbscript.d.ts" />

`#version 4`;

`#name OASIS STAR - Place ODOOM key (cursor)`;

`#description Places an ODOOM keycard/skull at mouse. Keys: Blue=5, Red=13, Yellow=6, Skull R/G/B=38/39/40. OQUAKE silver→13, gold→5.`;

var KEY_TYPES = { blue: 5, red: 13, yellow: 6, skull_red: 38, skull_blue: 39, skull_yellow: 40 };
var WHICH = "red";  // change to blue, yellow, skull_red, skull_blue, skull_yellow

var pos = UDB.Map.mousePosition;
if (!pos) { UDB.log("OASIS STAR: Click in map view first."); return; }
var type = KEY_TYPES[WHICH] || 13;
var t = UDB.Map.createThing(pos, type);
if (UDB.Map.isUDMF) { t.flags.skill1 = t.flags.skill2 = t.flags.skill3 = t.flags.skill4 = t.flags.skill5 = true; }
else { t.flags['1'] = t.flags['2'] = t.flags['4'] = true; }
UDB.log("OASIS STAR: Placed " + WHICH + " key (type " + type + ").");
