# LocalFile + Mongo CQL MVP

Updated 2 November 2026. Named source/launcher restored from retained pin 237403a75. Seven controlled tests, producer/consumer compilation and launcher syntax pass. No native fixture/source/seed replay. Acceptance below is historical, not a newly repeated run. Hosted LocalFile remains unverified.

## Hosted public-profile preparation,3November2026

New LocalFilePublicCqlProfile adds explicit public-catalogue query enrollment and
read-only activation without changing the ordinary writer. Activation requires
existing bounded canonical GUID files,matching startup hashes/public owner and
strict UTF8. Query dispatch reuses the actual LocalFile decoder/stream budget;
it rejects unlisted identities and later private/foreign response rows without
publishing partial results. Files are plaintext,not encrypted or snapshot-pinned.
Register this representation only in the CQL query-source registry,not as writable
authority. Native/storage signing and broader LocalFile ORM remain separate.

8new actual-filesystem cases pass,0failed/skipped;3/2selection,catalogue isolation,
public/owner/hash admission,cancellation,missing-source handling and later privacy
changes. Newly owned synthetic cases removed;original retained cohort untouched.
Proof:LocalState/COSMIC/HostedLocalFileSourceCurrent/public-profile-evidence.json.
Next:host configuration/explicit synthetic-public bootstrap,SDK/HTTP/Mongo5/4,
privacy/replica proof and sandbox card. No hosted registration or count increase yet.

Legacy unit-project gaps found while adding tests:CS0411 compares ProviderType to
its enum wrapper;CS1061 calls nonexistent GetProviderVersion. These template tests
remain unchanged/documented. New filesystem tests belong to the existing
integration project;its cached dependencies compile. Initial NETSDK1004 and both
compile-error logs/hashes preserved in the proof manifest,not relabelled green.

### Explicit hosted configuration

CQL_LOCALFILE_PUBLIC_SOURCE_JSON now registers the separate query profile through
Startup/LocalFileCqlRegistration. Schema1 requires exactly `schema`,`directory`,
`sourceOwner`,`records`;each record has exactly `id`,`sha256`. Directory must be
absolute;IDs are nonempty unique GUIDs;hashes are64hex digits. Maximum100records
and32768configuration bytes. No credentials,unknown fields or duplicate keys.
Parsing captures metadata only;it never creates directories or activates storage.

Public read grant,query tariff,presets and data provisioning remain separate
explicit deployment configuration. LocalFile is admitted for bounded unsorted
row/federation queries,not automatically into canonical three-store composition.
Existing browser preparation already supports GUID recipes and avatar-mapped
snapshot IDs;no frontend algorithm duplication is needed.

16profile/configuration/native-filesystem cases and30API/discovery/schema cases
pass,0failed/skipped. Original TRX/source hashes:
LocalState/COSMIC/HostedLocalFileSourceCurrent/host-registration-evidence.json.
Next implement the explicit public teaching-data bootstrap and verify hosted
native3/2,Mongo5/4,privacy/replicas and card. No deployment/configuration change
or hosted LocalFile acceptance yet;counts stay96local/60historical hosted proofs.

### Write-once public teaching-data bootstrap

CQL_LOCALFILE_PUBLIC_BOOTSTRAP_JSON is optional and requires an explicit source
profile plus OASIS_SANDBOX=true. Schema1 contains exactly `schema`,`records`;
each record contains `id`,`sha256`,`contentBase64`. The complete bundle must match
the declared source IDs/hashes,strict UTF8 and live public flat rows belonging
to the source owner. Unknown/duplicate fields,private/foreign/deleted/child rows
and corrupt content are rejected before dataset creation. Bundle budget128KiB,
individual decoded record32KiB. No account/key/new service is required.

The source parent/volume must already exist. One exclusive sibling bootstrap
lease owns creation. New source stores a durable intent before record files and
ACK after all writes;each write is create-only and flushed. A pre-existing source
without both matching receipts is not adopted,replayed or overwritten. Verified
ACK reuse compares exact bounded bytes and performs no dataset writes. Altered
or partial state requires explicit reconciliation;no automatic repair/fallback.
This is not a multi-file database transaction or a general migration tool.

