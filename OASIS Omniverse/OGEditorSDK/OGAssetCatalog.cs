// OGAssetCatalog — canonical cross-game asset registry for all 10 OASIS Omniverse games.
//
// Authoritative source for OASIS thing type numbering. UDB, TrenchBroom/C++ bridge,
// NetRadiant, DarkRadiant, Mapster32, and STARNET all read the same table via this SDK.
//
// Thing type scheme:
//   ODOOM       = standard Doom native types
//   OQUAKE      = 5001–5899   (Quake I — vkQuake)
//   OASIS Portal= 5900        (universal cross-game teleport marker)
//   OQUAKE2     = 6001–6999   (Quake II — Yamagi Q2 / Q2 RTX)
//   OQUAKE3     = 7001–7999   (Quake III Arena — Quake3e)
//   ODUKE3D     = 8001–8999   (Duke Nukem 3D — EDuke32 / Duke-RT)
//   OWOLF3D     = 9001–9499   (Wolfenstein 3D — ECWolf)
//   ODOOM3      = 10001–10999 (Doom 3 — dhewm3 / RBDOOM-3-BFG)

using System.Collections.Generic;

namespace OGEditorSDK
{
    /// <summary>OASIS Omniverse game identifiers — match the IDs used by the STAR API and Omniverse kernel.</summary>
    public static class OGameId
    {
        public const string ODOOM       = "ODOOM";
        public const string OQUAKE      = "OQUAKE";
        public const string OQUAKE2     = "OQUAKE2";
        public const string OQUAKE2_RTX = "OQUAKE2-RTX";
        public const string OQUAKE3     = "OQUAKE3";
        public const string ODUKE3D     = "ODUKE3D";
        public const string ODUKE3D_RT  = "ODUKE3D-RT";
        public const string OWOLF3D     = "OWOLF3D";
        public const string ODOOM3      = "ODOOM3";
        public const string ODOOM3_BFG  = "ODOOM3-BFG";

        /// <summary>Universal cross-game portal thing type (used in every OGame's map format).</summary>
        public const int PORTAL_THING_TYPE = 5900;

        public static readonly IReadOnlyList<string> All = new[]
        {
            ODOOM, OQUAKE, OQUAKE2, OQUAKE2_RTX, OQUAKE3,
            ODUKE3D, ODUKE3D_RT, OWOLF3D, ODOOM3, ODOOM3_BFG
        };
    }

    /// <summary>A single entry in the OG asset catalog.</summary>
    public sealed class OGAsset
    {
        public string GameId          { get; }
        public int    ThingType       { get; }
        public string DisplayName     { get; }
        /// <summary>Native entity classname for this asset in its source game (e.g. "monster_shambler", "PIGCOP", "Guard").</summary>
        public string NativeClassname { get; }
        /// <summary>Category tag for filtering (Keys, Weapons, Ammo, Health, Armor, Monsters, PowerUps, Portals, Misc).</summary>
        public string Category        { get; }

        public OGAsset(string gameId, int thingType, string displayName, string nativeClassname, string category = "Misc")
        {
            GameId          = gameId;
            ThingType       = thingType;
            DisplayName     = displayName;
            NativeClassname = nativeClassname;
            Category        = category;
        }

        public override string ToString() => DisplayName + " [" + GameId + "/" + ThingType + "]";
    }

