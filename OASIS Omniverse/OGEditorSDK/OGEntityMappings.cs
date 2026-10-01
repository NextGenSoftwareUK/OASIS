// OGEntityMappings — bidirectional lookup tables for cross-game entity conversion.
//
// Used by OGEditorSDK consumers (UDB OASISMapConverter, TrenchBroom plugin, etc.) to
// map between a native entity classname / actor name in a source game and the
// equivalent OASIS thing type, and vice-versa.
//
// Conversion philosophy:
//   - Functional equivalence is preferred over 1-to-1 naming. A Q2 Soldier maps
//     to a Doom Zombieman (same low-tier grunt role), not necessarily by name.
//   - The portal classname "oasis_portal" → thing type 5900 is universal.

using System.Collections.Generic;

namespace OGEditorSDK
{
    /// <summary>
    /// Static bidirectional mapping tables between native classnames / actor names
    /// and OASIS Omniverse thing types.
    /// </summary>
    public static class OGEntityMappings
    {
        // ── Quake I classname → Doom thing type ──────────────────────────────────

        public static readonly IReadOnlyDictionary<string, int> QuakeClassToDoom =
            new Dictionary<string, int>(System.StringComparer.OrdinalIgnoreCase)
        {
            { "oasis_portal",           5900 },
            { "item_key_gold",          5005 },
            { "item_key_silver",        5013 },
            { "weapon_shotgun",         5201 },
            { "weapon_supershotgun",    5202 },
            { "weapon_nailgun",         5203 },
            { "weapon_supernailgun",    5204 },
            { "weapon_grenadelauncher", 5205 },
            { "weapon_rocketlauncher",  5206 },
            { "weapon_lightning",       5207 },
            { "item_spikes",            5208 },
            { "item_shells",            5209 },
            { "item_rockets",           5210 },
            { "item_cells",             5211 },
            { "item_health",            5212 },
            { "item_health_small",      5213 },
            { "item_armor1",            5214 },
            { "item_armor2",            5215 },
            { "item_armorInv",          5216 },
            { "monster_demon",          5302 },
            { "monster_shambler",       5303 },
            { "monster_grunt",          5304 },
            { "monster_fish",           5305 },
            { "monster_ogre",           5309 },
            { "monster_dog",            3010 },
            { "monster_zombie",         3011 },
            { "monster_enforcer",       5366 },
            { "monster_spawn",          5368 },
            { "monster_hell_knight",    5369 },
        };

        // ── Quake II classname → Doom thing type ─────────────────────────────────

        public static readonly IReadOnlyDictionary<string, int> Quake2ClassToDoom =
            new Dictionary<string, int>(System.StringComparer.OrdinalIgnoreCase)
        {
            { "oasis_portal",           5900 },
            { "item_key_blue_key",      6001 },
            { "item_key_red_key",       6002 },
            { "item_key_commander_head",6003 },
            { "weapon_blaster",         6011 },
            { "weapon_shotgun",         6012 },
            { "weapon_supershotgun",    6013 },
            { "weapon_machinegun",      6014 },
            { "weapon_chaingun",        6015 },
            { "weapon_grenadelauncher", 6016 },
            { "weapon_rocketlauncher",  6017 },
            { "weapon_hyperblaster",    6018 },
            { "weapon_railgun",         6019 },
            { "weapon_bfg",             6020 },
            { "ammo_bullets",           6021 },
            { "ammo_shells",            6022 },
            { "ammo_grenades",          6023 },
            { "ammo_rockets",           6024 },
            { "ammo_cells",             6025 },
            { "ammo_slugs",             6026 },
            { "item_health_small",      6031 },
            { "item_health",            6032 },
            { "item_health_mega",       6033 },
            { "item_armor_jacket",      6041 },
            { "item_armor_combat",      6042 },
            { "item_armor_body",        6043 },
            { "monster_soldier",        6101 },
            { "monster_infantry",       6102 },
            { "monster_gunner",         6103 },
            { "monster_berserker",      6104 },
            { "monster_gladiator",      6105 },
            { "monster_flyer",          6106 },
            { "monster_medic",          6107 },
            { "monster_parasite",       6108 },
            { "monster_brain",          6109 },
            { "monster_supertank",      6110 },
            { "monster_tank",           6111 },
            { "monster_makron",         6112 },
        };

