# API versioning: WEB4 to WEB10

Every API has one version, set once in `OASISBootLoader.cs`, and the same number must appear everywhere it is written down.
`Scripts/api_version_sync.py` keeps them identical and CI fails when they drift.

| API | Constant (source of truth) | Release history | Postman collection (`OPORTAL-JS/postman`) |
|---|---|---|---|
| WEB4 | `OASISAPIVersion` (also `OASISRuntimeVersion`, always equal) | `ONODE/.../OASIS API RELEASE HISTORY.md` | `OASIS WEB4 API vX.Y.Z` |
| WEB5 | `STARAPIVersion` | `STAR ODK/.../STAR API RELEASE HISTORY.md` and `Docs/API/WEB5-STAR-API-RELEASE-HISTORY.md` | `STAR WEB5 API vX.Y.Z` |
| WEB6 | `WEB6APIVersion` | `WEB6/.../WEB6 API RELEASE HISTORY.md` and `Docs/API/WEB6-AI-API-RELEASE-HISTORY.md` | `OASIS WEB6 API vX.Y.Z` |
| WEB7-WEB10 | `WEB7APIVersion` ... `WEB10APIVersion` | `WEBn/.../WEBn API RELEASE HISTORY.md` | `OASIS WEBn API vX.Y.Z` |

Also kept in step: the sentence listing the versions in `Docs/Devs/GLOBAL_RELEASE_AUTOMATION.md`, and the Postman links and
labels in OPORTAL-JS (`assets/components/modals/dev-portal.html`, `source/main/index.html`, `source/dist/index.html`). Swagger titles, the version
endpoint and the startup log read the constants at runtime, so there is nothing else to edit. `OASISRuntimeVersion` (the OASIS Runtime ships
with the WEB4 API) is always set equal to `OASISAPIVersion`. `STARRuntimeVersion` has its own number and is not tied to WEB5.

## Bumping by hand

```bash
py -3 Scripts/api_version_sync.py --apply WEB4 5.4.0
```

Sets the constant, adds a history stub (replace its `TODO`), then runs `--sync`, which updates the runtime, the docs sentence and, when
`../OPORTAL-JS` (or `OASIS_OPORTAL_DIR`) exists, renames the Postman file and fixes its title and every portal link. Commit the OASIS and
OPORTAL-JS changes together.

## Checking

```bash
py -3 Scripts/api_version_sync.py --check
```

Lists every place that disagrees and exits non-zero. It runs in CI (`.github/workflows/api-version-sync.yml`) and in
`Scripts/Test-OASISGlobalRelease.ps1`. Where OPORTAL-JS or a submodule is not checked out, those places are reported as "not checked".

## The global release

`Scripts/Invoke-OASISGlobalRelease.ps1` writes the new constant and history entry when you advance WEB4, WEB5 or WEB6, then calls
`api_version_sync.py --sync`, so the runtime, docs sentence and (if present) OPORTAL-JS follow automatically. A runtime release in the same run
takes the new WEB4 version as its tag. The release does **not** open a pull request in OPORTAL-JS; run the same command locally in a checkout
that has `../OPORTAL-JS`, or run `--sync` there afterwards, and commit it. WEB7-WEB10 are outside the automation: bump them with `--apply`.
