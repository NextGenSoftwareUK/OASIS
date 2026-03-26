# OASIS Holons — Conceptual Overview

> **Read this first.** Every other doc in this folder assumes you understand the model described here.

---

## 1. What is a Holon?

A **holon** is OASIS's fundamental data unit — a self-describing object that knows both what it *is* and where it *belongs*.

The word comes from Arthur Koestler: a holon is simultaneously a **whole** (it stands on its own with an identity and fields) and a **part** (it nests inside a parent and contributes to a larger structure). That duality is what makes the OASIS data model composable at scale.

```
Omniverse
  └─ Universe
       └─ Galaxy Cluster
            └─ Galaxy
                 └─ Solar System
                      └─ Planet        ← your OAPP lives here
                           └─ Moon
                                └─ Zome      ← a named group of holons
                                     └─ Holon  ← the data record
                                          └─ Field  ← typed attribute
```

Every holon has:

| Property | Type | Meaning |
|---|---|---|
| `Id` | `Guid` | Canonical identity — the same across every storage provider |
| `Name` | `string` | Human-readable label |
| `Description` | `string` | Optional longer description |
| `HolonType` | `enum` | What kind of holon it is (OAPP, Quest, NFT, custom…) |
| `ParentHolonId` | `Guid` | Who owns this in the hierarchy |
| `MetaData` | `Dictionary<string, object>` | Any extra key-value data |
| `ProviderUniqueStorageKey` | `Dictionary<ProviderType, string>` | Per-provider storage key — same Id, different backends |
| `CreatedByAvatarId` | `Guid` | Who created it |
| `CreatedDate` / `ModifiedDate` | `DateTime` | Audit trail |
| `Version` / `VersionId` | `int` / `Guid` | Immutable history |

---

## 2. What is a Zome?

