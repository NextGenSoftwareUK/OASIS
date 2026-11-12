# Arbitrum balances with CQL

Updated 1 November 2026. This explicit read-only profile is live in the hosted demo.
Native CQL, published C#/CLI, local HTTP and hosted visitor/Mongo acceptance pass.
Browser interaction remains unverified for this profile. [Exact hosted proof](../WIP/COSMIC%20ORM/HOSTED_PUBLIC_SOURCE_ACCEPTANCE_2026-11-01.md).

## Run the live demo

Open the [WEB4 sandbox](https://web4.oasisomniverse.one/web4-sandbox.html), start a
demo session and open CQL Playground. Select **ArbitrumOASIS** when server discovery
marks it available. The server supplies four editable presets:

| Preset | Physical rows |
|---|---:|
| Three configured native records | 3 |
| Two-key native subset | 2 |
| Three native records plus your Mongo snapshots | 5 |
| Two native records plus your Mongo snapshots | 4 |

Use **Prepare my Mongo records** before the paired presets. It creates two private
snapshots through normal create-only saves; repeating it reuses acknowledged
records. Another visitor can see the public source but not your snapshots.
Matching SQLite/Neo4j copies were verified on the configured demo routes.

## Run the C# example

Reuse the retained nonsecret source catalogue and the existing SDK/runtime:

```powershell
dotnet run --project Examples/CosmicCqlDemo/CosmicCqlDemo.csproj `
    --no-build --no-restore -- `
    --arbitrum-public LocalState/COSMIC/HostedArbitrumPublicSourceCurrent/source.json
```

The spaced/commented [ArbitrumPublicWalkthrough.cs](../../Examples/CosmicCqlDemo/ArbitrumPublicWalkthrough.cs)
uses the shared HolonManager/LINQ pipeline. It selects three native keys, then two.
This console run performs native reads; it does not create Mongo records or rerun
hosted/browser acceptance. Build normally under the bounded provider build lease
if those compiled outputs are absent.

## Query contract and identity

- Native selection key: **ArbitrumAccountAddress**, equality or bounded constant-set membership.
- Use explicit provider scope. Unsupported predicates/operators are errors; this
  profile does not authorize an arbitrary scan, full ORM or signed source writes.
- Public native records and visitor-owned Mongo snapshots have different mapped
  OASIS IDs. Keep the source-reference metadata; never substitute a native key for
  a mapped holon ID. Preserve stable catalogue owners rather than regenerating them.
- Federation preserves physical copies. It does not provide a transactional snapshot
  spanning the source and Mongo. Public state may change between separate queries.
- If metadata encryption is enabled, grant this specific query key through trusted
  AdditionalQueryableKeys policy. Do not expand the global reserved unencrypted list.
- Pricing, permissions, source activation, native capabilities and work budgets are
  independent checks. Query permission does not grant signed storage authority.

See the [manual](../WIP/COSMIC%20ORM/CQL_MANUAL.md) for request syntax and scope;
[current status](../WIP/COSMIC%20ORM/CURRENT_STATUS.md) owns rollout state.
Earlier staged candidates are historical; never apply them over the current host.

Balances remain exact decimal strings in native base units. Each query binds reads to an RPC-reported finalized canonical block hash. This is not an independent consensus/settlement proof.
