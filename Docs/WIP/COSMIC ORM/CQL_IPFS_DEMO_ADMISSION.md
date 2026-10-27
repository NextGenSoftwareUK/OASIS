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
