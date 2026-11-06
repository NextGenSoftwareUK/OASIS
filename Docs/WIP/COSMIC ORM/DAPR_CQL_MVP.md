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

## Development WEB4 candidate

Opt-in settings are `CQL_DAPR_QUERY_STORE`, `CQL_DAPR_QUERY_GRPC_ENDPOINT`,
`CQL_DAPR_QUERY_PROBE_ID` and the protected `CQL_DAPR_QUERY_SIDECAR_TOKEN`.
The catalogue binds the named component, canonical IPv4 loopback endpoint
and first public ID. All 22 protected-settings tests passed, including
component and port substitution checks. Seven Dapr WEB4 admission checks and
seven Fastly regressions passed without skips at Core
`7ee05c39f2415d3da7ed41acb6514ff6dc2db87d`. Admission TRX SHA-256 is
`f1d76919f67201c6d056a3775231274fe5ead475b5e20c9d109fefee53de2c19`.
Candidate preparation also checks that the protected baseline has a valid
sidecar token. The token stays outside the reviewed catalogue and settings diff.

The sidecar must run in WEB4's network namespace and bind its application
API to loopback. Dapr's state API allowlist covers the state API group,
which includes mutation methods. Verify a dedicated backing store and its
write-denial policy before exposing this profile. Verify the actual sidecar
and native reads on the reviewed WEB4 instance before enabling visitor
settings. [Dapr API allowlists](https://docs.dapr.io/operations/configuration/api-allowlist/).

The Windows Dapr 1.18.9 SQLite component failed to start on a synthetic
database reopened with `mode=ro`. The second run reproduced SQLite disk I/O
error 3850 during the component ping after the writer had shut down cleanly.
It never reached the read or write-denial assertions. The failure log SHA-256
is `76daa1c4052e44efc38b91b1937bf97bf9ce985227d3f64703e5dd4ebed7dcdf`.
Keep hosted write denial open until a reviewed backing component passes it.

The local `state.redis` alternative passed with the existing Garnet 2.2.5
runtime. A dedicated writer sidecar seeded a synthetic record and shut down.
A second sidecar used a restricted Redis ACL principal and read the record
with its ETag. The attempted update returned Redis `NOPERM` through the
sidecar, and the value and ETag remained identical. The result SHA-256 is
`78db3be203f637125ae4cf0c37ad3e19ccad926c03c1d135870f58932a811e04`.
This qualifies the local permission model. The actual Development backing
store, sidecar, gRPC CQL results and visitor route still require verification.
[Dapr Redis component](https://docs.dapr.io/reference/components-reference/supported-state-stores/setup-redis/).
