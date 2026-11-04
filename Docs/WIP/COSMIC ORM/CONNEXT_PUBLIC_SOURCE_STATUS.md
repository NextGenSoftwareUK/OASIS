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