    /// <summary>
    /// Canonical catalog of all cross-game assets across the 10 OASIS Omniverse games.
    /// Any editor (UDB plugin, TrenchBroom C++ plugin, NetRadiant, STARNET web) reads this same table.
    /// </summary>
    public static class OGAssetCatalog
    {
        private static readonly List<OGAsset> _all = new List<OGAsset>
        {
            // ── OASIS Portal (universal — present in every game) ──────────────────
            new OGAsset("*",          5900,  "OASIS Portal",          "oasis_portal",           "Portals"),

            // ── ODOOM (Doom / Doom II) ────────────────────────────────────────────
            new OGAsset("ODOOM",     5,    "Blue Keycard",           "BlueCard",               "Keys"),
            new OGAsset("ODOOM",     13,   "Red Keycard",            "RedCard",                "Keys"),
            new OGAsset("ODOOM",     6,    "Yellow Keycard",         "YellowCard",             "Keys"),
            new OGAsset("ODOOM",     38,   "Red Skull Key",          "RedSkull",               "Keys"),
            new OGAsset("ODOOM",     39,   "Blue Skull Key",         "BlueSkull",              "Keys"),
            new OGAsset("ODOOM",     40,   "Yellow Skull Key",       "YellowSkull",            "Keys"),
            new OGAsset("ODOOM",     2001, "Shotgun",                "Shotgun",                "Weapons"),
            new OGAsset("ODOOM",     2002, "Chaingun",               "Chaingun",               "Weapons"),
            new OGAsset("ODOOM",     2003, "Rocket Launcher",        "RocketLauncher",         "Weapons"),
            new OGAsset("ODOOM",     2004, "Plasma Rifle",           "PlasmaRifle",            "Weapons"),
            new OGAsset("ODOOM",     2005, "Chainsaw",               "Chainsaw",               "Weapons"),
            new OGAsset("ODOOM",     2006, "BFG 9000",               "BFG9000",                "Weapons"),
            new OGAsset("ODOOM",     2007, "Clip",                   "Clip",                   "Ammo"),
            new OGAsset("ODOOM",     2008, "Shells",                 "Shell",                  "Ammo"),
            new OGAsset("ODOOM",     2010, "Rocket",                 "RocketAmmo",             "Ammo"),
            new OGAsset("ODOOM",     2047, "Cell",                   "Cell",                   "Ammo"),
            new OGAsset("ODOOM",     2048, "Cell Pack",              "CellPack",               "Ammo"),
            new OGAsset("ODOOM",     2049, "Ammo Box",               "ShellBox",               "Ammo"),
            new OGAsset("ODOOM",     2011, "Medikit",                "Medikit",                "Health"),
            new OGAsset("ODOOM",     2012, "Stimpack",               "Stimpack",               "Health"),
            new OGAsset("ODOOM",     2013, "Soul Sphere",            "Soulsphere",             "Health"),
            new OGAsset("ODOOM",     2014, "Health Potion",          "HealthBonus",            "Health"),
            new OGAsset("ODOOM",     2015, "Armor Bonus",            "ArmorBonus",             "Armor"),
            new OGAsset("ODOOM",     2016, "Armor Helmet",           "GreenArmor",             "Armor"),
            new OGAsset("ODOOM",     3004, "Zombieman",              "ZombieMan",              "Monsters"),
            new OGAsset("ODOOM",     9,    "Sergeant",               "ShotgunGuy",             "Monsters"),
            new OGAsset("ODOOM",     3001, "Imp",                    "DoomImp",                "Monsters"),
            new OGAsset("ODOOM",     3002, "Demon",                  "Demon",                  "Monsters"),
            new OGAsset("ODOOM",     58,   "Spectre",                "Spectre",                "Monsters"),
            new OGAsset("ODOOM",     3005, "Cacodemon",              "Cacodemon",              "Monsters"),
            new OGAsset("ODOOM",     3003, "Baron of Hell",          "BaronOfHell",            "Monsters"),
            new OGAsset("ODOOM",     69,   "Hell Knight",            "HellKnight",             "Monsters"),
            new OGAsset("ODOOM",     3006, "Lost Soul",              "LostSoul",               "Monsters"),
            new OGAsset("ODOOM",     65,   "Revenant",               "Revenant",               "Monsters"),
            new OGAsset("ODOOM",     66,   "Mancubus",               "Fatso",                  "Monsters"),
            new OGAsset("ODOOM",     64,   "Arch-Vile",              "Archvile",               "Monsters"),
            new OGAsset("ODOOM",     68,   "Pain Elemental",         "PainElemental",          "Monsters"),
            new OGAsset("ODOOM",     67,   "Arachnotron",            "Arachnotron",            "Monsters"),
            new OGAsset("ODOOM",     7,    "Spider Mastermind",      "SpiderMastermind",       "Monsters"),
            new OGAsset("ODOOM",     16,   "Cyberdemon",             "Cyberdemon",             "Monsters"),

            // ── OQUAKE (Quake I — vkQuake) ────────────────────────────────────────
            new OGAsset("OQUAKE",   5005, "Gold Key",               "item_key_gold",          "Keys"),
            new OGAsset("OQUAKE",   5013, "Silver Key",             "item_key_silver",        "Keys"),
            new OGAsset("OQUAKE",   5201, "Shotgun",                "weapon_shotgun",         "Weapons"),
            new OGAsset("OQUAKE",   5202, "Super Shotgun",          "weapon_supershotgun",    "Weapons"),
            new OGAsset("OQUAKE",   5203, "Nailgun",                "weapon_nailgun",         "Weapons"),
            new OGAsset("OQUAKE",   5204, "Super Nailgun",          "weapon_supernailgun",    "Weapons"),
            new OGAsset("OQUAKE",   5205, "Grenade Launcher",       "weapon_grenadelauncher", "Weapons"),
            new OGAsset("OQUAKE",   5206, "Rocket Launcher",        "weapon_rocketlauncher",  "Weapons"),
            new OGAsset("OQUAKE",   5207, "Thunderbolt",            "weapon_lightning",       "Weapons"),
            new OGAsset("OQUAKE",   5208, "Nails",                  "item_spikes",            "Ammo"),
            new OGAsset("OQUAKE",   5209, "Shells",                 "item_shells",            "Ammo"),
            new OGAsset("OQUAKE",   5210, "Rockets",                "item_rockets",           "Ammo"),
            new OGAsset("OQUAKE",   5211, "Cells",                  "item_cells",             "Ammo"),
            new OGAsset("OQUAKE",   5212, "Health",                 "item_health",            "Health"),
            new OGAsset("OQUAKE",   5213, "Small Health",           "item_health_small",      "Health"),
            new OGAsset("OQUAKE",   5214, "Green Armor",            "item_armor1",            "Armor"),
            new OGAsset("OQUAKE",   5215, "Yellow Armor",           "item_armor2",            "Armor"),
            new OGAsset("OQUAKE",   5216, "Mega Armor",             "item_armorInv",          "Armor"),
            new OGAsset("OQUAKE",   5302, "Demon",                  "monster_demon",          "Monsters"),
            new OGAsset("OQUAKE",   5303, "Shambler",               "monster_shambler",       "Monsters"),
            new OGAsset("OQUAKE",   5304, "Grunt",                  "monster_grunt",          "Monsters"),
            new OGAsset("OQUAKE",   5305, "Fish",                   "monster_fish",           "Monsters"),
            new OGAsset("OQUAKE",   5309, "Ogre",                   "monster_ogre",           "Monsters"),
            new OGAsset("OQUAKE",   3010, "Rottweiler",             "monster_dog",            "Monsters"),
            new OGAsset("OQUAKE",   3011, "Zombie",                 "monster_zombie",         "Monsters"),
            new OGAsset("OQUAKE",   5366, "Enforcer",               "monster_enforcer",       "Monsters"),
            new OGAsset("OQUAKE",   5368, "Spawn",                  "monster_spawn",          "Monsters"),
            new OGAsset("OQUAKE",   5369, "Hell Knight",            "monster_hell_knight",    "Monsters"),

            // ── OQUAKE2 (Quake II — Yamagi Q2 / Q2 RTX) ──────────────────────────
            new OGAsset("OQUAKE2",  6001, "Blue Keycard",           "item_key_blue_key",      "Keys"),
            new OGAsset("OQUAKE2",  6002, "Red Keycard",            "item_key_red_key",       "Keys"),
            new OGAsset("OQUAKE2",  6003, "Commander's Head",       "item_key_commander_head","Keys"),
            new OGAsset("OQUAKE2",  6011, "Blaster",                "weapon_blaster",         "Weapons"),
            new OGAsset("OQUAKE2",  6012, "Shotgun",                "weapon_shotgun",         "Weapons"),
            new OGAsset("OQUAKE2",  6013, "Super Shotgun",          "weapon_supershotgun",    "Weapons"),
            new OGAsset("OQUAKE2",  6014, "Machinegun",             "weapon_machinegun",      "Weapons"),
            new OGAsset("OQUAKE2",  6015, "Chaingun",               "weapon_chaingun",        "Weapons"),
            new OGAsset("OQUAKE2",  6016, "Grenade Launcher",       "weapon_grenadelauncher", "Weapons"),
            new OGAsset("OQUAKE2",  6017, "Rocket Launcher",        "weapon_rocketlauncher",  "Weapons"),
            new OGAsset("OQUAKE2",  6018, "Hyperblaster",           "weapon_hyperblaster",    "Weapons"),
            new OGAsset("OQUAKE2",  6019, "Railgun",                "weapon_railgun",         "Weapons"),
            new OGAsset("OQUAKE2",  6020, "BFG10K",                 "weapon_bfg",             "Weapons"),
            new OGAsset("OQUAKE2",  6021, "Bullets",                "ammo_bullets",           "Ammo"),
            new OGAsset("OQUAKE2",  6022, "Shells",                 "ammo_shells",            "Ammo"),
            new OGAsset("OQUAKE2",  6023, "Grenades",               "ammo_grenades",          "Ammo"),
            new OGAsset("OQUAKE2",  6024, "Rockets",                "ammo_rockets",           "Ammo"),
            new OGAsset("OQUAKE2",  6025, "Cells",                  "ammo_cells",             "Ammo"),
            new OGAsset("OQUAKE2",  6026, "Slugs",                  "ammo_slugs",             "Ammo"),
            new OGAsset("OQUAKE2",  6031, "Small Health",           "item_health_small",      "Health"),
            new OGAsset("OQUAKE2",  6032, "Health",                 "item_health",            "Health"),
            new OGAsset("OQUAKE2",  6033, "Mega Health",            "item_health_mega",       "Health"),
            new OGAsset("OQUAKE2",  6041, "Jacket Armor",           "item_armor_jacket",      "Armor"),
            new OGAsset("OQUAKE2",  6042, "Combat Armor",           "item_armor_combat",      "Armor"),
            new OGAsset("OQUAKE2",  6043, "Body Armor",             "item_armor_body",        "Armor"),
            new OGAsset("OQUAKE2",  6101, "Soldier",                "monster_soldier",        "Monsters"),
            new OGAsset("OQUAKE2",  6102, "Infantry",               "monster_infantry",       "Monsters"),
            new OGAsset("OQUAKE2",  6103, "Gunner",                 "monster_gunner",         "Monsters"),
            new OGAsset("OQUAKE2",  6104, "Berserker",              "monster_berserker",      "Monsters"),
            new OGAsset("OQUAKE2",  6105, "Gladiator",              "monster_gladiator",      "Monsters"),
            new OGAsset("OQUAKE2",  6106, "Flyer",                  "monster_flyer",          "Monsters"),
            new OGAsset("OQUAKE2",  6107, "Medic",                  "monster_medic",          "Monsters"),
            new OGAsset("OQUAKE2",  6108, "Parasite",               "monster_parasite",       "Monsters"),
            new OGAsset("OQUAKE2",  6109, "Brain",                  "monster_brain",          "Monsters"),
            new OGAsset("OQUAKE2",  6110, "Supertank",              "monster_supertank",      "Monsters"),
            new OGAsset("OQUAKE2",  6111, "Tank",                   "monster_tank",           "Monsters"),
            new OGAsset("OQUAKE2",  6112, "Makron",                 "monster_makron",         "Monsters"),

            // ── OQUAKE3 (Quake III Arena — Quake3e) ──────────────────────────────
            new OGAsset("OQUAKE3",  7001, "Rune of Strength",       "item_rune_strength",     "Runes"),
            new OGAsset("OQUAKE3",  7002, "Rune of Haste",          "item_rune_haste",        "Runes"),
            new OGAsset("OQUAKE3",  7003, "Rune of Regeneration",   "item_rune_regeneration", "Runes"),
            new OGAsset("OQUAKE3",  7004, "Rune of Resistance",     "item_rune_resistance",   "Runes"),
            new OGAsset("OQUAKE3",  7011, "Rocket Launcher",        "weapon_rocketlauncher",  "Weapons"),
            new OGAsset("OQUAKE3",  7012, "Railgun",                "weapon_railgun",         "Weapons"),
            new OGAsset("OQUAKE3",  7013, "Shotgun",                "weapon_shotgun",         "Weapons"),
            new OGAsset("OQUAKE3",  7014, "Lightning Gun",          "weapon_lightning",       "Weapons"),
            new OGAsset("OQUAKE3",  7015, "Plasma Gun",             "weapon_plasmagun",       "Weapons"),
            new OGAsset("OQUAKE3",  7016, "BFG",                    "weapon_bfg",             "Weapons"),
            new OGAsset("OQUAKE3",  7017, "Gauntlet",               "weapon_gauntlet",        "Weapons"),
            new OGAsset("OQUAKE3",  7018, "Machinegun",             "weapon_machinegun",      "Weapons"),
            new OGAsset("OQUAKE3",  7019, "Grenade Launcher",       "weapon_grenadelauncher", "Weapons"),
            new OGAsset("OQUAKE3",  7021, "Rocket Ammo",            "ammo_rockets",           "Ammo"),
            new OGAsset("OQUAKE3",  7022, "Slugs",                  "ammo_slugs",             "Ammo"),
            new OGAsset("OQUAKE3",  7023, "Shells",                 "ammo_shells",            "Ammo"),
            new OGAsset("OQUAKE3",  7024, "Lightning",              "ammo_lightning",         "Ammo"),
            new OGAsset("OQUAKE3",  7025, "Plasma",                 "ammo_plasma",            "Ammo"),
            new OGAsset("OQUAKE3",  7026, "Bullets",                "ammo_bullets",           "Ammo"),
            new OGAsset("OQUAKE3",  7027, "Grenades",               "ammo_grenades",          "Ammo"),
            new OGAsset("OQUAKE3",  7028, "BFG Ammo",               "ammo_bfg",               "Ammo"),
            new OGAsset("OQUAKE3",  7031, "Small Health",           "item_health_small",      "Health"),
            new OGAsset("OQUAKE3",  7032, "Health",                 "item_health",            "Health"),
            new OGAsset("OQUAKE3",  7033, "Large Health",           "item_health_large",      "Health"),
            new OGAsset("OQUAKE3",  7034, "Mega Health",            "item_health_mega",       "Health"),
            new OGAsset("OQUAKE3",  7041, "Armor Shard",            "item_armor_shard",       "Armor"),
            new OGAsset("OQUAKE3",  7042, "Combat Armor (Yellow)",  "item_armor_combat",      "Armor"),
            new OGAsset("OQUAKE3",  7043, "Body Armor (Red)",       "item_armor_body",        "Armor"),
            new OGAsset("OQUAKE3",  7051, "Quad Damage",            "item_quad",              "PowerUps"),
            new OGAsset("OQUAKE3",  7052, "Regeneration",           "item_regen",             "PowerUps"),
            new OGAsset("OQUAKE3",  7053, "Haste",                  "item_haste",             "PowerUps"),
            new OGAsset("OQUAKE3",  7054, "Battle Suit",            "item_enviro",            "PowerUps"),
            new OGAsset("OQUAKE3",  7055, "Flight",                 "item_flight",            "PowerUps"),
            new OGAsset("OQUAKE3",  7056, "Invisibility",           "item_invis",             "PowerUps"),

            // ── ODUKE3D (Duke Nukem 3D — EDuke32 / Duke-RT) ──────────────────────
            new OGAsset("ODUKE3D",  8001, "Blue Keycard",           "BLUEKEY",                "Keys"),
            new OGAsset("ODUKE3D",  8002, "Red Keycard",            "REDKEY",                 "Keys"),
            new OGAsset("ODUKE3D",  8003, "Yellow Keycard",         "YELLOWKEY",              "Keys"),
            new OGAsset("ODUKE3D",  8004, "Blue Access Card",       "ACCESSCARD",             "Keys"),
            new OGAsset("ODUKE3D",  8005, "Red Access Card",        "ACCESSCARD",             "Keys"),
            new OGAsset("ODUKE3D",  8006, "Yellow Access Card",     "ACCESSCARD",             "Keys"),
            new OGAsset("ODUKE3D",  8011, "Pistol",                 "PISTOL",                 "Weapons"),
            new OGAsset("ODUKE3D",  8012, "Shotgun",                "SHOTGUN",                "Weapons"),
            new OGAsset("ODUKE3D",  8013, "Chaingun Cannon",        "CHAINGUNSPRITE",         "Weapons"),
            new OGAsset("ODUKE3D",  8014, "RPG",                    "RPGSPRITE",              "Weapons"),
            new OGAsset("ODUKE3D",  8015, "Shrinker",               "SHRINKERSPRITE",         "Weapons"),
            new OGAsset("ODUKE3D",  8016, "Devastator",             "DEVISTATORSPRITE",       "Weapons"),
            new OGAsset("ODUKE3D",  8017, "Laser Tripbomb",         "TRIPBOMBSPRITE",         "Weapons"),
            new OGAsset("ODUKE3D",  8018, "Freezethrower",          "FREEZESPRITE",           "Weapons"),
            new OGAsset("ODUKE3D",  8019, "Expander",               "EXPANDERSPRITE",         "Weapons"),
            new OGAsset("ODUKE3D",  8021, "Small Medkit",           "FIRSTAIDKIT",            "Health"),
            new OGAsset("ODUKE3D",  8022, "Large Medkit",           "HEALTHBOX",              "Health"),
            new OGAsset("ODUKE3D",  8023, "Atomic Health",          "ATOMICHEALTH",           "Health"),
            new OGAsset("ODUKE3D",  8024, "Portable Medkit",        "FIRSTAIDKIT",            "Health"),
            new OGAsset("ODUKE3D",  8031, "Jetpack",                "JETPACK",                "PowerUps"),
            new OGAsset("ODUKE3D",  8032, "Scuba Gear",             "SCUBAGEAR",              "PowerUps"),
            new OGAsset("ODUKE3D",  8033, "Night Vision Goggles",   "NIGHTVISION",            "PowerUps"),
            new OGAsset("ODUKE3D",  8034, "Steroids",               "STEROIDS",               "PowerUps"),
            new OGAsset("ODUKE3D",  8035, "HoloDuke",               "HOLODUKE",               "PowerUps"),
            new OGAsset("ODUKE3D",  8101, "Assault Trooper",        "LIZTROOP",               "Monsters"),
            new OGAsset("ODUKE3D",  8102, "Pig Cop",                "PIGCOP",                 "Monsters"),
            new OGAsset("ODUKE3D",  8103, "Pig Cop Tank",           "PIGCOPBOAT",             "Monsters"),
            new OGAsset("ODUKE3D",  8104, "Enforcer",               "LIZMAN",                 "Monsters"),
            new OGAsset("ODUKE3D",  8105, "Commander",              "COMMANDER",              "Monsters"),
            new OGAsset("ODUKE3D",  8106, "Octabrain",              "OCTABRAIN",              "Monsters"),
            new OGAsset("ODUKE3D",  8107, "Protozoid Slimer",       "ROTATEGUN",              "Monsters"),
            new OGAsset("ODUKE3D",  8108, "Sentry Drone",           "RECON",                  "Monsters"),
            new OGAsset("ODUKE3D",  8109, "Battlelord",             "BATTLELORD",             "Monsters"),
            new OGAsset("ODUKE3D",  8110, "Alien Queen",            "QUEEN",                  "Monsters"),
            new OGAsset("ODUKE3D",  8111, "Overlord",               "OVERLORD",               "Monsters"),

            // ── OWOLF3D (Wolfenstein 3D — ECWolf / DECORATE actor classnames) ─────
            new OGAsset("OWOLF3D",  9001, "Gold Key",               "GoldKey",                "Keys"),
            new OGAsset("OWOLF3D",  9002, "Silver Key",             "SilverKey",              "Keys"),
            new OGAsset("OWOLF3D",  9011, "Pistol",                 "Pistol",                 "Weapons"),
            new OGAsset("OWOLF3D",  9012, "Machine Gun",            "MachineGun",             "Weapons"),
            new OGAsset("OWOLF3D",  9013, "Chaingun",               "Chaingun",               "Weapons"),
            new OGAsset("OWOLF3D",  9021, "Food",                   "Food",                   "Health"),
            new OGAsset("OWOLF3D",  9022, "First Aid Kit",          "FirstAidKit",            "Health"),
            new OGAsset("OWOLF3D",  9023, "One-Up",                 "ExtraLife",              "Health"),
            new OGAsset("OWOLF3D",  9024, "Ammo Clip",              "AmmoClip",               "Ammo"),
            new OGAsset("OWOLF3D",  9025, "Box of Ammo",            "BoxOfAmmo",              "Ammo"),
            new OGAsset("OWOLF3D",  9026, "Gold Cross",             "GoldenCross",            "Misc"),
            new OGAsset("OWOLF3D",  9027, "Silver Cross",           "SilverCross",            "Misc"),
            new OGAsset("OWOLF3D",  9028, "Gold Cup",               "GoldCup",                "Misc"),
            new OGAsset("OWOLF3D",  9029, "Crown",                  "Crown",                  "Misc"),
            new OGAsset("OWOLF3D",  9101, "Guard (Pistol)",         "Guard",                  "Monsters"),
            new OGAsset("OWOLF3D",  9102, "Officer",                "Officer",                "Monsters"),
            new OGAsset("OWOLF3D",  9103, "SS Guard",               "WolfensteinSS",          "Monsters"),
            new OGAsset("OWOLF3D",  9104, "Guard Dog",              "Dog",                    "Monsters"),
            new OGAsset("OWOLF3D",  9105, "Mutant",                 "Mutant",                 "Monsters"),
            new OGAsset("OWOLF3D",  9106, "Hans Grosse (Boss)",     "HansGrosse",             "Monsters"),
            new OGAsset("OWOLF3D",  9107, "Dr. Schabbs (Boss)",     "DrSchabbs",              "Monsters"),
            new OGAsset("OWOLF3D",  9108, "Hitler / Mecha-Hitler",  "AdolfHitler",            "Monsters"),

            // ── ODOOM3 / ODOOM3-BFG (Doom 3 — dhewm3 / RBDOOM-3-BFG) ────────────
            new OGAsset("ODOOM3",   10011, "Pistol",                "weapon_pistol",           "Weapons"),
            new OGAsset("ODOOM3",   10012, "Shotgun",               "weapon_shotgun",          "Weapons"),
            new OGAsset("ODOOM3",   10013, "Machine Gun",           "weapon_machinegun",       "Weapons"),
            new OGAsset("ODOOM3",   10014, "Chaingun",              "weapon_chaingun",         "Weapons"),
            new OGAsset("ODOOM3",   10015, "Rocket Launcher",       "weapon_rocketlauncher",   "Weapons"),
            new OGAsset("ODOOM3",   10016, "Plasma Rifle",          "weapon_plasmagun",        "Weapons"),
            new OGAsset("ODOOM3",   10017, "BFG 9000",              "weapon_bfg",              "Weapons"),
            new OGAsset("ODOOM3",   10018, "Chainsaw",              "weapon_chainsaw",         "Weapons"),
            new OGAsset("ODOOM3",   10019, "Soul Cube",             "weapon_soulcube",         "Weapons"),
            new OGAsset("ODOOM3",   10021, "Pistol Ammo",           "ammo_bullets",            "Ammo"),
            new OGAsset("ODOOM3",   10022, "Shotgun Shells",        "ammo_shells",             "Ammo"),
            new OGAsset("ODOOM3",   10023, "Machine Gun Ammo",      "ammo_mgammo",             "Ammo"),
            new OGAsset("ODOOM3",   10024, "Chaingun Belt",         "ammo_belt",               "Ammo"),
            new OGAsset("ODOOM3",   10025, "Rockets",               "ammo_rockets",            "Ammo"),
            new OGAsset("ODOOM3",   10026, "Plasma Cells",          "ammo_cells",              "Ammo"),
            new OGAsset("ODOOM3",   10031, "Small Med Pack",        "item_medkit_small",       "Health"),
            new OGAsset("ODOOM3",   10032, "Large Med Pack",        "item_medkit_large",       "Health"),
            new OGAsset("ODOOM3",   10034, "Armor Vest",            "item_armor",              "Armor"),
            new OGAsset("ODOOM3",   10035, "Security Armor",        "item_armor_security",     "Armor"),
            new OGAsset("ODOOM3",   10101, "Zombie",                "monster_zombie",          "Monsters"),
            new OGAsset("ODOOM3",   10102, "Imp",                   "monster_imp",             "Monsters"),
            new OGAsset("ODOOM3",   10103, "Pinky (Demon)",         "monster_pinky",           "Monsters"),
            new OGAsset("ODOOM3",   10104, "Cacodemon",             "monster_cacodemon",       "Monsters"),
            new OGAsset("ODOOM3",   10105, "Hell Knight",           "monster_hellknight",      "Monsters"),
            new OGAsset("ODOOM3",   10106, "Revenant",              "monster_revenant",        "Monsters"),
            new OGAsset("ODOOM3",   10107, "Mancubus",              "monster_mancubus",        "Monsters"),
            new OGAsset("ODOOM3",   10108, "Arch-Vile",             "monster_archvile",        "Monsters"),
            new OGAsset("ODOOM3",   10109, "Vagary",                "monster_vagary",          "Monsters"),
            new OGAsset("ODOOM3",   10110, "Guardian of Hell",      "monster_guardian",        "Monsters"),
            new OGAsset("ODOOM3",   10111, "Cyberdemon",            "monster_cyberdemon",      "Monsters"),
        };

