# Vertical Domain Holons

This document covers five domain-specific templates: agriculture/traceability, conservation/impact, creator economy, hospitality/events, and sovereign health records.

---

## AgriTraceabilityTemplate

**STARNET ID:** `112e14c9-b245-4cd3-9961-e2bd15a6f5b3`  
**DNA:** `STAR_Templates/star_dna/AgriTraceabilityTemplate.json`  
**Tags:** agriculture · traceability · eudr · supply-chain · farm · food · sustainability · provenance

### What it does

Full farm-to-shelf provenance for any agricultural commodity. Designed to sit **upstream of PortOSTemplate** — an `InlandLegHolon` in the port system references a `HarvestBatchHolon` here. Together they form an unbroken EUDR compliance chain from plot to customs clearance.

```
FarmHolon
  └─ PlotHolon                    (geo-anchored parcel, EUDR deforestation risk)
       └─ HarvestBatchHolon       (a harvest from the plot)
            ├─ ProcessingEventHolon[]   (fermentation, drying, milling…)
            ├─ CustodyHandoffHolon[]    (every change of hands)
            ├─ StorageEventHolon[]      (warehouse dwell periods)
            ├─ BatchCertHolon[]         (EUDR/organic/FairTrade — IPFS + chain)
            └─ ProductHolon[]           (retail SKUs assembled from the batch)
```

---

### Zome 1 — FarmZome

#### FarmHolon
The origin anchor for all traceability chains.

| Field | Notes |
|---|---|
| `OperatorAvatarId` | OASIS Avatar of the farmer/co-op — their wallet receives EUDR payment |
| `Commodity` | Enum: `cocoa` / `coffee` / `soy` / `palm` / `timber` / `cattle` / `rubber` / `maize` / `wheat` / `other` |
| `CountryCode` / `Region` | ISO country + administrative region |
| `AreaHectares` | Total farm size |

All EUDR-regulated commodities (cocoa, coffee, soy, palm, timber, cattle, rubber) are enumerated explicitly — any `EudrRequired: true` consignment in PortOS triggers a lookup back to this zome.

#### PlotHolon
A named parcel within the farm.

| Field | Notes |
|---|---|
| `GeoPolygonJson` | GeoJSON polygon — required for EUDR compliance (must prove no deforestation after 2020) |
| `DeforestationRisk` | `low` / `medium` / `high` / `unknown` — pre-assessed risk level |
| `Irrigation` | Soil + irrigation type for provenance narrative |

The `GeoPolygonJson` is the **EUDR critical field**. EU importers must submit this to customs. Storing it in the holon makes it instantly queryable by PortOS's `TradeDocumentHolon` verification flow.

---

### Zome 2 — CropBatchZome

#### HarvestBatchHolon
The atomic traceability unit — one harvest event from one (or more) plots.

| Field | Notes |
|---|---|
| `BatchId` | Carries through every downstream holon as a FK |
| `CropSeason` | e.g. `2025-main` |
| `HarvestDate` | When picking occurred |
| `QuantityKg` | Starting weight — compared to `OutputWeightKg` after processing |
| `QualityGrade` | Commodity-specific: `AA`, `Grade 1`, `Fine Flavour` etc. |
| `MoistureContentPct` | Quality parameter affecting shelf life |

#### ProcessingEventHolon
Each transformation step on the batch — building an immutable processing record.

| ProcessType | Commodity | Notes |
|---|---|---|
| `fermentation` | Cocoa | 5–7 day wet fermentation |
| `drying` | Coffee / Cocoa | Sun or mechanical drying |
| `milling` | Grain / Coffee | Hulling, polishing |
| `roasting` | Coffee / Cocoa | Temperature/time profile |
| `grading` | Any | QC inspection, rejects removed |
| `blending` | Any | Multi-origin blend event |
| `packaging` | Any | Final packing for export |

`InputWeightKg` vs `OutputWeightKg` tracks yield loss at each step — important for traceability calculations ("where did the missing 3% go?").

