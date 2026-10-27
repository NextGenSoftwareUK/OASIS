# IPFS demo admission

27 October 2026. Source audit/preflight only; CQL remains unenrolled.

Current: fresh reusable offline IPFS environment created under explicit user
instruction; original configuration absence is historical, not a current blocker.
178 controlled tests pass after save/delete acknowledgement and live parent
membership fixes. Native baseline52/52 passes; The CID point-profile implementation passes controlled tests; native CQL admission remains open.

## Environment and evidence

- Cached Kubo0.43.1 runtime reused; repository and DPAPI-protected key live in
  `LocalState/COSMIC/IPFSCurrent`. No new runtime download.
- Offline daemon uses loopback18547–18549; runner stops it in finally.
- Raw logs/TRX/reports live in worktree `TestResults/`; fixture ownership journals,
  original DNA and exact cleanup closures survive there being cleaned, under
  persistent `LocalState/COSMIC/IPFSCurrent`.
- Historical failed cohorts were independently verified before exact catalogue
  maintenance. Immutable blocks and archived DNA remain; no original save replay.

## Source/query boundary (general-query gap)

`IPFSOASIS.Init.cs: LoadLookupToJson` fetches and decrypts the configured lookup
root CID into a dictionary. `IPFSOASIS.Load.cs: ReadProjectedHolonAsync` then uses
a managed selector and ambiguity check before reading the authenticated blob.
`IPFSOASIS.PrivateStorage.cs` binds encrypted record context to kind/ID and checks
blob identity against the catalogue; it publishes the source CID on loaded data.

These mechanisms support storage lookup. They do not establish native arbitrary
metadata filtering, range queries, ordering, projection or Count/Any over IPFS.
Wrapping them in LINQ would still download/decrypt/filter a whole catalogue.
The demo must either expose a deliberately bounded direct-CID read capability or
select an explicit, separately admitted persistent query index. It cannot claim
server-side IPFS metadata querying from the existing dictionary lookup.

## Required gates

| Gate | Required evidence |
|---|---|
| Environment | Fresh explicitly authorized repository/configuration/key with owned fixture/journal review; no historical-recovery claim |
| Canonical storage | Native source records load with independent CID/blob identity and encrypted catalogue consistency; preserve historical failures |
| Snapshot identity | Immutable catalogue CID anchors the complete indexed source; independently verify it is the configured current catalogue |
| Query execution | Explicit native direct-CID or named persistent-index mode, exact operator capabilities and honest execution-provider identity |
| Source receipts | Catalogue CID, record CID, kind/ID/version/deletion and canonical hash; actor visibility bound before paging |
| Mutation/cleanup | Report catalogue/tombstone/unpin effects accurately; never promise erasure from all IPFS peers |
| C#/HTTP demo | Readable explicit-scope examples, unsupported-query rejection, provenance and owned-fixture cleanup; no hidden full-store filtering |

The current chain checkpoint types use finalized slots/block identities. Do not
invent chain slots or network finality for IPFS. Reuse identity/receipt ownership
where appropriate, but specify CID/catalogue freshness through the actual IPFS
source contract before extending shared execution.

Next: locate an existing retained environment through configuration/inventory.
If none can be recovered, keep IPFS native acceptance red and move to SOLID;
code-level CID/catalogue query design remains possible without starting storage.
No source or demo/parity cell is marked complete by this audit.

## Fresh demo environment (user authorized)

A new owned persistent repo/key now exists, using the cached Kubo0.43.1 runtime.
Create/reuse and offline native identity/version checks pass; daemon stopped.
See [environment report](CQL_DEMO_ENVIRONMENTS.md). The original historical repo
was not recovered. Next native V2 storage acceptance on the new owned fixture,
then the explicit CQL/source/index gates above. No storage/CQL cell is green yet.

## Native acknowledgement defect found and fixed

