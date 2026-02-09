/// <reference path="../../../udbscript.d.ts" />

`#version 4`;

`#name OASIS STAR - Place selected asset at cursor`;

`#description Place any ODOOM or OQUAKE asset at mouse (keycards, monsters, weapons, health, ammo). Choose game and asset from the dialog. OQUAKE assets are placed as their Doom-equivalent thing for use in ODOOM maps.`;

// Build flat list: "ODOOM|category|id|name" -> doomThingType
// OQUAKE keys use cross-mapping; other OQUAKE use doom equivalent below
var ASSETS = [
    ["ODOOM", "key", "blue_keycard", "Blue Keycard", 5],
    ["ODOOM", "key", "red_keycard", "Red Keycard", 13],
    ["ODOOM", "key", "yellow_keycard", "Yellow Keycard", 6],
    ["ODOOM", "key", "skull_red", "Red Skull Key", 38],
    ["ODOOM", "key", "skull_blue", "Blue Skull Key", 39],
    ["ODOOM", "key", "skull_yellow", "Yellow Skull Key", 40],
    ["ODOOM", "weapon", "shotgun", "Shotgun", 2001],
    ["ODOOM", "weapon", "chaingun", "Chaingun", 2002],
    ["ODOOM", "weapon", "rocket_launcher", "Rocket Launcher", 2003],
    ["ODOOM", "weapon", "plasma_rifle", "Plasma Rifle", 2004],
    ["ODOOM", "weapon", "chainsaw", "Chainsaw", 2005],
    ["ODOOM", "weapon", "bfg9000", "BFG 9000", 2006],
    ["ODOOM", "ammo", "clip", "Clip", 2007],
    ["ODOOM", "ammo", "shells", "Shells", 2008],
    ["ODOOM", "ammo", "rocket", "Rocket", 2010],
    ["ODOOM", "ammo", "cell", "Cell", 2047],
    ["ODOOM", "ammo", "cell_pack", "Cell Pack", 2048],
    ["ODOOM", "ammo", "ammo_box", "Ammo Box", 2049],
    ["ODOOM", "health", "medikit", "Medikit", 2011],
    ["ODOOM", "health", "stimpack", "Stimpack", 2012],
    ["ODOOM", "health", "soul_sphere", "Soul Sphere", 2013],
    ["ODOOM", "health", "health_potion", "Health Potion", 2014],
    ["ODOOM", "health", "armor_bonus", "Armor Bonus", 2015],
    ["ODOOM", "health", "armor_helmet", "Armor Helmet", 2016],
    ["ODOOM", "monster", "zombieman", "Zombieman", 3004],
    ["ODOOM", "monster", "sergeant", "Sergeant", 9],
    ["ODOOM", "monster", "imp", "Imp", 3001],
    ["ODOOM", "monster", "demon", "Demon", 3002],
    ["ODOOM", "monster", "spectre", "Spectre", 58],
    ["ODOOM", "monster", "cacodemon", "Cacodemon", 3005],
    ["ODOOM", "monster", "baron", "Baron of Hell", 3003],
    ["ODOOM", "monster", "hell_knight", "Hell Knight", 69],
    ["ODOOM", "monster", "lost_soul", "Lost Soul", 3006],
    ["ODOOM", "monster", "revenant", "Revenant", 65],
    ["ODOOM", "monster", "mancubus", "Mancubus", 66],
    ["ODOOM", "monster", "arch_vile", "Arch-Vile", 64],
    ["ODOOM", "monster", "pain_elemental", "Pain Elemental", 68],
    ["ODOOM", "monster", "arachnotron", "Arachnotron", 67],
    ["ODOOM", "monster", "spider_mastermind", "Spider Mastermind", 7],
    ["ODOOM", "monster", "cyberdemon", "Cyberdemon", 16],
    ["OQUAKE", "key", "silver_key", "Silver Key (→Red)", 13],
    ["OQUAKE", "key", "gold_key", "Gold Key (→Blue)", 5],
    ["OQUAKE", "weapon", "shotgun", "Shotgun (→Doom)", 2001],
    ["OQUAKE", "weapon", "supershotgun", "Super Shotgun (→Doom)", 2001],
    ["OQUAKE", "weapon", "nailgun", "Nailgun (→Doom)", 2002],
    ["OQUAKE", "weapon", "supernailgun", "Super Nailgun (→Doom)", 2002],
    ["OQUAKE", "weapon", "grenadelauncher", "Grenade Launcher (→Doom)", 2003],
    ["OQUAKE", "weapon", "rocketlauncher", "Rocket Launcher (→Doom)", 2003],
    ["OQUAKE", "weapon", "lightning", "Thunderbolt (→Doom)", 2004],
    ["OQUAKE", "ammo", "shells", "Shells (→Doom)", 2008],
    ["OQUAKE", "ammo", "spikes", "Nails (→Doom)", 2007],
    ["OQUAKE", "ammo", "rockets", "Rockets (→Doom)", 2010],
    ["OQUAKE", "ammo", "cells", "Cells (→Doom)", 2047],
    ["OQUAKE", "health", "health", "Health (→Doom)", 2011],
    ["OQUAKE", "health", "health_small", "Small Health (→Doom)", 2012],
    ["OQUAKE", "health", "armor1", "Green Armor (→Doom)", 2015],
    ["OQUAKE", "health", "armor2", "Yellow Armor (→Doom)", 2016],
    ["OQUAKE", "health", "armorInv", "Mega Armor (→Doom)", 2013],
    ["OQUAKE", "monster", "grunt", "Grunt (→Doom)", 3004],
    ["OQUAKE", "monster", "ogre", "Ogre (→Doom)", 9],
    ["OQUAKE", "monster", "demon", "Demon (→Doom)", 3002],
    ["OQUAKE", "monster", "dog", "Rottweiler (→Doom)", 3002],
    ["OQUAKE", "monster", "shambler", "Shambler (→Doom)", 3003],
    ["OQUAKE", "monster", "zombie", "Zombie (→Doom)", 3004],
    ["OQUAKE", "monster", "hell_knight", "Hell Knight (→Doom)", 69],
    ["OQUAKE", "monster", "enforcer", "Enforcer (→Doom)", 66],
    ["OQUAKE", "monster", "fish", "Fish (→Doom)", 3005],
    ["OQUAKE", "monster", "spawn", "Spawn (→Doom)", 68]
];

var choiceList = ASSETS.map(function(a) { return a[0] + " – " + a[3]; });
var q = new UDB.QueryOptions();
q.addOption("asset", "Asset to place", 0, choiceList[0], choiceList);
if (!q.query()) return;
var idx = choiceList.indexOf(q.options.asset);
if (idx < 0) { UDB.log("OASIS STAR: Invalid selection."); return; }
var row = ASSETS[idx];
var doomType = row[4];
var pos = UDB.Map.mousePosition;
if (!pos) { UDB.log("OASIS STAR: Click in map view first, then run this script again."); return; }
var t = UDB.Map.createThing(pos, doomType);
if (UDB.Map.isUDMF) {
    t.flags.skill1 = t.flags.skill2 = t.flags.skill3 = t.flags.skill4 = t.flags.skill5 = true;
} else {
    t.flags['1'] = t.flags['2'] = t.flags['4'] = true;
}
UDB.log("OASIS STAR: Placed " + row[3] + " (type " + doomType + ") at cursor.");
