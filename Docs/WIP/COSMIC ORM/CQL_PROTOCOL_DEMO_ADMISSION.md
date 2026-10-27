# Protocol demo readiness and next implementation

27 October 2026. These are admission findings, not CQL certification.
Priority remains cloud, chain, IPFS, SOLID, ActivityPub; unavailable environments
must not cause silent replacement or stop independent implementation.

| Provider | Current source/environment evidence | CQL implication |
|---|---|---|
| IPFS | Fresh authorized reusable offline environment. Native storage52/52, controlled191pass and native CQL/HolonManager/LINQ/C# walkthrough1pass. | Current-CID row profile proven; WEB4 HTTP and rich indexed/general demo remain open. See [IPFS admission](CQL_IPFS_DEMO_ADMISSION.md). |
| SOLID | Retained pinned container reused with isolated fresh demoPod/client/key. Native discovery/DPoP read and storage52/52 pass after missing deletecount rootfix;362controlledpass. | Original fixture cleanup independently verified without replay. Native storage fixtures cleaned, container stopped. SOLID record-point implementation passes384controls/22newcases. Native CQL/HolonManager/LINQ/C# sample1pass, denial/cleanup verified. WEB4/interactive demo and rich index remain open. See [profile](CQL_SOLID_NATIVE_PROFILE.md). |
| ActivityPub | Published matrix records native capability-v1 Social/Network5/5 and291 controlled regressions. Source uses the Mastodon HTTP API to project accounts/statuses. | Public read-only capability offers an independent implementation path. It does not establish generic ActivityPub storage, arbitrary LINQ or native CQL enrollment. |

Only exact configuration-path existence and the SOLID container's image/state
were inspected. No credential contents, keys, private Pod resources, daemon
starts, native reruns, downloads or replacement environments were involved.
Historical finding: no known retained configuration. User later authorized fresh isolated environments; see current table and resolution below.

## SOLID query boundary

`SOLIDOASIS.RecordHttp.cs` performs a deterministic resource GET for a record ID,
then canonical encrypted RDF decoding. Collection reads enumerate resources.
`SOLIDOASIS.Queries.cs` applies metadata selectors to those decoded collections.
That is useful storage behavior, but is not evidence of server-side metadata
querying, SPARQL availability or CQL predicate pushdown on a Pod.

An initial native point-read profile is possible only if it admits its exact ID
shape, preserves cancellation/bounds/access rules and rejects every unsupported
shape before resource I/O. Richer metadata/range/ordering queries need an explicit
server query extension or separately admitted index; do not assume every Pod
supports a query service. Hosted identity/access/native gates remain open.

## ActivityPub implementation boundary

`ActivityPubOASIS.StatusReads.cs` owns native Mastodon status reads and projection.
The native status ID is a provider key; the projected OASIS GUID is deliberately
not reversible. The same source owner validates returned native identity and
projects author, original object URI, content and visibility.

Consequently an initial CQL native status-point profile must select the **native
status key**, not reverse/hash-guess a projection GUID. It must expose this key
as an explicit queryable source field, admit only the actual supported predicate
shape, and preserve the existing projection owner. Unsupported predicates must
reject before HTTP. This is a Mastodon API profile, not proof that every
ActivityPub server implements that API.

Required first tests: exact native identity, missing/private/denied status,
malformed source evidence, wrong provider, unsupported query with zero HTTP,
cancellation, bounded body/result size and no automatic activation/submission.
Then prove an owned or existing public status through actual GETs, with accurate
instance/object provenance. No posting, federation messages or new account is
authorized by this read-only demo step.

Projection/scalar/timeline search and complete common-demo shapes remain separate
red gates. Do not mark a protocol's full demo green after a point-read milestone.
Logical source and execution profile must remain explicit throughout WEB4.

## Next action

The exact native status-key profile is implemented and native-proven:311 provider
controls,1,964 CQL regressions and one native manager/LINQ/sample gate pass.
See [profile guide](CQL_ACTIVITYPUB_NATIVE_PROFILE.md). WEB4 point row-route code
now passes718 affected controls and a native HTTP gate. Next
interactive-demo enrollment; broader queries remain red.
Do not rerun its already-green social baseline without a reproduced bug.
Continue SOLID exact fixture reconciliation and native storage verification; fresh environments are now available.

