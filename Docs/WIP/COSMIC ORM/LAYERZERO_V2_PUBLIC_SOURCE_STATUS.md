# LayerZero V2 public source qualification

4 November 2026. LayerZero Scan mainnet latest, limited to three records,
returned HTTP 200 and three distinct canonical 32-byte message GUIDs.
Three independent exact-GUID requests each returned HTTP 200 and one
matching message. The first two GUIDs define the future subset.

The evidence qualifies public indexed message access. CQL, hosted admission
and independent chain settlement remain unverified. The next adapter should
use only the exact GUID route and expose bounded metadata and reported status.

Raw intents, headers and responses are in TestResults/LayerZeroV2PublicSourceCurrent.
Discovery body SHA256 430543612E85E14FF8AD4667CA028218502372203115325A8F063F349B19199C.

[Official message identity and API](https://layerzero.network/blog/cross-chain-compliance)
· [Scan API contract](https://scan.layerzero-api.com/v1/swagger)

The exact GUID HTTP client compiles with the provider, zero build errors.
It validates the response identity and caps response bytes before decoding
endpoint IDs, nonce and Scan-reported status. The client is not yet wired
into CQL. Build log: TestResults/LayerZeroV2PublicSourceCurrent/scan-build.log.
