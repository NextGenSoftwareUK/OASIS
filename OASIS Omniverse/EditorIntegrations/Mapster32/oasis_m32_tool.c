/*
 * oasis_m32_tool.c — OASIS OGEngine companion tool for Mapster32 / EDuke32
 *
 * Mapster32 does not support native C plugin modules in the same way as
 * GtkRadiant-family editors, so this is a standalone CLI companion tool that:
 *
 *   oasis_m32_tool assets             — dump ODUKE3D asset catalog to stdout
 *   oasis_m32_tool portals <map.map>  — list portals in the map sidecar
 *   oasis_m32_tool add-portal <map.map> <dstGame> <dstMap>
 *                                     — append an OASIS portal to the sidecar
 *   oasis_m32_tool quests [gameId]    — list STAR API quests
 *   oasis_m32_tool convert <src.map> <srcFmt> <dst.map> <dstFmt>
 *                                     — convert map between formats
 *
 * Usage from Mapster32:
 *   1. Open the "User Defined Tools" / external tool dialog.
 *   2. Add this executable with appropriate arguments.
 *   3. Set working directory to the OASIS EditorIntegrations\Mapster32\ folder.
 *
 * The tool loads OGEditorClient.dll / libOGEditorClient.so at runtime from the
 * same directory.  No .NET runtime needed.
 *
 * Build:
 *   Windows:  cl /W3 oasis_m32_tool.c /link /out:oasis_m32_tool.exe
 *   Linux:    gcc -Wall -o oasis_m32_tool oasis_m32_tool.c -ldl
 */

#include <stdio.h>
#include <stdlib.h>
#include <string.h>

#ifdef _WIN32
#  define WIN32_LEAN_AND_MEAN
#  include <windows.h>
typedef HMODULE DylibHandle;
static DylibHandle dylib_open(const char* p)  { return LoadLibraryA(p); }
static void*       dylib_sym(DylibHandle h, const char* s) { return (void*)GetProcAddress(h,s); }
static void        dylib_close(DylibHandle h) { FreeLibrary(h); }
#  define LIBNAME "OGEditorClient.dll"
#else
#  include <dlfcn.h>
typedef void* DylibHandle;
static DylibHandle dylib_open(const char* p)  { return dlopen(p, RTLD_LAZY|RTLD_LOCAL); }
static void*       dylib_sym(DylibHandle h, const char* s) { return dlsym(h, s); }
static void        dylib_close(DylibHandle h) { dlclose(h); }
#  define LIBNAME "libOGEditorClient.so"
#endif

#include "OGEditorClient.h"

typedef void*  (*fn_init_t)(const char*, const char*);
typedef void   (*fn_dispose_t)(void*);
typedef int    (*fn_get_assets_t)(void*, const char*, char*, int);
typedef int    (*fn_get_portals_t)(void*, const char*, char*, int);
typedef int    (*fn_append_portal_t)(void*, const char*, const char*);
typedef int    (*fn_get_quests_t)(void*, const char*, char*, int);
typedef int    (*fn_convert_t)(const char*, const char*, const char*, const char*, char*, int);

static DylibHandle       g_lib             = NULL;
static void*             g_handle          = NULL;
static fn_init_t         g_init            = NULL;
static fn_dispose_t      g_dispose         = NULL;
static fn_get_assets_t   g_get_assets      = NULL;
static fn_get_portals_t  g_get_portals     = NULL;
static fn_append_portal_t g_append_portal  = NULL;
static fn_get_quests_t   g_get_quests      = NULL;
static fn_convert_t      g_convert         = NULL;

static int sdk_load(void) {
    g_lib = dylib_open(LIBNAME);
    if (!g_lib) { fprintf(stderr, "ERROR: cannot load %s\n", LIBNAME); return 0; }

#define LOAD(fn, name) g_##fn = (fn_##fn##_t)dylib_sym(g_lib, name); \
    if (!g_##fn) { fprintf(stderr,"ERROR: missing %s\n", name); return 0; }

    LOAD(init,           "ogeditor_init")
    LOAD(dispose,        "ogeditor_dispose")
    LOAD(get_assets,     "ogeditor_get_assets_json")
    LOAD(get_portals,    "ogeditor_get_portals_json")
    LOAD(append_portal,  "ogeditor_append_portal")
    LOAD(get_quests,     "ogeditor_get_quests_json")
    LOAD(convert,        "ogeditor_convert_map")
#undef LOAD

    g_handle = g_init("http://localhost:5000", "");
    if (!g_handle) { fprintf(stderr, "ERROR: ogeditor_init returned NULL\n"); return 0; }
    return 1;
}

static void sdk_unload(void) {
    if (g_handle && g_dispose) g_dispose(g_handle);
    g_handle = NULL;
    if (g_lib) dylib_close(g_lib);
    g_lib = NULL;
}

static void usage(void) {
    puts("OASIS Mapster32 companion tool");
    puts("Usage:");
    puts("  oasis_m32_tool assets");
    puts("  oasis_m32_tool portals <map-file>");
    puts("  oasis_m32_tool add-portal <map-file> <dstGame> <dstMap>");
    puts("  oasis_m32_tool quests [gameId]");
    puts("  oasis_m32_tool convert <src> <srcFmt> <dst> <dstFmt>");
}

int main(int argc, char* argv[]) {
    if (argc < 2) { usage(); return 1; }

    if (!sdk_load()) return 2;

    char buf[131072];
    int rc = 0;

    if (strcmp(argv[1], "assets") == 0) {
        rc = g_get_assets(g_handle, "ODUKE3D", buf, sizeof(buf));
        if (rc == 0) puts(buf);
        else fprintf(stderr, "ERROR: get_assets_json returned %d\n", rc);

    } else if (strcmp(argv[1], "portals") == 0 && argc >= 3) {
        rc = g_get_portals(g_handle, argv[2], buf, sizeof(buf));
        if (rc == 0) puts(buf);
        else fprintf(stderr, "ERROR: get_portals_json returned %d\n", rc);

    } else if (strcmp(argv[1], "add-portal") == 0 && argc >= 5) {
        char portal_js[512];
        snprintf(portal_js, sizeof(portal_js),
            "{\"thingId\":1,\"x\":0.0,\"y\":0.0,"
            "\"destinationGame\":\"%s\","
            "\"destinationMap\":\"%s\","
            "\"destinationX\":0.0,\"destinationY\":0.0,\"destinationZ\":0.0}",
            argv[3], argv[4]);
        rc = g_append_portal(g_handle, argv[2], portal_js);
        if (rc == 0) printf("Portal appended to sidecar for %s\n", argv[2]);
        else fprintf(stderr, "ERROR: append_portal returned %d\n", rc);

    } else if (strcmp(argv[1], "quests") == 0) {
        const char* game = (argc >= 3) ? argv[2] : "";
        rc = g_get_quests(g_handle, game, buf, sizeof(buf));
        if (rc == 0) puts(buf);
        else fprintf(stderr, "ERROR: get_quests_json returned %d\n", rc);

    } else if (strcmp(argv[1], "convert") == 0 && argc >= 6) {
        rc = g_convert(argv[2], argv[3], argv[4], argv[5], buf, sizeof(buf));
        if (rc == 0) { printf("Conversion result:\n%s\n", buf); }
        else fprintf(stderr, "ERROR: convert_map returned %d\n", rc);

    } else {
        usage();
        sdk_unload();
        return 1;
    }

    sdk_unload();
    return (rc == 0) ? 0 : 3;
}
