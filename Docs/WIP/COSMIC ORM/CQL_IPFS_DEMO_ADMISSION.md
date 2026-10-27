# IPFS demo admission

27 October 2026. Source audit/preflight only; CQL remains unenrolled.

## Current evidence

- Published provider matrix: canonical storage compiled, native suite0/52,
  configuration-blocked because the original repository/key are unavailable.
- Existing runtime `C:/Users/david/.cache/oasis-runtimes/kubo-0.43.1/ipfs.exe`
  exists. No download, daemon start, initialization or key replacement attempted.
- Neither `TestResults/IPFSCurrent` nor `LocalState/COSMIC/IPFSCurrent` in the
  cosmic worktree contains `repo/config` or `storage-key.dpapi`.
  This bounded check does not establish absence from every disk/backup.
- `Run-ReusableIpfsTests.ps1 -EnvironmentPath <existing-path>` can select the
  retained environment. It requires the original config/key before startup and
  validates cached Kubo SHA256. It must not target another checkout's outputs.

## Source/query boundary

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
| Retained environment | Existing repository/configuration and original protected key location, plus owned fixture/journal review; no silent replacement |
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