## Fresh environment authorization and resolution

User subsequently explicitly authorized creating missing environments. Cached
Kubo and retained CSS container/volume were reused to create a new IPFS repository
and isolated SOLID demo account/Pod/client/key. Native offline IPFS readiness and
SOLID issuer/DPoP ACL read now pass; both services stopped cleanly. See
[environment commands/evidence](CQL_DEMO_ENVIRONMENTS.md). The earlier missing
configuration findings remain historical; current baseline/CQL verification is
next, with no claim of recovering original data. Resume IPFS first, then SOLID.

## SOLID storage admission milestone

The shared native suite now takes its Pod namespace from the persistent demo
manifest and validates the protected identity WebID against that root. Original
hardcoded `oasis-auth-local` and cleanup-prone credential paths are not used by
this gate. Ownership/dispatch intent is persisted before writes in
`LocalState/COSMIC/SolidCurrent/core-{async|sync}.pending.json`.

Both modes reached18checks (36/52) and failed the first holon-delete count gate.
Conditional DELETE confirms source absence but omitted DeletedCount; four
sync/async GUID/key controls reproduce count0 instead of1. Fix sets count1 only
after successful conditional delete plus absence verification. Five failed-delete
controls assert no deletion acknowledgement/count. Full controlled suite362pass,
zero skips. Native original deletion is not replayed. Container stopped normally;
full native baseline and CQL remain red.

Commands from the cosmic worktree (do not replay while original journals exist):

```powershell
& Scripts/TestHosts/Invoke-WithProviderBuildLease.ps1 -Owner cosmic-solid-core -Operation {
    & Scripts/TestHosts/Run-ReusableSolidReadiness.ps1 -CoreStorage -RequireCleanShutdown
}
```

| Evidence relative to worktree | SHA256 |
|---|---|
| TestResults/SolidCurrent/Archive/delete-count-original-solid-core-current.trx | BB0ED61B2586DF9A5498307D03B2E1459A93CEA8A7E192104CD5E7E6E390C92A |
| TestResults/SolidCurrent/delete-before.trx | B5C8E9E2A147721FBFBBA75DBB00FF6E9D5EF362C6A9758D5469641AC7AFADD9 |
| TestResults/SolidCurrent/delete-fixed.trx | D8B6CC4E7EB8FF029D6592116F9C0EC83D40E59FE5B9460BA33C59B1067B6AE2 |
| TestResults/SolidCurrent/Archive/delete-count-original-solid-core-lifecycle.json | C5DF48CCE542DA0B45E37410F7926A0EF3AD0D023046FAC7CBAAA849C2DF468E |

## Verified SOLID native storage baseline

Exact original reconciliation1nativepass: both childDELETEs independently absent;
remaining avatar/detail and four holon records per mode independently owned and
conditionally removed. Durable `core-cleanup-complete.json` records two intents
and10cleanup outcomes; eight exact evidence/journal files hash-archived. No write
replay. Current baseline26checks per mode,52/52, Succeeded/FixtureCleanupComplete
true, zero skips. Dispatch journals cleared only after verified full cleanup.
Container stopped normally; reusable Pod/key/client/volume retained. CQL remains
unenrolled, not blocked on configuration.

| Evidence relative to worktree | SHA256 |
|---|---|
| TestResults/SolidCurrent/solid-core-current.trx | 5A013BEF2164228027CCD61763D08D2A67131BE01C620AD42AECACAD73ACF439 |
| TestResults/SolidCurrent/solid-core-cleanup.trx | 47A42DA11C7D7D1B8C51CD5E3B4115C7B03134F93E8A802142EB0D6FA2FAFA85 |
| TestResults/SolidCurrent/solid-core-lifecycle.json | 198D6BCE0E345B149BFC24E71B21273E310BCE638AFD38946D0AAD5848042780 |
| LocalState/COSMIC/SolidCurrent/core-cleanup-complete.json | 21BFB013F7DFFDCE874A374FC3F3A3E2EF9829F495E794925566D800DD5EF7DE |

