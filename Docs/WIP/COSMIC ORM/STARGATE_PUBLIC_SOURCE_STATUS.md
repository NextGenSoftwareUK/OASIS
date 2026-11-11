# Stargate V2 public source qualification

4 November 2026. LayerZero Scan returned three distinct GUIDs for the
documented Stargate V2 Ethereum TokenMessaging contract (endpoint 30101).
Three exact GUID lookups independently returned one matching row each and
confirmed the same destination endpoint and receiver contract.

This qualifies a bounded set of indexed Stargate message observations. It
does not establish per-user transfer amounts, independent settlement, quotes
or bridge execution. Next reuse the Scan client and enforce the Stargate
contract and endpoint on every exact read before mapping a CQL row.

Raw intents, headers and responses: TestResults/StargatePublicSourceCurrent.
Discovery SHA256 66271F55FC628EE5649FD066A25495FE11208CA32409504BB8DA288D646F1CC2.

[Stargate V2 mainnet contracts](https://stargateprotocol.gitbook.io/stargate/v2-developer-docs/technical-reference/mainnet-contracts)
· [Scan API](https://scan.layerzero-api.com/v1/swagger)

## Native CQL verified

Seven controlled cases and one real CQL test passed with zero skips. The
retained catalogue produced exact 3/2 selections. Foreign receiver and
endpoint are rejected before mapping. The first build failed on an enum
name collision, corrected with an explicit alias. Five LayerZero controlled
checks also passed after the shared profile change. WEB4 admission is next.
Stargate native TRX SHA256 16ACFAB425DCB66926712B3157F7C50E913AA20A4257DB723F53F75520553C87.

## WEB4 local HTTP verified

Opt-in admission and four preset plans passed. The isolated WEB4 HTTP run
returned native 3/2, paired 5/4, foreign-private 0/public 3 and nine metering
receipts. The fixture database was removed and the reused replica stopped.
The hosted publisher accepted the source-bound native and local HTTP receipts.
Local HTTP TRX SHA256 4BFC742B896794E502A44F4A5EA908D4B52BB171823CD3F82A3F88E82B55EF64.
Hosted settings, replicas and browser acceptance remain open.

## Hosted HTTP accepted - 7 November2026

Batch settingseaa1b7af-4fb7-4471-86b2-2071db135332 SUCCESS, instance
370c0335-3940-4f48-abef-7314caab801f, source15eb48673/Core85278a4,
image sha256:99940404f16658fab75f7b9a0d85082e688697095e19250fd4b6daef4de3f010.
Full11setting readback, original ACKs and active environment matched. Exact
release/settings stayed unchanged during each first hosted cohort. Native3/2,
Mongo5/4, first2private saves/repeat0new2existing, foreign-private0/public3 and
exactSQLite/Neo4j replicas2/2 passed. Original owner/foreign sessions,2save ACKs,
empty pending intents and report/deployment-bound acceptance receipt are sealed
under LocalState/COSMIC/HostedQueryAcceptance-StargateOASIS. Never replay this
accepted cohort or regenerate its original native/local fixture. This proves
the retained bounded read representation, not wallet execution, independent
bridge finality or complete provider/ORM parity. Website/browser gates remain
open. Evidence under TestResults/HostedCqlIntegrationCurrent:
- StargateOASIS-client-http-flow.json: 36264B, SHA256 dfc811c3aed87641c84356f7da9c8d562565b6e65251b695c586c112c2f52e41.
- StargateOASIS-isolation-http.json: 6171B, SHA256 1144e7dff54602cc51360a245ceeb6a281046f5c500c9c697ba33adf1d694a4b.
- StargateOASIS-native-http.json: 13576B, SHA256 4ef06a6be732c65036d417294dc4adac0091fc2404f62c593e8bb8b99c267689.

Public [card67](https://web4.oasisomniverse.one/web4-sandbox.html#cql-stargate-catalogue)
published in Web4Site611ee736f37589ab167a833d8425cd0d3bb82b1e. Real C# query
and evidence links pinned to backend25b4a8508. All119 website client controls
pass; exact public HTML verified. Original cohort untouched; browser gate open.

11 November UTC: StargateOASIS indexed message observation presets returned 3 full and 2 filtered rows in the live Development WEB4 CQL Playground. Original-owner paired browser verification remains open.
