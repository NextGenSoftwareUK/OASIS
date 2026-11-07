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

## Bounded CQL provider profile

Five controlled cases passed, covering complete ordered selection, missing,
duplicate and foreign rows, and HTTP failure. The real guarded .NET test
received HTTP 403 on activation, before any native CQL execution. The earlier
curl qualification remains valid for its own transport and does not establish
guarded .NET access. Preserve the fixed catalogue; investigate the transport
difference before retrying or admitting the profile to WEB4.
Native test report SHA256 59A01C004601BA89F43B839CAE92E193C484C5446621002BF15DC87E85618928.

The guarded client now requests JSON and identifies itself as OASIS. Five
controlled tests and the one real native CQL test passed with zero skips.
The original catalogue returned exact 3/2 selections. Both header changes
were tested together, so neither is established as the individual cause of
the original 403. The endpoint, guard and catalogue stayed the same.
Identified-client TRX SHA256 5F4B4A8562A07E53E1FB67D64823ACB26D089D07C1AFB195D70BB162CF98693D.
Next: explicit WEB4 registration, tariff and paired presets, then local HTTP federation.

## Local WEB4/Mongo acceptance

WEB4 admission and all four preset plans pass. Real local HTTP then passed
native 3/2, paired 5/4, foreign-private 0/public 3 and nine metering receipts.
Two owned private snapshots were saved in the isolated fixture, which was
removed after exact identity and count checks. The owned replica stopped.
Native Scan status may change; snapshots retain the status captured at save.
Hosted enrollment, replication and browser acceptance remain open.
Local HTTP TRX SHA256 802C5196CB767440896387F4445F43BCD69960BD6C873C0CC7ECC94BBD48DBF8.

## Hosted HTTP accepted - 7 November2026

Batch settingseaa1b7af-4fb7-4471-86b2-2071db135332 SUCCESS, instance
370c0335-3940-4f48-abef-7314caab801f, source15eb48673/Core85278a4,
image sha256:99940404f16658fab75f7b9a0d85082e688697095e19250fd4b6daef4de3f010.
Full11setting readback, original ACKs and active environment matched. Exact
release/settings stayed unchanged during each first hosted cohort. Native3/2,
Mongo5/4, first2private saves/repeat0new2existing, foreign-private0/public3 and
exactSQLite/Neo4j replicas2/2 passed. Original owner/foreign sessions,2save ACKs,
empty pending intents and report/deployment-bound acceptance receipt are sealed
under LocalState/COSMIC/HostedQueryAcceptance-LayerZeroV2OASIS. Never replay this
accepted cohort or regenerate its original native/local fixture. This proves
the retained bounded read representation, not wallet execution, independent
bridge finality or complete provider/ORM parity. Website/browser gates remain
open. Evidence under TestResults/HostedCqlIntegrationCurrent:
- LayerZeroV2OASIS-client-http-flow.json: 36367B, SHA256 0e40e57f3c56ba124b9b1e517c73be134a70867e73c5914dbd0a7a77a76baad4.
- LayerZeroV2OASIS-isolation-http.json: 6177B, SHA256 e160dcf09ea5de9e305d56990967680433b74f61ccde8cba3cac7267e9ad032f.
- LayerZeroV2OASIS-native-http.json: 13609B, SHA256 fb726e439120839f001d59b5df6111165d0705999763b6c2ed1cbbad1f2a7955.
