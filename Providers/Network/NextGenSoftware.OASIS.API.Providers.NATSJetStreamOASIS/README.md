# NATSJetStreamOASIS

NATS JetStream - high-throughput persistent pub/sub messaging.

Part of the [OASIS HOT-Swappable Provider Architecture](https://oasisomniverse.one).

## Usage

Register in your `OASIS_DNA.json`:

```json
{
  "DefaultProviders": ["NATSJetStreamOASIS"]
}
```

## Scoped CQL read profile

The provider uses NATS.Net 3.2.0. `NatsPublicCqlProfile` reads an explicitly
enrolled catalogue from an existing JetStream key-value bucket. Activation
verifies every enrolled record exists, has the expected identity and owner,
and is public before exposing the source. Reads use the existing bucket;
the read profile does not create buckets or save source records.

`CQL_NATS_PUBLIC_SOURCE_JSON` has exactly five fields: `schema` (1),
`endpoint`, `bucket`, `sourceOwner` and `ids`. Credentials are separate in
`CQL_NATS_PUBLIC_SOURCE_USERNAME` and `CQL_NATS_PUBLIC_SOURCE_PASSWORD`.
Never embed credentials in the endpoint or public descriptor.

There are two explicit configurations:

| Configuration | Contract |
|---|---|
| Remote source | `FromJson` requires a credential-free TLS endpoint outside loopback. The certificate chain must be trusted by the host. |
| Owned WEB4 sandbox | `OASIS_SANDBOX_NATS=true` and enabled sandbox DNA select `FromOwnedSandboxJson`. The endpoint is `tls://127.0.0.1:24222`, bucket `cosmic_cql_public`, reader `cql_reader`, with a dedicated verified CA at `/data/COSMIC/SandboxNATS/ca.pem`. |

The owned runtime restores three qualified public records once. Its bootstrap
and TLS identities have separate intent/acknowledgement files. Incomplete or
changed provisioning stops startup for reconciliation; startup does not
reseed records or rotate credentials automatically. The API starts after
native identity, TLS and reader authentication checks. The native server
has only the restricted reader account; no writer account is deployed.

The four sandbox presets query all three records, a two-record subset, and
each selection federated with the visitor's private MongoDB records. Explicit
snapshot saves target owned storage through the normal WEB4 flow. This scoped
read profile does not establish general messaging queries or full ORM parity.

Real native TLS and restored-source restart examples are in
[the read-only SDK qualification](../../../Scripts/TestHosts/NatsOwnedRestartQualification/Program.cs).
The corresponding [API admission tests](../../../ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests/CqlNatsHostedAdmissionTests.cs)
cover source configuration and the four presets. Current deployment and HTTP
acceptance gates are recorded in [the NATS WIP report](../../../Docs/WIP/COSMIC%20ORM/NATS_JETSTREAM_CQL_MVP.md).
Do not regenerate an accepted fixture or replay its save schedule to follow
these examples.

## Links

- [OASIS Website](https://oasisomniverse.one)
- [GitHub](https://github.com/NextGenSoftwareUK/OASIS)
- [NuGet](https://www.nuget.org/packages/NextGenSoftware.OASIS.API.Providers.NATSJetStreamOASIS/)
