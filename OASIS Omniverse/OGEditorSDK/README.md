# OGEditorSDK

**OASIS Omniverse Editor Plugin SDK** — a pure .NET Standard 2.0 library that provides the shared editor integration layer for all OGEngine-compatible map and game editors.

Plug into any map editor: **UDB (Ultimate Doom Builder)**, **TrenchBroom**, **NetRadiant**, **DarkRadiant**, **Mapster32**, or any web-based editor via REST API.

---

## Features

- **OGAsset Catalog** — unified asset registry across all supported editors
- **Map Sidecar** — OASIS metadata layer attached to any map file format
- **STAR API Client** — direct access to the STAR (Self-Transforming Avatar Reality) API for quest, portal, and mission binding from within an editor
- **Entity Conversion Tables** — cross-game entity mapping (Doom ↔ Quake ↔ Half-Life ↔ OGWorld)
- **Portal/Quest Binding** — bind inter-game portals and quests to map geometry at edit time

> **OGEditorSDK** is the creator-side mirror of **STARAPIClient**. STARAPIClient is used by games at runtime; OGEditorSDK is used by editors at design time.

---

## Installation

```bash
dotnet add package OGEditorSDK
```

Or via NuGet Package Manager: search `OGEditorSDK`.

---

## Quick Start

```csharp
using OGEditorSDK;

// Load the STAR API client for editor use
var starClient = new OGStarApiClient("https://api.web4.oasisomniverse.one");

// Attach an OASIS map sidecar to your map
var sidecar = new OGMapSidecar(mapPath: "mymap.wad");
sidecar.BindPortal(sourceEntity: "teleport_01", destinationWorld: "OurWorld");

// Resolve entity conversions between game formats
var mappings = OGEntityMappings.DoomToQuake("POSSESSED");
```

---

## Target Editors

| Editor | Format | Status |
|--------|--------|--------|
| Ultimate Doom Builder (UDB) | Doom/Hexen UDMF | Supported |
| TrenchBroom | Quake .map | Supported |
| NetRadiant / DarkRadiant | Quake III .map | Supported |
| Mapster32 | Duke Nukem Build | Supported |
| Web Editors | REST API | Supported |

---

## Part of the OASIS Omniverse

OGEditorSDK is part of the broader [OASIS API](https://oasisomniverse.one) ecosystem — a HOT-Swappable provider architecture connecting 55+ blockchains, databases, and networks. Write once, deploy anywhere across the entire OASIS Omniverse.

- Ecosystem: [oasisomniverse.one](https://oasisomniverse.one)
- REST API: [api.web4.oasisomniverse.one](https://api.web4.oasisomniverse.one)
- NuGet org: [nuget.org/profiles/OASISOmniverse](https://www.nuget.org/profiles/OASISOmniverse)
- GitHub: [github.com/NextGenSoftwareUK](https://github.com/NextGenSoftwareUK)

---

© NextGen World Ltd 2024 — MIT License
