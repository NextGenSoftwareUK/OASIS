# Synapse public source qualification

4 November 2026. Synapse RFQ indexer returned HTTP 200 for its documented
missing-proof observation route. The response contained 19 rows within the
256 KiB ceiling. Three distinct bridge transaction IDs were selected once.
Three exact transaction lookups each returned HTTP 200 and matched the
canonical Bridge.transactionId field.

The indexer response uses Bridge, BridgeRequest and BridgeRelay sections.
The CQL profile should validate the exact indexed identity and retain stable
chain/transaction metadata. Pending status can change. These observations
establish source access, with CQL and WEB4 verification still open.

Raw contract, intents and responses: TestResults/SynapsePublicSourceCurrent.
Discovery SHA256 58BBDB0982ABA84CCEB92972C9F37565403DF34A5280FAD81B23C611D707A8F7.

[Official Synapse REST API](https://api.synapseprotocol.com/api-docs/)

## Native CQL verified

Five controlled cases and one real native CQL test passed, zero skips.
The retained RFQ catalogue returned exact 3/2 selections. The profile
checks exact transaction identity and nonzero chain IDs before mapping.
It reuses the existing REST transport with a bounded response and deadline.
Native TRX SHA256 45DF2D27FAEEC51F82D15D99750F014E99CE42D4BCA6A791F5DF14FCE754F512.
Next: WEB4 opt-in admission and local Mongo federation.

## WEB4 local HTTP verified

Opt-in admission and four preset plans passed. The isolated WEB4 HTTP run
returned native 3/2, paired 5/4, foreign-private 0/public 3 and nine metering
receipts. Both test snapshots were removed with the isolated database. The
retained Mongo replica stopped. The hosted publisher accepted both receipts.
Local HTTP TRX SHA256 E61845DC1D1995BB4237C885E81A668660759AEF9994E48EDA72CAAED1877BA4.
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
under LocalState/COSMIC/HostedQueryAcceptance-SynapseOASIS. Never replay this
accepted cohort or regenerate its original native/local fixture. This proves
the retained bounded read representation, not wallet execution, independent
bridge finality or complete provider/ORM parity. Website/browser gates remain
open. Evidence under TestResults/HostedCqlIntegrationCurrent:
- SynapseOASIS-client-http-flow.json: 35251B, SHA256 67daa814c37b316184f80ee728fdb70d1a658792bfa608e47e18cd92b432b4a3.
- SynapseOASIS-isolation-http.json: 5883B, SHA256 89e83a8856bb21ac7c65571d77415b9e03d9c76061c3e3352fcae2d62767ff9d.
- SynapseOASIS-native-http.json: 13108B, SHA256 5eb4d2b6d78e7b0bec1f70d5baf6ae02784532f0a4f7b2b7c30339a963a7e4e0.
