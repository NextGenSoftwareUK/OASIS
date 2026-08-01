using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;

namespace NextGenSoftware.OASIS.STAR.WebAPI.Controllers
{
    /// <summary>
    /// OGAsset catalog endpoint — serves the canonical cross-game asset registry to the OGEngine Editor (UDB)
    /// and any other tool that needs live OASIS thing type data without bundling OGEditorSDK.
    ///
    /// Mirrors OGEditorSDK/OGAssetCatalog.cs exactly. Keep both files in sync when adding games or items.
    /// URL: GET /api/oassets?gameId={ODOOM|OQUAKE|OQUAKE2|OQUAKE2-RTX|OQUAKE3|ODUKE3D|ODUKE3D-RT|OWOLF3D|ODOOM3|ODOOM3-BFG}
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class OAssetsController : ControllerBase
    {
        // ── DTO ───────────────────────────────────────────────────────────────────────

        public sealed record OAssetDto(
            string gameId,
            int    thingType,
            string displayName,
            string nativeClassname,
            string category);

        // ── Catalog ───────────────────────────────────────────────────────────────────

        private static readonly List<OAssetDto> _all = new List<OAssetDto>
        {
            // Portal (universal — all games)
            new("*",         5900,  "OASIS Portal",          "oasis_portal",           "Portals"),

            // ODOOM (Doom / Doom II)
            new("ODOOM",     5,     "Blue Keycard",           "BlueCard",               "Keys"),
            new("ODOOM",     13,    "Red Keycard",            "RedCard",                "Keys"),
            new("ODOOM",     6,     "Yellow Keycard",         "YellowCard",             "Keys"),
            new("ODOOM",     38,    "Red Skull Key",          "RedSkull",               "Keys"),
            new("ODOOM",     39,    "Blue Skull Key",         "BlueSkull",              "Keys"),
            new("ODOOM",     40,    "Yellow Skull Key",       "YellowSkull",            "Keys"),
            new("ODOOM",     2001,  "Shotgun",                "Shotgun",                "Weapons"),
            new("ODOOM",     2002,  "Chaingun",               "Chaingun",               "Weapons"),
            new("ODOOM",     2003,  "Rocket Launcher",        "RocketLauncher",         "Weapons"),
            new("ODOOM",     2004,  "Plasma Rifle",           "PlasmaRifle",            "Weapons"),
            new("ODOOM",     2005,  "Chainsaw",               "Chainsaw",               "Weapons"),
            new("ODOOM",     2006,  "BFG 9000",               "BFG9000",                "Weapons"),
            new("ODOOM",     2007,  "Clip",                   "Clip",                   "Ammo"),
            new("ODOOM",     2008,  "Shells",                 "Shell",                  "Ammo"),
            new("ODOOM",     2010,  "Rocket",                 "RocketAmmo",             "Ammo"),
            new("ODOOM",     2047,  "Cell",                   "Cell",                   "Ammo"),
            new("ODOOM",     2048,  "Cell Pack",              "CellPack",               "Ammo"),
            new("ODOOM",     2049,  "Ammo Box",               "ShellBox",               "Ammo"),
            new("ODOOM",     2011,  "Medikit",                "Medikit",                "Health"),
            new("ODOOM",     2012,  "Stimpack",               "Stimpack",               "Health"),
            new("ODOOM",     2013,  "Soul Sphere",            "Soulsphere",             "Health"),
            new("ODOOM",     2014,  "Health Potion",          "HealthBonus",            "Health"),
            new("ODOOM",     2015,  "Armor Bonus",            "ArmorBonus",             "Armor"),
            new("ODOOM",     2016,  "Armor Helmet",           "GreenArmor",             "Armor"),
            new("ODOOM",     3004,  "Zombieman",              "ZombieMan",              "Monsters"),
            new("ODOOM",     9,     "Sergeant",               "ShotgunGuy",             "Monsters"),
            new("ODOOM",     3001,  "Imp",                    "DoomImp",                "Monsters"),
            new("ODOOM",     3002,  "Demon",                  "Demon",                  "Monsters"),
            new("ODOOM",     58,    "Spectre",                "Spectre",                "Monsters"),
            new("ODOOM",     3005,  "Cacodemon",              "Cacodemon",              "Monsters"),
            new("ODOOM",     3003,  "Baron of Hell",          "BaronOfHell",            "Monsters"),
            new("ODOOM",     69,    "Hell Knight",            "HellKnight",             "Monsters"),
            new("ODOOM",     3006,  "Lost Soul",              "LostSoul",               "Monsters"),
            new("ODOOM",     65,    "Revenant",               "Revenant",               "Monsters"),
            new("ODOOM",     66,    "Mancubus",               "Fatso",                  "Monsters"),
            new("ODOOM",     64,    "Arch-Vile",              "Archvile",               "Monsters"),
            new("ODOOM",     68,    "Pain Elemental",         "PainElemental",          "Monsters"),
            new("ODOOM",     67,    "Arachnotron",            "Arachnotron",            "Monsters"),
            new("ODOOM",     7,     "Spider Mastermind",      "SpiderMastermind",       "Monsters"),
            new("ODOOM",     16,    "Cyberdemon",             "Cyberdemon",             "Monsters"),

            // OQUAKE (Quake I — vkQuake)
            new("OQUAKE",    5005,  "Gold Key",               "item_key_gold",          "Keys"),
            new("OQUAKE",    5013,  "Silver Key",             "item_key_silver",        "Keys"),
            new("OQUAKE",    5201,  "Shotgun",                "weapon_shotgun",         "Weapons"),
            new("OQUAKE",    5202,  "Super Shotgun",          "weapon_supershotgun",    "Weapons"),
            new("OQUAKE",    5203,  "Nailgun",                "weapon_nailgun",         "Weapons"),
            new("OQUAKE",    5204,  "Super Nailgun",          "weapon_supernailgun",    "Weapons"),
            new("OQUAKE",    5205,  "Grenade Launcher",       "weapon_grenadelauncher", "Weapons"),
            new("OQUAKE",    5206,  "Rocket Launcher",        "weapon_rocketlauncher",  "Weapons"),
            new("OQUAKE",    5207,  "Thunderbolt",            "weapon_lightning",       "Weapons"),
            new("OQUAKE",    5208,  "Nails",                  "item_spikes",            "Ammo"),
            new("OQUAKE",    5209,  "Shells",                 "item_shells",            "Ammo"),
            new("OQUAKE",    5210,  "Rockets",                "item_rockets",           "Ammo"),
            new("OQUAKE",    5211,  "Cells",                  "item_cells",             "Ammo"),
            new("OQUAKE",    5212,  "Health",                 "item_health",            "Health"),
            new("OQUAKE",    5213,  "Small Health",           "item_health_small",      "Health"),
            new("OQUAKE",    5214,  "Green Armor",            "item_armor1",            "Armor"),
            new("OQUAKE",    5215,  "Yellow Armor",           "item_armor2",            "Armor"),
            new("OQUAKE",    5216,  "Mega Armor",             "item_armorInv",          "Armor"),
            new("OQUAKE",    5302,  "Demon",                  "monster_demon",          "Monsters"),
            new("OQUAKE",    5303,  "Shambler",               "monster_shambler",       "Monsters"),
            new("OQUAKE",    5304,  "Grunt",                  "monster_grunt",          "Monsters"),
            new("OQUAKE",    5305,  "Fish",                   "monster_fish",           "Monsters"),
            new("OQUAKE",    5309,  "Ogre",                   "monster_ogre",           "Monsters"),
            new("OQUAKE",    3010,  "Rottweiler",             "monster_dog",            "Monsters"),
            new("OQUAKE",    3011,  "Zombie",                 "monster_zombie",         "Monsters"),
            new("OQUAKE",    5366,  "Enforcer",               "monster_enforcer",       "Monsters"),
            new("OQUAKE",    5368,  "Spawn",                  "monster_spawn",          "Monsters"),
            new("OQUAKE",    5369,  "Hell Knight",            "monster_hell_knight",    "Monsters"),

            // OQUAKE2 (Quake II — Yamagi Q2 / Q2 RTX)
            new("OQUAKE2",   6001,  "Blue Keycard",           "item_key_blue_key",      "Keys"),
            new("OQUAKE2",   6002,  "Red Keycard",            "item_key_red_key",       "Keys"),
            new("OQUAKE2",   6003,  "Commander's Head",       "item_key_commander_head","Keys"),
            new("OQUAKE2",   6011,  "Blaster",                "weapon_blaster",         "Weapons"),
            new("OQUAKE2",   6012,  "Shotgun",                "weapon_shotgun",         "Weapons"),
            new("OQUAKE2",   6013,  "Super Shotgun",          "weapon_supershotgun",    "Weapons"),
            new("OQUAKE2",   6014,  "Machinegun",             "weapon_machinegun",      "Weapons"),
            new("OQUAKE2",   6015,  "Chaingun",               "weapon_chaingun",        "Weapons"),
            new("OQUAKE2",   6016,  "Grenade Launcher",       "weapon_grenadelauncher", "Weapons"),
            new("OQUAKE2",   6017,  "Rocket Launcher",        "weapon_rocketlauncher",  "Weapons"),
            new("OQUAKE2",   6018,  "Hyperblaster",           "weapon_hyperblaster",    "Weapons"),
            new("OQUAKE2",   6019,  "Railgun",                "weapon_railgun",         "Weapons"),
            new("OQUAKE2",   6020,  "BFG10K",                 "weapon_bfg",             "Weapons"),
            new("OQUAKE2",   6021,  "Bullets",                "ammo_bullets",           "Ammo"),
            new("OQUAKE2",   6022,  "Shells",                 "ammo_shells",            "Ammo"),
            new("OQUAKE2",   6023,  "Grenades",               "ammo_grenades",          "Ammo"),
            new("OQUAKE2",   6024,  "Rockets",                "ammo_rockets",           "Ammo"),
            new("OQUAKE2",   6025,  "Cells",                  "ammo_cells",             "Ammo"),
            new("OQUAKE2",   6026,  "Slugs",                  "ammo_slugs",             "Ammo"),
            new("OQUAKE2",   6031,  "Small Health",           "item_health_small",      "Health"),
            new("OQUAKE2",   6032,  "Health",                 "item_health",            "Health"),
            new("OQUAKE2",   6033,  "Mega Health",            "item_health_mega",       "Health"),
            new("OQUAKE2",   6041,  "Jacket Armor",           "item_armor_jacket",      "Armor"),
            new("OQUAKE2",   6042,  "Combat Armor",           "item_armor_combat",      "Armor"),
            new("OQUAKE2",   6043,  "Body Armor",             "item_armor_body",        "Armor"),
            new("OQUAKE2",   6101,  "Soldier",                "monster_soldier",        "Monsters"),
            new("OQUAKE2",   6102,  "Infantry",               "monster_infantry",       "Monsters"),
            new("OQUAKE2",   6103,  "Gunner",                 "monster_gunner",         "Monsters"),
            new("OQUAKE2",   6104,  "Berserker",              "monster_berserker",      "Monsters"),
            new("OQUAKE2",   6105,  "Gladiator",              "monster_gladiator",      "Monsters"),
            new("OQUAKE2",   6106,  "Flyer",                  "monster_flyer",          "Monsters"),
            new("OQUAKE2",   6107,  "Medic",                  "monster_medic",          "Monsters"),
            new("OQUAKE2",   6108,  "Parasite",               "monster_parasite",       "Monsters"),
            new("OQUAKE2",   6109,  "Brain",                  "monster_brain",          "Monsters"),
            new("OQUAKE2",   6110,  "Supertank",              "monster_supertank",      "Monsters"),
            new("OQUAKE2",   6111,  "Tank",                   "monster_tank",           "Monsters"),
            new("OQUAKE2",   6112,  "Makron",                 "monster_makron",         "Monsters"),

            // OQUAKE3 (Quake III Arena — Quake3e)
            new("OQUAKE3",   7001,  "Rune of Strength",       "item_rune_strength",     "Runes"),
            new("OQUAKE3",   7002,  "Rune of Haste",          "item_rune_haste",        "Runes"),
            new("OQUAKE3",   7003,  "Rune of Regeneration",   "item_rune_regeneration", "Runes"),
            new("OQUAKE3",   7004,  "Rune of Resistance",     "item_rune_resistance",   "Runes"),
            new("OQUAKE3",   7011,  "Rocket Launcher",        "weapon_rocketlauncher",  "Weapons"),
            new("OQUAKE3",   7012,  "Railgun",                "weapon_railgun",         "Weapons"),
            new("OQUAKE3",   7013,  "Shotgun",                "weapon_shotgun",         "Weapons"),
            new("OQUAKE3",   7014,  "Lightning Gun",          "weapon_lightning",       "Weapons"),
            new("OQUAKE3",   7015,  "Plasma Gun",             "weapon_plasmagun",       "Weapons"),
            new("OQUAKE3",   7016,  "BFG",                    "weapon_bfg",             "Weapons"),
            new("OQUAKE3",   7017,  "Gauntlet",               "weapon_gauntlet",        "Weapons"),
            new("OQUAKE3",   7018,  "Machinegun",             "weapon_machinegun",      "Weapons"),
            new("OQUAKE3",   7019,  "Grenade Launcher",       "weapon_grenadelauncher", "Weapons"),
            new("OQUAKE3",   7021,  "Rocket Ammo",            "ammo_rockets",           "Ammo"),
            new("OQUAKE3",   7022,  "Slugs",                  "ammo_slugs",             "Ammo"),
            new("OQUAKE3",   7023,  "Shells",                 "ammo_shells",            "Ammo"),
            new("OQUAKE3",   7024,  "Lightning",              "ammo_lightning",         "Ammo"),
            new("OQUAKE3",   7025,  "Plasma",                 "ammo_plasma",            "Ammo"),
            new("OQUAKE3",   7026,  "Bullets",                "ammo_bullets",           "Ammo"),
            new("OQUAKE3",   7027,  "Grenades",               "ammo_grenades",          "Ammo"),
            new("OQUAKE3",   7028,  "BFG Ammo",               "ammo_bfg",               "Ammo"),
            new("OQUAKE3",   7031,  "Small Health",           "item_health_small",      "Health"),
            new("OQUAKE3",   7032,  "Health",                 "item_health",            "Health"),
            new("OQUAKE3",   7033,  "Large Health",           "item_health_large",      "Health"),
            new("OQUAKE3",   7034,  "Mega Health",            "item_health_mega",       "Health"),
            new("OQUAKE3",   7041,  "Armor Shard",            "item_armor_shard",       "Armor"),
            new("OQUAKE3",   7042,  "Combat Armor (Yellow)",  "item_armor_combat",      "Armor"),
            new("OQUAKE3",   7043,  "Body Armor (Red)",       "item_armor_body",        "Armor"),
            new("OQUAKE3",   7051,  "Quad Damage",            "item_quad",              "PowerUps"),
            new("OQUAKE3",   7052,  "Regeneration",           "item_regen",             "PowerUps"),
            new("OQUAKE3",   7053,  "Haste",                  "item_haste",             "PowerUps"),
            new("OQUAKE3",   7054,  "Battle Suit",            "item_enviro",            "PowerUps"),
            new("OQUAKE3",   7055,  "Flight",                 "item_flight",            "PowerUps"),
            new("OQUAKE3",   7056,  "Invisibility",           "item_invis",             "PowerUps"),

            // ODUKE3D (Duke Nukem 3D — EDuke32 / Duke-RT)
            new("ODUKE3D",   8001,  "Blue Keycard",           "BLUEKEY",                "Keys"),
            new("ODUKE3D",   8002,  "Red Keycard",            "REDKEY",                 "Keys"),
            new("ODUKE3D",   8003,  "Yellow Keycard",         "YELLOWKEY",              "Keys"),
            new("ODUKE3D",   8004,  "Blue Access Card",       "ACCESSCARD",             "Keys"),
            new("ODUKE3D",   8005,  "Red Access Card",        "ACCESSCARD",             "Keys"),
            new("ODUKE3D",   8006,  "Yellow Access Card",     "ACCESSCARD",             "Keys"),
            new("ODUKE3D",   8011,  "Pistol",                 "PISTOL",                 "Weapons"),
            new("ODUKE3D",   8012,  "Shotgun",                "SHOTGUN",                "Weapons"),
            new("ODUKE3D",   8013,  "Chaingun Cannon",        "CHAINGUNSPRITE",         "Weapons"),
            new("ODUKE3D",   8014,  "RPG",                    "RPGSPRITE",              "Weapons"),
            new("ODUKE3D",   8015,  "Shrinker",               "SHRINKERSPRITE",         "Weapons"),
            new("ODUKE3D",   8016,  "Devastator",             "DEVISTATORSPRITE",       "Weapons"),
            new("ODUKE3D",   8017,  "Laser Tripbomb",         "TRIPBOMBSPRITE",         "Weapons"),
            new("ODUKE3D",   8018,  "Freezethrower",          "FREEZESPRITE",           "Weapons"),
            new("ODUKE3D",   8019,  "Expander",               "EXPANDERSPRITE",         "Weapons"),
            new("ODUKE3D",   8021,  "Small Medkit",           "FIRSTAIDKIT",            "Health"),
            new("ODUKE3D",   8022,  "Large Medkit",           "HEALTHBOX",              "Health"),
            new("ODUKE3D",   8023,  "Atomic Health",          "ATOMICHEALTH",           "Health"),
            new("ODUKE3D",   8024,  "Portable Medkit",        "FIRSTAIDKIT",            "Health"),
            new("ODUKE3D",   8031,  "Jetpack",                "JETPACK",                "PowerUps"),
            new("ODUKE3D",   8032,  "Scuba Gear",             "SCUBAGEAR",              "PowerUps"),
            new("ODUKE3D",   8033,  "Night Vision Goggles",   "NIGHTVISION",            "PowerUps"),
            new("ODUKE3D",   8034,  "Steroids",               "STEROIDS",               "PowerUps"),
            new("ODUKE3D",   8035,  "HoloDuke",               "HOLODUKE",               "PowerUps"),
            new("ODUKE3D",   8101,  "Assault Trooper",        "LIZTROOP",               "Monsters"),
            new("ODUKE3D",   8102,  "Pig Cop",                "PIGCOP",                 "Monsters"),
            new("ODUKE3D",   8103,  "Pig Cop Tank",           "PIGCOPBOAT",             "Monsters"),
            new("ODUKE3D",   8104,  "Enforcer",               "LIZMAN",                 "Monsters"),
            new("ODUKE3D",   8105,  "Commander",              "COMMANDER",              "Monsters"),
            new("ODUKE3D",   8106,  "Octabrain",              "OCTABRAIN",              "Monsters"),
            new("ODUKE3D",   8107,  "Protozoid Slimer",       "ROTATEGUN",              "Monsters"),
            new("ODUKE3D",   8108,  "Sentry Drone",           "RECON",                  "Monsters"),
            new("ODUKE3D",   8109,  "Battlelord",             "BATTLELORD",             "Monsters"),
            new("ODUKE3D",   8110,  "Alien Queen",            "QUEEN",                  "Monsters"),
            new("ODUKE3D",   8111,  "Overlord",               "OVERLORD",               "Monsters"),

            // OWOLF3D (Wolfenstein 3D — ECWolf)
            new("OWOLF3D",   9001,  "Gold Key",               "GoldKey",                "Keys"),
            new("OWOLF3D",   9002,  "Silver Key",             "SilverKey",              "Keys"),
            new("OWOLF3D",   9011,  "Pistol",                 "Pistol",                 "Weapons"),
            new("OWOLF3D",   9012,  "Machine Gun",            "MachineGun",             "Weapons"),
            new("OWOLF3D",   9013,  "Chaingun",               "Chaingun",               "Weapons"),
            new("OWOLF3D",   9021,  "Food",                   "Food",                   "Health"),
            new("OWOLF3D",   9022,  "First Aid Kit",          "FirstAidKit",            "Health"),
            new("OWOLF3D",   9023,  "One-Up",                 "ExtraLife",              "Health"),
            new("OWOLF3D",   9024,  "Ammo Clip",              "AmmoClip",               "Ammo"),
            new("OWOLF3D",   9025,  "Box of Ammo",            "BoxOfAmmo",              "Ammo"),
            new("OWOLF3D",   9026,  "Gold Cross",             "GoldenCross",            "Misc"),
            new("OWOLF3D",   9027,  "Silver Cross",           "SilverCross",            "Misc"),
            new("OWOLF3D",   9028,  "Gold Cup",               "GoldCup",                "Misc"),
            new("OWOLF3D",   9029,  "Crown",                  "Crown",                  "Misc"),
            new("OWOLF3D",   9101,  "Guard (Pistol)",         "Guard",                  "Monsters"),
            new("OWOLF3D",   9102,  "Officer",                "Officer",                "Monsters"),
            new("OWOLF3D",   9103,  "SS Guard",               "WolfensteinSS",          "Monsters"),
            new("OWOLF3D",   9104,  "Guard Dog",              "Dog",                    "Monsters"),
            new("OWOLF3D",   9105,  "Mutant",                 "Mutant",                 "Monsters"),
            new("OWOLF3D",   9106,  "Hans Grosse (Boss)",     "HansGrosse",             "Monsters"),
            new("OWOLF3D",   9107,  "Dr. Schabbs (Boss)",     "DrSchabbs",              "Monsters"),
            new("OWOLF3D",   9108,  "Hitler / Mecha-Hitler",  "AdolfHitler",            "Monsters"),

            // ODOOM3 / ODOOM3-BFG (Doom 3 — dhewm3 / RBDOOM-3-BFG)
            new("ODOOM3",    10011, "Pistol",                 "weapon_pistol",           "Weapons"),
            new("ODOOM3",    10012, "Shotgun",                "weapon_shotgun",          "Weapons"),
            new("ODOOM3",    10013, "Machine Gun",            "weapon_machinegun",       "Weapons"),
            new("ODOOM3",    10014, "Chaingun",               "weapon_chaingun",         "Weapons"),
            new("ODOOM3",    10015, "Rocket Launcher",        "weapon_rocketlauncher",   "Weapons"),
            new("ODOOM3",    10016, "Plasma Rifle",           "weapon_plasmagun",        "Weapons"),
            new("ODOOM3",    10017, "BFG 9000",               "weapon_bfg",              "Weapons"),
            new("ODOOM3",    10018, "Chainsaw",               "weapon_chainsaw",         "Weapons"),
            new("ODOOM3",    10019, "Soul Cube",              "weapon_soulcube",         "Weapons"),
            new("ODOOM3",    10021, "Pistol Ammo",            "ammo_bullets",            "Ammo"),
            new("ODOOM3",    10022, "Shotgun Shells",         "ammo_shells",             "Ammo"),
            new("ODOOM3",    10023, "Machine Gun Ammo",       "ammo_mgammo",             "Ammo"),
            new("ODOOM3",    10024, "Chaingun Belt",          "ammo_belt",               "Ammo"),
            new("ODOOM3",    10025, "Rockets",                "ammo_rockets",            "Ammo"),
            new("ODOOM3",    10026, "Plasma Cells",           "ammo_cells",              "Ammo"),
            new("ODOOM3",    10031, "Small Med Pack",         "item_medkit_small",       "Health"),
            new("ODOOM3",    10032, "Large Med Pack",         "item_medkit_large",       "Health"),
            new("ODOOM3",    10034, "Armor Vest",             "item_armor",              "Armor"),
            new("ODOOM3",    10035, "Security Armor",         "item_armor_security",     "Armor"),
            new("ODOOM3",    10101, "Zombie",                 "monster_zombie",          "Monsters"),
            new("ODOOM3",    10102, "Imp",                    "monster_imp",             "Monsters"),
            new("ODOOM3",    10103, "Pinky (Demon)",          "monster_pinky",           "Monsters"),
            new("ODOOM3",    10104, "Cacodemon",              "monster_cacodemon",       "Monsters"),
            new("ODOOM3",    10105, "Hell Knight",            "monster_hellknight",      "Monsters"),
            new("ODOOM3",    10106, "Revenant",               "monster_revenant",        "Monsters"),
            new("ODOOM3",    10107, "Mancubus",               "monster_mancubus",        "Monsters"),
            new("ODOOM3",    10108, "Arch-Vile",              "monster_archvile",        "Monsters"),
            new("ODOOM3",    10109, "Vagary",                 "monster_vagary",          "Monsters"),
            new("ODOOM3",    10110, "Guardian of Hell",       "monster_guardian",        "Monsters"),
            new("ODOOM3",    10111, "Cyberdemon",             "monster_cyberdemon",      "Monsters"),
        };

        // ── Aliases: RTX / RT / BFG variants share their base game's assets ──────────

        private static string NormalizeGameId(string gameId)
        {
            if (gameId == null) return null;
            return gameId.ToUpperInvariant() switch
            {
                "OQUAKE2-RTX" => "OQUAKE2",
                "ODUKE3D-RT"  => "ODUKE3D",
                "ODOOM3-BFG"  => "ODOOM3",
                var id        => id
            };
        }

        // ── Endpoint ──────────────────────────────────────────────────────────────────

        /// <summary>
        /// Returns all OASIS game assets, optionally filtered by gameId.
        /// OQUAKE2-RTX → aliased to OQUAKE2; ODUKE3D-RT → ODUKE3D; ODOOM3-BFG → ODOOM3.
        /// </summary>
        [HttpGet]
        public IActionResult GetAssets([FromQuery] string gameId = null)
        {
            var normalised = NormalizeGameId(gameId);

            IEnumerable<OAssetDto> result = _all;

            if (!string.IsNullOrEmpty(normalised))
                result = _all.Where(a =>
                    string.Equals(a.gameId, normalised, System.StringComparison.OrdinalIgnoreCase) ||
                    a.gameId == "*");

            return Ok(result);
        }
    }
}
