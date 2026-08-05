#include "OASISPanel.h"

#include "i18n.h"
#include "imodule.h"
#include "ui/imenumanager.h"
#include "itextstream.h"

#include <cstring>
#include <cstdio>

#ifdef _WIN32
#  define WIN32_LEAN_AND_MEAN
#  include <windows.h>
typedef HMODULE DylibHandle;
static DylibHandle dylib_open(const char* p) { return LoadLibraryA(p); }
static void*  dylib_sym(DylibHandle h, const char* s) { return (void*)GetProcAddress(h,s); }
static void   dylib_close(DylibHandle h) { FreeLibrary(h); }
#  define OGEDITOR_LIBNAME "OGEditorClient.dll"
#else
#  include <dlfcn.h>
typedef void* DylibHandle;
static DylibHandle dylib_open(const char* p) { return dlopen(p, RTLD_LAZY|RTLD_LOCAL); }
static void*  dylib_sym(DylibHandle h, const char* s) { return dlsym(h, s); }
static void   dylib_close(DylibHandle h) { dlclose(h); }
#  define OGEDITOR_LIBNAME "libOGEditorClient.so"
#endif

/* Exported to OASISPanel.cpp */
void*  g_ogeditor_handle   = nullptr;
int  (*g_fn_get_assets)(void*, const char*, char*, int)         = nullptr;
int  (*g_fn_get_portals)(void*, const char*, char*, int)        = nullptr;
int  (*g_fn_append_portal)(void*, const char*, const char*)     = nullptr;
int  (*g_fn_get_quests)(void*, const char*, char*, int)         = nullptr;
int  (*g_fn_bind_objective)(void*, const char*, const char*)    = nullptr;

static DylibHandle g_lib     = nullptr;

typedef void* (*fn_init_t)(const char*, const char*);
typedef void  (*fn_dispose_t)(void*);

static fn_dispose_t g_fn_dispose = nullptr;

static bool loadOGEditorSDK() {
    g_lib = dylib_open(OGEDITOR_LIBNAME);
    if (!g_lib) {
        rError() << "[OASIS] Cannot load " << OGEDITOR_LIBNAME << std::endl;
        return false;
    }

    auto load = [&](const char* sym) -> void* {
        void* fn = dylib_sym(g_lib, sym);
        if (!fn) rError() << "[OASIS] Missing export: " << sym << std::endl;
        return fn;
    };

    auto init_fn = (fn_init_t)load("ogeditor_init");
    g_fn_dispose        = (fn_dispose_t)load("ogeditor_dispose");
    g_fn_get_assets     = (decltype(g_fn_get_assets))load("ogeditor_get_assets_json");
    g_fn_get_portals    = (decltype(g_fn_get_portals))load("ogeditor_get_portals_json");
    g_fn_append_portal  = (decltype(g_fn_append_portal))load("ogeditor_append_portal");
    g_fn_get_quests     = (decltype(g_fn_get_quests))load("ogeditor_get_quests_json");
    g_fn_bind_objective = (decltype(g_fn_bind_objective))load("ogeditor_bind_objective");

    if (!init_fn || !g_fn_dispose || !g_fn_get_assets || !g_fn_get_portals ||
        !g_fn_append_portal || !g_fn_get_quests || !g_fn_bind_objective) {
        dylib_close(g_lib); g_lib = nullptr; return false;
    }

    g_ogeditor_handle = init_fn("http://localhost:5000", "");
    if (!g_ogeditor_handle) {
        rError() << "[OASIS] ogeditor_init returned NULL\n";
        dylib_close(g_lib); g_lib = nullptr; return false;
    }

    rMessage() << "[OASIS] OGEditorClient loaded OK.\n";
    return true;
}

/**
 * DarkRadiant module that registers the OASIS OGEngine panel.
 */
class OASISModule : public RegisterableModule
{
public:
    std::string getName() const override {
        static std::string _name("OASISOGEngine");
        return _name;
    }

    StringSet getDependencies() const override {
        static StringSet _deps;
        if (_deps.empty()) {
            _deps.insert(MODULE_MENUMANAGER);
            _deps.insert(MODULE_COMMANDSYSTEM);
        }
        return _deps;
    }

    void initialiseModule(const IApplicationContext& /*ctx*/) override {
        loadOGEditorSDK();

        GlobalCommandSystem().addCommand(
            "OASISOGEnginePanel",
            oasis::OASISPanel::ShowDialog
        );

        ui::menu::IMenuManager& mm = GlobalMenuManager();
        mm.add("main/map",
               "OASISOGEnginePanel",
               ui::menu::ItemType::Item,
               _("OASIS OGEngine..."),
               "icon_oasis.png",
               "OASISOGEnginePanel");
    }

    void shutdownModule() override {
        rMessage() << "OASISModule shutting down.\n";
        if (g_ogeditor_handle && g_fn_dispose) {
            g_fn_dispose(g_ogeditor_handle);
            g_ogeditor_handle = nullptr;
        }
        if (g_lib) {
            dylib_close(g_lib);
            g_lib = nullptr;
        }
    }
};

using OASISModulePtr = std::shared_ptr<OASISModule>;

extern "C" void DARKRADIANT_DLLEXPORT RegisterModule(IModuleRegistry& registry)
{
    module::performDefaultInitialisation(registry);
    registry.registerModule(OASISModulePtr(new OASISModule));
}
