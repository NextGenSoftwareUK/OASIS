# Safe Transaction Service indexing CQL

On 6 November 2026 the official Safe gateway returned indexing status
for Ethereum Mainnet, Base and Arbitrum One through the documented
`GET /api/v1/about/indexing/` endpoint. The anonymous probes returned
HTTP 200 with 297, 297 and 300 bytes respectively. Each reported an
integer chain head, token and contract indexing positions and Boolean
synchronization flags.

The read-only adapter admits exact IDs for three reviewed network routes.
It projects the chain identity, the three block positions and the three
synchronization flags. It never reads wallet data, proposals, signatures,
transaction history or native writes. Observations are service reports
and can change between reads. Anonymous requests are paced at the
published gateway limit within the process. An optional Safe API key is
sent only to the fixed HTTPS gateway, with redirects, proxies and cookies
disabled.

The real CQL test returned three and two exact rows. The two controlled
checks verified the protected header, fixed routes, exact projection,
rejection of out-of-catalogue IDs and rejection of a complete page after
one required network returned HTTP 429. All three passed at Core
`c2a8b2303264bd159ac55d71b684c712647c6d3b`. TRX SHA-256
`a8311ac1622396a92eaf8d563b6b38cc287ac464106e474e7f3f11d62f2e52ec`.

WEB4 admission, Mongo federation, hosted browser acceptance and the
legacy wallet provider's full interfaces are open.

[Safe Transaction Service](https://docs.safe.global/core-api/api-safe-transaction-service) ·
[Safe authentication and limits](https://docs.safe.global/core-api/how-to-use-api-keys).

## WEB4 read-only admission

Configure the reviewed network catalogue through
`CQL_SAFE_INDEXING_QUERY_SOURCE_JSON` and provide the separate protected
`CQL_SAFE_INDEXING_QUERY_API_KEY`. The profile requires its own read-only
permission and tariff. Wallet storage routing and write tariffs are
excluded. The protected candidate uses the same stable IDs as the adapter.

All 22 WEB4 tests passed, including ten ENS regressions, at Core
`db5de536e5eb690bbae7635208bc7836a3655fa1`. Admission TRX SHA-256
`ea467861182fe77d32f64cbd958ca857b9fb96110d70dbe141618735a34ee6bd`.
All 35 protected exact-key configuration tests passed.
Authenticated live WEB4 remains open after LocalFile.