---

### Zome 3 — ChainOfCustodyZome

#### CustodyHandoffHolon
Every time custody changes — this holon is written. The full array forms the legal chain of custody.

| HandoffType | Who creates it |
|---|---|
| `farmGate` | Farmer → co-operative |
| `cooperative` | Co-op → exporter |
| `exporter` | Exporter → freight forwarder |
| `port` | Handoff to PortOS system (InlandLegHolon created here) |
| `shipping` | Vessel loaded (VesselCallHolon in PortOS) |
| `importer` | Arrival at destination port |
| `processor` | Import processor / roaster |
| `retailer` | Final retail delivery |

`SealNumber` at each handoff can be verified against the `GateEventHolon.SealCheck` in PortOS — a mismatch triggers an `InlandExceptionHolon`.

#### StorageEventHolon
Warehouse dwell with `TemperatureC` and `HumidityPct`. Cold-chain breaks (temperature excursions) can be detected by comparing this against the `TemperatureControlled` flag in `ConsignmentHolon`.

---

### Zome 4 — CertificateZome

#### ProducerCertHolon
A certification held by the farm — valid for a period.

| CertType | Issuer examples | Notes |
|---|---|---|
| `EUDR` | EU Commission scheme | Required for regulated commodities to EU |
| `organic` | USDA, EU Organic, Soil Association | |
| `fairTrade` | Fairtrade International | |
| `rainforestAlliance` | Rainforest Alliance | |
| `GlobalGAP` | GlobalGAP | Agricultural good practice |
| `carbonNeutral` | Gold Standard, VCS | Links to ConservationImpactTemplate |

`IpfsCid` + `OnChainTxHash` make the certificate tamper-proof and publicly verifiable without trusting a central database.

#### BatchCertHolon
Certifies a **specific batch** — not just the farm. This is what customs officials check. `CoveredKg` specifies exactly how much of the batch is covered. `NftTokenId` mints an NFT certificate — tradeable proof of provenance that can accompany the goods.

---

### Zome 5 — RetailZome

#### ProductHolon
A retail SKU assembled from one or more batches.

| Field | Notes |
|---|---|
| `SourceBatchIds` | `string[]` — multi-origin blends reference multiple batches |
| `QrCodeUrl` | Scanned by consumers to see the full provenance chain |
| `ConsumerPageUrl` | Landing page showing farm → port → shelf story |
| `ProvenanceNftId` | NFT consumers can receive as proof of ethical purchase |

---

### AgriTraceability + PortOS Integration

```
PlotHolon (geo polygon for EUDR)
  └─ HarvestBatchHolon
       └─ CustodyHandoffHolon (type: port)
            → InlandLegHolon (PortOSTemplate)
                 └─ ContainerHolon
                      └─ ConsignmentHolon (EudrRequired: true)
                           └─ TradeDocumentHolon (DocType: EUDR)
                                └─ ESGCertHolon (CertType: EUDR)
```

The `BatchCertHolon.IpfsCid` is referenced in `TradeDocumentHolon.IpfsCid` — same file, same hash, same proof.

---

---

## ConservationImpactTemplate

**STARNET ID:** `e1f554dc-60b3-4e3b-9182-77ff636f9055`  
**DNA:** `STAR_Templates/star_dna/ConservationImpactTemplate.json`  
**Tags:** conservation · impact · carbon · climate · pulmon-verde · ixo · geo · sustainability

### What it does

A transparent, verifiable impact reporting system for conservation projects — designed for Pulmon Verde and IXO-style deployments. Every claim is geo-anchored, time-stamped, and evidence-linked. Carbon credits are on-chain NFTs; retirements are permanent and auditable.

```
ConservationProjectHolon
  ├─ ImpactMetricHolon[]       (what success looks like)
  │    └─ ImpactReadingHolon[] (time-series measurements)
  ├─ CarbonCreditBatchHolon[]  (credits issued from the project)
  │    └─ CreditRetirementHolon[] (permanent retirements)
  ├─ MonitoringEventHolon[]    (field visits, satellite, sensors)
  └─ FundingContributionHolon[] (donor and grant records)
```

