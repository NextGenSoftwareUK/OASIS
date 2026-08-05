/*
 * OGEditorClient.h  —  OGEditorSDK C ABI for native C++ editor plugins
 *
 * Allows TrenchBroom, NetRadiant, DarkRadiant, Mapster32, or any other
 * C/C++ editor to load OGEditorClient.dll and call into the managed SDK
 * without requiring any .NET knowledge on the C++ side.
 *
 * Pattern mirrors STARAPIClient/star_api.h.
 * The implementation lives in OGEditorSDK.Native (NativeAOT, .NET 8).
 *
 * All string in/out parameters are UTF-8.
 * Return value conventions:
 *   int  0 = success, negative = error (see OGEDITOR_ERR_* codes)
 *   char* out-buffers: caller allocates, SDK writes up to bufLen-1 bytes + null terminator
 *
 * Thread safety: ogeditor_init / ogeditor_dispose must be called from the same thread.
 *                Query methods (get_*) are safe to call from any thread.
 */

#pragma once

#ifdef _WIN32
#  define OGEditorClient __declspec(dllimport)
#else
#  define OGEditorClient
#endif

#ifdef __cplusplus
extern "C" {
#endif

/* ── Error codes ──────────────────────────────────────────────────────────── */

#define OGEDITOR_OK               0
#define OGEDITOR_ERR_INVALID_HANDLE  -1
#define OGEDITOR_ERR_BUFFER_TOO_SMALL -2
#define OGEDITOR_ERR_NETWORK         -3
#define OGEDITOR_ERR_NOT_FOUND       -4
#define OGEDITOR_ERR_JSON_PARSE      -5
#define OGEDITOR_ERR_IO              -6

/* ── Handle ───────────────────────────────────────────────────────────────── */

typedef void* OGEditorHandle;

/* ── Lifecycle ────────────────────────────────────────────────────────────── */

/**
 * Create an SDK handle connected to the STAR API.
 *
 * @param starApiBaseUrl  Base URL of the STAR API, e.g. "http://localhost:5000"
 * @param avatarId        Authenticated avatar GUID string (may be empty/null for read-only)
 * @return  Non-null handle on success; NULL on allocation failure.
 */
OGEditorClient OGEditorHandle ogeditor_init(
    const char* starApiBaseUrl,
    const char* avatarId);

/**
 * Destroy a handle and release all managed resources.
 * Passing NULL is a no-op.
 */
OGEditorClient void ogeditor_dispose(OGEditorHandle handle);

/* ── Asset catalog ────────────────────────────────────────────────────────── */

/**
 * Return the full asset catalog for one OGame as a JSON array.
 *
 * @param gameId  One of: "ODOOM", "OQUAKE", "OQUAKE2", "OQUAKE2-RTX",
 *                        "OQUAKE3", "ODUKE3D", "ODUKE3D-RT", "OWOLF3D",
 *                        "ODOOM3", "ODOOM3-BFG"
 *                Use "*" to get every asset across all games.
 * @param outBuf  Caller-allocated output buffer (UTF-8 JSON).
 * @param bufLen  Size of outBuf in bytes.
 * @return OGEDITOR_OK or error code.
 *
 * JSON element shape:
 *   { "gameId": "OQUAKE2", "thingType": 6101, "displayName": "Soldier",
 *     "nativeClassname": "monster_soldier", "category": "Monsters" }
 */
OGEditorClient int ogeditor_get_assets_json(
    OGEditorHandle handle,
    const char*    gameId,
    char*          outBuf,
    int            bufLen);

/**
 * Look up a single asset by OASIS thing type.
 * Returns OGEDITOR_ERR_NOT_FOUND if the type is unknown.
 */
OGEditorClient int ogeditor_get_asset_by_type(
    OGEditorHandle handle,
    int            thingType,
    char*          outBuf,
    int            bufLen);

/* ── Portals / map sidecar ────────────────────────────────────────────────── */

/**
 * Read the OASIS map sidecar for a given map file path.
 * Returns the sidecar JSON (portals + crossGameEntities arrays), or "{}" if no sidecar.
 *
 * @param mapFilePath  Absolute path to the .map / .wad / etc. file.
 */
OGEditorClient int ogeditor_get_portals_json(
    OGEditorHandle handle,
    const char*    mapFilePath,
    char*          outBuf,
    int            bufLen);

/**
 * Append a portal entry to the map sidecar AND persist it to the STAR API.
 *
 * @param mapFilePath  Absolute path to the map file.
 * @param portalJson   JSON object:
 *   { "thingId":42, "x":512.0, "y":-256.0,
 *     "destinationGame":"OQUAKE2", "destinationMap":"base1",
 *     "destinationX":0.0, "destinationY":0.0, "destinationZ":0.0 }
 * @return OGEDITOR_OK or error code.
 */
OGEditorClient int ogeditor_append_portal(
    OGEditorHandle handle,
    const char*    mapFilePath,
    const char*    portalJson);

/* ── Quests ───────────────────────────────────────────────────────────────── */

/**
 * Return a JSON array of quests from the STAR API, optionally filtered by gameId.
 * @param gameId  OGame ID filter, or NULL/empty for all quests.
 */
OGEditorClient int ogeditor_get_quests_json(
    OGEditorHandle handle,
    const char*    gameId,
    char*          outBuf,
    int            bufLen);

/**
 * Create a new quest via the STAR API.
 * @param questJson  JSON request body (name, description, gameId, …).
 * @param outBuf     Receives the created quest JSON on success.
 */
OGEditorClient int ogeditor_create_quest(
    OGEditorHandle handle,
    const char*    questJson,
    char*          outBuf,
    int            bufLen);

/**
 * Bind a quest objective to a specific map trigger (thing, linedef, sector).
 *
 * @param questId      Quest GUID string.
 * @param bindingJson  JSON body:
 *   { "objectiveId":"<guid>", "mapPath":"<abs-path>",
 *     "triggerType":"Thing|Linedef|Sector", "triggerId":<int> }
 */
OGEditorClient int ogeditor_bind_objective(
    OGEditorHandle handle,
    const char*    questId,
    const char*    bindingJson);

/* ── Missions ─────────────────────────────────────────────────────────────── */

/**
 * Return a JSON array of missions, optionally filtered by questId.
 */
OGEditorClient int ogeditor_get_missions_json(
    OGEditorHandle handle,
    const char*    questId,
    char*          outBuf,
    int            bufLen);

/* ── Entity mappings ──────────────────────────────────────────────────────── */

/**
 * Convert a native entity classname/actor name to an OASIS thing type.
 * @param sourceGame  OGame ID the classname belongs to (e.g. "OQUAKE2").
 * @param classname   Native entity classname (e.g. "monster_soldier").
 * @param outThingType Receives the OASIS thing type on success.
 * @return OGEDITOR_OK, or OGEDITOR_ERR_NOT_FOUND if unmapped.
 */
OGEditorClient int ogeditor_classname_to_thing_type(
    OGEditorHandle handle,
    const char*    sourceGame,
    const char*    classname,
    int*           outThingType);

/**
 * Convert an OASIS thing type to a native classname in the target game.
 * @param targetGame  OGame ID for the target format.
 * @param thingType   OASIS thing type.
 * @param outBuf      Receives the native classname string.
 */
OGEditorClient int ogeditor_thing_type_to_classname(
    OGEditorHandle handle,
    const char*    targetGame,
    int            thingType,
    char*          outBuf,
    int            bufLen);

/* ── Map format conversion (OGMapFormat SDK) ──────────────────────────────── */

/**
 * Return a JSON array listing all registered format adapters.
 *
 * JSON element shape:
 *   { "formatId": "quake2", "displayName": "Quake2",
 *     "extensions": [".map"], "family": 0 }
 * family: 0=Brush3D, 1=Sector2D, 2=Build2D, 3=Tile2D
 */
OGEditorClient int ogeditor_list_adapters(char* outBuf, int bufLen);

/**
 * Return the estimated conversion fidelity (0.0–1.0) from srcFormat to dstFormat.
 * 1.0 = lossless; <0.6 = significant manual cleanup required.
 * Returns -1.0 if either format is unknown.
 */
OGEditorClient float ogeditor_conversion_fidelity(
    const char* srcFormatId,
    const char* dstFormatId);

/**
 * Convert a map file from one format to another.
 *
 * @param srcPath       Absolute path to the source map file.
 * @param srcFormatId   Source format adapter ID (e.g. "quake2", "doom").
 * @param dstPath       Absolute path for the output file.
 * @param dstFormatId   Destination format adapter ID.
 * @param outResultJson Caller-allocated buffer that receives a JSON result:
 *   { "success": true/false, "fidelity": 0.95,
 *     "diagnostics": [{ "severity": "Warning", "message": "..." }, …] }
 * @param bufLen        Size of outResultJson in bytes.
 * @return OGEDITOR_OK on success (even if diagnostics contain warnings).
 *         OGEDITOR_ERR_IO if source could not be read or destination written.
 *         OGEDITOR_ERR_NOT_FOUND if an adapter ID is unknown.
 */
OGEditorClient int ogeditor_convert_map(
    const char* srcPath,
    const char* srcFormatId,
    const char* dstPath,
    const char* dstFormatId,
    char*       outResultJson,
    int         bufLen);

/* ── Utility ──────────────────────────────────────────────────────────────── */

/**
 * Return the SDK version string (e.g. "1.0.0").
 */
OGEditorClient int ogeditor_version(char* outBuf, int bufLen);

/**
 * Return the last error message string for a given handle.
 * Useful when a function returns a non-OK code and you need details.
 */
OGEditorClient int ogeditor_last_error(OGEditorHandle handle, char* outBuf, int bufLen);

#ifdef __cplusplus
}  /* extern "C" */
#endif
