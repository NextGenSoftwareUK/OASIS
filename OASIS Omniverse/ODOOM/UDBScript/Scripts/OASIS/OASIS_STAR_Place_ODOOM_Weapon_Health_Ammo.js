/// <reference path="../../../udbscript.d.ts" />

`#version 4`;

`#name OASIS STAR - Place ODOOM weapon/health/ammo (cursor)`;

`#description Places weapon, health, or ammo at mouse. Edit CATEGORY and ITEM. Weapons: 2001-2006. Health: 2011-2016. Ammo: 2007,2008,2010,2047-2049.`;

// category: "weapon" | "health" | "ammo"  and  item: id from that list
var CATEGORY = "weapon";
var ITEM = "shotgun";  // shotgun, chaingun, rocket_launcher, plasma_rifle, chainsaw, bfg9000 | medikit, stimpack, soul_sphere, etc. | clip, shells, rocket, cell, cell_pack, ammo_box

var ITEMS = {
    weapon: { shotgun: 2001, chaingun: 2002, rocket_launcher: 2003, plasma_rifle: 2004, chainsaw: 2005, bfg9000: 2006 },
    health: { medikit: 2011, stimpack: 2012, soul_sphere: 2013, health_potion: 2014, armor_bonus: 2015, armor_helmet: 2016 },
    ammo: { clip: 2007, shells: 2008, rocket: 2010, cell: 2047, cell_pack: 2048, ammo_box: 2049 }
};
var type = (ITEMS[CATEGORY] && ITEMS[CATEGORY][ITEM]) ? ITEMS[CATEGORY][ITEM] : 2011;
var pos = UDB.Map.mousePosition;
if (!pos) { UDB.log("OASIS STAR: Click in map view first."); return; }
var t = UDB.Map.createThing(pos, type);
if (UDB.Map.isUDMF) { t.flags.skill1 = t.flags.skill2 = t.flags.skill3 = t.flags.skill4 = t.flags.skill5 = true; }
else { t.flags['1'] = t.flags['2'] = t.flags['4'] = true; }
UDB.log("OASIS STAR: Placed " + CATEGORY + " " + ITEM + " (type " + type + ").");
