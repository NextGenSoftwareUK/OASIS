# Celestia public CQL source — interface review

2 November 2026. Red/unadmitted. No native qualification, CQL implementation,
source owner, hosted admission, public card or browser proof is established.
Provider totals do not change.

## Current source and contract

Existing `Providers/Network/NextGenSoftware.OASIS.API.Providers.CelestiaOASIS/CelestiaOASIS.cs`
uses a default localhost bridge at port26659, optional bearer authentication,
and activation through GET `/header/local`. It submits/retrieves DA blobs.
This is not an existing anonymous hosted query source. Legacy code is unchanged;
its broader blob-storage correctness is outside this read-profile milestone.

[Official Mainnet Beta documentation](https://docs.celestia.org/operate/networks/mainnet-beta/)
now documents `https://public-endpoint.celestia-mainnet.quiknode.pro` for
HTTP RPC/REST and DA JSON-RPC development/testing. It explicitly provides no
production SLA. The interface review does not prove live availability.
[Official node API design](https://github.com/celestiaorg/celestia-node/blob/main/docs/adr/adr-009-public-api.md)
distinguishes extended-header reads by height from blob retrieval and sampling.

## Proposed next red scope

After the current Chainflip release gate, qualify a bounded anonymous
extended-header query over three explicit mainnet heights, then a filtered pair.
First verify the current documented RPC method, network identifier, exact integer
height, header identity and response bounds using the shipping guarded transport.
Record an exclusive qualification intent before any native requests and retain
raw responses only under the worktree TestResults folder. Do not use paid keys,
submit blobs, run a node or change endpoints automatically after failure.

Only after native qualification: implement a separate read-only CQL source with
explicit height equality/membership, bounded complete selection before paging,
consistent identity/provenance, cancellation and request/response budgets. Include
controlled malformed/network/unknown-height failures, an actual spaced/commented
C# walkthrough, HTTP admission and one Mongo federation preset. Prove the original
cohort, privacy, reuse and exact replicas before a public card.

A returned extended header does not by itself prove independently verified
consensus, blob inclusion, availability sampling or successful blob writes. Any
such guarantees need their own implementation and proving tests. No new runtime,
download, environment, test data or account was introduced by this review.
