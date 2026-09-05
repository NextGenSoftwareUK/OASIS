/*
 * oasis_nr_plugin.c — OASIS OGEngine plugin for NetRadiant
 *
 * Implements the classic GtkRadiant/NetRadiant plugin ABI:
 *   - QERPluginTable entry point
 *   - Three commands with full GTK3 dialogs:
 *       "OASIS Asset Browser"  – lists cross-game assets from STAR API
 *       "OASIS Portal Placer" – appends an OASIS portal to the map sidecar
 *       "OASIS Quest Binder"  – binds a quest objective to a map trigger
 *
 * The plugin loads OGEditorClient.dll / libOGEditorClient.so at startup and calls
 * through the C ABI declared in OGEditorClient.h.  No .NET knowledge required.
 *
 * Build with NetRadiant SDK; requires GTK3 headers (-I/usr/include/gtk-3.0 etc.)
 * Copy the resulting .so / .dll into NetRadiant/plugins/
 */

#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <gtk/gtk.h>

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

/* ── NetRadiant plugin ABI typedefs ────────────────────────────────────────── */

typedef struct _QERPluginTable {
    int   m_nSize;
    void  (*m_pfnQERPlug_Init)(void* hApp, void* pMainWidget);
    void  (*m_pfnQERPlug_Dispatch)(const char* p, float* vMin, float* vMax, int bSingleBrush);
    int   (*m_pfnQERPlug_GetFuncTable)(void* pFuncTable);
} QERPluginTable;

/* ── Runtime-loaded OGEditorClient function pointers ───────────────────────── */

static DylibHandle     g_lib      = NULL;
static OGEditorHandle  g_handle   = NULL;

typedef OGEditorHandle (*fn_init_t)(const char*, const char*);
typedef void           (*fn_dispose_t)(OGEditorHandle);
typedef int            (*fn_get_assets_t)(OGEditorHandle, const char*, char*, int);
typedef int            (*fn_get_portals_t)(OGEditorHandle, const char*, char*, int);
typedef int            (*fn_append_portal_t)(OGEditorHandle, const char*, const char*);
typedef int            (*fn_get_quests_t)(OGEditorHandle, const char*, char*, int);
typedef int            (*fn_bind_objective_t)(OGEditorHandle, const char*, const char*);

static fn_init_t           g_fn_init           = NULL;
static fn_dispose_t        g_fn_dispose        = NULL;
static fn_get_assets_t     g_fn_get_assets     = NULL;
static fn_get_portals_t    g_fn_get_portals    = NULL;
static fn_append_portal_t  g_fn_append_portal  = NULL;
static fn_get_quests_t     g_fn_get_quests     = NULL;
static fn_bind_objective_t g_fn_bind_objective = NULL;

/* Main editor window — used as dialog parent */
static GtkWidget* g_main_window = NULL;

