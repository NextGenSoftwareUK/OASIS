# Appwrite DocumentsDB CQL MVP

The candidate uses the current Appwrite JSON query envelope for existing
storage reads and the exact-ID CQL profile. It sends typed equality, limit
and offset queries through the documented `queries[]` parameter.
[Appwrite DocumentsDB query guide](https://appwrite.io/docs/products/databases/documentsdb/queries)
[Official SDK query serializer](https://github.com/appwrite/sdk-for-node/blob/main/src/query.ts).

The query-only source reads one reviewed project's `oasis_holons`
collection on the current `/documentsdb/` route over HTTPS. It requests
an exact `oasis_id` and checks that the
response proves a unique, complete result. This permits existing documents
whose physical Appwrite IDs differ from their OASIS IDs. The shared profile
checks canonical payload identity and public visibility before paging.

Five CQL checks and 14 storage regressions passed with zero skips at Core
`fdb333a34365a9c6b93f58825c137e2ca3560122`. The TRX SHA-256 is
`61a80e96e1414a40c2e970191f92c351987d203e8f1aecaa1259099e6d18d7cb`.
Native proof requires an existing dedicated Appwrite project, a unique
`oasis_id` index on the teaching collection, a reviewed public fixture and
the read scope for that project's DocumentsDB route. The current scope is
`documentsdb.documents.read` in the Appwrite project documentation.
No hosted acceptance has run. LocalFile remains first for the live rollout.
[Appwrite API key scopes](https://appwrite.io/docs/partners/project/api-keys#scopes).

The native verifier compiled with zero errors at the same Core pin. Its
build log SHA-256 is `a13b56b8af9364724f6854a64732b88efbe57609d3b041e4baf3a641c267caac`.
The verifier checks the exact document identities and deletion
state, runs CQL 3/2/0, and compares native payload hashes before and after.
It also checks that the descriptor stayed identical. Its build can run
without a token. The credentialed run requires a reviewed descriptor and
the protected `CQL_APPWRITE_QUERY_READER_TOKEN` setting.
[Owner actions](../Release%20and%20Status/ACTIONS-FOR-DAVID.md#appwrite-documentsdb-teaching-project-access---2026-11-06).
