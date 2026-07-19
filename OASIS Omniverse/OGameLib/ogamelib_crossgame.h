/**
 * ogamelib_crossgame.h — OGameLib cross-game asset mapping defaults
 *
 * Default translation tables for ammo and weapons between ODOOM and OQuake.
 * Games include this header to get the shared constants; they may override
 * individual entries after calling ogamelib_crossgame_init_defaults().
 *
 * Key principle: one place for cross-game design constants so both games
 * stay in sync without maintaining separate copies of the same tables.
 */
#ifndef OGAMELIB_CROSSGAME_H
#define OGAMELIB_CROSSGAME_H

#ifdef __cplusplus
extern "C" {
#endif

#define OGAMELIB_CROSSGAME_MAP_MAX 16

typedef struct {
    char from[64];
    char to[64];
} ogamelib_crossgame_entry_t;

typedef struct {
    /* ODOOM ammo name → OQuake ammo name */
    ogamelib_crossgame_entry_t doom_ammo_to_quake[OGAMELIB_CROSSGAME_MAP_MAX];
    int doom_ammo_to_quake_count;

    /* OQuake ammo name → ODOOM ammo name */
    ogamelib_crossgame_entry_t quake_ammo_to_doom[OGAMELIB_CROSSGAME_MAP_MAX];
    int quake_ammo_to_doom_count;

    /* ODOOM weapon name → OQuake weapon name */
    ogamelib_crossgame_entry_t doom_weapon_to_quake[OGAMELIB_CROSSGAME_MAP_MAX];
    int doom_weapon_to_quake_count;

    /* OQuake weapon name → ODOOM weapon name */
    ogamelib_crossgame_entry_t quake_weapon_to_doom[OGAMELIB_CROSSGAME_MAP_MAX];
    int quake_weapon_to_doom_count;
} ogamelib_crossgame_maps_t;

/**
 * Populate maps with the shared default cross-game translation tables.
 * Call once at startup; then override individual entries as needed.
 */
static inline void ogamelib_crossgame_init_defaults(ogamelib_crossgame_maps_t* m)
{
    if (!m) return;

    /* ── Ammo: ODOOM → OQuake ── */
    m->doom_ammo_to_quake_count = 4;
#define SET(arr, i, f, t) do { \
    snprintf((arr)[i].from, sizeof((arr)[i].from), "%s", (f)); \
    snprintf((arr)[i].to,   sizeof((arr)[i].to),   "%s", (t)); \
} while (0)
    SET(m->doom_ammo_to_quake, 0, "Bullets", "Nails");
    SET(m->doom_ammo_to_quake, 1, "Shells",  "Shells");
    SET(m->doom_ammo_to_quake, 2, "Rockets", "Rockets");
    SET(m->doom_ammo_to_quake, 3, "Cells",   "Cells");

    /* ── Ammo: OQuake → ODOOM ── */
    m->quake_ammo_to_doom_count = 4;
    SET(m->quake_ammo_to_doom, 0, "Nails",   "Bullets");
    SET(m->quake_ammo_to_doom, 1, "Shells",  "Shells");
    SET(m->quake_ammo_to_doom, 2, "Rockets", "Rockets");
    SET(m->quake_ammo_to_doom, 3, "Cells",   "Cells");

    /* ── Weapons: ODOOM → OQuake ── */
    m->doom_weapon_to_quake_count = 6;
    SET(m->doom_weapon_to_quake, 0, "Chaingun",    "Nailgun");
    SET(m->doom_weapon_to_quake, 1, "Shotgun",     "Shotgun");
    SET(m->doom_weapon_to_quake, 2, "BFG9000",     "Lightning Gun");
    SET(m->doom_weapon_to_quake, 3, "RocketLauncher", "Rocket Launcher");
    SET(m->doom_weapon_to_quake, 4, "PlasmaGun",   "Grenade Launcher");
    SET(m->doom_weapon_to_quake, 5, "Chainsaw",    "Axe");

    /* ── Weapons: OQuake → ODOOM ── */
    m->quake_weapon_to_doom_count = 6;
    SET(m->quake_weapon_to_doom, 0, "Nailgun",         "Chaingun");
    SET(m->quake_weapon_to_doom, 1, "Shotgun",          "Shotgun");
    SET(m->quake_weapon_to_doom, 2, "Lightning Gun",    "BFG9000");
    SET(m->quake_weapon_to_doom, 3, "Rocket Launcher",  "RocketLauncher");
    SET(m->quake_weapon_to_doom, 4, "Grenade Launcher", "PlasmaGun");
    SET(m->quake_weapon_to_doom, 5, "Axe",              "Chainsaw");
#undef SET
}

/**
 * Look up a cross-game mapping entry by name (case-sensitive).
 * Returns the mapped-to name, or NULL if not found.
 */
static inline const char* ogamelib_crossgame_lookup(
    const ogamelib_crossgame_entry_t* entries, int count, const char* from_name)
{
    if (!entries || !from_name) return NULL;
    for (int i = 0; i < count; i++) {
        if (strcmp(entries[i].from, from_name) == 0)
            return entries[i].to;
    }
    return NULL;
}

#ifdef __cplusplus
}
#endif

#endif /* OGAMELIB_CROSSGAME_H */
