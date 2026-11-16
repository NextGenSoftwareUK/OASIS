# Atlas storage and WEB4 sign-in incident

Inventory measured on 16 November 2026 UTC using the Atlas connection configured for Railway WEB4 dev. The connection sees live, dev and staging namespaces as well as shared service databases. Figures are decimal MB of BSON data plus index bytes from MongoDB statistics. They measure the inventoried data, not the Atlas quota meter. System databases admin and local reported zero disk bytes and were excluded from application inventory.

## Cleanup and root cause

- The largest collection was oasis_subscription_accounting.subscription_usage_reconciliation. It contained 531,679 generated reports for two billing months, using 322.65 MB of logical data and indexes. The worker checks up to 25 account-months every minute and previously inserted a new document for every outcome, including unchanged findings and repeated failures. The oldest measured report was from 14 October.
- Removed the authorized derived test reports and rebuilt the existing lookup index. The collection had zero rows immediately afterward. A follow-up inventory already showed 225 regenerated reports on the deployed worker. The code fix updates the timestamp of an identical account-month result and preserves distinct findings, including failed checks. It's verified against Atlas and needs deployment.
- Removed and confirmed absent four empty Garden control collections (controls_20261102_plants, controls_20261102_reward_outbox, controls_20261102_care and controls_20261102_gardeners) and the empty Places test fixture provider_daily_budget_controls. Their names match repository test fixtures. They consumed 376,832 bytes of allocated collection and index storage before removal.
- Verified WEB4 registration, verification, authentication, signed subscriptions, orders, compact holon count and test-avatar deletion after cleanup. All returned HTTP 200.

## Remaining inventory

The follow-up scan measured 214.28 MB of application BSON data and indexes across the following 18 databases.

| Database | Collections | MB | Purpose and disposition |
| --- | ---: | ---: | --- |
| OASISAPI_DEV | 32 | 122.03 | Avatar, AvatarDetail and Holon hold identities and application records. HyperDriveSyncChanges, Entities, Operations, Commands, Checkpoints, Devices, Counters, Retention, Snapshots, SnapshotItems, FanOut and leases support sync, replay and recovery. HyperDriveDomainCapture state, identities and leases coordinate capture. COSMIC outbox, receipts, fences, sequence, migration gates, replica heads and bindings support failover and replication. AccessLogEvents and AvatarMfa challenge/event collections support security. An empty subscription usage collection is still present. Keep pending retention and deployed-use verification. |
| OASISAPI_LIVE | 20 | 25.82 | Identity, holons, HyperDrive sync and capture, and COSMIC outbox. Keep live-configured namespaces. |
| OASISAPI_SANDBOX | 4 | 0.041 | Empty Atlas-side Avatar, AvatarDetail, Holon and COSMIC outbox namespace. Sandbox deployment configuration references the database name. Keep pending host and environment verification. |
| OASISAPI_STAGING | 3 | 0.012 | Empty Avatar, AvatarDetail and Holon staging namespace. Keep staging schema. |
| SANDBOX_NATIVE_APPS | 3 | 0.064 | Customers, orders and products. Repository sandbox configuration uses this native import fixture. Keep. |
| oasis_gardens | 4 | 0.045 | Gardeners, plants, care and reward outbox. Keep application collections. |
| oasis_oland | 2 | 0.129 | Orders and parcel claims. Keep commerce records and claim inventory. |
| oasis_places | 1 | 0.037 | Active provider daily usage budget. Keep. |
| oasis_staging | 1 | 0.004 | Staging isolation marker. Keep. |
| oasis_subscription_accounting | 12 | 46.32 | Subscription records and orders, billing and usage audits, usage events, receipts, buckets, grants, external evidence, overage statements, migration state and rebuilt reconciliation reports. Keep ledger inputs and active schema. |
| oasis_subscription_accounting_staging | 1 | 0.004 | Staging accounting isolation marker. Keep. |
| oasis_web4_subscription_outbox | 1 | 18.05 | WEB4 settlement delivery and retry state. Keep; measure settled-row eligibility before adopting a retention policy. |
| oasis_web5_subscription_outbox | 1 | 0.668 | WEB5 settlement outbox. Keep. |
| oasis_web6_subscription_outbox | 3 | 0.436 | WEB6 settlement outbox, agent schedules and schedule runs. Keep. |
| oasis_web7_subscription_outbox | 1 | 0.156 | WEB7 settlement outbox. Keep. |
| oasis_web8_subscription_outbox | 1 | 0.154 | WEB8 settlement outbox. Keep. |
| oasis_web9_subscription_outbox | 1 | 0.156 | WEB9 settlement outbox. Keep. |
| oasis_web10_subscription_outbox | 1 | 0.154 | WEB10 settlement outbox. Keep. |

