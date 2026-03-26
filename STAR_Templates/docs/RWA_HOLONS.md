# RWA Holons — Property & Business Entity

This document covers two templates that share a common philosophy: **one canonical holon = one real-world entity**, acting as a *moving identity* across every platform that needs to reference it.

---

## RWAPropertyTemplate

**STARNET ID:** `97b83c2b-7930-4a0b-a9c3-a8b2655e18e7`  
**DNA:** `STAR_Templates/star_dna/RWAPropertyTemplate.json`  
**Tags:** rwa · property · real-estate · tokenisation · tmrw · launchboard · compliance · web4 · nft

### What it does

A single `PropertyHolon` is the authoritative identity for a real-world property. Its `Id` is the same everywhere — in TMRW Browser, Launchboard, a compliance engine, a financing portal, a marketplace. Platforms don't copy data; they query the same holon.

```
PropertyHolon  ◄─── canonical root
  ├─ PropertyAddressHolon     (location + geo)
  ├─ PropertyAreaHolon        (size metrics)
  ├─ ExternalIdHolon          (cadastral, MLS, tax parcel IDs)
  ├─ ValuationHolon[]         (time-series valuations)
  ├─ YieldHolon               (net yield, cap rate, annual rent)
  ├─ ListingHolon[]           (active and historical listings)
  ├─ OwnershipRecordHolon[]   (beneficial owners → Avatar)
  ├─ TokenisationHolon        (Web4 token + NFT bindings)
  ├─ CapTableHolon            (share class summary)
  ├─ LeaseHolon[]             (active and historical leases)
  ├─ MaintenanceHolon[]       (jobs and repairs)
  ├─ FinancingHolon[]         (mortgages, lines of credit)
  ├─ DispositionHolon         (sale/exit event)
  ├─ LegalDocumentHolon[]     (deed, title, planning — IPFS hash)
  ├─ JurisdictionHolon[]      (KYC rules per jurisdiction)
  └─ InsuranceHolon[]         (current and historical policies)
```

---

### Zome 1 — PropertyZome

#### PropertyHolon
The root. Contains the lifecycle `Status` field that drives platform behaviour.

| Field | Key values | Platform use |
|---|---|---|
| `PropertyOAPPType` | `"RWAProperty"` | Filter for all RWA property OAPPs |
| `AssetClass` | Commercial / Residential / Industrial / Land / MixedUse | TMRW asset class filter |
| `PropertyType` | Office / Retail / Multifamily / SingleFamily / Warehouse / Hotel | Detail view classification |
| `Status` | `prospect` → `underContract` → `owned` → `leased` → `sold` | Drives workflow gates |
| `Visibility` | `public` / `restricted` / `private` | TMRW discoverability |
| `CelestialScopeId` | Guid | COSMIC location — "properties in this city/region" |

#### PropertyAddressHolon
Structured address + `Latitude` / `Longitude`. Enables:
- Map-based discovery in TMRW ("properties near me")
- GeoHotSpot linkage for phygital property experiences
- Distance filters in search

#### ExternalIdHolon
Links to external registries. `CadastralId`, `MlsNumber`, `TaxParcelId`, `RegistryId`. One property, many external systems — the holon is the reconciliation point.

---

### Zome 2 — ValuationZome

#### ValuationHolon
Each write creates a **new record** — not an update. The full array forms a time series:
```
2023-01  £4.8M  (source: appraisal)
2024-06  £5.2M  (source: index)
2025-01  £5.5M  (source: lastSale)
```
Query `IsCurrent: true` for the latest. This history supports Launchboard valuation reporting and TMRW's "rising value" signals.

#### YieldHolon
Point-in-time income metrics. `NetYield`, `GrossYield`, `CapRate`, `AnnualRent`, `VacancyRate`. TMRW uses `NetYield` as a primary discovery filter ("show me >6% yield commercial").

#### ListingHolon
An active listing with `AskingPrice`, `Platform`, and `Status`. Multiple listings can exist simultaneously (e.g. listed on two platforms). `Status: sold` closes the listing without deleting it.

---

### Zome 3 — OwnershipZome

#### OwnershipRecordHolon
Each row = one Avatar's stake. Array of these gives the full beneficial ownership list at any point in time. `IsCurrentOwner: false` on transferred records preserves history.

#### TokenisationHolon
The **Web4 bridge**:
- `Web4TokenId` → the OASIS Web4 token (one logical token, many chain addresses)
- `Web4NftId` → if the property is represented as an NFT
- `TotalShares`, `SharePrice` → fractionalization parameters
- `TokenAddressByChain` → JSON map of `chainName → contractAddress` (cached from Web4 token)

