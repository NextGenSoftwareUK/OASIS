# Protocol demo readiness and next implementation

27 October 2026. These are admission findings, not CQL certification.
Priority remains cloud, chain, IPFS, SOLID, ActivityPub; unavailable environments
must not cause silent replacement or stop independent implementation.

| Provider | Current source/environment evidence | CQL implication |
|---|---|---|
| IPFS | Fresh authorized reusable offline environment. Native storage52/52, controlled191pass and native CQL/HolonManager/LINQ/C# walkthrough1pass. | Current-CID row profile proven; WEB4 HTTP and rich indexed/general demo remain open. See [IPFS admission](CQL_IPFS_DEMO_ADMISSION.md). |
| SOLID | Retained pinned container reused with isolated fresh demoPod/client/key. Native discovery/DPoP read passes; adapted sharedstorage suite36/52 exposes missing DeletedCount on confirmed holonDELETE. Fixed with362controlledpass. | Preserve exact native corependingjournals; independently reconcile original deletion outcomes/remaining owned records, then fresh52check baseline and explicit CQL. |
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
| TestResults/SolidCurrent/solid-core-current.trx | BB0ED61B2586DF9A5498307D03B2E1459A93CEA8A7E192104CD5E7E6E390C92A |
| TestResults/SolidCurrent/delete-before.trx | B5C8E9E2A147721FBFBBA75DBB00FF6E9D5EF362C6A9758D5469641AC7AFADD9 |
| TestResults/SolidCurrent/delete-fixed.trx | D8B6CC4E7EB8FF029D6592116F9C0EC83D40E59FE5B9460BA33C59B1067B6AE2 |
| TestResults/SolidCurrent/solid-core-lifecycle.json | C5DF48CCE542DA0B45E37410F7926A0EF3AD0D023046FAC7CBAAA849C2DF468E |