Bootstrap starts before hosted source activation. Read grants,tariffs and presets
remain independent explicit host configuration. DI owns profile disposal;host
container disposal deactivates the profile. Target later uses the existing/data
volume,not another server/environment. Existing LocalFile writer is unchanged.

16profile tests and41API/bootstrap/discovery/schema checks pass,0failed/skipped.
New synthetic filesystem cases removed;initial compile diagnostic preserved.
Proof:LocalState/COSMIC/HostedLocalFileSourceCurrent/bootstrap-evidence.json.
Next generate the declared source bundle,prepare protected hosted configuration,
qualify the combined release,then verify native3/2,Mongo5/4,privacy/replicas/card.
No live deployment/config/data/count change has occurred in this step.

## Demo

```powershell
Scripts/TestHosts/Run-CosmicLocalFileCqlNative.ps1 -Federated -Serve
Scripts/TestHosts/Run-CosmicLocalFileCqlNative.ps1 -Federated
```

The browser is localhost5110. One HolonManager query targets MongoDBOASIS and
LocalFileOASIS. Three retained file records plus two Mongo copies yield five
physical rows; selecting two IDs yields four. Each row preserves provider provenance.
The shared C# walkthrough and HTTP/browser examples have spaced code and comments.

Source uses the shipping provider's canonical GUID filename/decoder and common
bounded-ID compiler. It reads only selected files, never scans the directory to
filter content. It supports authorized ID equality/membership, Boolean algebra and
paging over at most100 IDs; metadata/content/order/children remain unsupported.
The result budget is1..10000 and the aggregate actual-byte read budget is4MiB.
Strict UTF8/identity/access validation rejects bad data; failures/cancellation
publish no partial rows. Only a missing individual file means absence; missing
source directories and access errors remain failures.

Files are plaintext: no record-encryption claim. Separate file/provider reads can
observe different moments; no cross-provider snapshot is claimed. Full history,
child writes, conditional writes and EF parity are separate acceptance gates.

## Historical preserved-branch evidence

- LocalFileCqlTests:7/7, zero skips. Catalogue isolation, unsupported shapes,
  inactive provider and cancellation. `TestResults/LocalFileCqlCurrent/localfile-cql-controls.trx`,
  SHA256 `BF7CF7BF4A7C61306004AF929A6BBF34C33DB8333D98E1D31A670181EB32D2CC`.
- LocalFileCqlNativeTests:1/1 initial and1/1 second-process reuse; native3/2 and
  authorized missing-file empty result; every source byte/path unchanged.
  Latest preflight `localfile-cql-native.trx`, SHA256
  `912FB420558D54C60B60BE8E9E78A120CA1A8BC0D4118AEE1BE59073652974D1`.
- LocalFileMongoCqlNativeTests:1/1 zero skips. SDK/HTTP5/4, provenance, unrelated
  Mongo ID excluded, forged actor/unknown preset rejected; inactive file provider
  yields HTTP503 without rows. `localfile-mongo-federation.trx`, SHA256
  `232AB77E0AE683226797D087C4339117CFBE5EAA17A96144572650B06B68B7CF`.
- Browser independently checked full5/subset4 and provenance. Screenshot:
  `TestResults/LocalFileCqlCurrent/localfile-mongo-browser.jpg`.
- Launcher routing:8 single/16 plans,18 paired/36 plans,22 paired rejections,
  3 archive rejections; listeners unchanged. `hub-routing.log` in the same folder.

## Retention and cleanup

Retained source `LocalState/COSMIC/LocalFileCql/source` plus seed/locks:5874bytes.
`demo-seed.json`173bytes, SHA256
`57ABA9D9930BB29886E5CD3D5D5B920CAC05F0B8E82C2902BE4CD47460EC266D`.
Reuse requires original source; pending setup must be reconciled, never replayed.
Runner and fixture own distinct locks so producer ownership does not deadlock the
consumer's source lock. Initial duplicate-lock wiring failed before source mutation;
fixed at that ownership layer, then verified native/paired execution passed.

