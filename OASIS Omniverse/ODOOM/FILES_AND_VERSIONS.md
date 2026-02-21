# ODOOM – File list and version docs

## Why there are multiple “version” files

There are **not** two independent version docs. There is **one source** and **several generated outputs**:

| Role | File | Description |
|------|------|-------------|
| **Single source (edit only this)** | **odoom_version.txt** | Line 1 = version (e.g. `1.0`), line 2 = build number (e.g. `1`). This is the only file you edit for version. |
| **Generator** | **generate_odoom_version.ps1** | Run by BUILD ODOOM.bat; reads odoom_version.txt and writes the files below. |
| **Generated (do not edit)** | **odoom_version_generated.h** | C header for the build (`ODOOM_VERSION`, `ODOOM_BUILD`, `ODOOM_VERSION_STR`). |
| **Generated** | **version_display.txt** | Human string e.g. `1.0 (Build 1)`; used by build/branding scripts. |
| **Generated** | **about.txt** | Full launcher “About” text; version line comes from odoom_version.txt. |

So: **odoom_version.txt** = source of truth; the rest are outputs. No redundancy—each generated file has a different use (C code, scripts, launcher UI).

---

## Full file list – ODOOM

### Version and build (source + generated)

| File | Description |
|------|-------------|
| **odoom_version.txt** | **Edit only this** for version and build. |
| **generate_odoom_version.ps1** | Generates odoom_version_generated.h, version_display.txt, about.txt. |
| **odoom_version_generated.h** | Generated C header; included by UZDoom build. |
| **version_display.txt** | Generated display string for scripts/branding. |
| **about.txt** | Generated launcher About text (release blurb + credits); also patched into UZDoom’s about during branding. |

### Build and run

| File | Description |
|------|-------------|
| **BUILD ODOOM.bat** | Main build: copy integration, run branding, CMake, package to `build\` (ODOOM.exe, Editor folder, etc.). |
| **BUILD & RUN ODOOM.bat** | Build if needed, then launch ODOOM.exe. |
| **apply_odoom_branding.ps1** | Patches UZDoom source: version.h, startscreen, status bar, about, Editor button, launcher button bar. |

### STAR integration (source)

| File | Description |
|------|-------------|
| **uzdoom_star_integration.cpp** | STAR API init, key pickup, door check, console commands; uses star_sync for async auth. |
| **uzdoom_star_integration.h** | Declarations for above. |
| **star_sync.c** / **star_sync.h** | Generic async auth/inventory layer (from STARAPIClient); add star_sync.c to UZDoom build. |
| **odoom_branding.h** | ODOOM name/version macros for C (used with OASIS_STAR_API). |

### Launcher Editor (optional UI)

| File | Description |
|------|-------------|
| **widgets/editorpage.cpp** | Editor “page” widget (optional; Editor is currently a **button** in the button bar, not a tab). |
| **widgets/editorpage.h** | Header for EditorPage. |

### Documentation

| File | Description |
|------|-------------|
| **README.md** | Quick start, doc index, version pointer, cross-game keys. |
| **WINDOWS_INTEGRATION.md** | Full Windows setup, build, STAR API, troubleshooting. |
| **CREDITS_AND_LICENSE.md** | UZDoom credit and GPL-3.0 obligations. |
| **RELEASE_NOTES.md** | Version history and release notes (human-maintained). |
| **CONTRIBUTING.md** | How to contribute. |
| **LICENSE** | Short license summary + GPL-3.0. |
| **LAUNCHER_EDITOR_TAB.md** | Where launcher code lives (UZDoom) and how the Editor **button** is added. |
| **FILES_AND_VERSIONS.md** | This file. |

### Assets and binaries (optional / build output)

| File | Description |
|------|-------------|
| **oasis_banner.png** | Optional; copied over launcher banners if present. |
| **soft_oal.dll** | Optional; copied to build if present. |
| **star_api.dll** | Often copied from Doom folder for local run; build also gets it from there. |
| **build/** | Build output (ODOOM.exe, Editor\, DLLs, etc.); can be regenerated. |

---

## Possible redundancy (ODOOM)

- **about.txt** vs **RELEASE_NOTES.md**: about.txt is **generated** launcher text (one ODOOM + one UZDoom blurb). RELEASE_NOTES.md is the **human** release history. The first-release paragraph overlaps in content but not purpose—keep both; about.txt is for the launcher UI, RELEASE_NOTES.md for the repo.
- **widgets/editorpage.***: Used only if you add an Editor **tab**. The current setup uses an Editor **button** (patched in launcherbuttonbar/launcherwindow). You can keep these for a future “Editor dialog” or remove if you never plan a tab.

No other files are redundant; version files are source vs generated, and docs serve different roles.
