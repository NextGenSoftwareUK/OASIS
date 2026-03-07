/*
** editorpage.h
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

#pragma once

class LauncherWindow;
class TextEdit;
struct FStartupSelectionInfo;

class EditorPage : public Widget
{
public:
	EditorPage(LauncherWindow* launcher, const FStartupSelectionInfo& info);
	void UpdateLanguage();
	void SetValues(FStartupSelectionInfo& info) const;

private:
	void OnGeometryChanged() override;

	LauncherWindow* Launcher = nullptr;
	TextEdit* Text = nullptr;
};
