# EVM public balance CQL walkthroughs

Updated 1 November 2026. These explicit anonymous profiles share one native EVM
reader and the same HolonManager/LINQ pipeline. They do not authorize wallet or
source-storage writes. Existing ten profiles below passed hosted native3/2,
Mongo-paired5/4, repeat preparation, isolation and replica checks. Berachain also passed its separate hosted gate on1b2626499/Core08c4977. [Current evidence](../WIP/COSMIC%20ORM/HOSTED_PUBLIC_SOURCE_ACCEPTANCE_2026-11-01.md).

| Profile | Chain ID | Native query key | CLI mode | Acceptance |
|---|---|---|---|---|
| Base | `0x2105` | `BaseAccountAddress` | `--base-public` | Hosted HTTP verified; browser open |
| Avalanche | `0xa86a` | `AvalancheAccountAddress` | `--avalanche-public` | Hosted HTTP verified; browser open |
| BNBChain | `0x38` | `BNBChainAccountAddress` | `--bnb-public` | Hosted HTTP verified; browser open |
| Optimism | `0xa` | `OptimismAccountAddress` | `--optimism-public` | Hosted HTTP verified; browser open |
| Linea | `0xe708` | `LineaAccountAddress` | `--linea-public` | Hosted HTTP verified; browser open |
| Scroll | `0x82750` | `ScrollAccountAddress` | `--scroll-public` | Hosted HTTP verified; browser open |
| ZkSync | `0x144` | `ZkSyncAccountAddress` | `--zksync-public` | Hosted HTTP verified; browser open |
| Abstract | `0xab5` | `AbstractAccountAddress` | `--abstract-public` | Hosted HTTP verified; browser open |
| Monad | `0x8f` | `MonadAccountAddress` | `--monad-public` | Hosted HTTP verified; browser open |
| Sei | `0x531` | `SeiAccountAddress` | `--sei-public` | Hosted HTTP verified; browser open |
| Berachain | `0x138de` | `BerachainAccountAddress` | `--berachain-public` | Hosted HTTP verified; browser open |

## C# demo

Reuse an existing stable source catalogue; do not create a wallet or reinstall:

```powershell
dotnet run --project Examples/CosmicCqlDemo/CosmicCqlDemo.csproj `
    --no-build --no-restore -- `
    --base-public LocalState/COSMIC/HostedBasePublicSourceCurrent/source.json
```

Substitute a mode above and its matching Hosted<Provider>PublicSourceCurrent file.
BNB uses HostedBnbPublicSourceCurrent. The commented <Provider>PublicWalkthrough.cs
files live in Examples/CosmicCqlDemo and call NativeKeySetWalkthrough. Their console
run proves native three-record/two-key selection only, not hosted/browser checks.

## Live demo

Open the [WEB4 sandbox](https://web4.oasisomniverse.one/web4-sandbox.html), start a
demo session, open CQL Playground and select a server-advertised ready profile.
Run the native three-record and two-record presets. Use **Prepare my Mongo records**
for two private snapshots, then the Mongo-paired five/four-row presets. Repeating
preparation reuses acknowledged records. Browser rendering/interaction acceptance
remains open; hosted HTTP/client-module acceptance is separately proven.

## Exact contract

- Equality/membership over configured native addresses; no implicit arbitrary scan.
- Fixed provider/chain/genesis binding; one finalized canonical hash per query.
  EIP1898 balance reads require that hash. No endpoint fallback or latest downgrade.
- Native quantities remain exact decimal strings, including UInt256 values beyond
  JavaScript Number precision. Finality is RPC-reported, not independent consensus proof.
- Sei explicitly accepts mixed-case native hash bytes and normalizes identity;
  other profiles retain their declared strict wire format.
- Source IDs differ from private snapshot IDs. Preserve mapping owners/provenance.
  Federation preserves physical copies, not a cross-provider transaction snapshot.
- Encrypted snapshot query keys need trusted AdditionalQueryableKeys grants;
  do not expand the system reserved-key list. Query-only permission never grants
  canonical signed writes or replication authority on the chain.

[Manual](../WIP/COSMIC%20ORM/CQL_MANUAL.md), [full provider status](../WIP/COSMIC%20ORM/PROVIDER_CQL_AND_SANDBOX_STATUS.md),
[current rollout](../WIP/COSMIC%20ORM/CURRENT_STATUS.md). Rootstock's selected endpoint
still rejects finalized headers; no profile was admitted by weakening that contract.
