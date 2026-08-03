# OGEditor Plugin Implementation Guide

Per-editor guide for integrating `OGEditorClient.dll` and implementing the OASIS menu, OASISStarPanel, and OASISPortalPanel in each satellite editor.

UDB already has full implementations of all of these. This guide is for **OQuakeEditor (TrenchBroom)**, **OQuake3Editor (NetRadiant)**, and **ODOOM3Editor (DarkRadiant)**.

---

## 1. OGEditorClient.dll — The Shared C ABI

All OASIS editor intelligence lives in UDB's OGEditorSDK, compiled to a native DLL via .NET NativeAOT. Every satellite editor calls this DLL rather than duplicating any logic.

**Header:** `UltimateDoomBuilder/Source/OGEditorSDK/Native/OGEditorClient.h`

**Build output:** `UltimateDoomBuilder/build/OGEditorClient.dll` (Windows) / `libOGEditorClient.so` (Linux) / `libOGEditorClient.dylib` (macOS)

### Loading the DLL

**C++ (TrenchBroom, DarkRadiant):**
```cpp
#include "OGEditorClient.h"

// Windows
HMODULE ogeditor_lib = LoadLibraryA("OGEditorClient.dll");
if (!ogeditor_lib) {
    // Try %APPDATA%\OASIS\bin\OGEditorClient.dll
    // Degrade gracefully — OASIS features unavailable
    return;
}
// Bind function pointers
auto ogeditor_get_thing_type =
    (int(*)(const char*, const char*))
    GetProcAddress(ogeditor_lib, "ogeditor_get_thing_type");
```

**C (NetRadiant):**
```c
#include "OGEditorClient.h"
#include <dlfcn.h>

void* ogeditor_lib = dlopen("libOGEditorClient.so", RTLD_LAZY);
if (!ogeditor_lib) {
    // Try ~/.oasis/bin/libOGEditorClient.so
    return;
}
int (*ogeditor_get_thing_type)(const char*, const char*) =
    dlsym(ogeditor_lib, "ogeditor_get_thing_type");
```

### Full API surface

```c
// ── Asset catalog ──────────────────────────────────────────────────────────

// Returns OASIS thing type for the given game and entity classname.
// Returns -1 if not found.
int ogeditor_get_thing_type(const char* game_id, const char* classname);

// Fills out_buf with up to buf_size OGAsset structs for the given game.
// Returns total count (may exceed buf_size if truncated).
int ogeditor_get_assets_for_game(const char* game_id,
                                  OGAsset*    out_buf,
                                  int         buf_size);

// Returns all assets across all games.
int ogeditor_get_all_assets(OGAsset* out_buf, int buf_size);

// ── Entity mapping ─────────────────────────────────────────────────────────

// Returns OASIS thing type for the given engine classname, searching all games.
int ogeditor_classname_to_thing_type(const char* classname);

// Fills out_classname with the engine classname for a given game+thing_type.
int ogeditor_thing_type_to_classname(const char* game_id, int thing_type,
                                      char* out_classname, int out_size);

// ── Portal pair management ─────────────────────────────────────────────────

// Register a portal pair. Returns 0 on success, -1 on error.
int ogeditor_register_portal_pair(
    const char* src_game,   const char* src_map,
    const char* exit_name,
    const char* dst_game,   const char* dst_map,
    float exit_x, float exit_y, float exit_z, float exit_angle);

// Get known portal exits for a given destination game+map.
// Used to populate the oasis_exit_name drop-down in editors.
int ogeditor_get_exits_for_map(const char* game_id, const char* map_name,
                                OGPortalExit* out_buf, int buf_size);

// ── Map sidecar ───────────────────────────────────────────────────────────

// Load the .oasis.json sidecar for the given map file path.
// Returns 0 on success, 1 if sidecar not found (not an error), -1 on parse error.
int ogeditor_sidecar_load(const char* map_file_path);

// Write/update the sidecar. Should be called after every map save.
int ogeditor_sidecar_save(const char* map_file_path);

// ── STAR Web API ───────────────────────────────────────────────────────────

// Authenticate with the STAR API. jwt may be NULL to attempt anonymous access.
// Returns 0 on success, -1 on failure. Stores token internally.
int ogeditor_star_connect(const char* api_url, const char* jwt);

// Returns 1 if connected, 0 otherwise.
int ogeditor_star_is_connected(void);

// Register the current map's portal topology with the STAR API.
// map_json is the JSON string of an OGMapSidecar.
int ogeditor_star_register_map(const char* game_id, const char* map_name,
                                const char* map_json);

// Get cross-game inventory for the given avatar.
int ogeditor_star_get_inventory(const char* avatar_id,
                                 OGItem* out_buf, int buf_size);

// ── OGEngine ──────────────────────────────────────────────────────────────

// Connect to a running OGEngine instance.
int ogeditor_ogengine_connect(const char* ogengine_url);

// Fill out with current OGEngine status.
int ogeditor_ogengine_get_status(OGEngineStatus* out);

// ── Map conversion ────────────────────────────────────────────────────────

// Convert a map file from one game format to another.
// out_path must be a writable path for the converted file.
int ogeditor_convert_map(const char* src_path,  const char* src_game,
                          const char* out_path,  const char* dst_game);
```

