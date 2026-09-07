# Removed & Moved Projects — Register

_Last updated: 2026-09-04_

A record of projects that were deleted from, or relocated within, the OASIS repo —
so their history stays findable and stale solution references can be understood
rather than re-added by mistake.

Compiled while getting `The OASIS - NoTests.sln` to build with zero errors. Several
solution entries pointed at paths that no longer existed; each one traced back to
either a deliberate removal or a move.

---

## Deliberately removed — do not restore

### OPORTAL (5 projects, 233 files)

Deleted in **`18831d7f2`** — _"remove redundant projects"_.

| Project | Files | `.cs` |
|---|---|---|
| `ONODE/NextGenSoftware.OASIS.API.ONODE.OPORTAL` | 225 | 8 |
| `ONODE/NextGenSoftware.OASIS.API.ONODE.OPORTAL.UnitTests` | 4 | 2 |
| `ONODE/NextGenSoftware.OASIS.API.ONODE.OPORTAL.UnitTests/...OPORTAL.Tests` | — | — |
| `ONODE/NextGenSoftware.OASIS.API.ONODE.OPORTAL.IntegrationTests` | 2 | 1 |
| `ONODE/NextGenSoftware.OASIS.API.ONODE.OPORTAL.TestHarness` | 2 | 1 |

**What it was:** an ASP.NET Core Razor Pages host scaffold. Only 12 of the 233 files
were C#, and those were template output — `Program.cs`, `Startup.cs`,
`App_Start/WebApiConfig.cs`, two copies of `ValuesController`, and the
`Index`/`Error`/`Privacy` page models. The other ~215 files were static web assets
(`ClientApp/oasis-pure-js/dist/...`, `wwwroot/...`) — a bundled build of an older
frontend.

**Why it stays removed:** superseded by the private **OPORTAL-JS** and
**OPORTAL-React** repos, which are the live portal implementations. The deleted
project was a host shell around a stale copy of that frontend.

**To inspect or recover:**

```bash
git show 18831d7f2^:ONODE/NextGenSoftware.OASIS.API.ONODE.OPORTAL/Startup.cs
git checkout 18831d7f2^ -- ONODE/NextGenSoftware.OASIS.API.ONODE.OPORTAL
```

---

## Moved — solution references needed repointing

| Project | Was | Now |
|---|---|---|
| `STARAPIClient` | `OASIS Omniverse/STARAPIClient` | renamed **OGEngineClient**, submodule at `OASIS Omniverse/OGEngineClient` (`7b88c0fdf`) |
| `NextGenSoftware.OASIS.ONODE.Client` | `ONODE/…` | `ONODE/ONODEManager/…` (submodule) |
| `NextGenSoftware.OASIS.ONODE.Manager` | `ONODE/…` | `ONODE/ONODEManager/…` (submodule) |
| `NextGenSoftware.OASIS.ONODE.Service` | `ONODE/…` | `ONODE/ONODEManager/…` (submodule) |
| `NextGenSoftware.OASIS.MCP.Server` | `MCP/…` | `WEB6/NextGenSoftware.OASIS.MCP.Server` (submodule) |

All five are present in the solutions again at their correct paths.

---

## Recovered from history

| Project | Deleted in | Restored to |
|---|---|---|
| `NextGenSoftware.OASIS.API.ONODE.Core.TestHarness` | `18831d7f2` | `ONODE/NextGenSoftware.OASIS.API.ONODE.Core.TestHarness` |
| `DemoQuestSeed` | `9c5de5798` | `OASIS Omniverse/DemoQuestSeed` |

`DemoQuestSeed` was being moved into the OGEngineClient submodule when
`9c5de5798` ("replace OGEngineClient and ONODE Manager/Client/Service with private
submodules") removed it from the parent — but it never landed in that submodule, so
it was recovered to the parent repo outside the submodule boundary.

`ONODE.Core.TestHarness` was restored without its `OASIS_DNA.json`, which is
deliberately never committed.

---

## Kept in the solution but made buildable

**OAPP Console DNA templates** — `STAR ODK/STAR OAPP DNA Templates/…Console.DLL` and
`…Console.NuGet`.

Their `Program.cs` is STAR scaffolding containing unsubstituted tokens
(`using {OAPPNAMESPACE};`, `{OAPPNAME}`, `[[MYCUSTOMTAG]]`), so it is not valid C#
until STAR generates an OAPP from it. It was already marked `<Compile Remove>`, but
the projects were still `OutputType=Exe`, so the compiler demanded an entry point it
had been told to ignore and failed with `CS5001`. They are now `OutputType=Library`.

Their `DNA\*.json` seeding target was also hooked to `BeforeTargets="Build"`, which
fires after content target paths are resolved — a clean checkout therefore failed
with `MSB3030`. It now runs at `AssignTargetPaths` and declares its `Content` items
inside the target, after the files are created.

---

## ArweaveOASIS — three copies resolved (2026-09-06)

The provider existed in three folders. Diffed before deciding:

| Copy | Content | Outcome |
|---|---|---|
| `Providers/Network/` | 5 files, 2158 lines, 80 members — the split partial-class version with real transaction signing (`BuildTransactionAsync`, `SignTransaction`, `PostTransactionAsync`, `DeepHash`) | **kept, moved to `Providers/Storage/`** |
| `Providers/Storage/` | 1 file, 1403 lines, 64 members — the pre-split monolith | deleted |
| `Providers/Blockchain/` | 0 `.cs` files — empty shell with only a csproj | deleted |

The split version is a strict superset. All 8 members that existed only in the
monolith have equivalents in it:

| Monolith | Split version |
|---|---|
| `DeactivateProvider`/`Async` | `DeActivateProvider`/`Async` — the monolith had the wrong casing and would not have satisfied the base class |
| `ToBase64Url` | `Base64UrlEncode` |
| `UploadJsonAsync` | `UploadJsonToArweaveAsync` |
| `GraphQlAsync` | GraphQL moved into `ArweaveService.cs` |
| `FindLatestTxIdAsync`, `FindAllTxIdsForTypeAsync` | `QueryByTagsAsync` |
| `EnsureActivatedAsync` | `Init` |

Arweave is permanent storage and the docs count it under Storage, so `Providers/Storage`
is the correct home. The move also fixed a live problem: the three Arweave test projects
already sat in `Providers/Storage/TestProjects/` and referenced `..\..\ArweaveOASIS`,
so they had been testing the superseded monolith while the solution built the Network
copy. They now resolve to the live implementation.

`OASISBootLoader` and `ONODE.Core` both referenced the old Network path and were
repointed.

---

## Related

- `FILE_SPLIT_LOST_METHODS.md` — methods deleted by partial-class split refactors
- `FILE_SPLIT_LOST_METHODS_SOURCE.md` — full recovered source for each
