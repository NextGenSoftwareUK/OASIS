# Sandbox cleanup audit — 17 November 2026 UTC

Physical cleanup is not accepted yet. The worker runs, but its summaries report incomplete avatar sweeps on SQLite/Neo4j. No new accounts or records were created for this audit.

## Observed configuration and runtime

Railway WEB4 dev reports SUCCESS on `8f826af5c7977b5dd49dc7d2f27ea35773b040a7`. Selected saved DNA fields enable sandbox cleanup with a 1,440-minute avatar retention, 15-minute interval, one-minute initial delay and maximum 50 avatars per sweep. The provider list is MongoDBOASIS, SQLLiteDBOASIS and Neo4jOASIS. No setting was changed.

The running worker logged startup at 20:28:09 UTC with retention `1.00:00:00` and interval `00:15:00`. Its 20:29:12 summary reported 0 completed avatar removals, 0 holon removals and 1 avatar kept; the 20:44:19 summary reported 0, 0 and 3 respectively. Warnings identify failed avatar deletion from SQLite and Neo4j. These summaries do not establish that no provider physically deleted anything: the current callback does not expose each native result or verify absence.

Only counts, timestamps, provider names and sanitized failure categories were retained. No record contents, identifiers, credentials, whole DNA or environment listings were printed or saved.

## Exact source checks

The inspected service and AvatarProfileController blobs match deployed parent `8f826af5c797`. The inspected cleanup/AvatarManager files match its Core pin `b6916890d75467be1a7d665b8acbaa10f7928c15`; Mongo repository files match pin `64f2aaa1e7ffda971c4c73ee7a3b6adc5f256f1a`.

| Path | Verified behavior | Evidence limitation |
|---|---|---|
| Avatar self-delete HTTP action | Calls DeleteAvatarAsync(id), whose default is softDelete=true | A successful acknowledgment is not physical reclamation |
| Sandbox cleanup host | Explicitly requests softDelete=false for avatar and holon deletion | Results are collapsed to booleans; native failure codes and absence are not retained |
| Core sandbox classifier | Requires username prefix, email domain, User type and expired CreatedDate; deletes owned holons before avatars | Cross-provider completion and race/partial-purge recovery need real-provider proof |
| Mongo avatar list | Version-zero GetAvatars includes tombstoned rows | Does not prove which provider a default-routed sweep observed |
| Mongo avatar hard delete | Transaction deletes the avatar/detail pair and checks acknowledgments | Does not prove all related history, entitlement or other-provider state was reclaimed |
| Mongo holon hard delete | Finds an exact observation, rejects ambiguous identity and conditionally deletes that BSON snapshot | This is source evidence, not a hosted cleanup receipt |

## Remaining engineering acceptance

Establish each provider's actual native state and error code before treating an already-missing target as success. An independent provider-pinned absence read must distinguish not-found from timeout, unavailable or denied. Do not add a catch-and-succeed or unconditional deletion retry.

Verify that partial deletion cannot remove the only discovery copy before other stores are reconciled. Keep real errors and owner-marker checks, preserve concurrent fixtures, and prove owned holon/account/history/entitlement cleanup separately. Existing named owner actions remain in the shared ACTIONS-FOR-DAVID document; this audit adds no account/approval request.

The separately qualified holon-read correction is not a fix or acceptance receipt for sandbox avatar reclamation.

## Diagnostic increment

Task 3f46972be60c adds provider/ErrorCode-only failure logging at the host callback, preserving the existing failed result. It is not yet compiled/deployed and is excluded from image candidate 9302000060fc. SQLite source has a typed RecordNotFound deletion outcome for an absent avatar; the observed runtime warning does not establish that outcome. Core already exposes RouteRequestToProviderAsync for independent no-failover observations during an intentional multi-provider aggregation. Any cleanup completion check must preserve admission and use authoritative provider state; it must not catch a native failure and assume absence.

## Provider routing and cross-store discovery increment

The previous host supplied provider preferences to ordinary managers. Those calls could still fail over or replicate, so an acknowledgement was not independent evidence for the named store. It also discovered expired avatars through one routed list, leaving partial purges able to lose their discovery copy. Candidate `7756987c72e8` uses request-owned pinned HolonManager contexts for holon work and the canonical explicit-provider HyperDrive aggregation route for avatar work. It reads all configured avatar lists before deleting anything, deduplicates consistent identities and rejects conflicting cleanup markers. Holon deletion acknowledgements must contain the requested ID. Existing manager conditional-write handling is retained; no missing-record result is converted to success.

The new compiled API regression covers independent/duplicate replicas, conflicting identity markers and invalid IDs. Syntax and exact dependency pins passed. [Focused build/contract run 44192504510](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/44192504510) is active and deliberately selects only that new case. This is not real-store acceptance. In particular, already-missing avatar/detail handling, partial-purge recovery, race-safe deletion, related histories and entitlement reclamation remain open. No new signed-in fixtures were created.
