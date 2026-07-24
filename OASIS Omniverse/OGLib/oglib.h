/**
 * oglib.h — OGLib master include
 *
 * Include this single header to pull in all OGLib utilities.
 * The implementation headers (oglib_config.h, oglib_beamin.h,
 * oglib_session.h) follow the single-header library pattern:
 * their implementation code is compiled only in the TU that defines
 * the corresponding OGLIB_*_IMPL macro before including oglib.h.
 *
 * QUICK START
 * -----------
 * In exactly ONE .c/.cpp file in your game:
 *
 *   #define OGLIB_SESSION_IMPL
 *   #define OGLIB_CONFIG_IMPL
 *   #define OGLIB_BEAMIN_IMPL
 *   #include "oglib.h"
 *
 * In all other files that need OGLib:
 *
 *   #include "oglib.h"
 *
 * FILES OVERVIEW
 * --------------
 *   oglib.h              — this file; master include
 *   oglib_str.h          — string helpers (contains_nocase, safe copy, trim)
 *   oglib_json.h         — minimal JSON key→value extractor/writer
 *   oglib_config.h       — oasisstar.json load/save; star_config_t struct
 *   oglib_beamin.h       — beamin/beamout workflow (auth, session restore, persist)
 *   oglib_session.h      — runtime DLL forwarders (GetProcAddress / dlsym shims)
 *   oglib_crossgame.h    — cross-game ammo/weapon mapping defaults
 *
 * See OGLib/README.md and OASIS Omniverse/ARCHITECTURE.md for full docs.
 */
#ifndef OGLIB_H
#define OGLIB_H

#include "oglib_str.h"
#include "oglib_json.h"
#include "oglib_crossgame.h"
#include "oglib_session.h"
#include "oglib_config.h"
#include "oglib_beamin.h"

#endif /* OGLIB_H */
