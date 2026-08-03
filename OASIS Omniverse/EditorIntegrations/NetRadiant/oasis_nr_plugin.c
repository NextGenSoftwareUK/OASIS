/*
 * oasis_nr_plugin.c — OASIS OGEngine plugin for NetRadiant
 *
 * Implements the classic GtkRadiant/NetRadiant plugin ABI:
 *   - IQUAKE2_MAJOR_VERSION / GtkRadiant plugin entry point
 *   - Exposes three commands via the Plugins menu:
 *       "OASIS Asset Browser"   – lists cross-game assets from STAR API
 *       "OASIS Portal Placer"  – creates an info_oasis_portal_enter brush entity
 *       "OASIS Quest Binder"   – binds a quest objective to the selected entity
 *
 * The plugin loads OGEditorClient.dll / libOGEditorClient.so at startup and calls
 * through the C ABI declared in OGEditorClient.h.  No .NET knowledge required.
 *
 * Build with the NetRadiant SDK (radiant/plugin.h included from the NetRadiant
 * source tree).  Copy the resulting .so / .dll into NetRadiant/plugins/.
 */

#include <stdio.h>
#include <stdlib.h>
#include <string.h>

#ifdef _WIN32
#  define WIN32_LEAN_AND_MEAN
#  include <windows.h>
#  define OASIS_DLLEXPORT __declspec(dllexport)
typedef HMODULE DylibHandle;
static DylibHandle dylib_open(const char* path)  { return LoadLibraryA(path); }
static void*      dylib_sym(DylibHandle h, const char* sym) { return (void*)GetProcAddress(h, sym); }
static void       dylib_close(DylibHandle h) { FreeLibrary(h); }
#else
#  include <dlfcn.h>
#  define OASIS_DLLEXPORT __attribute__((visibility("default")))
typedef void* DylibHandle;
static DylibHandle dylib_open(const char* path)  { return dlopen(path, RTLD_LAZY | RTLD_LOCAL); }
static void*      dylib_sym(DylibHandle h, const char* sym) { return dlsym(h, sym); }
static void       dylib_close(DylibHandle h) { dlclose(h); }
#endif

#include "OGEditorClient.h"

/* ── NetRadiant plugin ABI typedefs (mirror of radiant/plugin.h essentials) ─ */

typedef struct _QERPluginTable {
    int   m_nSize;
    void  (*m_pfnQERPlug_Init)(void* hApp, void* pMainWidget);
    void  (*m_pfnQERPlug_Dispatch)(const char* p, float* vMin, float* vMax, int bSingleBrush);
    int   (*m_pfnQERPlug_GetFuncTable)(void* pFuncTable);
} QERPluginTable;

/* ── Runtime-loaded ogeditor function pointers ────────────────────────────── */

static DylibHandle     g_lib      = NULL;
static OGEditorHandle  g_handle   = NULL;

typedef OGEditorHandle (*fn_init_t)(const char*, const char*);
typedef void           (*fn_dispose_t)(OGEditorHandle);
typedef int            (*fn_get_assets_t)(OGEditorHandle, const char*, char*, int);
typedef int            (*fn_append_portal_t)(OGEditorHandle, const char*, const char*);
typedef int            (*fn_get_quests_t)(OGEditorHandle, const char*, char*, int);
typedef int            (*fn_bind_objective_t)(OGEditorHandle, const char*, const char*);

static fn_init_t           g_fn_init           = NULL;
static fn_dispose_t        g_fn_dispose        = NULL;
static fn_get_assets_t     g_fn_get_assets     = NULL;
static fn_append_portal_t  g_fn_append_portal  = NULL;
static fn_get_quests_t     g_fn_get_quests     = NULL;
static fn_bind_objective_t g_fn_bind_objective = NULL;

