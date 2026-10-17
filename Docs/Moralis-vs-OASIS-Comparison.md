# Moralis vs OASIS — Comparison

## What is Moralis?

Moralis is a **Web3 data indexing and enrichment API**. It provides read-optimised, off-chain cached views of on-chain data across 30+ EVM chains — wallet balances, NFT metadata, transaction history, DeFi positions, token prices, etc. It is a developer backend service, not a blockchain protocol.

## What is OASIS?

OASIS is a **universal storage and identity abstraction layer**. It sits above chains, databases, cloud stores, IPFS, and social protocols, exposing a single provider-agnostic API. Moralis is one of the data sources OASIS can route through (via `MoralisOASIS`). OASIS additionally provides identity (avatars), karma, missions, quests, XR spatial mapping, and its own OApp/OAPP ecosystem.

---

## Feature Matrix

| Capability | Moralis | OASIS |
|---|---|---|
| **Multi-chain support** | 30+ EVM chains | 35+ chains + non-EVM (Holochain, IPFS, ActivityPub, etc.) |
| **NFT metadata & enrichment** | Yes — normalised, spam-filtered | Via MoralisOASIS or ThirdWebOASIS |
| **Wallet transaction history** | Yes | Via chain providers |
| **Token prices / DeFi positions** | Yes (Prices API, DeFi API) | No native — read from chain providers |
| **Webhooks / real-time streams** | Yes (Streams API) | No native webhooks yet |
| **Cross-chain identity** | No | Yes — OASIS Avatar (single identity across all chains/platforms) |
| **Karma / reputation layer** | No | Yes — built-in karma economy |
| **Missions & quests** | No | Yes — OApp mission/quest engine |
| **XR / spatial mapping** | No | Yes — OMap, WRLD3D, Mapbox integration |
| **Decentralised storage** | No | Yes — IPFS, Holochain, Arweave providers |
| **Social layer** | No | Yes — ActivityPub / Mastodon, Twitter, Discord |
| **Hot-swappable providers** | N/A | Yes — switch storage/chain at runtime |
| **On-chain write support** | No (read-only API) | Yes — via each chain's provider |
| **Self-hosted / open source** | No (SaaS only) | Yes — full open-source, self-hostable |
| **SDK languages** | JS/TS, Python, C# | C#/.NET primary; REST API for any language |
| **Authentication** | API key (centralised) | Decentralised avatar + JWT |
| **Rate limits** | Yes (plan-based CUs) | No limits when self-hosted |

---

## What Moralis Has That OASIS Doesn't (yet)

- **Spam NFT filtering** — automatic detection and filtering of known spam/scam tokens
- **Normalised NFT metadata** — resolves IPFS, Arweave, HTTP metadata into a uniform schema automatically
- **Token price feeds** — live ERC-20 price data from DEX liquidity pools
- **DeFi positions API** — decoded positions across Uniswap, Aave, Compound, etc.
- **Streams / Webhooks** — real-time on-chain event push to your endpoint
- **Entity resolution** — maps wallet addresses to ENS names, Lens handles, Farcaster IDs
- **30+ chain coverage out of the box** — Moralis maintains chain support; OASIS requires an individual provider per chain

## What OASIS Has That Moralis Doesn't

- **Unified cross-chain identity (Avatar)** — one persistent identity across every chain, social platform, and game
- **Karma / reputation economy** — on-chain and off-chain actions earn karma that gates features and rewards
- **Missions / quests engine** — gamified task system built into the protocol layer
- **XR / spatial integration** — holons can be anchored to real-world or virtual coordinates (OMap, Mapbox, WRLD3D)
- **Non-EVM chains** — Holochain, IPFS, Solana, Stellar, Cosmos, Polkadot, etc.
- **Decentralised storage providers** — IPFS, Holochain, Arweave as first-class storage
- **Social protocol adapters** — ActivityPub, Mastodon, Twitter, Discord, Telegram
- **Hot-swap provider architecture** — switch or combine backends without changing application code
- **Self-hosted and fully open source** — no vendor lock-in, no usage billing
- **OAPP / OApp ecosystem** — decentralised application marketplace running on OASIS

---

## Summary

Moralis excels as a **managed Web3 data enrichment service** — fast, convenient, pre-normalised EVM data with no infrastructure to run. It is read-only and centralised.

OASIS is a **decentralised protocol layer** with identity, reputation, missions, spatial integration, and storage abstraction spanning far more than just EVM chains. Where Moralis ends (enriched data delivery), OASIS begins (what you do with that data — and who you are while doing it).

They are complementary rather than competitive: `MoralisOASIS` lets OASIS apps consume Moralis-enriched chain data while keeping everything else (identity, karma, missions, persistence) within the OASIS ecosystem.