To check "does this Avatar hold any shares?": query wallet balance by `Web4TokenId` for the Avatar's wallet.

#### CapTableHolon
Summary-level cap table row per share class. Full details in Launchboard via `DataroomHolonId`. Keeps the PropertyHolon lightweight while linking to the full equity record.

---

### Zome 4 — LeaseZome

#### LeaseHolon
One row per tenancy agreement. `TenantAvatarId` links the tenant's OASIS Avatar — enabling cross-platform notifications (e.g. rent reminders via the Avatar's Telegram or email).

#### MaintenanceHolon
One row per job. `ContractorRef` is a free-text reference or an Avatar ID. Building a maintenance cost history for the property is as simple as summing `Cost` across all `Status: completed` records.

---

### Zome 5 — FinanceZome

#### FinancingHolon
Tracks debt on the property. `LenderAvatarId` can be an institutional Avatar. Multiple active financing records are possible (senior + mezzanine). `Status: refinanced` closes the old record when a new one is created.

#### DispositionHolon
The terminal event. Created when the property is sold. `PropertyHolon.Status` is updated to `sold` simultaneously. Preserves buyer identity (`CounterpartyAvatarId`), sale price, and document references.

---

### Zome 6 — ComplianceZome

#### LegalDocumentHolon
Each document gets:
- `IpfsCid` — the file is pinned to IPFS; the CID is the tamper-proof reference
- `OnChainTxHash` — optional on-chain proof of the CID
- `Verified: true` — set by a compliance agent after verification

Document types cover the full lifecycle: `deed`, `titleSearch`, `planningConsent`, `buildingWarrant`, `leaseAgreement`, `purchaseAgreement`, `mortgage`.

#### JurisdictionHolon
One row per jurisdiction where the property is regulated. `KycRequired`, `AccreditedOnly`, and `SecExemption` drive which onboarding gates apply when a new investor views the property in TMRW or Launchboard.

#### InsuranceHolon
Policy tracking with `ExpiryDate` enables automated alerts when policies are near expiry. Links to `DocHolonId` for the policy document.

---

### Moving Identity Flow

```
Launchboard creates PropertyHolon (Id: abc-123)
  → TMRW discovers it via GET /api/OAPPs?filter=PropertyOAPPType:RWAProperty
  → Compliance engine checks JurisdictionHolon for KYC rules
  → Investor buys 10 shares → OwnershipRecordHolon + TokenisationHolon updated
  → Web4 token balance updated on-chain
  → Lease signed → LeaseHolon created (same PropertyId: abc-123)
  → Property sold → DispositionHolon created, Status → "sold"
  → All platforms see the same lifecycle state via the same Id
```

---

---

## BusinessEntityTemplate

**STARNET ID:** `3b0d2c9a-f558-49ea-86fc-f62550336676`  
**DNA:** `STAR_Templates/star_dna/BusinessEntityTemplate.json`  
**Tags:** business · startup · rwa · equity · cap-table · launchboard · tmrw · tokenisation · compliance · web4

### What it does

Same moving-identity pattern as Property, applied to companies, funds, SPVs, and DAOs. One `BusinessHolon` per entity — TMRW, Launchboard, and investor portals all reference the same Id.

```
BusinessHolon  ◄─── canonical root
  ├─ LegalRegistrationHolon   (incorporation, company number, LEI)
  ├─ ContactHolon             (address, website, social links)
  ├─ ExternalIdHolon          (Crunchbase, LinkedIn, Pitchbook IDs)
  ├─ ShareClassHolon[]        (Common, Preferred, SAFE, Warrants)
  ├─ CapTableEntryHolon[]     (one row per stakeholder per class)
  ├─ TokenisationHolon        (Web4 token + NFT for equity)
  ├─ FundingRoundHolon[]      (Seed, Series A, Bridge…)
  ├─ FinancialSnapshotHolon[] (revenue, EBITDA, runway time series)
  ├─ TeamMemberHolon[]        (founders, board, advisors → Avatar)
  ├─ ExitHolon                (acquisition, IPO, wind-down)
  ├─ LegalDocumentHolon[]     (bylaws, SAFEs, SAA — IPFS hash)
  ├─ JurisdictionHolon[]      (operating jurisdictions + KYC rules)
  ├─ LicenceHolon[]           (financial, sector-specific licences)
  └─ DataroomHolon            (Launchboard due diligence link)
```

---

### Zome 1 — EntityZome

#### BusinessHolon
The root record.

