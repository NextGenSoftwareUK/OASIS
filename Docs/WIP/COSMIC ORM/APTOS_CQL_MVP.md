# Aptos CQL MVP - native and restart verified, paired demo pending

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

Including existing selector-failure propagation:18/18 zero skips,172ms.
`TestResults/AptosCqlCurrent/aptos-cql-regression.trx`, SHA256
`7185F12918952F0D85DDF666880F3216FBFB578E52458B434F71FC3BF169B8C9`.
These are controlled/admission proofs, not native chain or transaction evidence.

## Native and full restart acceptance

AptosCqlNativeTests passes1/1,zero skips,428ms: native3/2/missing-ID, exact Move
payloads unchanged, original source retained. Full node stop/start on the same
ledger then passes1/1,287ms, same genesis/module/account/catalogue/records.
- `TestResults/AptosCqlCurrent/aptos-cql-native.initial.trx`, SHA256
  `D10FA0E9E7D108A97A4A8DB3B8EA84D29B990DF666D904DCB8EB1B3E00F28A04`.
- `TestResults/AptosCqlCurrent/aptos-cql-restart.trx`, SHA256
  `B9BA79B3AE26EE511C35980D046C3C317DFDCDD00EC7ED4B65A8F6795E9E68B5`.

Actual native setup exposed a save-result gap: a successful committed write did
not set IsSaved/SavedCount or return its transaction hash. SaveHolonAsync now
publishes those only after the SDK's committed-success acknowledgement. Three native
saves assert the flags/count and independently verify transaction success, retaining
one receipt per source write. Inactive-save controls confirm no false saved state.

The independent test-only REST inspector required known-length application/json
without charset; its rejected requests were corrected at that framing boundary.
No source write was replayed: all later test runs reused the acknowledged manifest.

## Reusable environment

Authoritative recheck found CLI9.6.0 and framework cache after the prior absence
observation. Historical acquisition ownership is uncertain; this session reused,
not downloaded, them. CLI executable149682176bytes SHA256
`CD89CC585004D2DB100F23E4E093538222E142D919047AE65517434CDC12B354`;
retained ZIP49875715bytes matches vendor checksum. Framework cached commit
`4f0d278f806da6d7006f98f43a66f774f07e4d37`; compilation used skip-fetch.

One persistent `LocalState/COSMIC/AptosCql` contains ledger, CLI profile, source
identity, setup acknowledgements and three native write receipts. Synthetic signing
credentials/private node files stay there, never in docs/evidence commits. Seed
173bytes SHA256`EDF169CCE67EAF7DBF9ABF1C90FFC73D25A53622FA39D3B979A0AC80053E8F34`.
Both owned node processes were stopped with Ctrl-C; loopback18080/18081/18070 closed.
Preserve the original ledger/profile/receipts; never force-restart or reseed.

Resume only with `Scripts/TestHosts/Start-CosmicAptosCqlNode.ps1` in a terminal;
Ctrl-C stops that owner. It checks pinned CLI, original source and pending journals,
refuses occupied ports and performs no provisioning. No new downloads/installations.
The original per-run unique-temp/force-restart evidence runner stays unused.

## Exact next step

Add the Mongo-paired C#/HTTP/browser5/4 preset against this original source, preserve
receipts/catalogue and verify cleanup of only its owned Mongo fixture. Keep the
inventory row red until paired/browser acceptance passes. Then continue breadth.
Hosted Azure/S3 local launch/TRON historical source gates remain unchanged.

[Status](CURRENT_STATUS.md) / [inventory](CQL_ALL_PROVIDER_MVP_INVENTORY.md) /
[resource ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md).