        /// <summary>All assets across all games.</summary>
        public static IReadOnlyList<OGAsset> All => _all;

        /// <summary>All assets for a specific OGame (case-insensitive). OQUAKE2-RTX and ODUKE3D-RT share their base game's types.</summary>
        public static IReadOnlyList<OGAsset> ForGame(string gameId)
        {
            string normalised = gameId;
            if (string.Equals(gameId, OGameId.OQUAKE2_RTX, System.StringComparison.OrdinalIgnoreCase))
                normalised = OGameId.OQUAKE2;
            else if (string.Equals(gameId, OGameId.ODUKE3D_RT, System.StringComparison.OrdinalIgnoreCase))
                normalised = OGameId.ODUKE3D;
            else if (string.Equals(gameId, OGameId.ODOOM3_BFG, System.StringComparison.OrdinalIgnoreCase))
                normalised = OGameId.ODOOM3;

            var result = new List<OGAsset>();
            foreach (var a in _all)
                if (string.Equals(a.GameId, normalised, System.StringComparison.OrdinalIgnoreCase) ||
                    a.GameId == "*")
                    result.Add(a);
            return result;
        }

        /// <summary>Look up an asset by OASIS thing type.</summary>
        public static OGAsset ByThingType(int thingType)
        {
            foreach (var a in _all)
                if (a.ThingType == thingType) return a;
            return null;
        }

        /// <summary>Returns true if <paramref name="thingType"/> belongs to the OASIS cross-game range (>= 5000 or legacy OQUAKE types).</summary>
        public static bool IsOasisType(int thingType)
        {
            return thingType >= 5000 || thingType == 3010 || thingType == 3011;
        }
    }
}
