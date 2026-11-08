# Infura capability and CQL status

8 November 2026. The higher-priority live demos and their open gates remain in
[CURRENT_STATUS](CURRENT_STATUS.md). Infura is an independent remaining network
provider: current [MetaMask Connect guidance](https://docs.metamask.io/metamask-connect/)
uses Infura for supported EVM networks and relay access. This supports prioritizing
it among remaining RPC services; it is not a global popularity measurement.

## Retired storage contract

[Infura's official IPFS notice](https://support.metamask.io/develop/ipfs) says
uploads/pinning stopped 3 August and all access ended 15 August 2026. The old
provider still constructed that HTTP client, advertised storage and attempted
add/cat/list/unpin, with instructions to fix credentials on failure. This is an
upstream capability retirement, not a transient endpoint/authentication failure.

The provider now advertises network/Blockchain capability, removes the retired
IPFS client and returns `INFURA_IPFS_RETIRED` for its legacy canonical holon
operations, retaining public signatures. No substitute provider, automatic
migration, CID deletion, remote retry or write. Existing identifiers/data remain
intact; a retained copy can be explicitly placed on a supported IPFS provider.
Three actual-provider controls cover advertised capabilities, typed read/write
failures and unchanged retained CID/input. Exact cloud compile/test pending.
WEB4's historical descriptive catalogue still needs the same scope correction
when this provider boundary is verified; this change does not enroll anything.

## Current RPC and remaining demo gates

The existing Ethereum JSON-RPC code is separate from retired storage. It still
has unverified legacy behavior, including an assumed demo URL and missing-value
projection. It is not a qualified native CQL reader. Current [Infura setup](https://docs.infura.io/get-started/infura/)
requires an authorized key on the official Ethereum endpoint; no anonymous/demo
key request was made. No new paid account/hosting or credential values retained.

Next engineering: use the existing bounded EVM observation engine with a distinct
Infura identity, official Ethereum mainnet endpoint, private server-only credential,
chain/genesis and finalized canonical selection; no fallback to another gateway or
legacy IPFS. Verify the credential transport's error/log redaction before native
reads. Then qualify the native scoped source and paired WEB4 flow; no live/browser
or full provider parity claim until those gates pass. Upstream-retired Infura IPFS
storage is inapplicable, distinct from the accepted independent IPFS demo.
