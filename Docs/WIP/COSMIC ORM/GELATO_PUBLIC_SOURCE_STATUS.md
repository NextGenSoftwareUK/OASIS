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


## Supported Gasless contract review — 8 November 2026

Preserve the original Relay TLS/certificate failures above; no old oracle URL was retried. The existing OASIS provider still calls the legacy conversion-rate endpoints and has not been migrated or CQL-qualified.

The publisher's [versioned npm package metadata](https://registry.npmjs.org/@gelatocloud%2fgasless/0.0.12) identifies `@gelatocloud/gasless`0.0.12, archive168327bytes, SHA256c29a7f61c72b8ea1463b9fb7f219ae601f10b7fc837360f15416a8c89fa45174. SHA512 integrity was verified against the registry before in-memory inspection;224files/794645unpacked bytes, no extraction/install/dependency download. Registry metadata2261bytes SHA25652d8d64d6ad34099ee7becefb71c8483d7f0ce36bcf0c103a7d5c6b985ca02a3. The advertised GitHub repository returned404 through both direct API and web observation; this does not prove deletion or package invalidity. No source commit was established; published package identity/integrity is the available evidence, and cached search content is not current native API proof.

Actual published `_dist/relayer/evm/actions/getCapabilities.js`923bytes SHA256265eab62db5e7247536b8cb0a19d34599fc1370025abab0a6f7714b8df3e1dfb sends JSON-RPC `relayer_getCapabilities` with empty params. Its result schema is a chain-ID keyed object containing feeCollector and tokens (address/decimals). Published client constructor3459bytes SHA2567e3ecb7d8092d6f2e3c89b0ef3e2510f8a319a11da7a57c9ca91660c822e42fb constructs production `https://api.gelato.cloud/rpc` and Authorization Bearer from configured apiKey. Published constants distinguish production from `https://api.t.gelato.cloud`; no production/testnet endpoint substitution was attempted.

A bounded supported-chain capability reader is a possible next scope, distinct from relay transaction submission, fees, task history and full storage. First implement strict envelope/chain/address/token/integer parsing, bounds, cancellation and configured key handling, with controlled transport tests; then an authorized native response must establish actual supported chain records before choosing three admitted keys or asserting CQL3/2. No secret lookup, owner key request, account creation, native Gasless service request, signed transaction, fee/sponsorship spend or source enrollment occurred. Do not send an undefined/made-up Bearer token or reuse the deprecated oracle endpoint as a fallback. Native SDK/CQL/WEB4/replica/browser acceptance remains open; coverage unchanged.
