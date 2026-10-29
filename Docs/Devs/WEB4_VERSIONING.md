# WEB4 versioning: one number, everywhere

The WEB4 OASIS API and the OASIS Runtime ship under the **same version** (currently 5.3.0). Keep them identical.

## Where the version lives

| Place | What |
|---|---|
| `OASIS Architecture/NextGenSoftware.OASIS.OASISBootLoader/OASISBootLoader.cs` | `OASISAPIVersion` (**source of truth**) and `OASISRuntimeVersion` |
| Swagger title, `GET /api/settings/version`, startup log | Read `OASISAPIVersion` at runtime: nothing to edit |
| `ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI/OASIS API RELEASE HISTORY.md` | A `## X.Y.Z (dd/mm/yy)` entry |
| `Scripts/Test-OASISGlobalRelease.ps1` | Expected `OASISAPIVersion` and the required history entry |
| `Docs/Devs/GLOBAL_RELEASE_AUTOMATION.md` | "WEB4, WEB5 and WEB6 are ..." sentence |
| `OPORTAL-JS/postman/OASIS WEB4 API vX.Y.Z.postman_collection.json` | File name and collection title (sibling repo `../OPORTAL-JS`) |
| `OPORTAL-JS` `assets/components/modals/dev-portal.html`, `source/main/index.html`, `source/dist/index.html` | Links to that file |

WEB5-WEB10 have their own versions (`STARAPIVersion`, `WEB6APIVersion`, ...) and are not part of this rule.

## Bumping

```bash
py -3 Scripts/web4_version_sync.py --apply 5.4.0
```

This edits every place above (including renaming the Postman file in `../OPORTAL-JS`, or the folder in `OASIS_OPORTAL_DIR`) and adds a
history stub; replace its `TODO` with the real notes. Commit the OASIS and OPORTAL-JS changes together.

## Checking

```bash
py -3 Scripts/web4_version_sync.py --check
```

Exits non-zero and lists every place that disagrees. CI runs it (`.github/workflows/web4-version-sync.yml`); there the OPORTAL-JS repo is
absent, so only the OASIS-side places are checked. The global release automation (`Scripts/Invoke-OASISGlobalRelease.ps1`) can advance
`OASISRuntimeVersion` and `OASISAPIVersion` independently: run `--check` after a release and fix any drift with `--apply`.