The largest remaining individual collections before cleanup were dev HyperDriveSyncChanges (43.33 MB), usage audit (28.43 MB), dev Holon (24.87 MB), dev AvatarDetail (21.13 MB) and WEB4 usage outbox (18.05 MB). The release already has a checkpoint-aware sync compactor. Verify its deployed policy and recovery boundaries before pruning sync rows. Validate outbox receipt and replay requirements before removing completed entries.

## Why sign-in did not fail over

Railway WEB4 dev DNA had global auto failover enabled, and its general list included SQLite and Neo4j. The separate avatar login failover list, however, contained only MongoDBOASIS. The deployed storage-level auto replication setting was false, although the HyperDrive section declared replication enabled. Avatar authentication applies the login-specific list across both credential lookup and saving the refresh session. The quota-triggered createIndexes failure reached the outer authentication exception handler. Adding an alternate route alone would not establish that it has current verified credentials, revocation state and writable sessions.

The recovery gate is to prove a synchronized login account on a real alternate provider, then exercise MongoDB write rejection and unavailability through the deployed sign-in path. Verify refresh and authenticated follow-up requests on the surviving route, invalid password rejection, account revocation enforcement and return to MongoDB after recovery. The synchronized secondary and live controlled outage proof are still open.

## Deployment qualification

The storage fix is staged as 5c23c2da62922cf0cfa1848a374b749f4ab0ecd2 on Development 5b8dd4f9599c. Its only changes are the reconciler and the Atlas regression. Shared image run 43805665501 was still running when checked. The temporary staging index was removed.

The existing Core test AuthenticationRestoresItsRuntimeFailoverScopeWhenAvatarIsMissing verifies routing scope restoration only. It does not issue a refresh token or exercise an alternate database. The first authentication qualification must cover a readable but unwritable MongoDB primary as well as connectivity loss. After authenticating, use the issued refresh token and authenticated identity route while the primary is still unavailable. Negative controls cover a stale or unseeded secondary, rejected passwords and revoked credentials.

The focused MongoDB-to-SQLite authentication regression is wired to the reusable runner with the MongoAvatarLogin switch. It requires a native rejected write scoped to its disposable MongoDB client, one replicated verified avatar, a rejected wrong password, acknowledged SQLite sign-in, and refresh rotation on SQLite while MongoDB still rejects writes. Compilation and native results are pending. The standard MongoDB/SQLite matrix still selects its original cases.

## Sign-in DNA routing update

Saved and read back MongoDBOASIS, Neo4jOASIS, SQLLiteDBOASIS in the avatar sign-in override for every WEB4 through WEB10 Railway service in dev and production. The Railway updates were saved without triggering simultaneous deployments. Updated eight full parent-repository DNA configurations and two STAR defaults/templates. Provider-only overlays inherit their configuration and were not given a separate route. Native login and refresh qualification and rollout remain open.