A **zome** is a named container of related holons inside a celestial body (your OAPP's planet). Think of it as a module or schema namespace.

- Each zome groups holons that belong to the same **domain concern** (e.g. `VesselZome` only holds vessel-related holons).
- Zomes are defined in the **DNA JSON** file for an OAPP template.
- At runtime, STAR creates the zome structure on first deployment; subsequent OAPP instances inherit it.

---

## 3. What is STAR DNA?

The **DNA JSON** file is the blueprint for an OAPP. It defines:

```json
{
  "OAPPName": "MyTemplate",
  "OAPPType_enum": 5,               // 5 = OAPPTemplate
  "CelestialBodyDNA": {
    "Name": "MyPlanet",
    "CelestialBodies": [{
      "CelestialBodyType": "Planet",
      "Zomes": [{
        "Name": "MyZome",
        "Holons": [{
          "Name": "MyHolon",
          "Fields": [
            { "Name": "MyField", "Type": "string", "Required": true }
          ]
        }]
      }]
    }]
  }
}
```

When you call `POST /api/OAPPs/create` with this DNA, STAR:
1. Registers the OAPP on STARNET.
2. Creates the celestial body hierarchy in the COSMIC data model.
3. Makes the zomes and holon types available to any OAPP instance derived from this template.

---

## 4. The COSMIC Ontology

OASIS uses a **fixed spatial hierarchy** — the COSMIC model — as the universal namespace for all data:

```
Omniverse → Multiverse → Universe → GalaxyCluster → Galaxy
  → SolarSystem → Planet → Moon → Star → SuperStar
    → GreatGrandSuperStar → GrandSuperStar
```

Every piece of data lives *somewhere* in this tree. This gives you:

- **Place-based discovery**: "Show me all OAPPs in this solar system."
- **Shared context**: A quest, an NFT, and a property can all live on the same planet and know about each other.
- **COSMIC queries**: `GET /api/cosmic/{celestialBodyId}/children` walks the tree.

Your OAPPs use `CelestialBodyId` and `CelestialScopeId` fields to anchor holons to a location in this hierarchy.

---

## 5. How Holons Relate to Each Other

Holons form relationships via **foreign-key fields** (`Guid` references):

```
FarmHolon ─────────────────────────────────────────────────┐
  └─ PlotHolon (FarmId → FarmHolon)                        │
       └─ HarvestBatchHolon (PlotId → PlotHolon)           │
            └─ ChainOfCustodyHolon (BatchId → HarvestBatch)│
                 └─ CertificateHolon (BatchId → HarvestBatch)
```

There is no ORM foreign key enforced at the DB level — OASIS is provider-agnostic. The FK is a **contract** baked into the DNA. Clients and agents resolve the graph by following `*Id` fields through OASIS API calls.

---

## 6. Multi-Provider Persistence

One holon's data can be stored simultaneously on:

| Provider | Strength |
|---|---|
| **MongoDB** | Fast reads, rich queries |
| **Solana / Ethereum / BSC** | Immutable proof, tokenisation |
| **IPFS / Holochain** | Decentralised / censorship-resistant |
| **SQLite / Neo4j** | Local dev, graph queries |

The same `Id` resolves on any provider. `ProviderUniqueStorageKey` maps `ProviderType → storage address`. Your app code never changes — just the OASIS DNA config.

---

## 7. The Avatar — Universal Identity

Every holon is linked to an **Avatar** (the OASIS identity primitive):

- `CreatedByAvatarId` — who wrote the record
- Domain-specific fields like `OwnerAvatarId`, `PatientAvatarId`, `FunderAvatarId` — who the data *belongs to*

An Avatar carries:
- **Karma / XP / Level** — used by SubscriptionMembershipTemplate, CompetitionTournamentTemplate, QuestBuilderTemplate for gating
- **Wallets** — multi-chain wallet bindings for payments and NFT ownership
- **NFT inventory** — holons across all OAPPs
- **JWT session** — all API calls are Avatar-authenticated

---

## 8. Field Types

Fields in DNA definitions use these type strings:

| Type string | C# mapping | Notes |
|---|---|---|
| `Guid` | `System.Guid` | Always use for IDs and foreign keys |
| `string` | `string` | Plain text or JSON-serialised complex value |
| `string[]` | `List<string>` | Arrays of strings (tags, IDs, URLs) |
| `int` | `int` | Integer numbers |
| `double` | `double` | Decimal numbers (currency, coordinates, pct) |
| `bool` | `bool` | True/false flag |
| `DateTime` | `System.DateTime` | ISO 8601 timestamps |
| `Enum` | `string` with `Enum` constraint | Validated string from a fixed list |

When a field has `"Enum": [...]`, the array lists the only valid values. The API may reject records with values outside this list depending on validation config.

---

## 9. OAPPType Values

| Value | Name | Meaning |
|---|---|---|
| `1` | `App` | A deployed application instance |
| `2` | `OAPP` | A running OAPP |
| `3` | `OAPPTemplate` | A reusable blueprint on STARNET |
| `4` | `ZomeLib` | A shared zome library |
| `5` | `OAPPTemplate` | Used in `star_dna/` files — the template type |

All templates in `STAR_Templates/star_dna/` use `OAPPType_enum: 5`.

---

## 10. Key API Calls

| Action | Endpoint | Notes |
|---|---|---|
| Authenticate | `POST /api/Avatar/authenticate` | Returns JWT |
| Create OAPP | `POST /api/OAPPs/create` | DNA in body; API assigns new `id` |
| Publish to STARNET | `PUT /api/OAPPs/{id}` | Set `starnetdna.publishedOn`, `sourcePublicOnSTARNET: true` |
| List public OAPPs | `GET /api/OAPPs` | No auth needed |
| Get OAPP detail | `GET /api/OAPPs/{id}` | Auth optional |
| Save holon | `POST /api/Data/SaveHolon` | Generic holon write |
| Load holon | `GET /api/Data/LoadHolon/{id}` | Generic holon read |
| Search holons | `GET /api/Data/LoadAllHolons` | Filter by type, parent, metadata |

MCP tools mirror these:  
`star_beam_in` → authenticate | `star_list_oapps` → list | `star_create_oapp` → create.

---

## 11. Holon Lifecycle

```
Draft → Created → Published → Active → Archived / Deleted
         │                       │
         └─ PUT to update        └─ Versioned on every write
```

Every write increments `Version` and creates a new `VersionId`. OASIS keeps the full history — you can reconstruct any previous state by querying a specific `VersionId`.

---

## 12. Where to Go Next

| Document | What it covers |
|---|---|
| [PORT_OS_HOLONS.md](./PORT_OS_HOLONS.md) | 24 holons across 7 zomes for port intelligence |
| [RWA_HOLONS.md](./RWA_HOLONS.md) | Property and Business entity moving-identity holons |
| [SOCIAL_COMMUNITY_HOLONS.md](./SOCIAL_COMMUNITY_HOLONS.md) | Social graph, memberships, competitions |
| [VERTICAL_HOLONS.md](./VERTICAL_HOLONS.md) | Agriculture, conservation, creator economy, hospitality, health |
| `../star_dna/*.json` | Raw DNA files — the authoritative holon schema source |
| `../../starnet-manifest.json` | STARNET registry of all published templates |
