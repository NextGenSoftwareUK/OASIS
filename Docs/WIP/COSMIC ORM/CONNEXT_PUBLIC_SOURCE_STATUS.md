# Connext public source qualification

4 November 2026. A single read-only request to the mainnet PostgREST host
configured by the existing Connext provider used a four-field projection
and limit 3. The host did not resolve from the recovery laptop. Curl exited
6 and no HTTP response or transfer records were received. The request intent
and outcome are retained under TestResults/ConnextPublicSourceCurrent.

The status page lists a healthy Cartographer Transfers component, which
does not establish that the specific PostgREST hostname is accessible.
The bridge UI separately announces a sunset. Keep the legacy transfer read
gate open until a supported historical transfer endpoint is documented and
qualified. No Everclear source was substituted for Connext data.

[Connext status](https://status.connext.network/)
· [Connext Bridge notice](https://bridge.connext.network/)

A separate native source can be qualified using the original deployed
Connext contracts. The official Calls reference defines the origin XCalled
event with a bytes32 transfer ID and the destination transferStatus getter
for that ID. A bounded historical log window would discover distinct IDs.
Exact reads must bind the original contract, chain and block context.
This is a separate source design. No RPC access or CQL behavior has been
verified through it yet.

[Connext contract event and getter reference](https://docs.connext.network/developers/reference/contracts/calls).

## Ethereum origin event source qualified

One bounded Blockscout Ethereum address-log page returned 50 logs, including
16 decoded XCalled events for the official Connext deployment. Three distinct
transfer IDs were selected. Three exact transaction-log reads independently
matched contract address, event signature, transfer ID, transaction hash and
log index. This verifies indexed origin events. Destination execution, raw RPC
receipt verification and CQL remain open.
Discovery SHA256 4678F11E6D2355C09A185A5EA4AFBB1D75199215A1DA06CD9A830682DEA2C4C9.
Raw intents, headers and responses are retained in TestResults/ConnextPublicSourceCurrent.

[Official deployment](https://docs.connext.network/resources/deployments).

## Native CQL verified

Five controlled cases and one guarded live test passed, zero skips.
The retained catalogue returned exact 3/2 transfer selections. The profile
rejects wrong blocks and transfer identities and only maps complete indexed
Ethereum origin events. WEB4 admission and destination execution remain open.
Native TRX SHA256 0B4407FA16561E9F1791B12BC720595AFC934FF535DDA63C446076715950BBD2.

## WEB4 local HTTP verified

Opt-in admission and four preset plans passed. The isolated WEB4 HTTP run
returned native 3/2, paired 5/4, foreign-private 0/public 3 and nine metering
receipts. Both test snapshots were removed with the isolated database. The
retained Mongo replica stopped. The hosted publisher accepted both receipts.
Local HTTP TRX SHA256 A56DAA72D41114AE91F5D6F41811FAC0C1A14CC273E7B27204A3180BDF21FEBF.
Hosted settings, replicas and browser acceptance remain open.
