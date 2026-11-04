# Tableland public-source qualification

Updated 2 November 2026. Red: public access/version not qualified; no CQL or
hosted acceptance claim. Existing legacy implementation is unchanged.

## Official contract inspected

- Official docs pin: `cecaa6d4ec16bdc553ad3e9673e6490caf37b5e3` in
  `tablelandnetwork/docs`. This is historical documentation, not proof of current
  service availability.
- Inspected gateway query, formatting and API quickstart documents: three files,
  16,962 bytes plus SHA manifest retained at
  `LocalState/COSMIC/TablelandPublicQualificationCurrent/sdk`.
- Docs describe anonymous read-only `/api/v1/query`, GET/POST, bound `?`
  parameters, object-array results by default, and optional table formatting.
  Mainnet and testnet gateways are separate.
- The documented `healthbot` example contains a single counter row. It does not
  prove the required several-record filtered demo. No artificial row duplication
  or SQL-generated rows count as native records.
- The latest inspected SDK commit identity was resolved as
  `eb10ec9d401592050c55c9761339fb956c4e9b71`; no SDK was installed or claimed tested.

## Original access observation

`Probe-TablelandPublicReadiness.py` sent one GET to the documented mainnet
`https://tableland.network/api/v1/version`, using the default Python transport
identity, no credentials/cookies, no redirect following and no retry. The bound
was 15 seconds and 256 KiB. Four offline contract controls passed first.

The original response was **HTTP 302**, a 143-byte HTML redirect response,
not the documented version JSON. SHA256:
`12755429beb15d5eb57eafa45b8dba326343dd099bf0552038694c3856e8860e`.
The destination header was not retained; its destination and cause are unknown.
This is not evidence that all Tableland endpoints are unavailable or denied.

Original intent, report and compressed response are retained at
`LocalState/COSMIC/TablelandPublicQualificationCurrent/version.*`.
Raw response: `TestResults/TablelandPublicSourceCurrent/version-response.bin`.
No query, table creation, wallet, on-chain transaction, CQL adapter or hosted
configuration was dispatched. Do not replay this observation or follow an
unverified destination as an implicit fallback.

## Remaining acceptance

1. Establish a currently documented operator endpoint and network identity.
2. Identify a real public table containing at least three distinct records,
   its exact schema and stable keys, without credentials or paid hosting.
3. Qualify native full/subset results before implementing bounded metadata
   equality/membership, paging and strict projection through shared CQL.
4. Add controlled adapter tests, actual manager/LINQ/CLI proof, local HTTP/Mongo
   privacy checks, explicit read admission and four hosted presets.
5. Verify original hosted queries, ordinary private-copy saves/replication,
   isolation and repeat preparation before publishing a card.

## Legacy gaps documented only

The existing `TablelandOASIS.LoadHolonAsync(string providerKey, ...)` interpolates
the split table name and row ID into SQL before URL encoding. URL encoding does
not bind SQL values or validate an identifier. Its generic parsing also does not
establish stable holon ownership/identity, child/version semantics or CQL
capabilities. These legacy paths remain unaudited for native conformance and are
not the basis of a new demo acceptance claim. V2 work must enforce its own exact
source and query contract rather than silently routing through this method.

## Recovery laptop bound-query diagnostic

4 November 2026. The provider compiles with validated table identifiers and
a bound Int64 row key. A separate read-only diagnostic used the documented
example table and predicates, with a one-row projection limit. The gateway
returned HTTP 302 to https://textile.io/ with the same body SHA256 as the
earlier version diagnostic. No redirect was followed and no table data was
returned. Keep both observations and seek a supported Tableland query gateway.
Evidence under TestResults/TablelandRecoveryLaptopCurrent/bound-query.*.

[Operator parameter contract](https://blog.textile.io/weeknotes-base-sepolia).

The validator repository still links to the operator gateway documentation,
but that documentation also redirects to the Textile homepage. The validator,
SDK and docs repositories are not marked archived by GitHub. No operator
announcement of a successor public endpoint or a network shutdown was found.
Treat this as a public gateway access gap. The validator repository documents
a separately operated native validator, which would require its own RPC access
and resources before it could qualify as a demo source.

[Validator operator documentation](https://github.com/tablelandnetwork/go-tableland#running-a-validator).