### Struct definitions (from OGEditorClient.h)

```c
typedef struct {
    int         thing_type;
    char        game_id[16];
    char        name[64];
    char        classname[64];
    char        category[32];
} OGAsset;

typedef struct {
    char        targetname[64];
    char        game_id[16];
    char        map_name[64];
    float       x, y, z;
    float       angle;
} OGPortalExit;

typedef struct {
    char        name[64];
    char        category[32];
    int         thing_type;
    char        game_id[16];
} OGItem;

typedef struct {
    int         connected;          // 1 = connected, 0 = disconnected
    char        ogengine_url[128];
    char        avatar_id[64];
    char        avatar_name[64];
    int         xp;
    char        active_game[16];
    char        active_map[64];
} OGEngineStatus;
```

---

## 2. OQuakeEditor (TrenchBroom) — Implementation

### Plugin location

```
OQuakeEditor/app/TrenchBroom/src/oasis/
├── OASISPlugin.h
├── OASISPlugin.cpp          ← loads OGEditorClient.dll, initialises
├── OASISMenu.h
├── OASISMenu.cpp            ← OASIS top-level menu
├── OASISStarPanel.h
├── OASISStarPanel.cpp       ← dockable panel (QDockWidget)
├── OASISPortalPanel.h
├── OASISPortalPanel.cpp     ← portal pair panel (QDockWidget)
└── OGEditorAPI.h            ← thin C++ wrapper around OGEditorClient.h
```

### Hooking in the menu

TrenchBroom's main window is `TrenchBroomApp.cpp` → `MapFrame`. Add the OASIS menu after "Help":

```cpp
// In MapFrame::createMenus() or equivalent:
auto* oasisMenu = menuBar()->addMenu(tr("OASIS"));

// Open Map In submenu
auto* openInMenu = oasisMenu->addMenu(tr("Open Map In"));
connect(openInMenu->addAction(tr("UltimateDoomBuilder")),
        &QAction::triggered, this, &MapFrame::onOASISOpenInUDB);
connect(openInMenu->addAction(tr("OQuake3Editor (NetRadiant)")),
        &QAction::triggered, this, &MapFrame::onOASISOpenInNetRadiant);
connect(openInMenu->addAction(tr("ODOOM3Editor (DarkRadiant)")),
        &QAction::triggered, this, &MapFrame::onOASISOpenInDarkRadiant);

oasisMenu->addSeparator();
connect(oasisMenu->addAction(tr("Connect to OGEngine...")),
        &QAction::triggered, this, &MapFrame::onOASISConnectOGEngine);
connect(oasisMenu->addAction(tr("STAR API Status")),
        &QAction::triggered, this, &MapFrame::onOASISStarStatus);
// etc.
```

### Opening in another editor

```cpp
void MapFrame::onOASISOpenInUDB() {
    // Save current map first
    saveDocument();

    // Read editor path from %APPDATA%\OASIS\editor_config.json
    QString udbPath = OASISPlugin::editorPath("udb");
    if (udbPath.isEmpty()) {
        QMessageBox::warning(this, "OASIS",
            "UltimateDoomBuilder not found. Check OASIS\\editor_config.json.");
        return;
    }
    // Convert map to Doom format first
    QString convertedPath;
    if (currentGame() != "odoom") {
        convertedPath = convertMapToGame("odoom");
    } else {
        convertedPath = currentMapPath();
    }
    QProcess::startDetached(udbPath, { convertedPath });
}
```

### OASISStarPanel (QDockWidget)

