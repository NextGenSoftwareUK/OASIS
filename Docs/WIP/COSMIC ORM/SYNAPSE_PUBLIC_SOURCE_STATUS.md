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
