# Existing native CI evidence reconciliation — 2026-11-28

Reconciled19/19 cloud-hyperdrive-A provider receipts without rerunning a backend or downloading test environments. GitHub confirms each run and selected provider job succeeded. Each accepted source workflow requires26checks in both modes, Succeeded and FixtureCleanupComplete; its EXIT trap stops owned processes/containers and removes the fixture. Explicit bash shell supplies fail-fast behavior.

All19provider-directory Git trees exactly match Developmentbfaf79570927. Shared Core/dependencies have evolved: these are accepted historical native baselines, not new current-graph, hosted or all-interface certification. Matrix already recorded3ofthese passes;14had stale not-run rows and2Redis-family rows retained obsolete smoke-only status. Registry had none ofthese19receipts. Do not add19to the approximate115programme total.

The matrix now records115/163storage/specialized projects with an explicit native52-check baseline; this is an inventory tally, not a freshly independently auditedCI acceptance total.238tracked provider projects match the documented selection rules;75matrix rows are non-storage and163storage/specialized. Earlier234/56/178header values were stale. Legacy project variants are included: this is not an active-backend or fully implemented product count. The inventory receipt lists every matched project and role. Controlled52-check and4-case smoke rows are excluded. A whole-programme CI/source consolidation remains separate.

| Provider | Accepted CI | Provider source |
|---|---|---|
| DaprOASIS | [45519314086](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/45519314086) | `e6affb96ba17` |
| GUNOASIS | [45520161955](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/45520161955) | `0a43477d1775` |
| MatrixOASIS | [45535697095](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/45535697095) | `a4bbb0cfc30f` |
| OrbitDBOASIS | [45535697095](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/45535697095) | `a4bbb0cfc30f` |
| ArweaveOASIS | [45535697095](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/45535697095) | `a4bbb0cfc30f` |
| ScuttlebuttOASIS | [45535697095](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/45535697095) | `a4bbb0cfc30f` |
| AppwriteOASIS | [45535697095](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/45535697095) | `a4bbb0cfc30f` |
| PineconeOASIS | [45535697095](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/45535697095) | `a4bbb0cfc30f` |
| TablelandOASIS | [45535697095](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/45535697095) | `a4bbb0cfc30f` |
| CelestiaOASIS | [45535697095](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/45535697095) | `a4bbb0cfc30f` |
| CeramicOASIS | [45535697095](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/45535697095) | `a4bbb0cfc30f` |
| BlueSkyOASIS | [45535697095](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/45535697095) | `a4bbb0cfc30f` |
| FilecoinOASIS | [45700279460](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/45700279460) | `74270b1c0aad` |
| UrbitOASIS | [45704685480](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/45704685480) | `df060e83fa0d` |
| RadixOASIS | [45709199011](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/45709199011) | `328d15b5003e` |
| ONION-Protocol | [45711682481](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/45711682481) | `e51f98974d4b` |
| SignProtocolOASIS | [45713760017](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/45713760017) | `772c1f45fa6b` |

Additional accepted receipts: KeyDBOASIS and DragonflyOASIS, CI45753785312 at6b5464451fe3. The dedicated workflow requires26checks each mode and independent DBSIZE0before/after. Both current provider trees exactly match; no rerun.

Compact hash-bound receipts are in NativeEvidence/*-ci-reconciled-2026-11-28.json; they are not raw case reports. No new provider runs, local builds, SDK downloads, databases, worktrees or shared Atlas operations. Only Git objects and compact evidence retained. Hosted owner actions already exist; no new owner action.
