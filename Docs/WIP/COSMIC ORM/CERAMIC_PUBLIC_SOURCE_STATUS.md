# Ceramic public source qualification

Updated2November2026. Red: not CQL/native/hosted/browser qualified.

## Existing implementation

`Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.CeramicOASIS/CeramicOASIS.cs`
uses `https://ceramic-clay.3boxlabs.com` by default and an HTTP healthcheck. This
legacy implementation is inspected only; no changes or acceptance inferred.

## Source research

- [Official Ceramic FAQ](https://blog.ceramic.network/faq-ceramic-network/)
  describes self-hosted nodes or node-running services rather than an official
  hosted RPC offering. It is historical guidance, not proof that every current
  public read endpoint is unavailable.
- [Official js-ceramic source](https://github.com/ceramicnetwork/js-ceramic)
  supplies the HTTP client/node protocol; current pinned source review is next.
- The documented developer HTTP API URL
  `https://developers.ceramic.network/docs/protocol/js-ceramic/guides/ceramic-clients/javascript-clients/http-api`
  returned404through documentation browsing. This is not a native node failure.

No node probe, DID/session creation, stream discovery, writes, native source
identity, install, clone or paid service was performed. Do not treat third-party
API catalogues or old Clay examples as current mainnet qualification.

## Next gate

Inspect pinned official HTTP client/API schema and current operator documentation;
identify legitimate anonymous read access and several existing public StreamIDs.
Qualify bounded stream membership and network/source identity before an adapter.
Do not create a new node/environment merely to bypass missing access. Zcash's
existing live deployment/activation remains the immediate priority.

## Pinned client contract review

Official js-ceramic commit4050fc1021e9a682d588fac662d3d7ef6f2eb73e (main observed at qualification) retained3source files/22546bytes plus SHA256 manifest in LocalState/COSMIC/CeramicPublicQualificationCurrent/sdk. HTTP client uses api/v0 and exposes loadStream/multiQuery with StreamID/CommitID references; network enum distinguishes mainnet/testnet-clay/local/inmemory. The client default is localhost:7007, not evidence of a free hosted endpoint. No native stream read performed. Next inspect multiQuery response serialization/state and operator-authorized public endpoint/known existing StreamIDs.