---

### Zome 1 — ProjectZome

#### ConservationProjectHolon
Root identity for a project site.

| Field | Notes |
|---|---|
| `ProjectType` | `reforestation` / `oceanProtection` / `wildlifeHabitat` / `soilRestoration` / `wetlandConservation` / `biodiversity` / `cleanWater` / `cleanEnergy` / `other` |
| `GeoPolygonJson` | Project area boundary — used for satellite deforestation monitoring |
| `AreaHectares` | Project area |
| `SDGGoals` | `string[]` of UN SDG numbers (e.g. `["13", "15"]` for climate + land) |
| `GeoHotSpotId` | Links to OASIS GeoHotSpotManager — supports Quest-based engagement ("visit this forest") |

---

### Zome 2 — ImpactMetricZome

#### ImpactMetricHolon
Defines a measurable outcome with a target.

| MetricType | Unit example | How verified |
|---|---|---|
| `treesPlanted` | trees | Field count + satellite |
| `co2Sequestered` | tonnes CO₂e | Allometric equations + satellite |
| `speciesProtected` | species count | Biodiversity survey |
| `hectaresRestored` | ha | Satellite polygon analysis |
| `waterLitresCleaned` | litres/day | Sensor reading |
| `communitiesBenefited` | households | Field survey |

`TargetValue` vs `CurrentValue` vs `VerifiedValue` — three levels of rigour. `CurrentValue` is self-reported; `VerifiedValue` is set by a third-party auditor Avatar.

#### ImpactReadingHolon
One measurement per time point. `Source` field distinguishes `fieldSurvey`, `satellite`, `sensor`, `manual`, `thirdParty`. `IpfsCid` stores the evidence file (satellite image, sensor log, survey photo).

The full array of readings per metric builds a time series: **verified progress over the project lifetime**.

---

### Zome 3 — CarbonCreditZome

#### CarbonCreditBatchHolon
A batch of carbon credits generated by the project.

| Field | Notes |
|---|---|
| `QuantityTonnesCO2` | Volume of credits in this batch |
| `Vintage` | Year the sequestration occurred |
| `Standard` | `VCS` / `GoldStandard` / `CAR` / `ACR` / `Plan Vivo` |
| `RegistryId` | External registry serial number |
| `Status` | `pending` → `issued` → `sold` / `retired` / `cancelled` |
| `NftTokenId` | Each credit batch can be represented as an NFT — tradeable on STARNET Marketplace |

#### CreditRetirementHolon
**Permanent** — once created, credits are gone. `BuyerAvatarId` is the entity making the offset claim. `Purpose` describes what the retirement is for (e.g. "Scope 3 product offsetting 2025"). `CertificateUrl` is the PDF offset certificate generated from this record.

---

### Zome 4 — MonitoringZome

#### MonitoringEventHolon
Evidence-based field or remote observation.

| EventType | Who captures it |
|---|---|
| `fieldVisit` | Project team; photos + notes |
| `dronesurvey` | Aerial survey; images pinned to IPFS |
| `satellite` | Automated API pull (e.g. Global Forest Watch) |
| `sensorReading` | IoT device (water quality, CO₂ sensor) |
| `communityReport` | Local community member via mobile app |
| `audit` | Third-party verifier |

`EvidenceUrls` + `IpfsCid` ensure all evidence is immutably stored. Funders and regulators can query this zome to see the full monitoring history.

---

### Zome 5 — FunderZome

#### FundingContributionHolon
Transparent donation tracking.

| ContributionType | Notes |
|---|---|
| `donation` | Direct cash gift |
| `grant` | Institutional grant (government, foundation) |
| `carbonOffset` | Purchase of credits from this project's CarbonCreditBatch |
| `sponsorship` | Corporate sponsor |
| `impact-investment` | Returns expected; linked to project KPIs |