| Field | Key values | Use |
|---|---|---|
| `BusinessType` | Startup / SME / Fund / SPV / Trust / DAO | Drives lifecycle rules |
| `Industry` / `Sector` | Technology / FinTech / PropTech etc. | TMRW discovery filters |
| `Status` | `idea` → `preSeed` → `seed` → `operating` → `growth` → `scale` → `acquired` | Stage gate |
| `BusinessOAPPType` | `"Business"` | Filter for all business OAPPs |

#### LegalRegistrationHolon
Canonical legal identity: `CompanyNumber`, `TaxId`, `LEI` (Legal Entity Identifier), `DUNS`. These fields enable reconciliation with Companies House (UK), SEC EDGAR (US), and other registries. `LegalForm` distinguishes LLC, C-Corp, GmbH etc. — this matters for compliance rules.

#### ExternalIdHolon
`CrunchbaseId`, `LinkedInCompanyId`, `AngelListId`, `PitchbookId`. Links the OASIS holon to third-party databases. Aggregators can pull funding data from Crunchbase and write it back to `FundingRoundHolon` automatically.

---

### Zome 2 — EquityZome

#### ShareClassHolon
One record per share class. `ClassType` covers `Common`, `Preferred`, `Convertible`, `Warrant`, `Option`, `SAFENote`. `VotingRights` and `LiquidationPref` capture the key governance and financial terms without requiring a full term sheet.

#### CapTableEntryHolon
One row per stakeholder per class. `StakeholderAvatarId` links to an OASIS Avatar — enabling real-time notifications ("your Series A round is closing") through the Avatar's notification channels. `VestingStart` / `VestingEnd` support employee option tracking.

#### TokenisationHolon
Same Web4 pattern as RWAProperty:
- `Web4TokenId` — logical equity token
- `TotalSupply`, `TokenPrice` — token economics
- `TokenAddressByChain` — per-chain contract addresses
- `IsLive: true` — token is actively tradeable

---

### Zome 3 — FundingZome

#### FundingRoundHolon
Each round is a separate record — building a complete funding history. `RoundName` uses the standard enum: `PreSeed`, `Seed`, `SeriesA`, `SeriesB`, `GrowthEquity`, `Bridge`, `IPO`, `SAFE`, `Convertible`. `Valuation` + `ValuationType` (`preMoney` / `postMoney`) captures the headline number precisely.

The array of rounds enables automatic **valuation history** — TMRW can chart the trajectory from `PreSeed` to `SeriesB` using only this zome.

#### FinancialSnapshotHolon
Point-in-time financials: `Revenue`, `EBITDA`, `GrossMarginPct`, `RunwayMonths`, `BurnRateMonthly`. `Period` field (`annual`, `trailing12`, `quarterly`) clarifies the measurement window. Launchboard uses these snapshots for investor dashboards.

---

### Zome 4 — OperationsZome

#### TeamMemberHolon
One row per person. `AvatarId` links to OASIS identity — the person's Avatar carries their credentials, karma, and cross-platform history. `Role` enum covers `founder`, `ceo`, `cto`, `cfo`, `board`, `advisor`, `employee`, `contractor`. `IsCurrentMember: false` on departed team members preserves history.

#### ExitHolon
Created once — the terminal event. `ExitType` covers `acquisition`, `merger`, `IPO`, `MBO`, `windDown`, `liquidation`. `ReturnMultiple` (e.g. `3.2` = 3.2× return) is the key investor metric. `BusinessHolon.Status` → `acquired` or `closed` simultaneously.

---

### Zome 5 — ComplianceZome

#### DataroomHolon
Links this business to a Launchboard-style due diligence dataroom. `AccessLevel` enum (`public`, `nda`, `investor`, `restricted`) drives which investors can request access. `DocumentCount` and `LastUpdated` give a quick freshness signal without loading the full dataroom.

---

### Comparison: Property vs Business

| Concern | RWAPropertyTemplate | BusinessEntityTemplate |
|---|---|---|
| Root identity | PropertyHolon | BusinessHolon |
| Ownership | OwnershipRecordHolon | CapTableEntryHolon |
| Tokenisation | Web4 token → fractional shares | Web4 token → equity |
| Income | YieldHolon (rent) | FinancialSnapshotHolon (revenue) |
| Documents | LegalDocumentHolon (deed, title) | LegalDocumentHolon (bylaws, SAFE) |
| Terminal event | DispositionHolon (sale) | ExitHolon (acquisition / IPO) |
| External IDs | cadastral, MLS, taxParcel | companyNumber, LEI, Crunchbase |
| Compliance | JurisdictionHolon + InsuranceHolon | JurisdictionHolon + LicenceHolon |