After browser shutdown the same owned runner exited0. Exact temporary Mongo DB
`cosmic_cql_mongo_localfile_demo` was identity/content-validated and dropped; pending
and ready journals removed. Ports27029/5110 closed. Native source retained, no
runtime installation/download. Generated outputs remain in TestResults/bin/obj and
can be regenerated; never delete the original source with output cleanup.

[Current status](CURRENT_STATUS.md) / [inventory](CQL_ALL_PROVIDER_MVP_INVENTORY.md) /
[resource ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md).

## Current-branch integration acceptance —30 October2026

Selective port from preserved f89cf0085, without unrelated provider changes. Producer is built before consumer; original source is required. Runner archives successful reports by SHA256 and refuses unresolved reports before another execution. Dedicated5110 avoids the TRON5108 catalogue collision. Development51f3c4c3 fetched and already an ancestor of task HEAD.

Evidence under C:/Source/OASIS-cosmic/TestResults/LocalFileCqlCurrent/:

| Report | Result | SHA256 |
|---|---|---|
| localfile-cql-current-controls.trx |7passed/0skips |35F05A499B5EF009A70FDEEBBFE43685965019695B4325C5D1C45CEFB7E0E278 |
| localfile-cql-native.trx |1passed/0skips; original3/subset2/missing0, all source bytes/paths unchanged |08E6A7C5CBDD475314EABFCC84E36382C0978557F0254580BBAAC4368904F13B |
| localfile-mongo-federation.trx |1passed/0skips; SDK/HTTP5/4, isolation/rejections/inactive503, exact cleanup |8598CA24A37B3A12DEA7902FF158E8E0007F321963C21E6698E812FAE45FE0D5 |

Independent browser full5/subset4 verified; localfile-mongo-subset-current.png retained. After stop, runner exit0, Mongo fixture identity/content verified and dropped, pending/ready gone,27029/5110 stopped. Original5874-byte source/seed retained, seed hash unchanged. No runtime install/download/newenvironment.

Public section16/expanded command/fixed5110link verified: Web4Site main7bcfd230486837f97dde185406d92a407039c4f7, Production7452447621success. Screenshot localfile-catalogue-public.png SHA2563CB7A2E4759378F52E3E4B57D85228BE1685EFD56FDD2F7B8EB0E838A47209F1. HTML-only addition; hosted selector remains three databases. Backend changes remain task-branch only, no Railway deployment.

After-demo gaps remain plaintext/encryption, metadata/content queries, child semantics, conditional writes, broader HyperDrive and full parity. This bounded profile does not turn those gates green.


## Hosted public source qualification — 3 November 2026

A separately declared, retained public teaching dataset was exported once under
`LocalState/COSMIC/HostedLocalFilePublicDatasetCurrent`. Actual LocalFile/HolonManager/
LINQ export test passed 1/1: full3/subset2 and unchanged source bytes. The shipping
bootstrap parser independently admitted the retained bundle (1/1), without activation
or a repeated source query. Qualification hashes and original TRX paths are in
`LocalState/COSMIC/HostedLocalFileSourceCurrent/source-qualified.json`.

Five pure publisher tests pass: exact five-value scope, bounded four presets, free
query-only admission, no writable routing changes, existing-data preservation,
corrupt bundle rejection and deployed-release-before-mutation gate. These are
configuration controls, not hosted CQL proof.

Real read-only preparation stopped: DNA `OASIS.Sandbox.Enabled` is true but
`OASIS_SANDBOX` is absent. The new registration currently requires that separate
flag, contrary to the canonical DNA-backed `SandboxOptions` setting. Fix the new
registration/publisher at this boundary and add a regression before rollout.
No protected candidate, hosted configuration change, bootstrap or deployment was
performed by this preparation. Existing accepted sources were not replayed.

## Laptop recovery — 4 November 2026