`IsPublic: true` makes the contribution visible on the project's public page — enabling "funded by" attribution that's verifiable on-chain via `TxHash`.

---

---

## CreatorEconomyTemplate

**STARNET ID:** `9af2738f-f616-4537-b382-38e18f58c8db`  
**DNA:** `STAR_Templates/star_dna/CreatorEconomyTemplate.json`  
**Tags:** creator · music · nft · royalties · licensing · web4 · ipfs · content · streaming

### What it does

Content items with **on-chain royalty splits** — every sale, stream, or license triggers automatic distribution to all rights holders. No intermediary. Works for music, video, art, writing, podcasts, or any digital content.

```
ContentItemHolon (a track, video, artwork…)
  └─ CollectionHolon[]         (albums, series, portfolios)
  └─ RoyaltySplitHolon         (the split rules)
       └─ RoyaltyRecipientHolon[] (each rights holder + share %)
       └─ RoyaltyPaymentHolon[]   (payments executed)
  └─ LicenseHolon[]            (licensing agreements)
  └─ DistributionRecordHolon[] (per-platform distribution)
       └─ StreamingReportHolon[] (earnings reports)
```

---

### Zome 1 — ContentZome

#### ContentItemHolon
The canonical content record.

| Field | Notes |
|---|---|
| `ContentType` | `music` / `video` / `artwork` / `photography` / `writing` / `podcast` / `software` / `other` |
| `IpfsCid` | Full content file pinned to IPFS — decentralised permanent storage |
| `IsrcCode` | International Standard Recording Code (music) |
| `PreviewUrl` | 30-second preview clip (different IPFS CID) |
| `Visibility` | `public` / `licensed` / `private` / `nft-gated` |

`nft-gated` visibility: access requires owning an NFT from a specific collection. The gate check calls `GET /api/Nft/balance` for the viewer's Avatar.

#### CollectionHolon
Groups content. `CollectionType` covers `album`, `series`, `portfolio`, `playlist`, `bundle`. Multiple collections can reference the same `ContentId` — one track can be in multiple playlists.

---

### Zome 2 — RoyaltySplitZome

#### RoyaltySplitHolon
The container for a split definition. `SplitVersion` increments when the split is renegotiated — old payments honour the old split; new payments use the latest `IsActive: true` version.

#### RoyaltyRecipientHolon
One row per rights holder.

| Role | Example | SharePct |
|---|---|---|
| `artist` | Primary performer | 45% |
| `producer` | Beat producer | 20% |
| `songwriter` | Lyricist | 15% |
| `label` | Record label | 15% |
| `publisher` | Publishing house | 5% |

`WalletAddress` + `PreferredChain` direct payment to the recipient's wallet. If the recipient has an OASIS Avatar, use `AvatarId` — OASIS resolves the wallet via the Avatar's multi-chain wallet bindings.

**Sum of all `SharePct` in a split must equal 100.** Validated at write time.

#### RoyaltyPaymentHolon
Created whenever a payment is triggered. `TriggerType` (`sale`, `stream`, `license`, `tip`, `subscription`) determines the payment calculation context. `TxHash` is set when the on-chain distribution executes.

---

### Zome 3 — LicenseZome

#### LicenseHolon
A licensing agreement between creator and licensee.

| LicenseType | Notes |
|---|---|
| `exclusive` | Licensee has exclusive rights in the territory |
| `nonExclusive` | Multiple licensees can hold simultaneously |
| `sync` | Sync rights for film/TV/advertising |
| `broadcast` | Radio / TV broadcast |
| `commercial` | Use in advertising |
| `creative-commons` | Open licence — terms in `Description` |

`Platforms` (`string[]`) limits which platforms are covered. `Territory` limits geography. `FeeAmount` + `Currency` is the upfront licence fee (separate from ongoing royalties).

`DocHolonId` links to a signed PDF in `FilesController` / IPFS. `IpfsCid` is the file's content hash — tamper-proof contract storage.

