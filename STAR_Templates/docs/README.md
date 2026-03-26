# STAR Templates — Holon Documentation

This folder contains human-readable documentation for every OAPP template published on STARNET. The authoritative schema source is always the DNA JSON in `../star_dna/` — these docs explain the *why* and *how*.

---

## Start Here

**[HOLONS_OVERVIEW.md](./HOLONS_OVERVIEW.md)** — Read this first. Explains what holons are, how zomes work, the COSMIC hierarchy, multi-provider persistence, field types, and the OASIS Avatar identity model.

---

## Template Docs

### Infrastructure & Logistics
| Document | Templates covered |
|---|---|
| [PORT_OS_HOLONS.md](./PORT_OS_HOLONS.md) | `PortOSTemplate` — 7 zomes, 24 holons, full port intelligence |

### Real World Assets
| Document | Templates covered |
|---|---|
| [RWA_HOLONS.md](./RWA_HOLONS.md) | `RWAPropertyTemplate` + `BusinessEntityTemplate` — moving identity for property and companies |

### Social & Community
| Document | Templates covered |
|---|---|
| [SOCIAL_COMMUNITY_HOLONS.md](./SOCIAL_COMMUNITY_HOLONS.md) | `SocialGraphTemplate` + `SubscriptionMembershipTemplate` + `CompetitionTournamentTemplate` |

### Vertical Domains
| Document | Templates covered |
|---|---|
| [VERTICAL_HOLONS.md](./VERTICAL_HOLONS.md) | `AgriTraceabilityTemplate` + `ConservationImpactTemplate` + `CreatorEconomyTemplate` + `HospitalityEventsTemplate` + `SovereignHealthRecordTemplate` |

---

## All Published Templates (24)

| # | Template | STARNET ID | Zomes | Key domain |
|---|---|---|---|---|
| 1 | PanGalacticTimebank | `da9c06d7` | — | Time banking |
| 2 | PanGalacticMonitor | `10c02eb4` | — | Geo monitoring |
| 3 | ForumTemplate | `2374eb0c` | — | Discussion |
| 4 | DAOTemplate | `fa181560` | — | Governance |
| 5 | MarketplaceTemplate | `a52192d1` | — | P2P commerce |
| 6 | GameTemplate | `23a6df3a` | — | Gaming |
| 7 | CredentialingTemplate | `7ff4bfbf` | — | Credentials & badges |
| 8 | AgentServiceMarketplaceTemplate | `04210da6` | — | AI agent services |
| 9 | QuestBuilderTemplate | `5e2e3c0d` | — | Quests & missions |
| 10 | EscrowTimebankTemplate | `54a6ccb3` | — | Escrow & time credits |
| 11 | GeoNFTHuntTemplate | `b0a26e0b` | — | Location-based NFTs |
| 12 | ClanGuildTemplate | `a534d406` | — | Clans & guilds |
| 13 | SovereignDataVaultTemplate | `9e006af2` | — | Personal data vault |
| 14 | **PortOSTemplate** | `d4cd6e87` | 7 | Port intelligence |
| 15 | **RWAPropertyTemplate** | `97b83c2b` | 6 | RWA property |
| 16 | **BusinessEntityTemplate** | `3b0d2c9a` | 5 | Business entity |
| 17 | **SocialGraphTemplate** | `05495506` | 5 | Decentralised social |
| 18 | **SubscriptionMembershipTemplate** | `dc2562b4` | 4 | Gated membership |
| 19 | **CompetitionTournamentTemplate** | `a64f5fda` | 4 | Tournaments |
| 20 | **AgriTraceabilityTemplate** | `112e14c9` | 5 | Farm-to-shelf |
| 21 | **ConservationImpactTemplate** | `e1f554dc` | 5 | Impact + carbon |
| 22 | **CreatorEconomyTemplate** | `9af2738f` | 4 | Royalties + licensing |
| 23 | **HospitalityEventsTemplate** | `d80ca1c5` | 4 | Phygital events |
| 24 | **SovereignHealthRecordTemplate** | `a35226fa` | 5 | Sovereign health |

Templates in **bold** were designed in this session and have dedicated holon documentation above.

---

## Cross-Template Linkages

Some templates are designed to connect directly:

```
AgriTraceabilityTemplate
  └─ HarvestBatchHolon → links to PortOSTemplate
       └─ ContainerHolon, TradeDocumentHolon, ESGCertHolon

ConservationImpactTemplate
  └─ CarbonCreditBatchHolon → can link to AgriTraceabilityTemplate
       └─ ProducerCertHolon (CertType: carbonNeutral)

SovereignHealthRecordTemplate
  └─ HealthProviderHolon → can link to CredentialingTemplate
       └─ CredentialNftId (provider's verified qualification)

HospitalityEventsTemplate
  └─ CheckInHolon → can trigger CompetitionTournamentTemplate
       └─ EggChallengeHolon (visit attraction to unlock challenge)

SubscriptionMembershipTemplate
  └─ MembershipHolon → can gate SocialGraphTemplate
       └─ AccessGrantHolon (membership = social access tier)

CompetitionTournamentTemplate
  └─ TournamentHolon → can embed in ClanGuildTemplate
       └─ ParticipantHolon (TeamName = clan name)
```

---

## Files

```
STAR_Templates/
  docs/
    README.md                    ← this file
    HOLONS_OVERVIEW.md           ← conceptual model
    PORT_OS_HOLONS.md            ← PortOS template reference
    RWA_HOLONS.md                ← Property + Business entity
    SOCIAL_COMMUNITY_HOLONS.md   ← Social + Membership + Tournament
    VERTICAL_HOLONS.md           ← Agri + Conservation + Creator + Hospitality + Health
  star_dna/
    *.json                       ← authoritative DNA schemas
../starnet-manifest.json         ← STARNET registry (all 24 templates)
```