Resumed the original task branch in C:/Source/OASIS2. WEB4 registration now
uses the same DNA-backed SandboxOptions.Enabled as the host. Publisher preflight
requires DNA sandbox mode, and all five publisher tests pass with the redundant
environment flag absent. WEB4 bootstrap and registration tests passed 13/13 against the rebuilt API.
The wider LocalFile filter had the same 13 passes and one gated skip for the
missing retained export. Evidence: TestResults/HostedLocalFileSourceCurrent/localfile-laptop.trx.

The original hosted-source qualification receipt is absent at its expected
LocalState path in this checkout. Recover the original source configuration,
bundle, export acknowledgment and qualification receipt together, then check their
hashes through the publisher before preparing a candidate. No hosted mutation
was performed from this checkout.

## LocalFile admission and hosted verifier ready

Core a9a238371 permits explicit LocalFile unsigned query tariffs. WEB4 rebuilt
under the shared build lease and passed the focused admission regression
(1 passed, 0 skipped). Explicit query-only enrollment is required and a
LocalFile SaveHolon tariff is rejected for that enrollment.
TRX: TestResults/HostedLocalFileSourceCurrent/localfile-tariff.trx
SHA256 A396D948AE2F224C95C50BF987C0F74DE9E09F46820A00143C963A68FE572B50.

The hosted verifier now accepts LocalFileOASIS. The sibling Web4Site checkout
was fast-forwarded to 2170b6728 to recover the published client modules. Their
ID recipe accepts the publisher's synthetic presets and composes five/four
ID predicates from three/two source IDs and two mapped snapshot IDs. This is
offline request construction evidence.

Once both owner inputs arrive, validate the original bundle through publisher
prepare, review the five-variable candidate, verify the exact code deployment
and Core pin. Confirm the existing Development WEB4 persistent volume is
mounted at /data before applying the candidate once. Confirm the service and environment in the Railway dashboard because this laptop's volume list command has no explicit project/environment/service selectors. The bootstrap checks
that the parent directory exists, which does not establish persistence.
Observe the resulting settings
deployment before running Verify-HostedQuerySource.py LocalFileOASIS --prepare
with a fresh provider-scoped journal. Check native 3/2, paired 5/4, repeat
reuse, foreign-private 0/public 3 and both exact replicas before publishing
the card. Preserve partial journals and reconcile any uncertain mutation
before continuing. No hosted acceptance or deployment from this checkpoint.

## Recovery laptop replacement source — 4 November 2026

David authorized recreation. The one-shot SDK export created a separate
public teaching cohort with new IDs and passed 1/1 (full 3, subset 2, unchanged
native bytes). The shipping bootstrap parser passed 1/1 against the exact
replacement bytes. The source-bound receipt records both TRX hashes and the
publisher accepted the replacement. Retain replacement-export.trx and
replacement-bootstrap.trx under TestResults/HostedLocalFileSourceCurrent.
Railway still requires private sign-in before preparing the hosted candidate.

Replacement bundle startup passed 1/1 with zero skips. Only the destination
directory was substituted for the isolated Windows check. The actual startup
service installed the unchanged bundle and the public profile activated. The
owned directory was removed. No hosted mount or HTTP acceptance is implied.
TRX SHA256 57143976524E7A671A1CA7860CB4E9BEB624CB9EBFC5C17949B3E506F54D14C4.

### Release scope review

Development b718f2879 is an ancestor of task head 1fe05dbae at this checkpoint.
The task branch also contains the Tableland adapter change af2384d6f. Keep
that independent change outside the LocalFile deployment batch. LocalFile
and Hyperlane share the Core pin. Hyperlane stays disabled until its own
explicit settings and acceptance. Validate the selected parent gitlink and
Railway manifest together and recheck current Development before publishing.

Candidate c64adf4748856b0acaae02e24a7f8edd16d2a139 is published on
codex/cosmic-localfile-release-candidate. The candidate retains the LocalFile
publisher, exact-release guard, hosted verifier, shared Railway CLI resolution and shipping bootstrap. Its
WEB4 API tree, Core pin and Docker definitions match the tested task head.
Development still points at b718f2879. Railway sign-in, mount confirmation,
candidate review and live acceptance remain open.

