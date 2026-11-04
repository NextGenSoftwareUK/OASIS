# Gelato public CQL source status

4 November 2026. The existing GelatoNetworkOASIS activation probes
`https://relay.gelato.network/oracles/1/conversionRate`. Bounded HTTPS probes for
Ethereum, Optimism and Polygon failed during TLS negotiation on the recovery
laptop. Windows curl reported SEC_E_CERT_EXPIRED on the Ethereum probe. The
alternative hostname in Gelato's legacy Swagger documentation also failed TLS
negotiation from this machine. These probes produced zero verified source records.

Gelato [archived the Relay SDK and directs migration to its Gasless SDK](https://github.com/gelatodigital/relay-sdk).
Keep Gelato out of the WEB4 demo admission until an official, reproducible
read contract can provide bounded records with stable identity. Start that
investigation with the supported Gasless contract and evaluate historical
on-chain relay evidence as a separately scoped source. Signed transaction
submission and fee estimation require their own migration and acceptance.