The first fresh V2 baseline executed2/52 checks: both modes activated, then failed
`avatar-save` at `save acknowledgement/count`. The DNA files already contained
published catalogue CIDs. SaveAvatarAsync and SaveAvatarDetailAsync returned an
entity wrapper without IsSaved/SavedCount, and persistence exceptions escaped
the OASISResult contract. This was a real provider boundary defect, not a reason
to replace the new environment or replay saves.

One shared save-result owner now acknowledges IsSaved/count1 only after the
existing record/catalogue/durable DNA publication completes. Failures preserve
the original exception, including post-blob IPFSRecordPublicationException and
its acknowledged CID; they return no successful result/count.

Twelve actual bundled-SDK controls first failed, then pass: avatar/detail,
sync/async, success, rejected record upload and rejected catalogue upload after
record acknowledgement. Full controlled cohort174pass, zero skips. A separate
native read-only gate1pass confirms both original avatar IDs and usernames from
their original published CIDs, with unchanged DNA file hashes. No Save/Delete/GC
was issued by that inspection; daemon stopped after the gate.

Evidence relative to TestResults/IPFSCurrent:

| Evidence | SHA256 |
|---|---|
| ipfs-core-current.trx (original2/52 baseline, two failed schedules) | 7F2073FD694150195BE1CA89BAC3E1781BDEE71914005C79B531303E528D9846 |
| avatar-ack-before.trx (12 genuine failures) | 7A8CE28D4B1B5ACB74E33136BD0127A328D2735FF7D60C936DC41111FE85F7BB |
| avatar-ack-controlled.trx (174 pass) | CA4C405CE2FBA7B20C4468D64D1CD351446AB145F61C294315CE67E3E54811A5 |
| ipfs-core-inspect.trx (1 native read-only pass) | 3AE333ABB23C532F07B63AE9E9BF5E871CE31368E04A2EC8D2BC56CDBEEA12CA |
| original-save-inspection.json | 844247411279285D6318F6271EBE5BEBFE10160847C073546837E350453116DE |

Compact fixture intent/IDs/deletion/cleanup journals now live in persistent
LocalState/COSMIC/IPFSCurrent/conformance-{mode}.pending.json before dispatch.
Full raw reports remain in TestResults. Unresolved journals/DNA/report guards
prevent a fresh suite from overwriting or replaying the original operations.

Exact next step: verify each original catalogue contains only its known owned
avatar, perform acknowledged exact-ID cleanup, independently verify absence,
preserve original report/DNA hashes, then admit a fresh52-check run. Do not simply
rerun ConformanceOnly while the original fixtures exist. CQL admission remains
red; no52/52 claim is made from the controlled fix or read-only inspection.

## Original closure and graph-count milestone

Both original avatar catalogues were independently verified to contain exactly
their one expected record before cleanup. Exact logical deletions acknowledged;
catalogues are empty and ID reads report RecordNotFound. Original DNA bytes,
historical CIDs, reports and identity journals were preserved by hash-verified
archive moves. The durable closure is
`LocalState/COSMIC/IPFSCurrent/avatar-ack-cleanup.json` (SHA256
35B277B4F408AC9DF41E5A88D36573CB0480B48958C3DEF04142D8C268899F78).
Native cleanup1pass/no skips, TRX `ipfs-core-cleanup.trx`, SHA256
DA00A0A8D35B7934A946AC5B1826461665FE2CE0BDA27B54185C75083FC61E7D.

A fresh cohort on the same repository/key reached18/52: nine checks per mode
pass, including avatar/detail writes and lookups. It then failed graph-save at
save acknowledgement/count. The graph publisher omitted SavedCount, and the
single-holon facade's manual result wrapper lost it. Four stateful bundled-SDK
controls reproduced count2 expected/count0 actual across sync/async single/batch.

The graph publication owner now reports its selected node count only after all
selected publications complete. The single-holon facade uses the existing shared
result-copy owner, preserving counts and error/metadata rather than inventing a
second result contract. Full controlled cohort178pass/no skips.

