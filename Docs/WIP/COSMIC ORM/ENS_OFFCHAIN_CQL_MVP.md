# ENS Offchain CQL MVP

The source reads a reviewed catalogue of canonical ENS names from the
public Web3.bio index. It projects only the name, the observed Ethereum
address and provenance. Stable logical Holon IDs come from the source
owner, provider, index namespace and ENS name. The read profile has no
source writes or scans. It preserves the distinction between Web3.bio
index observations and direct ENS resolver evidence.

Five controlled tests and one real Web3.bio CQL test passed with zero
skips at Core `fb544f634f9daef20d028c76fec58eb8b9da21bf`. The native
test verified three public names and a two-name subset. Its TRX SHA-256 is
`e448108a0509e20be0cf37b34946c7d35eb3f13ae720e01fdb6f5d913f31c8df`.
The controlled tests reject foreign, malformed and out-of-catalogue
identities. The opt-in WEB4 registration takes the protected
`CQL_ENS_OFFCHAIN_QUERY_SOURCE_JSON` with the owner, names and probe.
The reviewed publisher derives the same logical IDs as the .NET mapper.
Nine WEB4 checks and nine Turso regressions passed with zero skips at
Core `472018f5c5df368b14ce4541045cbb6c0b95c790`. The admission TRX SHA-256
is `bfad70cf91531abe7e00b2cb2f1797cc4280931cbd2d7ecb104506c09d33b9e7`.
All 69 offline hosted tooling tests passed. Live federation remains open.

[Web3.bio Profile API](https://api.web3.bio/).

## Local WEB4 federation and upstream quota

On 6 November 2026, the public index returned HTTP 429 on the final
foreign-actor public federation check. The outbound status sequence was
200, 200, 429, then WEB4 HTTP 503 with `CQL_QUERY_FAILED`. Earlier native
and paired queries in that diagnostic run completed. The failure returned
no partial query result. Two previously acknowledged version-one Mongo
copies were read and verified without replaying their writes. Their
original receipts remain available for authenticated continuation.
The diagnostic TRX SHA-256 is
`341985eba4b1c484947e2039ae36a6a0983bfa93c893ce7f9a5e871b4eefb92b`.

Use the separate protected `CQL_ENS_OFFCHAIN_QUERY_API_KEY` setting for
the hosted profile. The adapter sends the key as `X-API-KEY: Bearer ...`
only on the fixed Web3.bio HTTPS origin, with redirects disabled. The
protected configuration preparation checks the key before admitting the
profile. The catalogue and visitor-facing presets carry no secret.
Authenticated native and live WEB4 acceptance remain open.
[Web3.bio authentication](https://api.web3.bio/#api-key-and-authentication).

Seven controlled provider tests, ten WEB4 registration tests and all
34 protected exact-key configuration tests passed. Both .NET suites ran
at Core `c2a8b2303264bd159ac55d71b684c712647c6d3b`. Provider TRX SHA-256
`3498d481ff7e0618f626df777273ed16cb03feafec082997c4991ae9345380b5`
and WEB4 TRX SHA-256
`e569b99ce5dd94611cf5fe858167c7a213e6849ad896c7980daf66283ffa4343`.