## WEB4 IPFS / SOLID row admission implementation

Compiler SupportedReadProfile now admits explicit IPFSOASIS and SOLIDOASIS on
POST `api/data/query-cql`. Their exact native point capabilities remain the
execution authority. Explicit OrderBy None is required; Descending rejects.
Default Name ordering is not silently stripped. Federation/composition providers
and CRUD tariff enrollment retain the original three-database scope.

Server query tariffs independently allow seven unsigned read providers, with
unique entries, bounded catalogue versions, nonnegative costs and explicit
server permission. Signed chains remain rejected. Prices and actor identities
are not accepted from query JSON. Missing/inactive host registrations do not
trigger activation, failover or another source.

Three new controls genuinely fail before enrollment;721 affected WEB4/tariff
regressions pass after the fix. Core7beacc2 pushed/verified; parent gitlink and
Docker manifest advance together. Native HTTP/receipt acceptance and interactive
host wiring for these two profiles remain open; no deployment is claimed.

Example request shape (requires activated configured provider, authoritative
server read policy and configured tariff):

```json
{
  "provider": "SOLIDOASIS",
  "orderBy": "None",
  "take": 1,
  "includeMetadata": true,
  "filters": [
    {
      "metadataPath": ["SolidRecordId"],
      "scalar": "String",
      "operator": "Equal",
      "value": "a1234567-b123-4123-8123-123456789abc"
    }
  ]
}
```

For IPFS use provider IPFSOASIS, metadataPath IPFSRecordCid and the acknowledged
current record CID. Omit caller ActorId/cost fields; unknown wire fields reject.
Scalar/projection/graph/arbitrary filters remain unsupported for both adapters.

| Evidence relative to worktree | SHA256 |
|---|---|
| TestResults/ProtocolCqlCurrent/web4-before.trx | 0AC4E701C70CC8C698ADC45FC9E94CE331DDE042C87A71734939773DD86675AB |
| TestResults/ProtocolCqlCurrent/web4-fixed.trx | 3B931C119A8534C053C703D7B890685A78DAB61F9C5FE8D44B5BFDB8AE278A74 |

## Native WEB4 row / receipt acceptance milestone

Both new native gates pass1/1, no skips: actual offline Kubo and retained private
CSS Pod. Each uses the production CqlV1Controller, resource filter, compiler and
endpoint sources through a real loopback Kestrel listener. Shared test helper
`Providers/Shared/TestSupport/ProtocolPointWeb4Acceptance.cs` checks six requests:
anonymous401, forged actor400, unsupported sort/scalar/projection400, valid rows200.
The row carries the native record CID or canonical Pod resource URI. Exactly one
provider query receipt is appended, unit1, fixture tariff0.001USD/catalogue
protocol-test-v1. Other requests produce no provider query receipts.

Identity/reservation/read catalogue/outbox are controlled test fixtures; this does
not certify production JWT, durable settlement or hosted deployment. Source adapters
are real native backends. Exact owned fixtures are verified/cleaned, durable
cql-web4-complete closures retained, Kestrel disposed and backend services stopped.
No native write replay. Core1db9017 adds only friend access for the two native test
assemblies; no public API expansion. Gitlink and Docker pin advance jointly.

Next: interactive/demo-host enrollment for the proven point profiles, rich query
index designs and the remaining common-demo/production acceptance gates.

| Evidence relative to worktree | SHA256 |
|---|---|
| TestResults/IPFSCurrent/ipfs-cql-web4-native.trx | 5453B1199EE6E53AE154BC7C1B7637E8A85986444D69B6442D09D5FF58E13009 |
| TestResults/SolidCurrent/solid-cql-web4-native.trx | 4EDCCD7E24F5BFB44AC02F81F1C017133AC82899DCC2E1AD8F88EB6112593F6E |
| TestResults/SolidCurrent/solid-cql-web4-lifecycle.json | A9C6CD6BFC17F30DA6380A0802E84A374173C1F52163FC116EE9D7C51A85DD29 |
