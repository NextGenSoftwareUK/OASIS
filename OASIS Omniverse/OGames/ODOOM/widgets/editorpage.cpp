/*
** editorpage.cpp
**
** Editor tab of launcher (ODOOM)
**
**---------------------------------------------------------------------------
**
** Copyright 2025-2026 NextGen World Ltd
**
** SPDX-License-Identifier: GPL-3.0-or-later
**
**---------------------------------------------------------------------------
**
*/

#include "editorpage.h"
#include "launcherwindow.h"
#include "zstring.h"

EditorPage::EditorPage(LauncherWindow* launcher, const FStartupSelectionInfo& info) : Widget(nullptr), Launcher(launcher)
{
	Text = new TextEdit(this);
	Text->SetText("Editor\n\nThis tab can be used for map editing, script tools, or other ODOOM editor features.");
	Text->SetReadOnly(true);
}

void EditorPage::SetValues(FStartupSelectionInfo& info) const
{
}

void EditorPage::UpdateLanguage()
{
}

void EditorPage::OnGeometryChanged()
{
	double w = GetWidth();
	double h = GetHeight();
	Text->SetFrameGeometry(0.0, 0.0, w, h);
	Launcher->UpdatePlayButton();
}
