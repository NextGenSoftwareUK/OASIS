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
