# Garnet CQL demo — admitted generation2

## Source recovery — 2 November 2026

Named CQL/provider/test/PowerShell/Python sources restored from retained pin
237403a75. Eight current controlled CQL cases, producer/consumer compilation
and syntax checks pass. Shared Mongo fixture support has one source under
Providers/Shared/TestSupport. Native services, credentials and original fixtures
were not opened/replayed; native/browser evidence below is historical. Hosted
Garnet remains unverified.


Updated 30 October 2026. Current scoped demo acceptance; full ORM/provider parity
and recovery of the first failed source remain unverified.

## Launch and demonstrate

From `C:/Source/OASIS-cosmic`:

```powershell
Scripts/TestHosts/Run-CosmicGarnetLinuxDemo.ps1 -Federated -Serve
```

Open http://127.0.0.1:5123/. Choose **Query all records** (5 physical rows), then
**Query two-record subset** (4). Both keep Garnet/Mongo provider labels. Finish with
the local **Stop demo** button and wait for verified fixture cleanup and runner
exit. Start one Linux Mongo demo at a time.

The shared, commented [C# walkthrough](../../../Examples/CosmicCqlDemo/IdentityFederationWalkthrough.cs)
selects an explicit provider scope and applies
`Where(row => selectedIds.Contains(row.Id))` through HolonManager. Trusted access
catalogues belong to each source; caller-selected IDs do not grant access.

## Current verified scope

- Pinned Microsoft Garnet2.2.5 runtime, owned loopback socket and native product
  version; encrypted Redis-protocol executor with distinct Garnet CQL authority.
- Original admitted generation2:3 live records/1 tombstone; bounded authorized ID
  equality/membership/Boolean selection, missing/deleted exclusion and subsequent
  paging; cancellation, result budgets and unsupported-shape rejection.
- Original encrypted string values, keys, seeds, admission, config, checkpoint files
  and acknowledgements unchanged through queries and actual checkpoint restart.
  No new SAVE, business-write replay, empty probe or fixture admission was run.
- SDK/HTTP5/4, forged actor/unknown preset400, inactive source503 without rows;
  browser independently returned5/4. Unrelated Mongo record excluded.
- Exact `cosmic_cql_mongo_garnet_demo` identities/collections verified before drop;
  pending/active/ready files absent and16392/27039/5123 listeners absent afterward.
  Garnet SIGTERM exit-15 follows an acknowledged checkpoint; this is not a
  clean-dispose or uncheckpointed crash-durability claim.
- First failed fixture refused before startup; all18 original file hashes unchanged.
  Its pending/active receipts remain evidence, not proof of a currently live process.

## Current evidence

12 MSTest cases and6 lifecycle controls passed, no failures/skips; one additional
original-fixture refusal check passed. Files are under
`C:/Source/OASIS-cosmic/TestResults/GarnetCqlCurrent/`; do not commit raw output.

| Evidence | Passed | SHA256 |
|---|---:|---|
| garnet-current-controls.trx |8|F25CC02B9D0B9201EF8A69ABFB99ED8A5E5B88BB128B9F792B40F652570BEE5E|
| garnet-g2-cql-native.trx |1|CA44DAA6B6022EAA5B7767F6D77D14D9BE8105031B5B3CBADB5ABDCD52A05D9E|
| garnet-g2-cql-restart.trx |1|F82A1A4D1F8FC862818B88312E31186A31A2737ACE7B262B1F20AA80162A5088|
| garnet-g2-mongo-init.trx |1|76C0F786D9336E4A38B40099D915064E7573096D7BD64CD36E1AFC7B3EDF8BA1|
| garnet-g2-mongo-federation.trx |1|AFCDC5B27B1C20671FEC5360E360676E01A04ACDE9413468B072219FB332161D|
| current-lifecycle-controls.log |6|6C094240B5050C73C20CE1379B42EDD2E7D075D408C843B7903D85ACC15B5093|
| current-original-refusal.json |1|CC2DE0A0304DB45178BFE1C371FB9F1F9A527DA45A2FEDE4E1B56A88610A04EA|

Browser evidence: `garnet-mongo-subset-current.png`. Public CQL section29 main
f7d8392 / Production7453175882 publishes the local launch catalogue, not hosted
Garnet connectivity. Backend remains task branch; no Railway deployment.

## Retention and remaining work

Retain `/var/tmp/oasis-garnet-cql/generations/2` and its parent protected credentials
together. Seed SHA2562B7DFBA20505855D5B37D59BD7C89578697F2114CCF5173F58B616FD96FA9799
unchanged. Prior verification receipt is hash-archived; checkpoint payloads unchanged.
The first source lost uncheckpointed in-memory payloads historically; generation2
uses separate admitted IDs and does not recover that loss.

Catalogue maximum100 and decoded4MiB budget after native reads; general name/text/
metadata queries, graph/conditional/history semantics, central registration,
external configuration/atomicity admission, AOF/crash recovery, HyperDrive/cluster/
failover and distributed snapshots remain separate work. Original native profile
uses Lua and LuaTransactionMode; protocol compatibility alone certifies none of these.
[Historical proof](Archive/GARNET_CQL_MVP_PRESERVED_2026-10-30.md),
[first-source failure](Archive/GARNET_CQL_PRE_GENERATION2_2026-10-29.md),
[resource ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md), [later work](AFTER_DEMO_BACKLOG.md).

## WEB4 query profile preparation

The opt-in WEB4 source uses CQL_GARNET_QUERY_ settings with suffixes
ADDRESS, PORT, TLS_HOST, USERNAME, PASSWORD, STORAGE_KEY and PROBE_ID.
Use the owned source's public TLS endpoint, existing encryption key and a
command-restricted reader on a dedicated teaching-only instance. The probe must be the first ID
in the reviewed public catalogue. Keep credentials in protected settings.
The provider keeps its own encrypted key namespace and rejects a live
record whose public flag has been removed. The shared SDK profile permits
socket connections only to the configured address and port.

The shared native test category GarnetHostedCqlNative uses the WEB4 registration with
COSMIC_GARNET_HOSTED_CQL_SOURCE and the same protected settings. It
checks public 3/2, paging, missing/deleted exclusion and unchanged returned
records. The protected settings preparer validated the public catalogue
shape and probe binding. WEB4 admission and native fixture compilation are
running locally. The actual product still needs an approved TLS/ACL source
before hosted visitor acceptance.

The pinned Microsoft Garnet 2.2.5 Windows executable passed a loopback
TLS 1.3 and RESP test with hostname verification, anonymous-read denial,
an authenticated GET and write denial. This version rejected the
key-specific ACL rule at startup and a command-restricted reader could
GET a second synthetic key. Use a dedicated Garnet instance with only
teaching data and a reader ACL that denies writes. The live hosted test
must also verify that the SDK can connect with the restricted reader and
perform the scoped CQL reads. [Pinned ACL implementation](https://github.com/microsoft/garnet/blob/8d007138208ce030eb10a50c80e636b1678b83e4/libs/server/ACL/ACLParser.cs).

The shared WEB4 admission run passed 13/13 against Core
`0561f11287d7913b235106cfc261484ae73db41c`. The native TLS fixture compiled
with zero errors. The admission tests covered distinct authority, settings
validation and rejecting an SDK connection to a different endpoint.

The disposable Windows fixture then passed one real CQL test through
StackExchange.Redis with a TLS-verified restricted reader. It used the
normal provider saves to create the encrypted records and checked 3/2,
one-row paging, deleted and absent exclusion, public flags, and identical
native ciphertext hashes before and after. Native TRX SHA256
`03b1f5b609baef1c3aa9287938794d9b1c21fc13511d03774d96b34d46025c6b`.
The fixture used loopback only. It does not qualify a hosted instance.

From the repository root, build the Garnet integration project and run
`python -B Scripts/TestHosts/run-cosmic-garnet-windows-public.py` within
the same shared build lease. The launcher expects the verified runtime
receipt in the recovery-laptop qualification directory and refuses to
reuse the evidence directory. Archive the completed evidence before
a deliberate new fixture run.
