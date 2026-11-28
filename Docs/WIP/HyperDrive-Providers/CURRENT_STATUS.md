# HyperDrive/provider current status

Updated2026-11-28UTC. Shared Atlas is not a test backend; protected deliveries535/543 remain untouched. Work uses owned branch refs and isolated CI, preserving concurrent physical checkouts.

## Railway shipping gate verified

WEB6/WEB7/WEB9 deployments for8c7a3148 failed at the unconditional Miden SDK install. Qualified root fix moves Miden into the existing publish-graph packager: a shipped Miden assembly requires its complete SDK, while API graphs without that provider do not install it. CI46113637170passed both real Miden-consumer and WEB6publish-graph checks plus cleanup. Corrected-source03bf0ef24bcf rollout reports7/7 Railway dev deployment statuses SUCCESS (2026-11-28T06:20Z); accepted native storage cases were not repeated. No running-environment setting or secret changed.

## Progress and scope

| Scope | Done/total | Evidence / limits |
|---|---|---|
| Tracked provider-project inventory | 238/238 | Exact tracked project-path set matches matrix;75non-storage and163storage/specialized. Includes legacy variants, not active backend admission. Earlier234/56/178headline was stale. |
| Matrix native core-storage baseline rows | 123/163 | Both26-check modes recorded. This inventory tally combines historical accepted evidence; it is not123current-graph/hosted/all-interface certifications. |
| Requested Firebase/Turso/PocketBase current-source rechecks | 3/3 | CI46099124837,46099193518,46103379382; own real fixtures and cleanup. |
| Existing accepted CI receipts reconciled | 19/19 |17cloud-native +2Redis-family; exact provider trees unchanged. No reruns.14not-run and2smoke-only rows corrected;3already recorded. |
| Recovered older search native receipts | 4/4 | OpenSearch source-matched pass retained; Meilisearch/Solr failures and Weaviate source changes now have separate current-source passes below. Older receipts retained, no double credit. |
| Meilisearch/Solr current-source rechecks | 2/2 | CI46117709175and46117714885,52/52each, zero skips, exact Development67663f814422tree and owned cleanup. Existing Core fix reused. |
| Weaviate changed-source native gate | 1/1 | CI46118279343,52/52, zero skips, exact Development67663f814422 tree and owned cleanup. |
| New Garnet native gate | 1/1 | CI46122538895;52/52, zero skips, native DBSIZE0 and owned cleanup. |
| New Typesense native gate | 1/1 | CI46114331452;26sync+26async, zero skips, native empty collections and owned volume cleanup. |
| New Elasticsearch/Qdrant native gates | 2/2 | CI46111639871;52/52 each, zero skips, native empty stores and owned cleanup. |
| Canonical public storage-switch regression | 2/2 | CI46109531969, real SQLite/LocalFile, zero skips. WEB4 shipping build46109274477passed. |

See [provider matrix](PROVIDER_MATRIX.md), [reconciliation](ACCEPTED_CI_RECONCILIATION_2026-11-28.md), hash-bound NativeEvidence receipts, and [release gates](HYPERDRIVE_V2_CURRENT_RELEASE_GATES.md). The original about115/234programme estimate is historical; do not increase it by requalification/reconciliation counts.

## Latest verified Development batch

Qualified batch preserves Development8ed94e278c2263a5aea4c35fd432514671df6ed8 and Core0fd03dede3f2caaa02ec6e2c5ff75319f9528e55 with paired Railway manifest. Typesense native52/52 is added; OpenSearch existing25Nov native52/52 is source-matched without rerun. Meilisearch/Solr25Nov failures now have current native passes using the already-shipped Corec19b8e1 fix.