static int load_ogeditor(void) {
#ifdef _WIN32
    const char* libname = "OGEditorClient.dll";
#else
    const char* libname = "libOGEditorClient.so";
#endif
    g_lib = dylib_open(libname);
    if (!g_lib) { fprintf(stderr, "[OASIS] Could not load %s\n", libname); return 0; }

    g_fn_init           = (fn_init_t)          dylib_sym(g_lib, "ogeditor_init");
    g_fn_dispose        = (fn_dispose_t)        dylib_sym(g_lib, "ogeditor_dispose");
    g_fn_get_assets     = (fn_get_assets_t)     dylib_sym(g_lib, "ogeditor_get_assets_json");
    g_fn_append_portal  = (fn_append_portal_t)  dylib_sym(g_lib, "ogeditor_append_portal");
    g_fn_get_quests     = (fn_get_quests_t)     dylib_sym(g_lib, "ogeditor_get_quests_json");
    g_fn_bind_objective = (fn_bind_objective_t) dylib_sym(g_lib, "ogeditor_bind_objective");

    if (!g_fn_init || !g_fn_dispose || !g_fn_get_assets ||
        !g_fn_append_portal || !g_fn_get_quests || !g_fn_bind_objective) {
        fprintf(stderr, "[OASIS] Missing exports in OGEditorClient — version mismatch?\n");
        dylib_close(g_lib); g_lib = NULL; return 0;
    }

    g_handle = g_fn_init("http://localhost:5000", "");
    if (!g_handle) { fprintf(stderr, "[OASIS] ogeditor_init returned NULL\n"); return 0; }

    fprintf(stderr, "[OASIS] OGEditorClient loaded OK.\n");
    return 1;
}

/* ── Plugin commands ─────────────────────────────────────────────────────── */

static void cmd_asset_browser(void) {
    char buf[65536];
    if (!g_handle) { fprintf(stderr, "[OASIS] SDK not initialised.\n"); return; }
    int rc = g_fn_get_assets(g_handle, "*", buf, sizeof(buf));
    if (rc != 0) { fprintf(stderr, "[OASIS] get_assets_json error %d\n", rc); return; }

    /* Print to NetRadiant console — a full GUI dialog would require GTK widgets
       linked from the NetRadiant SDK; for now we write to the output window. */
    fprintf(stdout, "[OASIS Asset Catalog]\n%s\n", buf);
}

static void cmd_portal_placer(void) {
    if (!g_handle) { fprintf(stderr, "[OASIS] SDK not initialised.\n"); return; }
    /* In a full integration this reads the current map path from the Radiant
       IMap interface and opens a dialog for the user to fill portal fields.
       Here we demonstrate the API call pattern. */
    const char* map_path  = "current_map.map";   /* replace with IMap::getFilename() */
    const char* portal_js = "{"
        "\"thingId\":1,"
        "\"x\":0.0,\"y\":0.0,"
        "\"destinationGame\":\"ODOOM\","
        "\"destinationMap\":\"e1m1\","
        "\"destinationX\":0.0,\"destinationY\":0.0,\"destinationZ\":0.0"
        "}";
    int rc = g_fn_append_portal(g_handle, map_path, portal_js);
    if (rc == 0)
        fprintf(stdout, "[OASIS] Portal appended to sidecar.\n");
    else
        fprintf(stderr, "[OASIS] append_portal error %d\n", rc);
}

static void cmd_quest_binder(void) {
    char buf[32768];
    if (!g_handle) { fprintf(stderr, "[OASIS] SDK not initialised.\n"); return; }
    int rc = g_fn_get_quests(g_handle, "OQUAKE3", buf, sizeof(buf));
    if (rc != 0) { fprintf(stderr, "[OASIS] get_quests_json error %d\n", rc); return; }
    fprintf(stdout, "[OASIS Quest List for OQUAKE3]\n%s\n", buf);
    /* Full UI: show a dialog, let user pick quest+objective, call bind_objective() */
}

/* ── NetRadiant plugin ABI entry points ────────────────────────────────────── */

static const char* PLUGIN_NAME    = "OASIS OGEngine";
static const char* PLUGIN_CMDS    = "OASIS Asset Browser;OASIS Portal Placer;OASIS Quest Binder";

OASIS_DLLEXPORT const char* QERPlug_GetName(void)     { return PLUGIN_NAME; }
OASIS_DLLEXPORT const char* QERPlug_GetCommandList(void) { return PLUGIN_CMDS; }

OASIS_DLLEXPORT void QERPlug_Init(void* hApp, void* pMainWidget) {
    (void)hApp; (void)pMainWidget;
    load_ogeditor();
}

OASIS_DLLEXPORT void QERPlug_Dispatch(const char* p,
                                       float* vMin, float* vMax,
                                       int bSingleBrush) {
    (void)vMin; (void)vMax; (void)bSingleBrush;
    if (strcmp(p, "OASIS Asset Browser")  == 0) cmd_asset_browser();
    else if (strcmp(p, "OASIS Portal Placer") == 0) cmd_portal_placer();
    else if (strcmp(p, "OASIS Quest Binder")  == 0) cmd_quest_binder();
}

OASIS_DLLEXPORT void QERPlug_Shutdown(void) {
    if (g_handle && g_fn_dispose) g_fn_dispose(g_handle);
    g_handle = NULL;
    if (g_lib) dylib_close(g_lib);
    g_lib = NULL;
}