---

### Zome 4 — StreamingZome

#### DistributionRecordHolon
One record per platform per content item. `Platform` enum: `spotify`, `appleMusic`, `youtube`, `soundcloud`, `ipfs`, `oasis`. `PlatformContentId` is the external platform's own ID for the item — enables reverse lookups.

#### StreamingReportHolon
Periodic earnings report. `Plays` + `Downloads` + `EarningsGross` / `EarningsNet` per period. Aggregated across all `DistributionRecordHolon` records, this gives the creator's full income picture across all platforms.

---

---

## HospitalityEventsTemplate

**STARNET ID:** `d80ca1c5-1da8-45b2-9eb2-631aa8cb5d56`  
**DNA:** `STAR_Templates/star_dna/HospitalityEventsTemplate.json`  
**Tags:** hospitality · events · phygital · nft-ticket · venue · loyalty · alton-towers · geo · karma

### What it does

Phygital hospitality — physical venues with digital-native ticketing, geo check-ins that accumulate Avatar Karma, and loyalty tiers. Designed for Alton Towers and similar real-world partner deployments.

```
VenueHolon
  └─ AttractionHolon[]      (rides, stages, areas — each geo-anchored)
  └─ EventHolon[]           (concerts, park days, private events)
       └─ TicketTierHolon[] (GA, VIP, Early Bird…)
            └─ TicketHolon[] (issued tickets — each is an NFT)
  └─ CheckInHolon[]         (geo-verified presence proofs)
  └─ LoyaltyRecordHolon     (one record per Avatar per venue)
```

---

### Zome 1 — VenueZome

#### VenueHolon
Root identity for a physical venue.

| Field | Notes |
|---|---|
| `VenueType` | `themepark` / `arena` / `conference` / `hotel` / `restaurant` / `festival` / `gallery` / `stadium` / `other` |
| `GeoHotSpotId` | Links to OASIS GeoHotSpotManager — enables STAR Quest integration ("visit Alton Towers to unlock") |
| `Capacity` | Max concurrent visitors |

#### AttractionHolon
A named zone or ride within the venue — also geo-anchored. Each `AttractionHolon` has its own `GeoHotSpotId`, enabling per-attraction check-in and Karma awards. A visitor who checks in at 5 different rides earns more Karma than one who only checks in at the gate.

---

### Zome 2 — EventZome

#### EventHolon
A date-bound event at the venue.

| Field | Notes |
|---|---|
| `EventType` | `concert` / `festival` / `conference` / `parkDay` / `privateEvent` / `exhibition` / `tournament` / `other` |
| `Status` | `draft` → `onSale` → `soldOut` / `live` → `completed` / `cancelled` |
| `TicketsSold` | Denormalised counter (updated on each TicketHolon creation) |

#### TicketTierHolon
Defines a tier: `TierName` (e.g. `"General Admission"`, `"VIP Experience"`), `Price`, `MaxQuantity`. `IsNFTTicket: true` triggers NFT minting on purchase. `SaleStartsAt` / `SaleEndsAt` enables timed presale windows.

---

### Zome 3 — TicketNFTZome

#### TicketHolon
One ticket per Avatar per purchase.

| Field | Notes |
|---|---|
| `QrCode` | Encoded ticket ID — scanned at venue gate |
| `NftTokenId` | The OASIS NFT token — transferable (resale enabled) |
| `NftTxHash` | On-chain mint transaction |
| `Status` | `issued` → `redeemed` (on gate scan) / `transferred` / `cancelled` / `refunded` |

**Resale flow:** Avatar A holds `TicketHolon` with `NftTokenId`. A transfers the NFT to Avatar B. PortOS/gate system checks `OwnerAvatarId` of the NFT at scan time — the ticket now belongs to B. `Status: redeemed` is set on scan and is non-transferable after that.

---

### Zome 4 — ExperienceZome