| Evidence relative to TestResults/IPFSCurrent | SHA256 |
|---|---|
| graph-ack-before.trx (4 genuine failures) | 8A8E5AB78A45AC8A304570F2561D127D08FE87AFE3105C9345789CD54CF6E790 |
| graph-ack-fixed.trx (178 pass) | 0CDAF163D8AB27980E792B4CF02D92455143F41317B4C1C16713DAB1620B292E |
| Archive/graph-ack-original-core.trx (historical18/52 graph failure) | 11CFF4321E23D3C3686B3BAA3BB78B1CE4246812F5DFD501DA792760DD0CA476 |

Both graph catalogues and all four owned records per mode were independently
verified before exact logical index cleanup. Native cleanup1pass, durable closure
`LocalState/COSMIC/IPFSCurrent/graph-ack-cleanup.json`; original DNA, journals and
reports hash-archived with immutable blocks preserved. No graph save replay.

The next full native run reached36/52 (18 checks per mode), stopping at the first
holon deletion acknowledgement. The tombstone publication succeeded but the result
omitted IsDeleted/DeletedCount. Four controlled SDK tests reproduce this failure;
the publication owner now sets deletion acknowledgement/count after durable save.
The full controlled suite passes178, zero skips. Current native fixtures remain
preserved pending independent reconciliation; do not replay the suite or delete.

| Evidence relative to TestResults/IPFSCurrent | SHA256 |
|---|---|
| delete-contract-before.trx (4 reproduced failures) | DAD18F0CBC0A3A653791786BB0A8B194983D5E3F387621B46BD55F1F2D831A96 |
| delete-contract-fixed.trx (178 pass) | E28A4520F37608174A834EED0C68F8284C599D898F7C5563EDAAEDEA1096CD57 |
| ipfs-core-current.trx (native36/52) | 6260487505F769B6AFEC5F00F44E265C7EC33710E7A120D2ECC0DEB3F028F77B |
| ipfs-graph-cleanup.trx (exact graph cleanup1pass) | 589985541C57680D8121F7ECA2112FF6854C830F7D4D3AD873BCFA35B9BCDA35 |

Next: independently reconcile exact latest tombstone fixtures, then full native baseline.
Services are stopped; CQL and full native certification remain red.

## Live membership and lifecycle corrections

Native deletion acknowledgement now passes, exposing deleted child membership in
parent queries. Four controlled tests reproduce the bug; the live parent selector
now excludes tombstones while point/raw collection reads retain them.178 controls
pass. Exact7-record catalogues per mode were independently verified and removed;
`parent-ack-cleanup.json` is closed. No original deletion was replayed.

Avatar deletion had the same missing result acknowledgement; four controlled
SDK tests reproduce it. Its publication owner now sets IsDeleted/DeletedCount
only after the catalogue pointer commits.178 controls pass. Full native rerun
uses fresh fixture identities in the same reusable environment.

## Verified native baseline milestone

Both sync/async reports are complete,26passed each, Succeeded and
FixtureCleanupComplete true.52/52 native storage checks, zero skips.
Pending dispatch journals removed after verified fixture cleanup; daemon stopped.
No new environments/downloads or GC. CQL remains red.

| Evidence relative to worktree | SHA256 |
|---|---|
| TestResults/IPFSCurrent/ipfs-core-current.trx | 2A13A35A8E2CB202CEB6B6EDDAD7B6526B51190386900955846575F45FF1BADD |
| TestResults/IPFSCurrent/parent-contract-before.trx | C04A1F094C778FD0D25FE66DE934A297C3A21C00BF3DE8AC3C6B1CDBA9099FEA |
| TestResults/IPFSCurrent/avatar-delete-before.trx | 70DAC7ABFF9D03229F573F2D5C175695C47A71C90154581DB558831DBCABCC0D |
| TestResults/IPFSCurrent/avatar-delete-fixed.trx | F50742686A31823C096FB98E834506576652C1D4205F4853E98EF5C5C3A7CEA4 |
| TestResults/IPFSCurrent/ipfs-parent-cleanup.trx | 190F4708508A0CF6826455BAE467512CE621AAE733F13B84D05C325CEE625E7C |

