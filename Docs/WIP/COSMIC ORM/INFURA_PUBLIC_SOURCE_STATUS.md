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
failures and unchanged retained CID/input. Exact cloud compile/test is accepted:
[42535999021](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/42535999021),
job128900305746, frozen source122ae35d343d112e21fac550a151114b955e0005/Coree62ec44.
Artifact13639481220 (1638bytes), SHA256
`d3a707b762aa37fb0e4deb03ab6dce3f32de130675b7a36d63018452062cbe24`
verified in memory: exact3/3 tests executed/passed, zero skipped. WEB4's
historical descriptive catalogue now states the same scope. Only description
text changed there; no enrollment, CQL API or route change. No live deployment.

## Current RPC and remaining demo gates

The existing Ethereum JSON-RPC code is separate from retired storage. It still
has unverified legacy behavior, including an assumed demo URL and missing-value
projection. It is not a qualified native CQL reader. Current [Infura setup](https://docs.infura.io/get-started/infura/)
requires an authorized key on the official Ethereum endpoint; no anonymous/demo
key request was made. [Current key policy](https://docs.infura.io/dashboard/how-to/secure-an-api/api-key/)
requires that key in the request path. For a secret-required key, use the exact
[official HTTPS Basic authentication example](https://docs.infura.io/dashboard/how-to/secure-an-api/api-key-secret/#call-apis-using-an-api-key-secret)
and keep both values in private server configuration. No new paid account/hosting or credential values retained.

The scoped Infura reader reuses the bounded EVM engine for chain/genesis checks
and finalized canonical account balances. It requires a server API key and secret
and uses Infura Ethereum mainnet. Cloud run 42536290104 compiled the reader with
the retirement controls.

WEB4 source 78607a087 and Core 64a691b add opt-in registration and query tariff
eligibility. The configuration names are CQL_INFURA_QUERY_SOURCE_OWNER,
CQL_INFURA_API_KEY and CQL_INFURA_API_SECRET. Use protected server settings for
the credentials. The fixed teaching set contains the zero address, burn address
and Ethereum deposit contract. The first registration test failed while populating its configuration fixture.
The fixture now uses an in-memory configuration source. Corrected run
[42536679933](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/42536679933)
includes registration, tariff and credential destination/redaction controls.
The shared owner list tracks access for native verification.

Remaining gates include inspecting that run, verifying credential handling and
native reads with authorized access, qualifying the exact WEB4 package and paired
query flow, then live and browser acceptance. The independent IPFS demo covers
IPFS separately from the retired Infura service.
