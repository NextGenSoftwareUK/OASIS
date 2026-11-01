# Starknet public source qualification

Updated 1 November 2026. Endpoint qualification red; no CQL/API/hosted grant or count increase.

## Observed result

The operator advertises `https://starknet-rpc.publicnode.com` as its Mainnet RPC. One anonymous read-only `starknet_specVersion` request returned HTTP403 before chain identity, accepted-block discovery or any header read. No automatic retry/alternate endpoint, API key, SDK install, wallet/account or sourceOwner/catalogue creation. The reason for403was not established; do not infer an archive requirement, paid-access requirement or a broken canonical SDK from this observation.

Raw `TestResults/StarknetPublicSourceCandidateCurrent/shape-readiness.json`, 168bytes, SHA256 `2e842dafa732cf37e4b6e06dcec295c17cad0a89c62dc90f14aa97ae5baaa944`. It records failed qualification, HTTP403, zero records and zero mutations. No native CQL tests or browser proof were run.

Existing Starknet provider uses the same advertised Mainnet default, but its signed SDK paths were not executed in this probe. Preserve this scope distinction in the full provider report.

## Next safe work

Continue the exact Elrond deployment/hosted gates first. Before another Starknet acceptance attempt, qualify a currently documented suitable free endpoint/explicit access contract; do not loop on the same403or install a node/runtime. A future source needs exact JSON-RPC envelope/chain checks, bounded complete native key selections, correct field-element/numeric domains, explicit API-reported block status, and tests before any CQL/hosted count grant. Do not imply independent settlement, signature or consensus verification.

This is agent-owned research; no new David action/account request. Browser bridge reset also remains Transport closed; no Windows control resumed.

Primary contracts: [operator endpoint listing](https://starknet.publicnode.com/), [official Starknet OpenRPC specification](https://github.com/starkware-libs/starknet-specs/blob/master/api/starknet_api_openrpc.json).

Denial diagnostic: HTTP403, servercloudflare, JSON response, cloudflare/access-denied body indicators, no retry-after/proxy-block-reason header. Policy cause remains unverified. Raw denial-diagnostic.json 574bytes, SHA256 `ca81564db2db3d33c317cfd20f9196abbaeeefccfb39adbe643ebd0cb155bbbe`. No acceptance retry or endpoint/config migration.
