# Launcher code location and Editor button

The game launcher (splash window with Play, Settings, About, Release notes tabs) is **inside the UZDoom source**, not in the OASIS repo.

## Where the launcher code lives (UZDoom clone)

In your UZDoom clone (e.g. `C:\Source\UZDoom`):

| Path | Purpose |
|------|--------|
| **src/widgets/launcherwindow.cpp** | Main launcher window; creates `TabWidget` and adds tabs (Play, Settings, Multiplayer, About, Release Notes). |
| **src/widgets/launcherwindow.h** | Declares `LauncherWindow`, tab pointers, `Start()`, `Exit()`, `OnEditorButtonClicked()`. |
| **src/widgets/launcherbuttonbar.cpp**, **.h** | Bottom button bar: **Play** (left), **Editor** (centre), **Exit** (right). |
| **src/widgets/aboutpage.cpp**, **releasepage.cpp**, etc. | Tab content. |

The **Release notes** tab content comes from **releasepage.cpp**. The **About** tab reads **about.txt** from the game data.

## Editor button (ODOOM)

ODOOM adds an **Editor** button in the **centre** of the button bar, between **Play Game** and **Exit**. This is applied by **patch_uzdoom_engine.ps1** (step 8):

- **launcherbuttonbar.h** – `EditorButton` member and `OnEditorButtonClicked()`.
- **launcherbuttonbar.cpp** – Create and lay out `EditorButton`, wire click to `GetLauncher()->OnEditorButtonClicked()`.
- **launcherwindow.h** – `void OnEditorButtonClicked();`
- **launcherwindow.cpp** – `OnEditorButtonClicked()` implementation (placeholder; you can add a dialog or launch an editor here).

No new source files are added; only the existing launcher files are patched. The **widgets/editorpage.*** files in ODOOM are optional (e.g. for a future Editor dialog when the button is clicked).
