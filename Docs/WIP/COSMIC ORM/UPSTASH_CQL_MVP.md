# Upstash CQL MVP admission

## Current-branch integration —30 October2026

Existing official free scratch admission was unexpired at2026-10-31T01:59:59Z.
**It expires2026-10-31T21:15:49.3541451Z.** Permanent free hosting remains a red gate.
No account, purchase, provisioning or replacement source was created.

```powershell
Scripts/TestHosts/Run-CosmicUpstashCqlNative.ps1 -Federated -Serve
```

Before expiry, open http://127.0.0.1:5117/, run both buttons, then stop locally and
wait for cleanup. Dedicated5117 avoids SQLServer5115. Startup refuses expired
admission before using the protected identity. Producer builds before consumer;
successful reports archive by hash, unresolved failures stop another run.

Current raw evidence under `TestResults/UpstashCqlCurrent/`:

| Report | Result | SHA256 |
|---|---|---|
| upstash-cql-current-controls.trx |12passed/0skips |D58FE16536EC9E422936ED1C4ADEAF49F33F5799F277751446CF50081000AA62 |
| upstash-provider-current-controls.trx |5passed/0skips |C4C08F0C9F572440F0718DCB3C535826F3594527E89120A0FEAEFB50C98BC80A |
| upstash-cql-native.trx |1passed/0skips |5EF49E55BEB540E6DFDA88742922606C8BF9E35A599F23B460D1DCB2AB29EF56 |
| upstash-mongo-federation.trx |1passed/0skips |343EEA4D5826CF8B9172F3589E88754D4AF064E589ED68DE8C2AF4018B4E4934 |

SDK/HTTP/browser all5/subset4 verified with provider provenance. Original hosted
values/index membership, seed/snapshot/three ACK hashes unchanged; no business write.
Exact temporary Mongo fixture verified/dropped, pending/ready absent, runner exit0,
27029/5117 stopped. Retained source metadata8files763B and protected identity remain.
Native browser proof: `upstash-mongo-subset-current.png`. No install/download.

Public section23 verified: Web4Site mainfebf8162f3c6fb03e43df11722619459b9c95652,
Production7452743804success; expiry, expanded command/fixed5117link visible.
Screenshot `TestResults/UpstashCqlCurrent/upstash-catalogue-public.png`, SHA256
65C3268C6AFCA3B0440556E6CA371FD92754C8A71E8591F6987B2C59D54DB6BA.
Backend remains task-branch only; no Railway deployment. Hosted selector unchanged.
Historical proof below retains old5115, superseded by5117. Plaintext, multi-command
write atomicity, content-query, expiry and full HyperDrive/parity gaps remain open.

Updated29 October2026. **Scoped hosted native/repeat and Mongo-paired C#/HTTP/browser MVP verified.**

## Implemented scope

An explicit authoritative catalogue of at most100 UUIDs compiles to one native
MGET against the exact holon:{GUID-D} keys used by the existing save/load methods.
No keyspace scan or managed business predicate is substituted. Missing/deleted
records are removed before bounded-page offset/limit. Response positions/count,
record identity and authority are validated; malformed/error responses publish no
partial page. Aggregate decoded payload budget4MiB is checked after allocation,
not a pre-allocation bound. Cancellation is propagated into HTTP send/body read.
Providers are observed separately; no distributed snapshot claim.

The existing shared REST helper now disposes requests/responses and rejects Redis
command errors even when HTTP status is200. Activation requires PONG and publishes
lifecycle state; failed activation/deactivation clear it. Save/load and CQL share
the existing JSON codec and transport; no alternate storage path is introduced.
Stored JSON in this provider is plaintext: a separate retained storage-key file is
not evidence that this provider encrypts records.

## Verified controls

Seventeen pass, zero failed/skipped,244ms: nine CQL controls, five current provider
metadata tests and three loopback activation outcomes (PONG, command error, null).
Existing stale tests referenced a removed version method and compared an enum
wrapper directly; these now test the current category/value contract.

- Tests: UpstashCqlTests.cs and UpstashOASISUnitTests.cs in UpstashOASIS.UnitTests.
- Evidence: `TestResults/UpstashCqlCurrent/upstash-cql-controls.trx`.
- SHA256: `9A32920872FCE08A1098F61D4ED0BCE8FFF08DC373C44804C0CD65F5BA705017`.