#### CheckInHolon
A geo-verified proof-of-presence.

| Field | Notes |
|---|---|
| `GeoHotSpotId` | Which hotspot was triggered |
| `Latitude` / `Longitude` | Device GPS at check-in time |
| `KarmaAwarded` | Karma added to Avatar on this check-in |
| `BadgeNftId` | Optional NFT badge minted on check-in (e.g. "Rode Nemesis 10 times") |
| `ProofHash` | SHA-256 of (AvatarId + GeoHotSpotId + Timestamp) — anti-replay |

The **Avatar accumulates Karma** across every check-in. After N check-ins, the Avatar's `KarmaScore` crosses the threshold for a higher `LoyaltyRecordHolon` tier.

#### LoyaltyRecordHolon
One record per Avatar per venue. Updated in-place (not versioned) for performance.

| TierName | Example threshold | Benefit |
|---|---|---|
| `bronze` | 1 visit | 5% F&B discount |
| `silver` | 5 visits | Free fast-track |
| `gold` | 15 visits | Lounge access |
| `platinum` | 30 visits | Annual pass |
| `vip` | Invite only | Behind-the-scenes access |

Tier advancement can be gated by `KarmaScore` (via SubscriptionMembershipTemplate), `PointsBalance`, or `TotalVisits`.

---

---

## SovereignHealthRecordTemplate

**STARNET ID:** `a35226fa-3e3d-46c6-9d68-041785e5d4dd`  
**DNA:** `STAR_Templates/star_dna/SovereignHealthRecordTemplate.json`  
**Tags:** health · medical · sovereign · privacy · consent · ehr · privacymage · vault · ipfs · holochain

### What it does

A patient-controlled Electronic Health Record where **the patient decides who sees what**. Data is encrypted at rest across multiple providers (MongoDB, IPFS, Holochain). Consent grants are on-chain — tamper-proof and auditable. Designed for PrivacyMage integration.

```
PatientProfileHolon (the patient's sovereign identity)
  ├─ HealthProviderHolon[]      (verified GPs, specialists, hospitals)
  ├─ ConsultationHolon[]        (clinical encounters)
  ├─ TestResultHolon[]          (blood work, imaging, pathology)
  ├─ PrescriptionHolon[]        (active and historical medications)
  ├─ ConsentGrantHolon[]        (who can see which records)
  └─ AccessLogHolon[]           (every read, by whom, when)
```

---

### Zome 1 — PatientZome

#### PatientProfileHolon
The patient's sovereign health identity. `IsEncrypted: true` by default — all fields are AES-256 encrypted at the OASIS provider layer before storage.

| Field | Notes |
|---|---|
| `AvatarId` | Links to OASIS Avatar — one canonical identity |
| `BloodType` | Emergency-accessible even in restricted access mode |
| `Allergies` | `string[]` — critical safety field |
| `ChronicConditions` | `string[]` — ongoing diagnoses |
| `EmergencyContactName` / `Phone` | Accessible under `emergency` access type |

---

### Zome 2 — ProviderZome

#### HealthProviderHolon
A verified healthcare provider.

| Field | Notes |
|---|---|
| `ProviderAvatarId` | Provider's OASIS Avatar — they authenticate to write records |
| `ProviderType` | `gp` / `specialist` / `hospital` / `clinic` / `pharmacy` / `lab` / `therapist` / `dentist` |
| `LicenceNumber` / `LicencingBody` | Verified credential — can link to `CredentialingTemplate` OAPP |
| `IsVerified` | Set by OASIS admin or a trusted verification Oracle |
| `CredentialNftId` | NFT credential from CredentialingTemplate — portable proof of qualifications |

---

### Zome 3 — RecordZome

#### ConsultationHolon
A clinical encounter — encrypted by default.

| Field | Notes |
|---|---|
| `ConsultType` | `inPerson` / `telehealth` / `emergency` / `followUp` / `specialist` |
| `ChiefComplaint` | Why the patient attended |
| `DiagnosisCodes` | `string[]` of ICD-10 codes |
| `Notes` | Clinical notes — most sensitive field |
| `IpfsCid` | Full structured consultation record stored on IPFS |

