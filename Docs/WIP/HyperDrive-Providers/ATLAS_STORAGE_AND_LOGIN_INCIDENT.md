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