## Hosted native verification

The retained official free scratch resource was reused without provisioning. The
runner checks expiry2026-10-31T21:15:49.3541451Z before credential use and restricts
the protected identity to the original HTTPS Upstash service. Credentials are
unprotected only in-process, passed to the awaited test via environment, restored
and excluded from logs. No account, purchase or infrastructure installation occurred.

Three acknowledged synthetic saves and load round trips pass. Bounded native MGET
returns3/2/missing rows; paging and result-budget rejection pass with no partial
results. Native GET value hashes and membership of the three owned IDs in the
existing holons:all index survive fresh clients/processes unchanged. No keyspace
scan, shared-index replacement or unrelated-key deletion occurs. This is fresh
client repeat verification, not a hosted service restart or durability/failover test.

SaveHolonAsync now reports IsSaved=true/SavedCount=1 after its existing native
record/index operations succeed. Native assertions prove those acknowledgements;
the multi-command save is not certified atomic and wider concurrency remains open.
The old integration file had an incorrect Holon/Avatar namespace import, now fixed.

- Test: `UpstashCqlNativeTests.cs` in UpstashOASIS.IntegrationTests.
- Runner: `Scripts/TestHosts/Run-CosmicUpstashCqlNative.ps1`.
- Initial result1passed2s/no skips; `TestResults/UpstashCqlCurrent/upstash-cql-initial.trx`.
- Initial SHA256: `EF1B978C4A1EA0E28FC6489811C59E07B94088582B859F566616C0B444C842BE`.
- Repeat result1passed1s/no skips; `TestResults/UpstashCqlCurrent/upstash-cql-native.trx`.
- Latest native/repeat preflight SHA256: `4AB29E843712870030AF63AE7CAFC15D330B92D15EA21CF47B55F1C991A6F19B`.
- Seed SHA256: `B24961E3DB2A04F6F772B3472581BEC281B9CDEF7B13EFE4CEF8F20770480B35`.
- Persistent source directory `LocalState/COSMIC/UpstashCql`: 763bytes,
  plus three hosted records/index entries (native size unmeasured), reusable until expiry.

## Hosted Mongo-paired teaching demo

Shared C#, HTTP and browser queries return five physical rows (hosted Upstash3 +
local Mongo2), then four for the two-ID subset. Provider copies remain labelled.
Forged actor/unknown preset reject, and an inactive Upstash target returns unavailable
without partial rows. Only the owned temporary Mongo database is removed afterward;
the original hosted source/credentials remain unchanged.

- Fixture: `UpstashMongoCqlNativeTests.cs` in UpstashOASIS.IntegrationTests.
- Result: one passed24s, zero failed/skipped.
- Evidence: `TestResults/UpstashCqlCurrent/upstash-mongo-federation.trx`.
- SHA256: `75B64517C4928A4E37358A406018AC1FB7386828BD8ED2CB80859309468187E2`.
- Browser: `TestResults/UpstashCqlCurrent/upstash-mongo-browser.jpg`.
- Runner26758 exit0; owned Mongo/HTTP listeners absent; ownership journal removed;
  browser tab closed. Protected credential environment restored.
- Hub: 8 single/16plans,24 paired/48plans,34 paired rejections and3 archive
  rejections pass; listeners unchanged.

```powershell
# Inspect without services/credentials.
Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider Upstash -Federated -ShowPlan

# SDK/HTTP checks, followed by loopback5115 browser demo.
Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider Upstash -Federated

# Automatic verification and teardown.
Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider Upstash -Federated -VerifyOnly
```

Stop using the page button to finish temporary Mongo cleanup. **This retained free
scratch expires2026-10-31T21:15:49.3541451Z**; the runner refuses expired admission.
Missing/expired original source requires explicit reconciliation, never automatic
reseed or allocation. A permanently available hosted demo needs separately admitted
free infrastructure. Canonical JSON remains plaintext and multi-command saves are
not certified atomic; arbitrary predicates/full ORM parity remain open.

## Exact next step

Continue missing provider MVPs from the inventory. Preserve this scope and source
until expiry; do not harden green rows absent a reproduced bug. Broader correctness,
replication, migration and parity acceptance remain unchanged.

[Breadth reconciliation](PROVIDER_BREADTH_RECONCILIATION_2026-10-30.md) / [status](CURRENT_STATUS.md).