```cpp
class OASISStarPanel : public QDockWidget {
    Q_OBJECT
public:
    explicit OASISStarPanel(QWidget* parent);

private:
    void refresh();            // polls ogeditor_ogengine_get_status() every 5s
    void onAssetSelected();    // places entity at cursor on double-click

    QLabel*      m_statusLabel;
    QComboBox*   m_gameFilter;
    QComboBox*   m_categoryFilter;
    QLineEdit*   m_searchBox;
    QListWidget* m_assetList;
    QListWidget* m_inventoryList;
    QListWidget* m_portalList;
    QTimer*      m_pollTimer;
};
```

---

## 3. OQuake3Editor (NetRadiant) — Implementation

### Plugin location

```
OQuake3Editor/contrib/oasis/
├── oasis_plugin.c           ← module entry point, loads libOGEditorClient.so
├── oasis_menu.c             ← OASIS menu (GtkMenu)
├── oasis_panel.c            ← OASIS panel (GtkWidget inside a GtkWindow)
└── oasis_api.h              ← thin C wrapper around OGEditorClient.h
```

### Module registration

NetRadiant plugins export `QERPluginInfo`, `QERPluginInit`, and optionally `QERPluginDispatch`. In `oasis_plugin.c`:

```c
#include "iplugin.h"
#include "oasis_api.h"

const char* QERPluginInfo(void) {
    return "OASIS Omniverse Integration v1.0";
}

int QERPluginInit(void* hApp, void* pMainWidget) {
    oasis_api_load();   // dlopen libOGEditorClient.so
    oasis_menu_create((GtkWidget*)pMainWidget);
    oasis_panel_create((GtkWidget*)pMainWidget);
    return 0;
}
```

### OASIS menu (GTK)

```c
void oasis_menu_create(GtkWidget* main_window) {
    GtkWidget* menu_bar = /* get main menu bar */;
    GtkWidget* oasis_item = gtk_menu_item_new_with_label("OASIS");
    GtkWidget* oasis_menu = gtk_menu_new();
    gtk_menu_item_set_submenu(GTK_MENU_ITEM(oasis_item), oasis_menu);

    // Open In submenu
    GtkWidget* open_in = gtk_menu_item_new_with_label("Open Map In");
    GtkWidget* open_in_menu = gtk_menu_new();
    gtk_menu_item_set_submenu(GTK_MENU_ITEM(open_in), open_in_menu);

    GtkWidget* open_in_udb = gtk_menu_item_new_with_label("UltimateDoomBuilder");
    g_signal_connect(open_in_udb, "activate",
                     G_CALLBACK(on_open_in_udb), NULL);
    gtk_menu_shell_append(GTK_MENU_SHELL(open_in_menu), open_in_udb);
    gtk_menu_shell_append(GTK_MENU_SHELL(oasis_menu), open_in);

    // ... other items
    gtk_menu_shell_append(GTK_MENU_SHELL(menu_bar), oasis_item);
    gtk_widget_show_all(oasis_item);
}

static void on_open_in_udb(GtkMenuItem* item, gpointer data) {
    const char* udb_path = oasis_editor_config_get("udb");
    const char* map_path = /* get current map path from NetRadiant */;
    char cmd[1024];
    snprintf(cmd, sizeof(cmd), "\"%s\" \"%s\" &", udb_path, map_path);
    system(cmd);
}
```

---

## 4. ODOOM3Editor (DarkRadiant) — Implementation

### Plugin location

```
ODOOM3Editor/plugins/oasis/
├── OASISPlugin.h
├── OASISPlugin.cpp          ← IPlugin implementation
├── OASISMenu.h
├── OASISMenu.cpp            ← IMenuManager integration
├── OASISPanel.h
├── OASISPanel.cpp           ← wxPanel in a dockable window
└── OGEditorAPI.h            ← thin C++ wrapper
```

### Plugin registration

DarkRadiant uses a module registry. `OASISPlugin.cpp`:

```cpp
#include "imodule.h"
#include "iuimanager.h"
#include "imenumanager.h"
#include "OASISMenu.h"
#include "OASISPanel.h"
#include "OGEditorAPI.h"

class OASISPlugin : public RegisterableModule {
public:
    const std::string& getName() const override {
        static std::string name = "OASISPlugin";
        return name;
    }

    const StringSet& getDependencies() const override {
        static StringSet deps = { MODULE_UIMANAGER, MODULE_MENUMANAGER };
        return deps;
    }

    void initialiseModule(const IApplicationContext& ctx) override {
        OGEditorAPI::load();      // LoadLibrary / dlopen
        OASISMenu::install();     // add OASIS menu to main menu bar
        OASISPanel::install();    // add dockable panel
    }
};

// Module registration (DarkRadiant plugin entry point)
extern "C" void DARKRADIANT_DLLEXPORT RegisterModule(IModuleRegistry& registry) {
    registry.registerModule(std::make_shared<OASISPlugin>());
}
```

