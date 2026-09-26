#pragma once

#include "wxutil/dialog/DialogBase.h"
#include "wxutil/dataview/TreeView.h"
#include "wxutil/TreeModel.h"

#include <wx/panel.h>
#include <wx/notebook.h>
#include <wx/textctrl.h>
#include <wx/button.h>
#include <wx/choice.h>
#include <wx/stattext.h>

#include <string>

namespace oasis {

/**
 * Three-tab dialog providing OASIS OGEngine integration within DarkRadiant:
 *
 *  Tab 1 – Asset Browser : lists cross-game assets from the STAR API catalog
 *  Tab 2 – Portal Placer : creates/edits oasis_portal_enter entities in the map
 *  Tab 3 – Quest Binder  : lists quests and binds objectives to map triggers
 *
 * All SDK calls go through OGEditorClient.dll (loaded at plugin init time).
 */
class OASISPanel : public wxutil::DialogBase
{
public:
    static void ShowDialog(const cmd::ArgumentList& args);

private:
    OASISPanel(wxWindow* parent);

    void populateAssets();
    void populateQuests();

    void onRefreshAssets(wxCommandEvent& ev);
    void onRefreshQuests(wxCommandEvent& ev);
    void onPlacePortal(wxCommandEvent& ev);
    void onBindObjective(wxCommandEvent& ev);

    wxNotebook*   _notebook;

    // Asset Browser tab
    wxTextCtrl*   _assetJson;
    wxChoice*     _gameFilter;

    // Portal Placer tab
    wxTextCtrl*   _dstGame;
    wxTextCtrl*   _dstMap;
    wxTextCtrl*   _dstExitName;

    // Quest Binder tab
    wxTextCtrl*   _questJson;
    wxChoice*     _questGameFilter;
    wxTextCtrl*   _questId;
    wxTextCtrl*   _objectiveId;
};

} // namespace oasis