The native MongoDB 8.2.1 and SQLite controlled test compiled and reached the refresh assertion. MongoDB still served reads and rejected writes. The wrong password was rejected, the correct login returned a JWT, the new refresh token was absent from MongoDB and present in SQLite. The follow-on refresh reported Avatar not found. The saved Railway route is a configuration change only. The remaining refresh defect and hosted secondary authority checks prevent claiming end-to-end failover.

The refresh failure is a route-authority mismatch. WEB4 uses the default provider to look up the refresh token. MongoDB remained readable, so it returned its authoritative RecordNotFound and ended routing before the SQLite store that acknowledged the session write. The HyperDrive write result already carries the acknowledged provider in its request-owned route observation. Preserve the primary's authoritative miss for unbound credentials and use verifiable session ownership for a token issued by a different store, including rotation and revocation.

Core b53f7dc2 binds new V2 refresh credentials to the acknowledged session provider with a signed envelope. Legacy opaque tokens continue through the existing lookup. For bound tokens, renewal and revocation writes stay on that provider. The native test also checks an altered provider selector, rejection of the raw storage marker, rejection after rotation and rejection after explicit revocation. The focused build and native rerun are in progress. Legacy-mode production and hosted Neo4j failover are outside this proof.

The revised .NET 10 Core and native integration project built with zero errors. The scoped MongoDB 8.2.1 and SQLite test passed (1/1, zero skipped, 37-second test duration). MongoDB remained readable and rejected writes. SQLite acknowledged sign-in, rotation and explicit revocation. Invalid password, raw credential, changed provider selector and both revoked tokens were rejected. This proves the isolated V2 session path under a definite write rejection. Hosted replica admission, Neo4j session failover, total connectivity loss and production Legacy remain open.

The current Development Core pin also introduces a second-factor completion path. Core 7a438f76 retains that gate and binds the session after each of the four verified sign-in completions, including MFA. The native regression now exercises MFA completion against the same writable SQLite and rejecting MongoDB pair. This checks token issuance after MFA completion, while the actual second-factor verifier remains outside this test. Exact-build verification is in progress.

Railway WEB4 dev deployed the report growth correction as 66254bc24af5 (624fd80a). Public health returned HTTP 200 in 1.092 seconds. Direct Atlas sampling saw keyed reports increase from 125 to 150 during the initial scan, with the latest keyed check advancing by one minute. The last append-only report remained 06:27:17 UTC across both samples. This is live cutover evidence. Row reuse after a complete scan still needs a live observation. The production WEB4 revision was cf47b47e595b.

Live Atlas scan after startup proved reuse. The keyed report count stayed at 260, 25 existing report timestamps advanced, zero new states were inserted, and the latest legacy report timestamp stayed fixed. The rebased refresh release candidate 8c192a2f628e is published on its CI-only ref. It contains only the Core pin, matching manifest and focused test and runner wiring. Shared WEB4 image run 43816412860 was active at the check.

After the live reuse check, audited all 2,220 reports regenerated by the old worker between emergency cleanup and cutover. Every report matched the complete state of a current keyed row after excluding the report ID and observation timestamp and normalizing finding order. Removed those exact IDs using the cutover timestamp guard, verified them absent and verified all 260 keyed reports remained. Logical BSON removed was 1,109,492 bytes. The exact refresh and MFA candidate 8c192a2f628e is now building in WEB4 image run 43819686961.

The exact Development-rebased Core 7a438f76 and extended native test built with zero errors. Native MongoDB and SQLite passed 1/1 in 46 seconds, with no skipped tests. MFA completion issued a SQLite-bound credential during MongoDB write rejection, rotated it through SQLite, and rejected replay. The test exercises completion after factor verification. The deployed WEB4 dev Atlas revision also issued a normal signed session in 648 ms, served the signed subscription list in 341 ms, and confirmed test-avatar removal.

Rebased the verified session changes over the new Development head 6771e17f6207, preserving its three Core COSMIC query-file changes and parent Neo4j query tests. Core 1b3cb05f97b5 and parent 9fb52e18bbce were published on CI-only refs. The earlier image run 43819686961 was still active. Both temporary Git indexes were removed.

