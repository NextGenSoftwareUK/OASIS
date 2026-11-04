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
