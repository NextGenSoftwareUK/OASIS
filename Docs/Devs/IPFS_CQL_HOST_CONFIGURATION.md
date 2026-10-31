# Hosting an IPFS CQL source

CQL uses the configured IPFS provider and its authenticated encrypted catalogue.
The WEB4 host must have a reachable native Kubo RPC service, the catalogue's
original storage key and the complete selected record blocks. A local launcher
or a configured URL alone does not establish a working hosted source.

## Configuration

Configure the existing `OASIS.StorageProviders.IPFSOASIS` DNA section:

```json
{
  "ConnectionString": "http://127.0.0.1:5001",
  "LookUpIPFSAddress": "ACKNOWLEDGED_CATALOGUE_CID",
  "EnableCqlCidKeySets": true
}
```

Supply `OASIS_IPFS_STORAGE_ENCRYPTION_KEY` through the host's secret controls.
It is the Base64 encoding of the existing 32-byte catalogue/record key. Never
replace it to recover an unreadable source, put it in DNA or commit it to docs.
Keep the native administrative RPC on loopback or a trusted private network.

Explicitly enroll IPFS as a query-only source in the storage admission policy
and configure its query tariff. This does not enroll it for API writes,
replication, failover, or default-provider selection. See
[query-only admission](CQL_QUERY_ONLY_ADMISSION.md).

Read-only activation checks the native version with cancellation and a bounded
timeout. It does not initialize a daemon, generate a key, create records, write
a catalogue or select another provider after a failure. Missing native resources
must be provisioned explicitly before enrollment.

## Supported query shape

With membership enabled, one `IPFSRecordCid` string metadata `In` predicate
selects at most eight canonical CIDs. The default remains the existing single-CID
equality profile when membership is disabled. The native adapter authenticates
one immutable catalogue and the selected record envelopes, then applies mandatory
visibility before publishing a bounded page. Unsupported business filters,
ordering and graph requests reject during planning; this is not a general IPFS
search engine or a full-store application scan.

Native rows can participate in the ordinary bounded CQL federation with Mongo.
Its explicit native-key selection and moving-provider-read consistency remain
visible; neither federation nor replication creates a global atomic snapshot.

## Preparing the retained sandbox source

From the existing COSMIC worktree, with its retained offline IPFS repository/key:

```powershell
pwsh -File Scripts/TestHosts/Prepare-HostedIpfsSource.ps1
```

The runner owns the build lease, bounds build workers and uses the original
cached Kubo runtime. It publishes one separate public three-record demo dataset
and verifies three-record and two-record native queries. Repeating it verifies
the acknowledged source rather than saving it again. A pending source write
requires reconciliation; it is never automatically reseeded.

Durable source intent, acknowledgement, DNA and CAR exports live under
`LocalState/COSMIC/HostedIpfsSourceCurrent`; generated logs/TRX belong only in
`TestResults/HostedCqlIntegrationCurrent`. This source is retained product demo
data, not disposable test output. The runner gracefully stops its owned daemon.

Transfer and verify **all four** CAR exports: the catalogue and three records.
The encrypted catalogue contains CID values, not UnixFS links to every record,
so exporting the catalogue alone is insufficient. Preserve the original source
and protected key. After native import, independently verify the catalogue,
selected envelopes and real hosted CQL responses before marking the source ready.
Current deployment evidence and remaining gates are in
[CURRENT_STATUS](../WIP/COSMIC%20ORM/CURRENT_STATUS.md).

Deletion/unpinning cannot promise erasure from every IPFS peer. Use exact owned
fixture maintenance and the resource ledger when retiring this source.

Native runtime reference: [official Kubo distribution and Docker images](https://github.com/ipfs/kubo#docker).


## Optional owned sandbox runtime

The WEB4 image carries the pinned official Kubo0.43.1binary. Set
`OASIS_SANDBOX_IPFS=true` only on a node whose authoritative DNA has
`OASIS.Sandbox.Enabled=true`. Other deployments keep the normal API entrypoint.
No separate sandbox flag or default-provider change grants source access.

`OASIS_SANDBOX_IPFS_BOOTSTRAP_JSON` supplies an operator-prepared, schema1bundle
with `catalogueCid` and `blocks` entries (`cid`, lowercase `sha256`, `carBase64`).
It contains the verified encrypted CAR exports, not the source encryption key.
Keep the bundle out of committed seeds and raw documentation. Deliver the original
key separately through `OASIS_IPFS_STORAGE_ENCRYPTION_KEY` secret configuration.

The runtime owns `/data/COSMIC/SandboxIPFS` on the existing persistent volume.
It writes a durable intent before native initialization/import and acknowledges
only verified native blocks. Repeated starts verify the same acknowledged source;
an existing directory without acknowledgement or a changed source refuses startup.
Do not delete uncertain state or generate a replacement key to bypass this gate.

The node runs offline with RPC bound to127.0.0.1:5001, no gateway/swarm listener.
This is a hosted isolated native Kubo demo, not a public-network availability claim.
The process owner waits for the pinned native version and exact peer identity
before starting WEB4, forwards termination and observes both children. There is
no automatic business save/import retry or replacement storage path.
