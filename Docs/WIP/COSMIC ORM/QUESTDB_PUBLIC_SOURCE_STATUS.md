# QuestDB public demo qualification

Updated2November2026. Native/CQL/hosted/browser acceptance not yet run.

## Contract and free access

Official QuestDB docs describe demo.questdb.io as read-only. Retained REST and
demo-schema source at questdb/documentation commit
ead9ae6f8656e45793f018eee243a4b81d7b1895 lives with SHAmanifest under
LocalState/COSMIC/QuestDbPublicQualificationCurrent/sdk.

[REST contract](https://questdb.com/docs/connect/compatibility/rest-api/)
now documents /api/v1/sql/execute and /execute; /exec is deprecated. Choose one
explicit qualified endpoint/version; never silently switch on errors. JSON uses
columns/dataset/count, with schema/type metadata separate from row arrays.

[Demo data schema](https://questdb.com/docs/cookbook/demo-data-schema/)
distinguishes simulated FX data from real crypto trades. Native price columns
are DOUBLE, so do not claim exact financial decimal arithmetic or reinterpret
binary floating values as lossless money. The initial candidate can use native
symbol metadata (BTC-USDT/ETH-USDT/SOL-USDT), excluding price/amount data entirely.

## Next gate

Qualify the documented endpoint with bounded fixed read-only SQL: native build
identity and three/two selected existing symbols. Verify exact response schema,
unique keys/counts and error behavior. Do not treat generated long_sequence rows
as native data acceptance. Preserve first failure/denial; no fallback endpoint,
new service, account or installation. No native call or CQL grant yet.

The existing PGWire storage provider is separate and remains unchanged; public
SQL query acceptance would not certify owned holon writes or full ORM parity.
ClickHouse rollout remains immediate priority.