        // ── Quake III classname → Doom thing type ─────────────────────────────────

        public static readonly IReadOnlyDictionary<string, int> Quake3ClassToDoom =
            new Dictionary<string, int>(System.StringComparer.OrdinalIgnoreCase)
        {
            { "oasis_portal",           5900 },
            { "weapon_rocketlauncher",  7011 },
            { "weapon_railgun",         7012 },
            { "weapon_shotgun",         7013 },
            { "weapon_lightning",       7014 },
            { "weapon_plasmagun",       7015 },
            { "weapon_bfg",             7016 },
            { "weapon_gauntlet",        7017 },
            { "ammo_rockets",           7021 },
            { "ammo_slugs",             7022 },
            { "ammo_shells",            7023 },
            { "ammo_lightning",         7024 },
            { "ammo_plasma",            7025 },
            { "item_health_small",      7031 },
            { "item_health",            7032 },
            { "item_health_large",      7033 },
            { "item_health_mega",       7034 },
            { "item_armor_shard",       7041 },
            { "item_armor_combat",      7042 },
            { "item_armor_body",        7043 },
            { "item_quad",              7051 },
            { "item_regen",             7052 },
            { "item_haste",             7053 },
            { "item_enviro",            7054 },
            { "item_flight",            7055 },
            { "item_invis",             7056 },
        };

        // ── Duke Nukem 3D actor name → Doom thing type ───────────────────────────

        public static readonly IReadOnlyDictionary<string, int> DukeActorToDoom =
            new Dictionary<string, int>(System.StringComparer.OrdinalIgnoreCase)
        {
            { "BLUEKEY",          8001 },
            { "REDKEY",           8002 },
            { "YELLOWKEY",        8003 },
            { "PISTOL",           8011 },
            { "SHOTGUN",          8012 },
            { "CHAINGUNSPRITE",   8013 },
            { "RPGSPRITE",        8014 },
            { "SHRINKERSPRITE",   8015 },
            { "DEVISTATORSPRITE", 8016 },
            { "TRIPBOMBSPRITE",   8017 },
            { "FREEZESPRITE",     8018 },
            { "EXPANDERSPRITE",   8019 },
            { "FIRSTAIDKIT",      8021 },
            { "HEALTHBOX",        8022 },
            { "ATOMICHEALTH",     8023 },
            { "JETPACK",          8031 },
            { "SCUBAGEAR",        8032 },
            { "NIGHTVISION",      8033 },
            { "STEROIDS",         8034 },
            { "HOLODUKE",         8035 },
            { "LIZTROOP",         8101 },
            { "PIGCOP",           8102 },
            { "PIGCOPBOAT",       8103 },
            { "LIZMAN",           8104 },
            { "COMMANDER",        8105 },
            { "OCTABRAIN",        8106 },
            { "ROTATEGUN",        8107 },
            { "RECON",            8108 },
            { "BATTLELORD",       8109 },
            { "QUEEN",            8110 },
            { "OVERLORD",         8111 },
        };

        // ── Wolfenstein 3D DECORATE classname → Doom thing type ──────────────────