### OASIS menu (wxWidgets via DarkRadiant IMenuManager)

```cpp
void OASISMenu::install() {
    auto& menuMgr = GlobalMenuManager();

    menuMgr.add("main", "oasis",
        ui::menuFolder, "OASIS", "", "");

    menuMgr.add("main/oasis", "openIn",
        ui::menuFolder, "Open Map In", "", "");

    menuMgr.add("main/oasis/openIn", "openInUDB",
        ui::menuItem, "UltimateDoomBuilder", "",
        "oasis_open_in_udb");

    menuMgr.add("main/oasis/openIn", "openInTrenchBroom",
        ui::menuItem, "OQuakeEditor (TrenchBroom)", "",
        "oasis_open_in_trenchbroom");

    menuMgr.add("main/oasis", "separator1",
        ui::menuSeparator, "", "", "");

    menuMgr.add("main/oasis", "connectOGEngine",
        ui::menuItem, "Connect to OGEngine...", "",
        "oasis_connect_ogengine");

    // Register command handlers
    GlobalCommandSystem().addCommand("oasis_open_in_udb",
        std::bind(&OASISMenu::onOpenInUDB));
    GlobalCommandSystem().addCommand("oasis_connect_ogengine",
        std::bind(&OASISMenu::onConnectOGEngine));
}
```

---

## 5. UltimateDoomBuilder — Already Implemented

UDB already has the full OASIS implementation. For reference, the existing entry points are:

| Class | Location | Notes |
|-------|----------|-------|
| `OASISStarPanel` | `Plugins/UDBScript/Controls/OASISStarPanel.cs` | Dockable OASIS asset browser |
| `OASISPortalPanel` | `Plugins/UDBScript/Controls/OASISPortalPanel.cs` | Portal pair UI |
| `OASISMapConverter` | `Plugins/UDBScript/OASISMapConverter.cs` | Cross-format entity conversion |
| `OASISMapSidecar` | `Plugins/UDBScript/OASISMapSidecar.cs` | Sidecar read/write |
| `OGAssetCatalog` | `OGEditorSDK/OGAssetCatalog.cs` | Canonical asset list |
| `OGStarApiClient` | `OGEditorSDK/OGStarApiClient.cs` | STAR Web API HTTP client |
| `OGMapSidecar` | `OGEditorSDK/OGMapSidecar.cs` | Sidecar model |
| `NativeExports` | `OGEditorSDK/Native/NativeExports.cs` | NativeAOT → OGEditorClient.dll |

The OASIS menu in UDB should be added via the existing plugin menu infrastructure at `Source/Plugins/UDBScript/` using the `IMenusForm` or `BuilderPlug` menu registration.

---

## 6. editor_config.json Discovery and Writing

Every editor reads the shared config on startup and writes its own path into it on first launch:

```
Windows: %APPDATA%\OASIS\editor_config.json
Linux:   ~/.oasis/editor_config.json
macOS:   ~/Library/Application Support/OASIS/editor_config.json
```

**On first launch** (if file does not exist or this editor's key is missing):
```cpp
// TrenchBroom, on startup
void OASISPlugin::registerSelf() {
    auto config = loadEditorConfig();
    config["editors"]["oquake_editor"] = QApplication::applicationFilePath();
    config["OGEditorClient_path"] = findOGEditorApi();
    saveEditorConfig(config);
}
```

**`findOGEditorApi()`** searches in order:
1. Same directory as this editor's executable
2. `%APPDATA%\OASIS\bin\` (Windows) / `~/.oasis/bin/`
3. `config["OGEditorClient_path"]` if already set
4. Returns empty string if not found → OASIS features degrade gracefully

---

## 7. Testing the Integration

Once `OGEditorClient.dll` is loaded in a satellite editor:

```
1. Open OQuakeEditor, load any Quake2 map
2. OASIS menu should appear in the menu bar
3. OASIS → STAR API Status should show "Connected" if OGEngine is running
4. OASIS → Browse OASIS Assets should open the panel
5. Place an oasis_portal_enter brush in the map
6. In the entity properties, oasis_exit_name drop-down should show
   known exit points from the STAR API portal registry
7. Save the map — sidecar oasis_{mapname}.json should appear next to the .map file
8. OASIS → Register Map with OASIS should push the topology to the STAR API
9. OASIS → Open Map In → UltimateDoomBuilder should launch UDB with the map
```
