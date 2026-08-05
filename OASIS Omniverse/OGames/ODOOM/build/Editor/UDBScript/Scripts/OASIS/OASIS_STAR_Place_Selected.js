/// <reference path="../../udbscript.d.ts" />

`#version 4`;

`#name OASIS STAR - Place selected asset at cursor`;

`#description Place any ODOOM or OQUAKE asset at mouse (keycards, monsters, weapons, health, ammo). Choose game first, then choose an asset filtered by game.`;

// [game, category, id, name, placementType]
// placementType = thing type written to map. Globally unique: ODOOM = Doom type, OQUAKE = unique 5xxx/53xx/3010/3011.
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
    ["OQUAKE", "key", "silver_key", "Silver Key", 5013],
    ["OQUAKE", "key", "gold_key", "Gold Key", 5005],
    ["OQUAKE", "weapon", "shotgun", "Shotgun", 5201],
    ["OQUAKE", "weapon", "supershotgun", "Super Shotgun", 5202],
    ["OQUAKE", "weapon", "nailgun", "Nailgun", 5203],
    ["OQUAKE", "weapon", "supernailgun", "Super Nailgun", 5204],
    ["OQUAKE", "weapon", "grenadelauncher", "Grenade Launcher", 5205],
    ["OQUAKE", "weapon", "rocketlauncher", "Rocket Launcher", 5206],
    ["OQUAKE", "weapon", "lightning", "Thunderbolt", 5207],
    ["OQUAKE", "ammo", "shells", "Shells", 5209],
    ["OQUAKE", "ammo", "spikes", "Nails", 5208],
    ["OQUAKE", "ammo", "rockets", "Rockets", 5210],
    ["OQUAKE", "ammo", "cells", "Cells", 5211],
    ["OQUAKE", "health", "health", "Health", 5212],
    ["OQUAKE", "health", "health_small", "Small Health", 5213],
    ["OQUAKE", "health", "armor1", "Green Armor", 5214],
    ["OQUAKE", "health", "armor2", "Yellow Armor", 5215],
    ["OQUAKE", "health", "armorInv", "Mega Armor", 5216],
    ["OQUAKE", "monster", "grunt", "Grunt", 5304],
    ["OQUAKE", "monster", "ogre", "Ogre", 5309],
    ["OQUAKE", "monster", "demon", "Demon", 5302],
    ["OQUAKE", "monster", "dog", "Rottweiler", 3010],
    ["OQUAKE", "monster", "shambler", "Shambler", 5303],
    ["OQUAKE", "monster", "zombie", "Zombie", 3011],
    ["OQUAKE", "monster", "hell_knight", "Hell Knight", 5369],
    ["OQUAKE", "monster", "enforcer", "Enforcer", 5366],
    ["OQUAKE", "monster", "fish", "Fish", 5305],
    ["OQUAKE", "monster", "spawn", "Spawn", 5368]
];

var gameList = ["ODOOM", "OQUAKE"];
var qGame = new UDB.QueryOptions();
qGame.addOption("game", "Game", 0, gameList[0], gameList);
if (!qGame.query()) return;

var selectedGame = qGame.options.game;
if (typeof selectedGame === "string" && /^[0-9]+$/.test(selectedGame))
    selectedGame = gameList[parseInt(selectedGame, 10)] || gameList[0];
if (typeof selectedGame === "number")
    selectedGame = gameList[selectedGame] || gameList[0];
if (gameList.indexOf(selectedGame) < 0)
    selectedGame = gameList[0];

var filtered = ASSETS.filter(function(a) { return a[0] === selectedGame; });
if (filtered.length === 0) { UDB.log("OASIS STAR: No assets found for " + selectedGame + "."); return; }

var choiceList = filtered.map(function(a) { return a[3] + " (" + a[4] + ")"; });
var qAsset = new UDB.QueryOptions();
qAsset.addOption("asset", "Asset to place", 0, choiceList[0], choiceList);
if (!qAsset.query()) return;
var idx = -1;
if (typeof qAsset.options.asset === "string" && /^[0-9]+$/.test(qAsset.options.asset)) {
    idx = parseInt(qAsset.options.asset, 10);
} else if (typeof qAsset.options.asset === "number") {
    idx = qAsset.options.asset;
} else {
    idx = choiceList.indexOf(qAsset.options.asset);
}
if (idx < 0) { UDB.log("OASIS STAR: Invalid selection."); return; }
var row = filtered[idx];
if (typeof UDB.setPendingStarPlacement !== "function") {
    UDB.log("OASIS STAR: setPendingStarPlacement not available. Update UDBScript plugin.");
    return;
}
UDB.setPendingStarPlacement(row[4], row[3]);
UDB.log("OASIS STAR: Selected " + row[3] + " (" + selectedGame + ", type " + row[4] + "). Click on the map to place.");











