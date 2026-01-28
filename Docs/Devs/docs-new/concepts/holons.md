# What are Holons?

> The holon is the core of OASIS. Everything else—APIs, blockchains, apps—builds on top of it.

<Info>
  **New to OASIS?** Start here. Holons are the identity-first data units that persist independently of any single database, blockchain, or cloud. Once you understand holons, the rest of the platform (Avatar, NFT, Data API, STAR) makes sense.
</Info>

---

## The big idea

A **holon** is a self-contained unit of identity and data that:

- **Persists independently** of where it’s stored (MongoDB, Solana, IPFS, etc.). The same holon can be replicated across multiple providers; its identity doesn’t depend on any one of them.
- **Can be part of a whole** — it has parents and children. You can nest holons infinitely (e.g. a Mission contains Quests, which contain steps; a Star contains Planets, which contain Moons).
- **Has a type** (Avatar, Mission, NFT, Planet, custom, etc.) so OASIS and your app know how to interpret it.
- **Is inspectable** — version, metadata, and provider keys are part of the model, so you can reason about state and replication.

So: **holon = identity-first unit that can live in many execution environments and belong to larger structures.**

---

## Why “holon”?

In systems theory, a *holon* is something that is both a **part** and a **whole**: it’s a whole in its own right, and it’s also a part of a larger system. In OASIS:

- A **Mission** is a whole (it has a name, description, rewards) and a part (it can sit under an Avatar or a Star).
- An **Avatar** is a whole (user identity, karma, wallets) and can own many holons (missions, NFTs, files).
- A **Planet** is a whole (celestial body) and a part of a Solar System.

That “part-whole” idea is why OASIS uses holons for almost everything: users, content, game objects, and app data. One model, many use cases.

---

## Core properties

| Property | What it means |
|----------|----------------|
| **Id** | Globally unique identifier (GUID) for this holon. Stable across providers. |
| **HolonType** | Kind of holon (Avatar, Mission, Quest, NFT, Star, Planet, etc.). Lets you filter and reason about data. |
| **Name / Description** | Human-readable label and description. |
| **MetaData** | Key-value store for app-specific or global metadata. |
| **ParentHolonId** | Links this holon to a parent (optional). Enables trees and nesting. |
| **ProviderUniqueStorageKey** | Per-provider storage key (e.g. MongoDB `_id`, Solana account). OASIS uses this to load/save from different backends. |
| **Version** | Version number; OASIS can track history and support versioned loads. |

So when you **load** a holon, you get one canonical identity (Id, Type, Name, …) that may be stored in one or more execution contexts (DBs, chains). When you **save** a holon, OASIS (and HyperDrive) can replicate it to the providers you configure.

---

## Where holons live in the stack

OASIS is often described in layers (see the [Visual Canon ring mapping](../../../OASIS_VISUAL_CANON_RING_MAPPING.md)):

1. **Holon (centre)** — The concept you’re reading about. Identity-first, provider-agnostic.
2. **Kernel** — Coordination only: how to route, replicate, and persist. No product logic.
3. **Interfaces** — How you *use* holons: **WEB4 OASIS API** (Data API, Avatar, NFT, …), **STAR SDK**, MCP tools.
4. **Execution contexts** — Blockchains, databases, clouds (Solana, MongoDB, IPFS, etc.). They *host* holons but don’t *define* identity.
5. **Edge** — Apps and products (OPORTAL, games, Hitchhikers, your app) that store and display holons.

So: **you work with holons through the APIs and SDKs (Interfaces); OASIS and HyperDrive decide where they are stored (Execution contexts).**

---

## Holon types (examples)

OASIS defines many **HolonType** values so that avatars, missions, NFTs, celestial bodies, and custom data are all first-class. Examples:

| Category | Examples |
|----------|----------|
| **Identity** | Avatar, AvatarDetail |
| **Content / gameplay** | Mission, Chapter, Quest, InventoryItem |
| **Celestial (STAR)** | Star, Planet, Moon, SolarSystem, Galaxy |
| **NFTs** | Web3NFT, Web4NFT, Web5NFT, Web4GeoNFT, Web5GeoNFT |
| **Structure** | Zome, OAPP, Holon (generic) |
| **Other** | Building, Park, Proposal, LiquidityPool |

You can load/save by type (e.g. “load all holons of type Mission”) or by parent (“load all children of this holon”). The [Data API](web4-oasis-api/data-storage/holons-api.md) and [Using Holons](guides/using-holons.md) guides show how.

---

## What you do with holons

- **Load** — By Id, or by parent + type, or all of a type. Optionally load children recursively.
- **Save** — Create or update. Optionally save children; optionally replicate to multiple providers.
- **Delete** — Soft or hard delete.
- **Add / remove** — Attach or detach child holons to/from a parent.

All of this is available via:

- **WEB4 OASIS API** — [Data API](web4-oasis-api/data-storage/holons-api.md) (`/api/data`): `load-holon`, `save-holon`, `load-holons-for-parent`, etc.
- **STAR SDK** — When building games or metaverse apps, you often work with holons through the STAR ODK (celestial bodies, missions, NFTs).
- **MCP** — AI agents can call OASIS tools that load/save holons.

---

## Next steps

- **[How to use holons](../guides/using-holons.md)** — Quick start: load your first holon, save one, work with children. Code in TypeScript and cURL.
- **[Data API (Holons)](../web4-oasis-api/data-storage/holons-api.md)** — Full reference for `/api/data` holon endpoints (load, save, delete, load children).
- **[Getting started](../getting-started/overview.md)** — Register an avatar, get a JWT, then call any API (including Data).

---

## Summary

| Idea | Takeaway |
|------|----------|
| **What** | Holon = identity-first unit; persists across DBs/chains; can have parents and children. |
| **Why** | One model for users, content, and app data; same semantics everywhere. |
| **Where** | Centre of OASIS; you use them via Data API, STAR SDK, MCP. |
| **How** | Load by Id or parent/type; save (with optional replication); delete; manage children. |

---

*Last updated: January 2026*
