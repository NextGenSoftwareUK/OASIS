/**
 * ogamelib.h — OGameLib master include
 *
 * Include this single header to pull in all OGameLib utilities.
 * The implementation headers (ogamelib_config.h, ogamelib_beamin.h,
 * ogamelib_session.h) follow the single-header library pattern:
 * their implementation code is compiled only in the TU that defines
 * the corresponding OGAMELIB_*_IMPL macro before including ogamelib.h.
 *
 * QUICK START
 * -----------
 * In exactly ONE .c/.cpp file in your game:
 *
 *   #define OGAMELIB_SESSION_IMPL
 *   #define OGAMELIB_CONFIG_IMPL
 *   #define OGAMELIB_BEAMIN_IMPL
 *   #include "ogamelib.h"
 *
 * In all other files that need OGameLib:
 *
 *   #include "ogamelib.h"
 *
 * FILES OVERVIEW
 * --------------
 *   ogamelib.h              — this file; master include
 *   ogamelib_str.h          — string helpers (contains_nocase, safe copy, trim)
 *   ogamelib_json.h         — minimal JSON key→value extractor/writer
 *   ogamelib_config.h       — oasisstar.json load/save; star_config_t struct
 *   ogamelib_beamin.h       — beamin/beamout workflow (auth, session restore, persist)
 *   ogamelib_session.h      — runtime DLL forwarders (GetProcAddress / dlsym shims)
 *   ogamelib_crossgame.h    — cross-game ammo/weapon mapping defaults
 *
 * See OGameLib/README.md and OASIS Omniverse/ARCHITECTURE.md for full docs.
 */
#ifndef OGAMELIB_H
#define OGAMELIB_H

#include "ogamelib_str.h"
#include "ogamelib_json.h"
#include "ogamelib_crossgame.h"
#include "ogamelib_session.h"
#include "ogamelib_config.h"
#include "ogamelib_beamin.h"

#endif /* OGAMELIB_H */