### Recovery laptop preflight — 4 November 2026

The remote Development head is still b718f287933b7e79e862edbcf7d3bc13c37ba559.
The published candidate c64adf4748856b0acaae02e24a7f8edd16d2a139 is a
fast-forward from that head. The candidate manifest and Core gitlink both
resolve to a9a238371c385137c0464ff259b8117d3d540345. The candidate delta
includes disabled Hyperlane code and shared rollout tooling. Tableland and
Turso provider changes are outside the delta.

The retained replacement-export.trx SHA256 is
D546EF5E2C18F0E63CCB4E1CB0B7117752CCDFA5D2D3AD9D92CBB0A75542C25B.
The retained replacement-bootstrap.trx SHA256 is
1DAEBB87626CE25840CEAAC219CAAAAEE654D7C8B118555BF830777F43505684.
The startup receipt matches the SHA256 above. The Railway account check
returned Unauthorized. Persistent mount and deployment identity still
require authenticated inspection before any settings mutation.

The publisher read the actual retained replacement files and accepted both
source-bound qualification receipts without accessing Railway. The LocalFile
publisher, shared configuration and release guard, and publisher test file
match the published candidate byte-for-byte. All eight publisher tests passed,
including the release guard before mutation and protection of existing settings.

The live Development examplesVersion=2 provider catalogue returned 60 rows
with the retained rehearsal visitor. MongoDB was available with federation.
LocalFile and Hyperlane were absent. The existing visitor session was renewed
through its journaled refresh. No new visitor or provider snapshot was created.

The GitHub WEB4 image workflow builds and publishes an image but does not
inspect the Railway Development mount. The gated Railway release workflow
runs on master and targets staging and production. Neither satisfies the
Development WEB4 mount gate. The published candidate contains gitlinks for
all three private manifest pins (API Core, ONODE Core and WEB6). Use the
candidate pins for deployment review because the task branch has newer pins.

### Readiness before starting a hosted cohort

After the exact settings deployment succeeds, run
`python Scripts/Sandbox/Verify-HostedQuerySource.py LocalFileOASIS --readiness`.
This reuses the retained rehearsal visitor and requires both native and both
paired presets to be available. The check passed for existing ClickHouse and
reported LocalFile unavailable before rollout. Start the LocalFile
`--prepare` flow only after its readiness check passes. Discovery readiness
does not replace the native, federation, privacy or replica checks.

### Apply from the reviewed release checkout

The publisher checks both local HEAD and Railway's latest Development WEB4
deployment against the full reviewed commit. Running apply from the advancing
COSMIC task branch will fail even after the candidate is live. Use the existing
checkout at the exact reviewed release after coordinating its switch. Preserve
unrelated changes and verify the manifest and gitlinks before publishing.

For the currently published candidate, the PowerShell commands below apply
only after confirming the existing /data volume and a SUCCESS deployment of
the same commit. If Development advances, review the new candidate and use
its exact SHA instead.

```powershell
git rev-parse HEAD
$env:COSMIC_CQL_EXPECTED_DEPLOYMENT_COMMIT = 'c64adf4748856b0acaae02e24a7f8edd16d2a139'
python Scripts/Sandbox/Publish-HostedLocalFileConfiguration.py
python Scripts/Sandbox/Publish-HostedLocalFileConfiguration.py apply
```

Review prepared-review.json before apply. If the protected preparation differs
from current service settings or the latest deployment differs from the reviewed
SHA, reconcile that state before continuing. After applying, observe the settings
deployment and run readiness before preparing the visitor cohort.

Offline candidate assembly passed using the actual retained qualified bundle
and synthetic enabled-sandbox settings with an existing zero-cost IPFS tariff.
The publisher assembled exactly five settings, four presets and an 8,159-byte
bootstrap for the three retained rows. All eight publisher tests passed again.
Actual Development settings compatibility still requires authenticated prepare.
