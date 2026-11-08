# SubQuery public-source qualification

7 November 2026 on the recovery laptop (8 November UTC). No new CQL or hosted acceptance claim.

The existing provider exposes a GraphQL HTTP client. Its activation/default
schema and legacy storage methods have not been certified by this probe.

The official [Polkadot manifest example](https://www.subquery.network/doc/indexer/build/manifest/chain-specific/polkadot.html)
publishes `https://api.subquery.network/sq/subquery/polkadot-dictionary` as an
optional dictionary. The official [query-service guide](https://academy.subquery.network/doc/indexer/run_publish/query/query.html)
distinguishes indexed-project GraphQL access from the chain RPC used by an indexer.
An RPC response therefore does not qualify this GraphQL source.

One bounded anonymous POST with the read-only query
`{ _metadata { chain lastProcessedHeight lastProcessedTimestamp } }` returned
HTTP404,95bytes, SHA256
`7421f2d09e7164dc7f16d5204bdf1fb161487e11baa2c75d09467bfb6ca3f26c`.
Redirects were disabled, timeout20seconds, response budget16KiB. No automatic
retry, alternate endpoint, API key, node install or database was used.
The result establishes failure of this particular documented example endpoint;
it does not establish that every SubQuery deployment is unavailable.

Original intent/public outcome are retained under
`LocalState/COSMIC/RecoveryLaptopResume20261107/subquery-dictionary-readiness.*.json`.
They prevent accidental repetition of this same readiness attempt.

Next qualify a currently published indexed-project endpoint and its real schema
and chain identity before implementing/admitting a bounded CQL reader. Native
manager, WEB4, federation, privacy, snapshot/replica and browser gates remain open.
No configured provider endpoint was changed and no owner action was requested.

[Provider inventory](PROVIDER_CQL_AND_SANDBOX_STATUS.md) · [Current status](CURRENT_STATUS.md).