Railway readback showed dev Neo4j is reachable on its private network and holds four Avatar nodes. This count cannot establish completeness against MongoDB's identity collection. Dev SQLite has a persistent volume. Production WEB4 points Neo4j at loopback and has no persistent SQLite volume. Storage-level automatic replication is disabled in both environments. The three-provider route order therefore cannot establish sign-in availability from those configuration values alone.

A dev Neo4j credential appeared in diagnostic output. Read-only scope inspection found matching values in the private dev Neo4j service and all seven dev WEB API DNA variables. No matching values were found in the inspected production or staging service variables. Rotation and service verification remain open. No credential value is recorded in this report.

David approved staging a replacement dev Neo4j account and rolling the WEB APIs one at a time. Verified a new database login through Railway SSH and staged the matching credentials in all seven dev WEB API DNA variables, preserving each service's remaining configuration. Started the first isolated rollout on WEB10 deployment 1495fc9b. Existing clients continue using the old login until their service has switched. The global and repository agent instructions now require OASIS DNA synchronization and secret-safe verification.

WEB10 deployment 1495fc9b reached SUCCESS. Its running environment matched the staged DNA hash and its documented public Swagger returned HTTP 200 with an OpenAPI document. WEB10's /api/health returned 404; its startup maps Swagger and lacks that route although its Docker healthcheck uses it. Started the next isolated restart, WEB9 deployment 803b873f, at 07:25:14 UTC. The old Neo4j login remains valid until six remaining services are verified.

WEB9 deployment 803b873f and WEB8 deployment ddb08c4c reached SUCCESS. Each running DNA hash matched its saved configuration and each public Swagger endpoint returned HTTP 200 with an OpenAPI document. WEB7 deployment b783559f started at 07:31:16 UTC. Three of the seven running services have been verified. The original Neo4j login remains active during the rollout.

The task branch adds ASP.NET Core liveness at /api/health for WEB7 through WEB10, before HTTPS redirection and application authentication. Docker WEB5 through WEB10 use the Node executable already in the final images and require an exact HTTP 200 over loopback. WEB6 uses its existing /v1/health endpoint. The probe checks HTTP process liveness. The patched images still need build and runtime verification.

The six Dockerfile probe commands passed 24 checks over loopback HTTP. Each exited zero for HTTP 200 and nonzero for HTTP 302, 404 and 503. The check ran the probe commands with a real local listener and the existing local Node installation. It created no files. Patched image builds and startup checks remain open.

WEB7 deployment b783559f reached SUCCESS. Its running DNA hash matched the saved configuration and public Swagger returned HTTP 200 with an OpenAPI document. Started WEB6 deployment 2bc98826 at 07:34:42 UTC. Four of seven running services have been verified.

WEB6 deployment 2bc98826 reached SUCCESS. Its running DNA hash matched the saved configuration and its public /v1/health returned HTTP 200 with status ok. Started WEB5 deployment f6c1e5b5 at 07:38:47 UTC. Five of seven running services have been verified.

WEB5 deployment f6c1e5b5 reached SUCCESS. Its running DNA hash matched the saved configuration and its public /api/health returned HTTP 200 with status healthy. Started the final API restart, WEB4 deployment 0b2a7e68, at 07:44:45 UTC. Six of seven running services have been verified. Original password revocation is pending WEB4 verification.

WEB4 deployment 0b2a7e68 reached SUCCESS. Its running DNA hash matched the saved configuration and its public /api/health returned HTTP 200. All seven running dev APIs now use the replacement account. Both rotated dev Neo4j administrator variables were saved and verified in Railway without restarting Neo4j. A database-side check confirmed successful queries with both the new administrator password and the replacement API login, and confirmed that Neo4j rejected the original password. The running Neo4j container takes the saved environment values on its next restart. No credentials were written to source.