static int load_ogeditor(void) {
#ifdef _WIN32
    const char* libname = "OGEditorClient.dll";
#else
    const char* libname = "libOGEditorClient.so";
#endif
    g_lib = dylib_open(libname);
    if (!g_lib) { fprintf(stderr, "[OASIS] Could not load %s\n", libname); return 0; }

#define BIND(fn, sym) \
    g_fn_##fn = (fn_##fn##_t)dylib_sym(g_lib, sym); \
    if (!g_fn_##fn) { fprintf(stderr, "[OASIS] Missing export: %s\n", sym); return 0; }

    BIND(init,           "ogeditor_init")
    BIND(dispose,        "ogeditor_dispose")
    BIND(get_assets,     "ogeditor_get_assets_json")
    BIND(get_portals,    "ogeditor_get_portals_json")
    BIND(append_portal,  "ogeditor_append_portal")
    BIND(get_quests,     "ogeditor_get_quests_json")
    BIND(bind_objective, "ogeditor_bind_objective")
#undef BIND

    g_handle = g_fn_init("http://localhost:5000", "");
    if (!g_handle) { fprintf(stderr, "[OASIS] ogeditor_init returned NULL\n"); return 0; }

    fprintf(stderr, "[OASIS] OGEditorClient loaded OK.\n");
    return 1;
}

/* ── Shared: game list ──────────────────────────────────────────────────────── */

static const char* const GAME_IDS[] = {
    "*", "ODOOM", "ODOOM3", "ODOOM3-BFG", "OQUAKE", "OQUAKE2",
    "OQUAKE2-RTX", "OQUAKE3", "ODUKE3D", "ODUKE3D-RT", "OWOLF3D",
    "OHERETIC", "OHEXEN", "OSHADOWWARRIOR", "OMORROWIND", "OMINECRAFT", NULL
};

/* ── Shared: map file-chooser callback ─────────────────────────────────────── */

static void on_browse_map(GtkButton* btn, gpointer user_data) {
    (void)btn;
    GtkWidget *chooser = gtk_file_chooser_dialog_new(
        "Select Map File", GTK_WINDOW(g_main_window),
        GTK_FILE_CHOOSER_ACTION_OPEN,
        "_Cancel", GTK_RESPONSE_CANCEL,
        "_Open",   GTK_RESPONSE_ACCEPT,
        NULL);
    GtkFileFilter *f = gtk_file_filter_new();
    gtk_file_filter_set_name(f, "Map files (*.map)");
    gtk_file_filter_add_pattern(f, "*.map");
    gtk_file_chooser_add_filter(GTK_FILE_CHOOSER(chooser), f);
    if (gtk_dialog_run(GTK_DIALOG(chooser)) == GTK_RESPONSE_ACCEPT) {
        gchar *path = gtk_file_chooser_get_filename(GTK_FILE_CHOOSER(chooser));
        gtk_entry_set_text(GTK_ENTRY(user_data), path);
        g_free(path);
    }
    gtk_widget_destroy(chooser);
}

/* ── Asset Browser ──────────────────────────────────────────────────────────── */

typedef struct { GtkWidget *text_view; GtkWidget *combo; } AssetCtx;

static void do_fetch_assets(AssetCtx *ctx) {
    gchar *game = gtk_combo_box_text_get_active_text(GTK_COMBO_BOX_TEXT(ctx->combo));
    char   buf[131072];
    int    rc = g_fn_get_assets(g_handle, game ? game : "*", buf, sizeof(buf));
    g_free(game);
    GtkTextBuffer *tb = gtk_text_view_get_buffer(GTK_TEXT_VIEW(ctx->text_view));
    if (rc == 0) {
        gtk_text_buffer_set_text(tb, buf, -1);
    } else {
        char err[64]; snprintf(err, sizeof(err), "[SDK error %d]", rc);
        gtk_text_buffer_set_text(tb, err, -1);
    }
}

static void on_refresh_assets(GtkButton *btn, gpointer data) {
    (void)btn;
    do_fetch_assets((AssetCtx*)data);
}

static void cmd_asset_browser(void) {
    if (!g_handle) {
        GtkWidget *d = gtk_message_dialog_new(GTK_WINDOW(g_main_window),
            GTK_DIALOG_MODAL, GTK_MESSAGE_ERROR, GTK_BUTTONS_OK,
            "OASIS SDK not initialised — is OGEditorClient.dll present?");
        gtk_dialog_run(GTK_DIALOG(d)); gtk_widget_destroy(d); return;
    }

    GtkWidget *dlg = gtk_dialog_new_with_buttons(
        "OASIS Asset Browser", GTK_WINDOW(g_main_window),
        GTK_DIALOG_DESTROY_WITH_PARENT,
        "_Close", GTK_RESPONSE_CLOSE, NULL);
    gtk_window_set_default_size(GTK_WINDOW(dlg), 700, 520);
    GtkWidget *box = gtk_dialog_get_content_area(GTK_DIALOG(dlg));
    gtk_container_set_border_width(GTK_CONTAINER(box), 8);

    /* Game filter + Refresh row */
    GtkWidget *hrow = gtk_box_new(GTK_ORIENTATION_HORIZONTAL, 6);
    gtk_box_pack_start(GTK_BOX(hrow), gtk_label_new("Game:"), FALSE, FALSE, 0);
    GtkWidget *combo = gtk_combo_box_text_new();
    for (int i = 0; GAME_IDS[i]; ++i)
        gtk_combo_box_text_append_text(GTK_COMBO_BOX_TEXT(combo), GAME_IDS[i]);
    gtk_combo_box_set_active(GTK_COMBO_BOX(combo), 0);
    gtk_box_pack_start(GTK_BOX(hrow), combo, FALSE, FALSE, 0);
    GtkWidget *rbtn = gtk_button_new_with_label("Refresh");
    gtk_box_pack_start(GTK_BOX(hrow), rbtn, FALSE, FALSE, 0);
    gtk_box_pack_start(GTK_BOX(box), hrow, FALSE, FALSE, 4);

    /* Scrolled JSON text view */
    GtkWidget *scroll = gtk_scrolled_window_new(NULL, NULL);
    gtk_scrolled_window_set_policy(GTK_SCROLLED_WINDOW(scroll),
                                    GTK_POLICY_AUTOMATIC, GTK_POLICY_AUTOMATIC);
    GtkWidget *tv = gtk_text_view_new();
    gtk_text_view_set_editable(GTK_TEXT_VIEW(tv), FALSE);
    gtk_text_view_set_monospace(GTK_TEXT_VIEW(tv), TRUE);
    gtk_container_add(GTK_CONTAINER(scroll), tv);
    gtk_box_pack_start(GTK_BOX(box), scroll, TRUE, TRUE, 4);

    AssetCtx ctx = { tv, combo };
    g_signal_connect(rbtn, "clicked", G_CALLBACK(on_refresh_assets), &ctx);
    do_fetch_assets(&ctx);

    gtk_widget_show_all(dlg);
    gtk_dialog_run(GTK_DIALOG(dlg));
    gtk_widget_destroy(dlg);
}

/* ── Portal Placer ──────────────────────────────────────────────────────────── */

static void cmd_portal_placer(void) {
    if (!g_handle) {
        GtkWidget *d = gtk_message_dialog_new(GTK_WINDOW(g_main_window),
            GTK_DIALOG_MODAL, GTK_MESSAGE_ERROR, GTK_BUTTONS_OK,
            "OASIS SDK not initialised — is OGEditorClient.dll present?");
        gtk_dialog_run(GTK_DIALOG(d)); gtk_widget_destroy(d); return;
    }

    GtkWidget *dlg = gtk_dialog_new_with_buttons(
        "OASIS Portal Placer", GTK_WINDOW(g_main_window),
        GTK_DIALOG_MODAL | GTK_DIALOG_DESTROY_WITH_PARENT,
        "_Cancel", GTK_RESPONSE_CANCEL,
        "_Append Portal", GTK_RESPONSE_OK, NULL);
    gtk_window_set_default_size(GTK_WINDOW(dlg), 500, 310);
    GtkWidget *box = gtk_dialog_get_content_area(GTK_DIALOG(dlg));
    gtk_container_set_border_width(GTK_CONTAINER(box), 12);

    GtkWidget *grid = gtk_grid_new();
    gtk_grid_set_row_spacing(GTK_GRID(grid), 8);
    gtk_grid_set_column_spacing(GTK_GRID(grid), 8);
    gtk_box_pack_start(GTK_BOX(box), grid, TRUE, TRUE, 0);

    /* Row 0: Current map path + Browse */
    GtkWidget *ml = gtk_label_new("Current Map:");
    gtk_widget_set_halign(ml, GTK_ALIGN_END);
    gtk_grid_attach(GTK_GRID(grid), ml, 0, 0, 1, 1);
    GtkWidget *map_entry = gtk_entry_new();
    gtk_entry_set_placeholder_text(GTK_ENTRY(map_entry), "path/to/current.map");
    gtk_widget_set_hexpand(map_entry, TRUE);
    gtk_grid_attach(GTK_GRID(grid), map_entry, 1, 0, 1, 1);
    GtkWidget *browse_btn = gtk_button_new_with_label("Browse…");
    gtk_grid_attach(GTK_GRID(grid), browse_btn, 2, 0, 1, 1);
    g_signal_connect(browse_btn, "clicked", G_CALLBACK(on_browse_map), map_entry);

    /* Row 1: Destination Game */
    GtkWidget *dgl = gtk_label_new("Destination Game:");
    gtk_widget_set_halign(dgl, GTK_ALIGN_END);
    gtk_grid_attach(GTK_GRID(grid), dgl, 0, 1, 1, 1);
    GtkWidget *dst_game = gtk_combo_box_text_new_with_entry();
    for (int i = 1; GAME_IDS[i]; ++i)   /* skip "*" */
        gtk_combo_box_text_append_text(GTK_COMBO_BOX_TEXT(dst_game), GAME_IDS[i]);
    gtk_combo_box_set_active(GTK_COMBO_BOX(dst_game), 0);
    gtk_widget_set_hexpand(dst_game, TRUE);
    gtk_grid_attach(GTK_GRID(grid), dst_game, 1, 1, 2, 1);

    /* Row 2: Destination Map */
    GtkWidget *dml = gtk_label_new("Destination Map:");
    gtk_widget_set_halign(dml, GTK_ALIGN_END);
    gtk_grid_attach(GTK_GRID(grid), dml, 0, 2, 1, 1);
    GtkWidget *dst_map = gtk_entry_new();
    gtk_entry_set_placeholder_text(GTK_ENTRY(dst_map), "e.g. maps/mars_city1.map");
    gtk_widget_set_hexpand(dst_map, TRUE);
    gtk_grid_attach(GTK_GRID(grid), dst_map, 1, 2, 2, 1);

    /* Row 3: Exit Name */
    GtkWidget *enl = gtk_label_new("Exit Name:");
    gtk_widget_set_halign(enl, GTK_ALIGN_END);
    gtk_grid_attach(GTK_GRID(grid), enl, 0, 3, 1, 1);
    GtkWidget *exit_name = gtk_entry_new();
    gtk_entry_set_placeholder_text(GTK_ENTRY(exit_name),
                                   "targetname of oasis_portal_exit entity");
    gtk_widget_set_hexpand(exit_name, TRUE);
    gtk_grid_attach(GTK_GRID(grid), exit_name, 1, 3, 2, 1);

    gtk_widget_show_all(dlg);

    if (gtk_dialog_run(GTK_DIALOG(dlg)) == GTK_RESPONSE_OK) {
        const char *map_path = gtk_entry_get_text(GTK_ENTRY(map_entry));
        gchar *game_val      = gtk_combo_box_text_get_active_text(
                                   GTK_COMBO_BOX_TEXT(dst_game));
        const char *dst_map_val = gtk_entry_get_text(GTK_ENTRY(dst_map));
        const char *exit_val    = gtk_entry_get_text(GTK_ENTRY(exit_name));

        if (!map_path || !*map_path) {
            GtkWidget *w = gtk_message_dialog_new(GTK_WINDOW(g_main_window),
                GTK_DIALOG_MODAL, GTK_MESSAGE_WARNING, GTK_BUTTONS_OK,
                "Map path is required — save the map first or use Browse.");
            gtk_dialog_run(GTK_DIALOG(w)); gtk_widget_destroy(w);
        } else {
            char js[512];
            snprintf(js, sizeof(js),
                "{\"thingId\":1,\"x\":0.0,\"y\":0.0,"
                "\"destinationGame\":\"%s\","
                "\"destinationMap\":\"%s\","
                "\"exitName\":\"%s\","
                "\"destinationX\":0.0,\"destinationY\":0.0,\"destinationZ\":0.0}",
                game_val ? game_val : "", dst_map_val, exit_val);

            int rc = g_fn_append_portal(g_handle, map_path, js);
            GtkWidget *r = gtk_message_dialog_new(GTK_WINDOW(g_main_window),
                GTK_DIALOG_MODAL,
                rc == 0 ? GTK_MESSAGE_INFO : GTK_MESSAGE_ERROR, GTK_BUTTONS_OK,
                rc == 0 ? "Portal appended to map sidecar."
                        : "SDK error %d — check OGEditorClient log.", rc);
            gtk_dialog_run(GTK_DIALOG(r)); gtk_widget_destroy(r);
        }
        g_free(game_val);
    }
    gtk_widget_destroy(dlg);
}

/* ── Quest Binder ───────────────────────────────────────────────────────────── */

typedef struct { GtkWidget *text_view; GtkWidget *game_combo; } QuestCtx;

static void do_fetch_quests(QuestCtx *ctx) {
    gchar *game = gtk_combo_box_text_get_active_text(GTK_COMBO_BOX_TEXT(ctx->game_combo));
    char   buf[65536];
    int    rc = g_fn_get_quests(g_handle, game ? game : "", buf, sizeof(buf));
    g_free(game);
    GtkTextBuffer *tb = gtk_text_view_get_buffer(GTK_TEXT_VIEW(ctx->text_view));
    if (rc == 0) {
        gtk_text_buffer_set_text(tb, buf, -1);
    } else {
        char err[64]; snprintf(err, sizeof(err), "[SDK error %d]", rc);
        gtk_text_buffer_set_text(tb, err, -1);
    }
}

static void on_refresh_quests(GtkButton *btn, gpointer data) {
    (void)btn;
    do_fetch_quests((QuestCtx*)data);
}

typedef struct {
    GtkWidget *quest_entry;
    GtkWidget *obj_entry;
    GtkWidget *map_entry;
} BindCtx;

static void on_bind_objective(GtkButton *btn, gpointer data) {
    (void)btn;
    BindCtx    *ctx      = (BindCtx*)data;
    const char *quest_id = gtk_entry_get_text(GTK_ENTRY(ctx->quest_entry));
    const char *obj_id   = gtk_entry_get_text(GTK_ENTRY(ctx->obj_entry));
    const char *map_path = gtk_entry_get_text(GTK_ENTRY(ctx->map_entry));

    if (!quest_id || !*quest_id || !obj_id || !*obj_id) {
        GtkWidget *w = gtk_message_dialog_new(GTK_WINDOW(g_main_window),
            GTK_DIALOG_MODAL, GTK_MESSAGE_WARNING, GTK_BUTTONS_OK,
            "Quest GUID and Objective GUID are both required.");
        gtk_dialog_run(GTK_DIALOG(w)); gtk_widget_destroy(w);
        return;
    }

    char js[512];
    snprintf(js, sizeof(js),
        "{\"objectiveId\":\"%s\","
        "\"mapPath\":\"%s\","
        "\"triggerType\":\"Entity\","
        "\"triggerId\":0}",
        obj_id, map_path ? map_path : "");

    int rc = g_fn_bind_objective(g_handle, quest_id, js);
    GtkWidget *r = gtk_message_dialog_new(GTK_WINDOW(g_main_window),
        GTK_DIALOG_MODAL,
        rc == 0 ? GTK_MESSAGE_INFO : GTK_MESSAGE_ERROR, GTK_BUTTONS_OK,
        rc == 0 ? "Objective bound successfully."
                : "SDK error %d — check OGEditorClient log.", rc);
    gtk_dialog_run(GTK_DIALOG(r)); gtk_widget_destroy(r);
}

static void cmd_quest_binder(void) {
    if (!g_handle) {
        GtkWidget *d = gtk_message_dialog_new(GTK_WINDOW(g_main_window),
            GTK_DIALOG_MODAL, GTK_MESSAGE_ERROR, GTK_BUTTONS_OK,
            "OASIS SDK not initialised — is OGEditorClient.dll present?");
        gtk_dialog_run(GTK_DIALOG(d)); gtk_widget_destroy(d); return;
    }

    GtkWidget *dlg = gtk_dialog_new_with_buttons(
        "OASIS Quest Binder", GTK_WINDOW(g_main_window),
        GTK_DIALOG_DESTROY_WITH_PARENT,
        "_Close", GTK_RESPONSE_CLOSE, NULL);
    gtk_window_set_default_size(GTK_WINDOW(dlg), 700, 580);
    GtkWidget *box = gtk_dialog_get_content_area(GTK_DIALOG(dlg));
    gtk_container_set_border_width(GTK_CONTAINER(box), 8);

    /* Game filter + Refresh row */
    GtkWidget *hrow = gtk_box_new(GTK_ORIENTATION_HORIZONTAL, 6);
    gtk_box_pack_start(GTK_BOX(hrow), gtk_label_new("Game:"), FALSE, FALSE, 0);
    GtkWidget *game_combo = gtk_combo_box_text_new();
    for (int i = 0; GAME_IDS[i]; ++i)
        gtk_combo_box_text_append_text(GTK_COMBO_BOX_TEXT(game_combo), GAME_IDS[i]);
    gtk_combo_box_set_active(GTK_COMBO_BOX(game_combo), 0);
    gtk_box_pack_start(GTK_BOX(hrow), game_combo, FALSE, FALSE, 0);
    GtkWidget *rbtn = gtk_button_new_with_label("Refresh Quests");
    gtk_box_pack_start(GTK_BOX(hrow), rbtn, FALSE, FALSE, 0);
    gtk_box_pack_start(GTK_BOX(box), hrow, FALSE, FALSE, 4);

    /* Scrolled quest JSON display */
    GtkWidget *scroll = gtk_scrolled_window_new(NULL, NULL);
    gtk_scrolled_window_set_policy(GTK_SCROLLED_WINDOW(scroll),
                                    GTK_POLICY_AUTOMATIC, GTK_POLICY_AUTOMATIC);
    gtk_widget_set_size_request(scroll, -1, 260);
    GtkWidget *tv = gtk_text_view_new();
    gtk_text_view_set_editable(GTK_TEXT_VIEW(tv), FALSE);
    gtk_text_view_set_monospace(GTK_TEXT_VIEW(tv), TRUE);
    gtk_container_add(GTK_CONTAINER(scroll), tv);
    gtk_box_pack_start(GTK_BOX(box), scroll, TRUE, TRUE, 4);

    QuestCtx qctx = { tv, game_combo };
    g_signal_connect(rbtn, "clicked", G_CALLBACK(on_refresh_quests), &qctx);
    do_fetch_quests(&qctx);

    /* Separator */
    gtk_box_pack_start(GTK_BOX(box),
        gtk_separator_new(GTK_ORIENTATION_HORIZONTAL), FALSE, FALSE, 4);

    /* Bind fields grid */
    GtkWidget *grid = gtk_grid_new();
    gtk_grid_set_row_spacing(GTK_GRID(grid), 6);
    gtk_grid_set_column_spacing(GTK_GRID(grid), 8);
    gtk_box_pack_start(GTK_BOX(box), grid, FALSE, FALSE, 4);

    GtkWidget *ql = gtk_label_new("Quest GUID:");
    gtk_widget_set_halign(ql, GTK_ALIGN_END);
    gtk_grid_attach(GTK_GRID(grid), ql, 0, 0, 1, 1);
    GtkWidget *quest_entry = gtk_entry_new();
    gtk_entry_set_placeholder_text(GTK_ENTRY(quest_entry), "quest GUID from catalog above");
    gtk_widget_set_hexpand(quest_entry, TRUE);
    gtk_grid_attach(GTK_GRID(grid), quest_entry, 1, 0, 2, 1);

    GtkWidget *ol = gtk_label_new("Objective GUID:");
    gtk_widget_set_halign(ol, GTK_ALIGN_END);
    gtk_grid_attach(GTK_GRID(grid), ol, 0, 1, 1, 1);
    GtkWidget *obj_entry = gtk_entry_new();
    gtk_entry_set_placeholder_text(GTK_ENTRY(obj_entry), "objective GUID");
    gtk_widget_set_hexpand(obj_entry, TRUE);
    gtk_grid_attach(GTK_GRID(grid), obj_entry, 1, 1, 2, 1);

    GtkWidget *mpl = gtk_label_new("Map Path:");
    gtk_widget_set_halign(mpl, GTK_ALIGN_END);
    gtk_grid_attach(GTK_GRID(grid), mpl, 0, 2, 1, 1);
    GtkWidget *map_entry = gtk_entry_new();
    gtk_entry_set_placeholder_text(GTK_ENTRY(map_entry), "path/to/current.map");
    gtk_widget_set_hexpand(map_entry, TRUE);
    gtk_grid_attach(GTK_GRID(grid), map_entry, 1, 2, 1, 1);
    GtkWidget *browse2 = gtk_button_new_with_label("Browse…");
    gtk_grid_attach(GTK_GRID(grid), browse2, 2, 2, 1, 1);
    g_signal_connect(browse2, "clicked", G_CALLBACK(on_browse_map), map_entry);

    GtkWidget *bind_btn = gtk_button_new_with_mnemonic("_Bind to Selected Entity");
    gtk_box_pack_start(GTK_BOX(box), bind_btn, FALSE, FALSE, 4);

    BindCtx bctx = { quest_entry, obj_entry, map_entry };
    g_signal_connect(bind_btn, "clicked", G_CALLBACK(on_bind_objective), &bctx);

    gtk_widget_show_all(dlg);
    gtk_dialog_run(GTK_DIALOG(dlg));
    gtk_widget_destroy(dlg);
}

/* ── NetRadiant plugin ABI entry points ─────────────────────────────────────── */

static const char* PLUGIN_NAME = "OASIS OGEngine";
static const char* PLUGIN_CMDS = "OASIS Asset Browser;OASIS Portal Placer;OASIS Quest Binder";

OASIS_DLLEXPORT const char* QERPlug_GetName(void)        { return PLUGIN_NAME; }
OASIS_DLLEXPORT const char* QERPlug_GetCommandList(void) { return PLUGIN_CMDS; }

OASIS_DLLEXPORT void QERPlug_Init(void* hApp, void* pMainWidget) {
    (void)hApp;
    g_main_window = GTK_WIDGET(pMainWidget);
    load_ogeditor();
}

OASIS_DLLEXPORT void QERPlug_Dispatch(const char* p,
                                       float* vMin, float* vMax,
                                       int bSingleBrush) {
    (void)vMin; (void)vMax; (void)bSingleBrush;
    if      (strcmp(p, "OASIS Asset Browser") == 0) cmd_asset_browser();
    else if (strcmp(p, "OASIS Portal Placer") == 0) cmd_portal_placer();
    else if (strcmp(p, "OASIS Quest Binder")  == 0) cmd_quest_binder();
}

OASIS_DLLEXPORT void QERPlug_Shutdown(void) {
    if (g_handle && g_fn_dispose) g_fn_dispose(g_handle);
    g_handle = NULL;
    if (g_lib) dylib_close(g_lib);
    g_lib = NULL;
}
