# Development synchronization — 29 October 2026

Inbound integration verified on `work/cosmic-cql` in `C:/Source/OASIS-cosmic`.
This is not an outgoing Development deployment or all-provider acceptance.

- Incoming parent: `87df54630`; incoming Core: `113055c0596d26e9ce2f6ee0099ccc939e1ca1cb`.
- Merged Core: `c2b16274c4e6847f6b0c84f36a2741ff3549dd80`, pushed to its task
  branch and independently verified with `git ls-remote` before parent publication.
- Parent merge: `1cd6afc11`. Other contributors' release/API/documentation changes
  were preserved. The Core gitlink and deployment manifest both use the merge SHA.
- Pre-push hook reconciliation preserves the working Python version check and
  fail-closed validation, while also admitting the upstream `py` launcher.

## Reproduced integration issue and fix

The incoming avatar/holon sharing endpoints failed WEB4 compilation with two
CS0165 errors: `request?.ToChange(out var error)` did not assign `error` when the
body was null. Each endpoint now rejects a missing body before parsing a non-null
request. Invalid permission names retain their normal input-error result; provider
activation and avatar access are not reached for these malformed requests.

Four new controller assertions cover missing holon body, both avatar/detail
targets and invalid permission names. The original failed build log remains
`TestResults/DevelopmentSyncCurrent/compatibility-run.initial-compile-failure.log`.
No alternate storage path, exception swallowing or default permission grant added.

## Verification

| Check | Result | Evidence under worktree TestResults/DevelopmentSyncCurrent |
|---|---|---|
| Core producer and full Core controls | 4528 passed, zero failed/skipped | `core-sync.trx` |
| Shared producer and full storage controls | 1599 passed, zero failed/skipped | `shared-sync.trx` |
| Shipping WEB4 producer | Build passed | `compatibility-run.log` |
| GraphQL schema + sharing admission | 5 passed, zero failed/skipped | `web4-sync.trx` |
| Railway manifest/seven Dockerfiles/HEAD gitlinks | Passed | `python Scripts/validate_railway_dependency_manifest.py --require-gitlinks` |
| Pin synchronization | Passed | `python Scripts/sync_railway_dependency_manifest.py --check` |

SHA256:

- Core: `8C6D621C12C29A4B9706C36D9EC9501AA915D81D98E0C55C8AA439CA8EDC4EF7`.
- Shared: `41ECABAD67AA958AC0FB399B033CC0B80196F34012E8F867F8555948A63BA4D3`.
- WEB4: `9D7FB5102BDD7365AE27BEEA914B29A38C538F6C7D3630A5D56E7520E9D2FFA2`.

All builds used one lease per batch, existing dependencies and bounded workers.
No native services, new environments, dependency restores/downloads or deployments were needed.
Existing native MVP evidence remains scoped to its recorded revisions; this merge
does not independently re-prove every native provider or new access endpoint flow.

## Next

The [refreshed inventory](CQL_ALL_PROVIDER_MVP_INVENTORY.md) still has 226 concrete
identities, 17 scoped proof links, 33 central cases and 11 source-only project names
requiring classification. Start Base authority/native/Mongo-paired MVP proof next.
Hosted Azure remains pending; deep parity and migration gates remain deferred/open.
