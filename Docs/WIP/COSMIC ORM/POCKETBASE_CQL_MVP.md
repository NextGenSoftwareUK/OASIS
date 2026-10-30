# PocketBase CQL MVP â€” native verified, paired demo pending

Updated 29 October 2026. Native service and restart are verified. Mongo-federated demo remains **unverified**.
This provider is not included in the green demo count.

## Implemented and tested

- Reuses the shared bounded identity compiler: GUID equality/membership, boolean composition and paging; at most 100 admitted identities.
- One native REST filter over the canonical `oasis_id` projection, with live-record filtering and stable identity ordering. Unsupported predicates fail explicitly.
- Required page metadata, complete response, identity agreement, duplicate/order rejection, cancellation, result budgets and a 4 MiB body limit.
- Generated caller ID/provider key become visible only after matching native acknowledgement. Ambiguous dispatched writes require reconciliation before replay.
- Common canonical reader validates projection identity and tombstones for point, parent, all-record and CQL reads.
- Activation state and authentication cleanup follow provider lifecycle.

## Evidence

Provider and unit project builds pass. 41 targeted tests passed, zero failed/skipped:
[query/save controls](../../../Providers/Cloud/TestProjects/NextGenSoftware.OASIS.API.Providers.PocketBaseOASIS.UnitTests/PocketBaseCqlTests.cs)
and [read contract](../../../Providers/Cloud/TestProjects/NextGenSoftware.OASIS.API.Providers.PocketBaseOASIS.UnitTests/PocketBaseReadContractTests.cs).

Regenerable raw evidence: `C:/Source/OASIS-cosmic/TestResults/PocketBaseCqlCurrent/pocketbase-controls.trx`.
SHA256: `ad0511fdf80e473554aac870278728ee5e8a6f57d216fb8e0585fd02f2badc4b`.
Synthetic HTTP controls do not establish native PocketBase compatibility or durability.

## Native service and restart proof

Pinned official PocketBase **0.40.6**, Linux amd64, loopback `18090`; no hosted claim.
Runtime `/var/tmp/oasis-runtimes/pocketbase-0.40.6` (33,524,502 bytes).
Archive SHA256 `0e1f609158ed945f809f8a02ae012aa0427129c66ebb63c1aee76f2c0309373f`;
binary SHA256 `8e9805c9d92c50a4ff2bce97579d13e2ddfa138cab10cf6f9e8f6db8016ffa30`.
The verified download archive was removed; runtime/license/admission receipt retained.
Use the pinned runtime, not assumptions from moving documentation.
[Official deployment/storage guidance](https://pocketbase.io/docs/going-to-production/).

Original source `/var/tmp/oasis-pocketbase-cql`: four acknowledged saves, one
acknowledged soft delete, three live records. Read scope includes a missing ID;
filtered query returns two live records, paging returns one. Unsupported name
predicate is rejected before execution. Native and restarted tests passed **1+1**,
then the retained fixture was reused with no writes and passed **1+1** again.
The service exited zero after owned SIGINT on each run. No active service remains.

Exact native rows/projections/payload hash before and after restart:
`9b37b1f59edcab41fe0a26a834a3c5cf56e9c8907fda17d09340b19b5ecca771`.
Latest TRX hashes:
- native: `ac006c4e2d74ebb87b99313b7b702edda1848fff2bf7c9ffd71935298133605d`
- restart: `3b8d3018721e5e090258a509a6d4ad273eb2d26bed80d9583e56c6b82545f550`

Evidence: `TestResults/PocketBaseCqlCurrent/{native,restart}.trx`.
[Test source](../../../Providers/Cloud/TestProjects/NextGenSoftware.OASIS.API.Providers.PocketBaseOASIS.IntegrationTests/PocketBaseNativeCqlTests.cs).
Setup and every write are journalled before dispatch; pending outcomes refuse
replay. The runner verifies the retained exact baseline before subsequent reads.
Admin secret is mode0600 outside disposable output; never print it.

After building the integration project with the required lease/worker flags:
```powershell
wsl -d Ubuntu -- python3 /mnt/c/Source/OASIS-cosmic/Scripts/TestHosts/prepare-cosmic-pocketbase-linux.py
wsl -d Ubuntu -- python3 /mnt/c/Source/OASIS-cosmic/Scripts/TestHosts/run-cosmic-pocketbase-linux.py
```

## Exact next step and remaining gaps

Reuse this original service/source for one Mongo-paired C#/HTTP/browser preset.
Do not increment the scoped demo count until the paired preset is verified.

Storage currently uses plaintext canonical JSON. Upsert is read-then-write, not atomic conditional concurrency. Child-depth/history and atomic concurrency remain unverified; avatar/detail save ownership is not certified by the holon tests. No automatic retries, hosted availability, full parity or central-factory claim.
