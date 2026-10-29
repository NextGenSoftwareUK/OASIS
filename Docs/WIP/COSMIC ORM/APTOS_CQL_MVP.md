# Aptos CQL MVP - controls verified, native demo pending

29 October 2026. The adapter uses the shipping Aptos0.1.1-beta official SDK Move
views and canonical GUID save keys. No alternate HTTP or managed-scan query path.

## Scope and root fix

Shared bounded-ID compilation admits exact AptosOASIS authority/catalogue,
ID equality/membership, Boolean algebra and paging, at most100 selected IDs.
Metadata/content/order/children remain unsupported. Each selected key uses existing
has_record/get_record views, then validates canonical ID/actor and hides deleted
records. Errors publish no partial rows; result and decoded payload budgets apply.

The shared existing has_record decoder previously treated unknown response types
as false (absence), and get_record treated missing response values as null records.
Both now require exactly one protocol result. Presence accepts only a native Boolean;
present payloads require a nonempty byte vector, strict UTF8 and nonnull canonical
JSON. Official SDK byte-array/JArray/hex representations remain supported explicitly.
Malformed data cannot silently masquerade as a missing record or replacement text.

Records use the existing plaintext Move payload format: no encryption claim.
Reads still require the existing configured account; read-only owner configuration
without a signing credential is a separate gap. SDK View has no cancellation-token
parameter in this pinned version. CQL checks before/after each awaited view and
publishes no rows on caller cancellation; an in-flight SDK call must finish first.
It does not detach the call or claim prompt in-flight cancellation. Separate views
may observe separate ledger versions. The4MiB budget measures reserialized decoded
records, not native response allocations. No snapshot/full-provider parity claim.

## Verified controls

10/10 zero skips: compiler/admission/cancellation plus strict Boolean/payload
representations and malformed-value rejection. Source: AptosCqlTests,
AptosRecordPresenceTests and AptosRecordPayloadTests.
Report `TestResults/AptosCqlCurrent/aptos-cql-controls.trx`, SHA256
`EF46C2BF7578759B9AF2C79653196DB42DDC2BC96A18B65A9C2015EAFF559115`.

Including existing selector-failure propagation:18/18 zero skips,166ms.
`TestResults/AptosCqlCurrent/aptos-cql-regression.trx`, SHA256
`844789F0927EBE3E4C9F55C4D1FC8913747F131FEB012076F7168E6B4DC8291E`.
These are controlled/admission proofs, not native chain or transaction evidence.

## Exact next step

Reuse/check other pinned Aptos CLI caches, then restore CLI9.6.0 once only if no
usable copy exists. The documented Windows provider-evidence/aptos-cli/9.6.0 directory
exists but contains no runtime files. Existing Scripts/run_aptos_provider_evidence.ps1
uses a unique temp ledger and --force-restart; do not execute that per-run setup.
Use one persistent LocalState/COSMIC/AptosCql source/ledger/catalogue and cached CLI,
with explicit initial admission and guarded restart reuse. Logs/TRX in TestResults.
No CLI/node download or start occurred during this milestone.

Then prove native3/2/missing/restart/source integrity and Mongo-paired SDK/HTTP/browser
5/4 before changing the inventory row green. Hosted Azure remains pending. S3 local
launch policy gate and original TRON historical/source gates remain documented.

[Status](CURRENT_STATUS.md) / [inventory](CQL_ALL_PROVIDER_MVP_INVENTORY.md) /
[resource ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md).
