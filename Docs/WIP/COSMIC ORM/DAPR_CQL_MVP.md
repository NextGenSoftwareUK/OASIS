# Dapr CQL MVP

The read-only profile uses the existing Dapr .NET SDK to request strongly
consistent reads of `oasis/holon/{id:N}` from one named state component. It
uses an explicit IPv4 loopback sidecar endpoint, authenticates with a sidecar
token and caps gRPC replies at 1 MiB. The shared query profile checks public
visibility and canonical identity before paging. The query backend rejects
listing, writes, deletes and non-Holon reads.
[Dapr .NET client](https://docs.dapr.io/developing-applications/sdks/dotnet/dotnet-client/dotnet-daprclient-usage/).

Six focused tests passed with zero skips at Core
`79405fdb497834f275ef10edeb7d4b3b2c47ee38`. The run includes four CQL and
endpoint controls and two storage index regressions. TRX SHA-256 is
`2adfe17177d0204e3bf1e512993854692f0cf03351158570a8eaf98a3360edd4`.

Native verification passed on a pinned portable Dapr 1.18.9 sidecar and a
dedicated in-memory SQLite state component. The synthetic fixture returned
3/2/0 and its values and ETags were identical before and after the reads.
The native log SHA-256 is
`eeb88d1f7b32f6b76564d8ffafc8bde3993a10b0d2a8332ddfcb1230c904269f`.
The sidecar stopped, ports 54861–54863 were clear and no database file remained.
Hosted WEB4 enrollment
requires a co-located sidecar and a reviewed state component and remains open.
LocalFile keeps the first live Development rollout slot.
[Dapr SQLite component](https://docs.dapr.io/reference/components-reference/supported-state-stores/setup-sqlite/).