- Miden: reused Claude0289f57c, completed locked official SDK installation across7API images and normal constructor configuration. CI46106768815 passed52/52 plus native worker-loss/reopen/disposal case, zero skips. Deterministic stores retain signing journals; owned CI fixture/stores/output removed. Hosted admission remains MIDEN-HOSTED1.
- Delegated identity: Eclipse46101901966, Telos46101946563, SEEDS46101275004 pass52/52 and composing-key/update round trips. EOSIO canonical save/key lookup aligned; original6c95573c reused.
- Public controls: one canonical registry and selection engine; target activates before publication, failure preserves the healthy selection, borrowed providers remain active, and actual switch timestamps are retained. Native controls passed2/2; no whole-v2/hosted certification implied.
- Existing KeyDB/Dragonfly CI45753785312 already passed52/52 and independently empty keyspaces. Reused accepted source; no repeat run.

## Remaining work

Provider-account/capability gaps, retained-data compatibility and current shared-dependency graph acceptance remain in the matrix and [ACTIONS-FOR-DAVID](../Release%20and%20Status/ACTIONS-FOR-DAVID.md). Hosted-only providers are skipped until their isolated accounts/fixtures exist. No secrets in source/evidence.

Global HyperDrive release gates remain open. Preserve the accepted2026-11-23 real MongoDB/SQLite/SQLServer triad evidence in MongoSqliteSqlServerTriadHyperDriveTests.cs (replication,9read round-robin, failover, outbox convergence); do not repeat unchanged checks. Hosted multi-provider acceptance and eligible network-provider fan-out are separate. Never clear/replay/re-authorize held deliveries to make a gate pass.

## Current site inventory

11repository updates published, including the founders site, Omniverse, both WEB4sites, Docs/Learn, WEB6/WEB8, STAR/STARNET and Our World.10public pages/catalogues observed updated; WEB8change is README-only. Three public catalogues contain all238project entries; the added4are non-storage. Source and public-check receipt: NativeEvidence/site-provider-inventory-publication-2026-11-28.json. OPORTALruntime counts remain actual registered providers.

## OPORTAL continuation

Preserve Claude’s broad popup fixes. Dev/checkout CI46102330540 passed203/203executed browser checks per target;80authenticated checks skipped because the repository has no test-account secrets. Report upload quota failed separately. OPORTAL is not claimed100%complete. Existing credential custody is logged once as OPORTAL-CI1; no broad shared-Atlas test authorized.

Corrected public MCP documentation links and runtime download labels in OPORTALdev6f98a143. Removed35superseded CI artifacts/9,649,851,300bytes; release/latest-good artifacts retained. This recovered remote artifact storage, not laptop disk. Latest local disk observation:34.67GiBfree; this resumed batch created no local runtime/build/test database or SDK download.

## Coordination and cleanup

Own branches: work/gpt-provider-evidence-reconciliation, work/gpt-hyperdrive-release-gates, work/gpt-miden-native-integration. Claims on claims branch; scope completed items with commit evidence. Integrate only verified non-overlapping batches, at most once per30minutes, after queue checks. Human status/evidence docs also synchronized to master; executable inventories stay paired with source until release. No production-code promotion implied.

Ledger: [GPT_HYPERDRIVE_LEDGER.md](GPT_HYPERDRIVE_LEDGER.md). Private Git indexes removed in finally; compact receipts retained. No shared Atlas operations or protected queue changes. Earlier status is retained in Git history and Archive/CURRENT_STATUS_LOG_TO_2026-11-23.md.

## OPORTAL router compatibility and evidence reconciliation

Previously accepted real-API journeys were rechecked against the changed router: Social Graph persisted contact-to-composer keyboard handoff and Activity Feed native read/refresh. Existing accepted popup cohorts are not repeated. CI46118199642passed2/2router compatibility journeys and exact cleanup; earlier runs exposed harness declarations and the current router X-to-/portal.html contract. No whole-portal acceptance is claimed. Shared Atlas remains untouched.

OPORTAL current source inventory45/45is reconciled in `NextGenSoftwareUK/OPORTAL-JS` dev4c4d90af/main4ecb86db, Docs/WIP/CURRENT_ACCEPTANCE.md. Original Social/Activity45172482199and Gifts/Inventory/native histories retained; no new-screen credit for repeated flows. Remaining valid signup/recovery, ONET/ONODE bridge, Holon Diff and billing gates are explicit.
