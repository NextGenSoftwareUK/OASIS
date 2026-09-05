#include "OASISPanel.h"

#include "i18n.h"
#include "icommandsystem.h"
#include "imap.h"

#include <wx/sizer.h>
#include <wx/panel.h>
#include <wx/stattext.h>

#include <cstdio>

/* OGEditorClient.dll loaded at plugin init; function pointers set in oasis.cpp */
extern "C" {
#include "OGEditorClient.h"
}

/* These pointers are set by OASISModule::initialiseModule() in oasis.cpp */
extern void*  g_ogeditor_handle;
extern int  (*g_fn_get_assets)(void*, const char*, char*, int);
extern int  (*g_fn_get_portals)(void*, const char*, char*, int);
extern int  (*g_fn_append_portal)(void*, const char*, const char*);
extern int  (*g_fn_get_quests)(void*, const char*, char*, int);
extern int  (*g_fn_bind_objective)(void*, const char*, const char*);

namespace oasis {

namespace {
    const char* GAME_OPTIONS[] = {
        "*", "ODOOM3", "ODOOM3-BFG", "OQUAKE", "OQUAKE2", "OQUAKE2-RTX",
        "OQUAKE3", "ODUKE3D", "ODUKE3D-RT", "OWOLF3D", nullptr
    };
}

/* static */
void OASISPanel::ShowDialog(const cmd::ArgumentList& /*args*/) {
    OASISPanel* dlg = new OASISPanel(nullptr);
    dlg->ShowModal();
    dlg->Destroy();
}

OASISPanel::OASISPanel(wxWindow* parent)
    : wxutil::DialogBase(_("OASIS OGEngine"), parent)
{
    SetMinSize(wxSize(640, 480));

    wxBoxSizer* outerSizer = new wxBoxSizer(wxVERTICAL);

    _notebook = new wxNotebook(this, wxID_ANY);

    /* ── Tab 1: Asset Browser ─────────────────────────────────────────── */
    {
        wxPanel* tab = new wxPanel(_notebook);
        wxBoxSizer* s = new wxBoxSizer(wxVERTICAL);

        wxBoxSizer* row = new wxBoxSizer(wxHORIZONTAL);
        row->Add(new wxStaticText(tab, wxID_ANY, _("Game:")), 0, wxALIGN_CENTER_VERTICAL|wxRIGHT, 6);
        _gameFilter = new wxChoice(tab, wxID_ANY);
        for (int i = 0; GAME_OPTIONS[i]; ++i)
            _gameFilter->Append(GAME_OPTIONS[i]);
        _gameFilter->SetSelection(0);
        row->Add(_gameFilter, 0, wxRIGHT, 8);
        wxButton* refresh = new wxButton(tab, wxID_ANY, _("Refresh"));
        refresh->Bind(wxEVT_BUTTON, &OASISPanel::onRefreshAssets, this);
        row->Add(refresh, 0);
        s->Add(row, 0, wxEXPAND|wxALL, 8);

        _assetJson = new wxTextCtrl(tab, wxID_ANY, wxEmptyString,
            wxDefaultPosition, wxDefaultSize, wxTE_MULTILINE|wxTE_READONLY|wxHSCROLL);
        s->Add(_assetJson, 1, wxEXPAND|wxLEFT|wxRIGHT|wxBOTTOM, 8);

        tab->SetSizer(s);
        _notebook->AddPage(tab, _("Asset Browser"));
    }

    /* ── Tab 2: Portal Placer ─────────────────────────────────────────── */
    {
        wxPanel* tab = new wxPanel(_notebook);
        wxFlexGridSizer* grid = new wxFlexGridSizer(2, 8, 8);
        grid->AddGrowableCol(1, 1);

        auto addRow = [&](const wxString& label, wxTextCtrl*& ctrl, const wxString& hint) {
            grid->Add(new wxStaticText(tab, wxID_ANY, label), 0, wxALIGN_CENTER_VERTICAL);
            ctrl = new wxTextCtrl(tab, wxID_ANY, hint);
            grid->Add(ctrl, 1, wxEXPAND);
        };

        addRow(_("Destination Game:"), _dstGame,     _("e.g. ODOOM3"));
        addRow(_("Destination Map:"),  _dstMap,      _("e.g. game/maps/mars_city1.map"));
        addRow(_("Exit Name:"),        _dstExitName, _("targetname of oasis_portal_exit"));

        wxButton* place = new wxButton(tab, wxID_ANY, _("Append Portal to Sidecar"));
        place->Bind(wxEVT_BUTTON, &OASISPanel::onPlacePortal, this);

        wxBoxSizer* s = new wxBoxSizer(wxVERTICAL);
        s->Add(grid, 0, wxEXPAND|wxALL, 12);
        s->Add(place, 0, wxLEFT|wxBOTTOM, 12);
        tab->SetSizer(s);
        _notebook->AddPage(tab, _("Portal Placer"));
    }

    /* ── Tab 3: Quest Binder ──────────────────────────────────────────── */
    {
        wxPanel* tab = new wxPanel(_notebook);
        wxBoxSizer* s = new wxBoxSizer(wxVERTICAL);

        wxBoxSizer* row = new wxBoxSizer(wxHORIZONTAL);
        row->Add(new wxStaticText(tab, wxID_ANY, _("Game:")), 0, wxALIGN_CENTER_VERTICAL|wxRIGHT, 6);
        _questGameFilter = new wxChoice(tab, wxID_ANY);
        for (int i = 0; GAME_OPTIONS[i]; ++i)
            _questGameFilter->Append(GAME_OPTIONS[i]);
        _questGameFilter->SetSelection(0);
        row->Add(_questGameFilter, 0, wxRIGHT, 8);
        wxButton* refreshQ = new wxButton(tab, wxID_ANY, _("Refresh Quests"));
        refreshQ->Bind(wxEVT_BUTTON, &OASISPanel::onRefreshQuests, this);
        row->Add(refreshQ, 0);
        s->Add(row, 0, wxEXPAND|wxALL, 8);

        _questJson = new wxTextCtrl(tab, wxID_ANY, wxEmptyString,
            wxDefaultPosition, wxDefaultSize, wxTE_MULTILINE|wxTE_READONLY|wxHSCROLL);
        s->Add(_questJson, 1, wxEXPAND|wxLEFT|wxRIGHT|wxBOTTOM, 8);

        wxFlexGridSizer* grid = new wxFlexGridSizer(2, 6, 6);
        grid->AddGrowableCol(1, 1);

        auto addRow = [&](const wxString& label, wxTextCtrl*& ctrl) {
            grid->Add(new wxStaticText(tab, wxID_ANY, label), 0, wxALIGN_CENTER_VERTICAL);
            ctrl = new wxTextCtrl(tab, wxID_ANY);
            grid->Add(ctrl, 1, wxEXPAND);
        };
        addRow(_("Quest GUID:"),     _questId);
        addRow(_("Objective GUID:"), _objectiveId);
        s->Add(grid, 0, wxEXPAND|wxLEFT|wxRIGHT|wxBOTTOM, 8);

        wxButton* bind = new wxButton(tab, wxID_ANY, _("Bind to Selected Entity"));
        bind->Bind(wxEVT_BUTTON, &OASISPanel::onBindObjective, this);
        s->Add(bind, 0, wxLEFT|wxBOTTOM, 8);

        tab->SetSizer(s);
        _notebook->AddPage(tab, _("Quest Binder"));
    }

    outerSizer->Add(_notebook, 1, wxEXPAND|wxALL, 4);
    outerSizer->Add(CreateButtonSizer(wxCLOSE), 0, wxEXPAND|wxBOTTOM|wxRIGHT, 8);
    SetSizerAndFit(outerSizer);

    populateAssets();
    populateQuests();
}

void OASISPanel::populateAssets() {
    if (!g_ogeditor_handle || !g_fn_get_assets) {
        _assetJson->SetValue(_("SDK not initialised — is OGEditorClient.dll present?"));
        return;
    }
    char buf[131072];
    std::string game = _gameFilter->GetStringSelection().ToStdString();
    int rc = g_fn_get_assets(g_ogeditor_handle, game.c_str(), buf, sizeof(buf));
    _assetJson->SetValue(rc == 0 ? buf : wxString::Format("[error %d]", rc));
}

void OASISPanel::populateQuests() {
    if (!g_ogeditor_handle || !g_fn_get_quests) {
        _questJson->SetValue(_("SDK not initialised."));
        return;
    }
    char buf[65536];
    std::string game = _questGameFilter->GetStringSelection().ToStdString();
    int rc = g_fn_get_quests(g_ogeditor_handle, game.c_str(), buf, sizeof(buf));
    _questJson->SetValue(rc == 0 ? buf : wxString::Format("[error %d]", rc));
}

void OASISPanel::onRefreshAssets(wxCommandEvent& /*ev*/) { populateAssets(); }
void OASISPanel::onRefreshQuests(wxCommandEvent& /*ev*/) { populateQuests(); }

void OASISPanel::onPlacePortal(wxCommandEvent& /*ev*/) {
    if (!g_ogeditor_handle || !g_fn_append_portal) return;

    /* Obtain current map file path from DarkRadiant IMap */
    std::string mapPath;
    if (GlobalMapModule().isModified() || GlobalMapModule().getMapName().empty()) {
        wxMessageBox(_("Save the map first so a file path is available."),
                     _("OASIS Portal"), wxOK|wxICON_WARNING, this);
        return;
    }
    mapPath = GlobalMapModule().getMapName().ToStdString();

    char js[512];
    snprintf(js, sizeof(js),
        "{\"thingId\":1,\"x\":0.0,\"y\":0.0,"
        "\"destinationGame\":\"%s\","
        "\"destinationMap\":\"%s\","
        "\"exitName\":\"%s\","
        "\"destinationX\":0.0,\"destinationY\":0.0,\"destinationZ\":0.0}",
        _dstGame->GetValue().ToStdString().c_str(),
        _dstMap->GetValue().ToStdString().c_str(),
        _dstExitName->GetValue().ToStdString().c_str());

    int rc = g_fn_append_portal(g_ogeditor_handle, mapPath.c_str(), js);
    if (rc == 0)
        wxMessageBox(_("Portal appended to map sidecar."), _("OASIS Portal"), wxOK, this);
    else
        wxMessageBox(wxString::Format(_("SDK error %d"), rc), _("OASIS Portal"), wxOK|wxICON_ERROR, this);
}

void OASISPanel::onBindObjective(wxCommandEvent& /*ev*/) {
    if (!g_ogeditor_handle || !g_fn_bind_objective) return;

    std::string mapPath;
    if (!GlobalMapModule().getMapName().empty())
        mapPath = GlobalMapModule().getMapName().ToStdString();

    char js[512];
    snprintf(js, sizeof(js),
        "{\"objectiveId\":\"%s\","
        "\"mapPath\":\"%s\","
        "\"triggerType\":\"Entity\","
        "\"triggerId\":0}",
        _objectiveId->GetValue().ToStdString().c_str(),
        mapPath.c_str());

    int rc = g_fn_bind_objective(g_ogeditor_handle,
                                 _questId->GetValue().ToStdString().c_str(),
                                 js);
    if (rc == 0)
        wxMessageBox(_("Objective bound."), _("OASIS Quest"), wxOK, this);
    else
        wxMessageBox(wxString::Format(_("SDK error %d"), rc), _("OASIS Quest"), wxOK|wxICON_ERROR, this);
}

} // namespace oasis