## CQL current-CID point profile implementation

`IPFSOASIS.Cql.cs` implements the shared `ICosmicQueryStorageProvider` contract.
The generic HolonManager/planner/executor can discover the provider capability;
this does not enroll IPFS in WEB4 tariffs, federation, scalar or projection APIs.

```csharp
// Explicit IPFS scope; use the CID acknowledged by SaveHolonAsync.
var query = CosmicQuery<Holon>.Create(
    CosmicQueryScope.Named(ProviderType.IPFSOASIS).Result);

query = query.Where(holon =>
    CosmicQueryMetadata.Value<string>(holon, IPFSOASIS.CqlRecordCidKey)
        == acknowledgedRecordCid).Result;

// Production callers bind authoritative read access through HolonManager.
// A direct provider call is an execution primitive, not an authorization grant.
```

Only one exact `IPFSRecordCid` equality is admitted; ordering, graph expansion,
indexed sources and additional business predicates reject before source I/O.
Skip past the point or Take(0) produces an empty page without daemon reads.
The selected configured immutable catalogue is authenticated/validated, then an
exact dictionary CID lookup selects at most one authenticated holon blob. Current
catalogue membership is required: historical or foreign CIDs return no row.
This reads the whole encrypted catalogue (4MiB envelope budget), not an IPFS
server-side metadata scan. A persistent query index is still needed for general
queries. Shared codec expansion is bounded to4MiB; source reads accept cancellation.

Deleted or denied records return no rows. Source errors fail with no partial rows;
no public gateway, provider switching or native write occurs. Returned rows have
no children, canonical source CID and an authoritative synthetic IPFSRecordCid key.

13 new controlled cases cover public/owner/denied/deleted/absent records, invalid
CID, cancellation, inactive provider, unsupported shape, empty window, corrupt
catalogue/blob and oversized stream. Public case also uses the shared executor.
Full provider controlled cohort191pass/0skip. Native Kubo query, HolonManager demo,
WEB4 admission and broader indexed operators remain red.

| Evidence | SHA256 |
|---|---|
| TestResults/IPFSCurrent/cql-controlled.trx (191 pass) | 298087E0DDC55E8BF6B6DE45182D9D7040299CCAA6367F51223B39FBA0BA95C1 |

## Native CID / manager / C# walkthrough milestone

The actual `Examples/CosmicCqlDemo/IpfsPointWalkthrough.cs` is compiled into the
native test. Kubo0.43.1 offline run passes1/1, zero skips. Two synthetic owned
holons are saved once; separate canonical CID read and shared HolonManager/LINQ
sample agree on ID, name and sourceCID; returned children are empty as declared.
The public row is read by a distinct actor under OwnerOrPublic read policy.

All two catalogue entries and their payload owners are verified before exact
fixture maintenance. Empty catalogue and point absence verified; durable
`LocalState/COSMIC/IPFSCurrent/cql-point-complete.json` records completion.
Original immutable blobs remain. Runner terminal, daemon stopped, no GC.

The current-CID native row profile and callable C# sample are proven. WEB4 HTTP
and interactive host enrollment, indexed rich queries and full common demo
acceptance remain open. This is not full IPFS/CQL parity.

| Evidence relative to worktree | SHA256 |
|---|---|
| TestResults/IPFSCurrent/ipfs-cql-native.trx | 45C77791DFA846EBA0852C1FA6E4DD298FF9FE7783EC92DE80EF5E5A882D159A |
| LocalState/COSMIC/IPFSCurrent/cql-point-complete.json | 5AF34CB600CC775619D1D3805B358E6F6AD414B98EF6DD5C36AD22C8D2B9C078 |

WEB4 explicit row-profile/compiler/readtariff enrollment now passes721 affected controls. Native HTTP/receipt acceptance and interactive host remain open. [Request contract/evidence](CQL_PROTOCOL_DEMO_ADMISSION.md#web4-ipfs--solid-row-admission-implementation).
