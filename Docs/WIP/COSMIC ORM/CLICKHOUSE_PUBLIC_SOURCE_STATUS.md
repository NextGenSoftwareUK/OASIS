# ClickHouse public source qualification

Updated2November2026. No CQL/native-write/hosted/browser acceptance yet.

## Free public access

[Official playground documentation](https://github.com/ClickHouse/clickhouse-docs/blob/89036285e33b88b424ebc397cce3cb70e9c2b5a9/docs/getting-started/playground.md)
publishes HTTPS play.clickhouse.com, userexplorer/play, empty password, read-only
SQL and usage quotas. No new account/server/install is needed. This is a public
operator dataset, separate from owning/saving COSMIC holons in ClickHouse.

## Original qualification and preserved failure

Probe-ClickHousePublicReadiness.py has7controlled tests passing. First native
identity query returnedHTTP200 and verified explorer/read-only mode. The selected
stackoverflow.posts schema query returnedHTTP200 with no matching columns, so the
qualifier stopped before either post query. Original qualification remains failed;
never replay it or relabel it successful. Native writes0.

A separate explicit public catalogue discovery returnedHTTP200 with at most64
system.tables names, not user content. It includes blogs.countries and
blogs.country_codes, as well as default.hackernews and other public datasets.
The original absent-table gate is preserved; choosing an actual catalogue table
requires a new source-specific qualification, not a silent producer fallback.

## Durable evidence and next step

Original intents/reports/compressed raw bodies/SHA256manifest:
LocalState/COSMIC/ClickHousePublicQualificationCurrent.
Raw results: TestResults/ClickHousePublicSourceCurrent.

Next inspect the exact blogs.countries schema using a bounded metadata query,
then qualify three existing country keys and a two-key filtered subset. Keep
SQL templates fixed/parameterized, bound reads/time/body size, preserve Unicode
and integer precision, reject native errors/partial pages. Add a separate
read-only CQL profile only after that source contract passes. Legacy provider
implementation remains unchanged/docs-only. No CQL/provider count increase.

## Countries source qualified - 2 November 2026

Exactblogs.countries schema: nameString, coordinatesMultiPolygon. New source-specific original3/2queries pass forFrance/Germany/Italy thenFrance/Germany, identical subset projections. Polygon counts21/22/29 remain canonical UInt64decimalstrings; no raw geometry or personal/contentpayload copied.4newcontrolled tests pass, including UInt64max/invalidnumeric/duplicate/oversize/keyset cases. The previous absentStackOverflowtable qualification stays failed and is not replayed. No writes/account/install. Durable countriesintent/report/compressed originals/SHAmanifest retained beside original failures. No CQL/provider count increase yet.

countries-schema.json:374bytes SHA256 `61e838f12ca06ff1d444eee333a46068c009d6d67bb2ff7905cef68b73f6600c`.

countries-1.json:434bytes SHA256 `8e5bd2001beeeaf381110515db57c752849364f8eacc978a38355a8ad3bbd473`.

countries-2.json:382bytes SHA256 `8fd47281ba99e60c6f0d39f02c32c451a34f8072f90e8276366e262a1bd0e613`.

Exact next step: implement explicit read-only ClickHouse country metadata source, configurednameeq/IN+paging using fixed parameterizedSQL and guardedHTTP; reject writes/nativeerrors/incompleteselections before publication. Then manager/CLI/localHTTP+Mongo gates, tariff/hosted integration and sandbox card. Keep legacy ADO/storage paths unchanged; no fullSQLparity claim.