        public static readonly IReadOnlyDictionary<string, int> WolfActorToDoom =
            new Dictionary<string, int>(System.StringComparer.OrdinalIgnoreCase)
        {
            { "GoldKey",        9001 },
            { "SilverKey",      9002 },
            { "Pistol",         9011 },
            { "MachineGun",     9012 },
            { "Chaingun",       9013 },
            { "Food",           9021 },
            { "FirstAidKit",    9022 },
            { "ExtraLife",      9023 },
            { "AmmoClip",       9024 },
            { "BoxOfAmmo",      9025 },
            { "GoldenCross",    9026 },
            { "SilverCross",    9027 },
            { "GoldCup",        9028 },
            { "Crown",          9029 },
            { "Guard",          9101 },
            { "Officer",        9102 },
            { "WolfensteinSS",  9103 },
            { "Dog",            9104 },
            { "Mutant",         9105 },
            { "HansGrosse",     9106 },
            { "DrSchabbs",      9107 },
            { "AdolfHitler",    9108 },
        };

        // ── Reverse lookup: OASIS thing type → native classname (Q-engine / Build / Wolf) ─

        private static Dictionary<int, string> _reverseCache;

        /// <summary>Look up the native classname for an OASIS thing type (uses first registered mapping).</summary>
        public static string NativeClassnameForThingType(int thingType)
        {
            if (_reverseCache == null)
            {
                _reverseCache = new Dictionary<int, string>();
                AddReverse(QuakeClassToDoom);
                AddReverse(Quake2ClassToDoom);
                AddReverse(Quake3ClassToDoom);
                AddReverse(DukeActorToDoom);
                AddReverse(WolfActorToDoom);
            }
            string v;
            return _reverseCache.TryGetValue(thingType, out v) ? v : null;
        }

        private static void AddReverse(IReadOnlyDictionary<string, int> fwd)
        {
            foreach (var kv in fwd)
                if (!_reverseCache.ContainsKey(kv.Value))
                    _reverseCache[kv.Value] = kv.Key;
        }

        // ── Format-aware lookup (used by OGMapFormat pipeline) ────────────────

        /// <summary>
        /// Return the OASIS thing type for a classname in the given format adapter ID
        /// (e.g. "quake", "quake2", "duke3d"). Returns -1 if not found.
        /// OASIS portal enter/exit classnames always return 5900 regardless of format.
        /// </summary>
        public static int ClassnameToDstThingType(string classname, string srcFormatId)
        {
            if (string.IsNullOrEmpty(classname)) return -1;
            if (classname.Equals("oasis_portal_enter", System.StringComparison.OrdinalIgnoreCase) ||
                classname.Equals("oasis_portal_exit",  System.StringComparison.OrdinalIgnoreCase))
                return 5900;

            var map = GetMapForFormat(srcFormatId);
            if (map != null && map.TryGetValue(classname, out int tt)) return tt;
            return -1;
        }

        /// <summary>
        /// Return the native classname for an OASIS thing type in the given destination
        /// format adapter ID. Falls back to the cross-game reverse lookup if the format
        /// has no specific entry.
        /// </summary>
        public static string ThingTypeToDstClassname(int thingType, string dstFormatId)
        {
            var map = GetMapForFormat(dstFormatId);
            if (map != null)
                foreach (var kv in map)
                    if (kv.Value == thingType) return kv.Key;
            return NativeClassnameForThingType(thingType);
        }

        private static IReadOnlyDictionary<string, int> GetMapForFormat(string formatId)
        {
            if (string.IsNullOrEmpty(formatId)) return null;
            if (formatId.Equals("quake",  System.StringComparison.OrdinalIgnoreCase)) return QuakeClassToDoom;
            if (formatId.Equals("quake2", System.StringComparison.OrdinalIgnoreCase)) return Quake2ClassToDoom;
            if (formatId.Equals("quake3", System.StringComparison.OrdinalIgnoreCase)) return Quake3ClassToDoom;
            if (formatId.Equals("duke3d", System.StringComparison.OrdinalIgnoreCase)) return DukeActorToDoom;
            if (formatId.Equals("wolf3d", System.StringComparison.OrdinalIgnoreCase)) return WolfActorToDoom;
            return null;  // doom, udmf, doom3 use numeric thing IDs — no classname table
        }
    }
}