#### TestResultHolon
Diagnostic results.

| TestType | LoincCode usage |
|---|---|
| `bloodWork` | LOINC codes for each analyte |
| `imaging` | DICOM reference via `FileHolonId` |
| `genetic` | Result summary; full data on IPFS |
| `ecg` | Rhythm interpretation + raw file |

`Interpretation` enum (`normal` / `abnormal` / `critical`) drives triage. `critical` results trigger an `emergency` access grant automatically for the treating physician.

---

### Zome 4 — PrescriptionZome

#### PrescriptionHolon
A medication prescription.

| Field | Notes |
|---|---|
| `DosageForm` | `tablet` / `capsule` / `liquid` / `injection` / `inhaler` / `patch` / `topical` |
| `Frequency` | Free text: "once daily at night", "BD with food" |
| `Duration` | "14 days" / "ongoing" |
| `ExpiresAt` | Repeat prescriptions expire — triggers renewal reminder |
| `Status` | `active` → `completed` / `cancelled` / `expired` |

Active prescriptions (`Status: active`) are surfaced to any provider with a `ConsentGrantHolon` covering `RecordTypes: ["prescriptions"]`.

---

### Zome 5 — ConsentZome

This is the most important zome — it controls all access.

#### ConsentGrantHolon
A patient's explicit permission for a specific party to read specific record types.

| Field | Notes |
|---|---|
| `GranteeType` | `provider` / `insurer` / `researcher` / `employer` / `family` / `emergency` |
| `RecordTypes` | `string[]` — e.g. `["consultations", "prescriptions"]` (not TestResults) |
| `ExpiresAt` | Time-limited consent — auto-expires; no indefinite grants |
| `IsRevoked` | Patient can revoke at any time — `RevokedAt` is set |
| `OnChainTxHash` | Consent grant recorded on-chain — patient has cryptographic proof it was given |

**Minimal disclosure principle:** a patient grants a GP access to `["prescriptions", "allergies"]` but not `["testResults", "mentalHealth"]`. Each array element is a separate permission.

**Emergency access:** `GranteeType: emergency` grants read access to `BloodType`, `Allergies`, and `ChronicConditions` — even without the patient's active consent — for verified emergency providers only.

#### AccessLogHolon
Every read is logged — no silent access.

| Field | Notes |
|---|---|
| `AccessorAvatarId` | Who accessed |
| `ConsentId` | Which grant authorised this access |
| `RecordTypeAccessed` | What they looked at |
| `WasAuthorised` | `false` = access was denied (attempted unauthorised access) |
| `AccessMethod` | `api` / `ui` / `export` / `emergency` |

The full log is queryable by the patient at any time — complete transparency over who has seen their data.

---

### Data Flow Example: GP Consultation

```
1. Patient grants consent (ConsentGrantHolon):
   GranteeType: provider, GranteeAvatarId: Dr. Smith's Avatar
   RecordTypes: ["consultations", "prescriptions", "allergies"]
   ExpiresAt: 2026-12-31, OnChainTxHash: 0xabc...

2. Dr. Smith authenticates with OASIS Avatar JWT

3. Dr. Smith reads PatientProfileHolon (allergies visible → ConsentGrant valid)
   → AccessLogHolon created (WasAuthorised: true)

4. Consultation occurs → Dr. Smith writes ConsultationHolon
   (encrypted, IpfsCid set, CreatedByAvatarId: Dr. Smith)

5. Prescription written → PrescriptionHolon created (Status: active)

6. Patient revokes consent:
   ConsentGrantHolon.IsRevoked = true, RevokedAt = now, OnChainTxHash = 0xdef...

7. Dr. Smith attempts to read → Access denied
   → AccessLogHolon created (WasAuthorised: false)
   → Patient sees this in their audit log
```
