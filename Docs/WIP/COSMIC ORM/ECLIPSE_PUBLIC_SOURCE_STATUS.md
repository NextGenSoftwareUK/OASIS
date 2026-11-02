# Eclipse public source — interface review

2 November 2026. Red/unqualified; no native requests, source owner, CQL adapter,
hosted admission or card. ENS remains the active release; this review used its
verified deployment wait and does not change that release or its candidate.

Existing provider: Providers/Network/NextGenSoftware.OASIS.API.Providers.EclipseOASIS/EclipseOASIS.cs.
Its legacy adapter uses Solana-style RPC but has no generic CQL implementation.
Observed legacy gaps: getSlot activation does not establish network identity;
account helper returns null for HTTP failure and avatar mapping can substitute
zero/false/empty values; transaction loads/signature scans are different scopes
from bounded public account metadata. Legacy code unchanged; issues docs-only.

## Verified official contract constraints

- [RPC and explorers](https://docs.eclipse.xyz/developers/rpc-and-block-explorers)
  lists https://mainnetbeta-rpc.eclipse.xyz as free testing RPC, rate limited to
  3requests/second, with potential temporary IP bans above that rate. Any native
  gate must respect this limit; no automatic endpoint/proxy switching or retries.
- [Differences from Solana](https://docs.eclipse.xyz/developers/differences-between-eclipse-and-solana.md)
  says native currency is ETH with9decimals. A Solana-compatible wire balance is
  not SOL and is not Ethereum18decimal wei. Preserve unsigned integer text and
  explicit currency/decimals; never copy Solana labels into this profile.
- The same page cautions that Solana program addresses must actually exist on
  Eclipse. Reusing an address string alone is not a valid three-record proof.

## Proposed bounded MVP, still unverified

Reuse cached Solnet/public-key primitives, guarded HTTP and existing runtime;
review SolanaPublicAccountMetadataSource for shareable protocol responsibilities
without changing its completed catalogue or ownership. No new node/environment.

Before any native request record one exclusive exact endpoint/method/budget intent.
Qualify network-reported genesis identity, finalized account context and three
explicit configured account metadata records. Prefer one bounded getMultipleAccounts
request for shared context, excluding account data with an explicit data slice.
Do not invent genesis hash or assume Solana mainnet identity. Current official
sources reviewed here do not establish an independent Eclipse genesis hash.

Potential catalogue: system program and documented sysvars, but exact keys and
existence must be verified before adopting a catalogue/source owner. Null accounts
must be explicit absence or an error according to the declared contract; not zero
balance or a successful record. No source owner/config/admission before native gate.

Initial CQL scope would be configured EclipseAccountAddress equality/IN and paging,
public flat holons with endpoint/network/context/currency provenance, then C#3/2,
HTTP/Mongo5/4/privacy and original hosted/replica acceptance. Native transactions,
account-data download, history scans, Ethereum settlement/Celestia availability
verification and full ORM remain separate red scopes. No counts changed.
