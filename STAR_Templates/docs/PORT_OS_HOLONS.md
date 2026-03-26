# PortOSTemplate — Holon Reference

**STARNET ID:** `d4cd6e87-7b16-4066-9b8a-1a0949d291d0`  
**DNA:** `STAR_Templates/star_dna/PortOSTemplate.json`  
**Tags:** port · supply-chain · logistics · customs · lfg · esg · eudr · dashboard

---

## Overview

PortOSTemplate models the **complete door-to-berth cargo journey** for multi-site port deployments (Abidjan PAA, San Pedro). It feeds a System 08 / ACI-style Port Intelligence Dashboard with live KPIs across nine domains: berth, gate, yard, customs, hinterland, pre-gate, ESG, revenue, and security.

24 holons are organised into **7 zomes**. Every holon is FK-linked, forming a single queryable chain:

```
FarmHolon (AgriTraceabilityTemplate)
  └─ InlandLegHolon ──── CarrierHolon ──── WarehouseHolon
       └─ HinterlandETAHolon
       └─ InlandExceptionHolon
       └─ SlotBookingHolon ──── VehicleHolon
            └─ GateEventHolon
                 └─ ContainerHolon ──── ConsignmentHolon ──── ShipmentHolon
                      └─ YardPositionHolon ──── YardBlockHolon ──── TerminalHolon ──── PortHolon
                      └─ PortVisitHolon
                      └─ CustomsTriageHolon
                      └─ TradeDocumentHolon
                      └─ ESGCertHolon
                      └─ HandoffSLAHolon
                      └─ RevenueItemHolon
                           └─ VesselCallHolon ──── VesselHolon ──── BerthHolon
                                └─ AlertHolon
                                └─ SweepSnapshotHolon
                                └─ ShiftHolon
```

---

## Zome 1 — PortInfrastructureZome

Fixed physical geography. Every other holon traces back to a record in this zome.

### PortHolon
**Purpose:** Root identity for a port site.

| Field | Type | Notes |
|---|---|---|
| `PortId` | Guid | Primary key |
| `PortCode` | string | e.g. `ABIDJ`, `SNPED` |
| `Site` | Enum | `abidjan` \| `sanpedro` \| `other` |
| `CountryCode` | string | ISO 3166-1 alpha-2 |
| `Timezone` | string | e.g. `Africa/Abidjan` |
| `OperatorAvatarId` | Guid | OASIS Avatar of the port operator |
| `IsActive` | bool | Soft-disable flag |

**Relationships:** Parent of all `TerminalHolon`, `VesselCallHolon`, `AlertHolon`, `RevenueItemHolon`, `SweepSnapshotHolon`.

---

### TerminalHolon
**Purpose:** An operational zone within a port (container, bulk, ro-ro).

| Field | Type | Notes |
|---|---|---|
| `TerminalId` | Guid | |
| `PortId` | Guid | FK → PortHolon |
| `Name` | string | |
| `TerminalType` | Enum | `container` \| `bulk` \| `ro-ro` \| `general` \| `liquid` |
| `OperatorName` | string | |
| `IsActive` | bool | |

---

### BerthHolon
**Purpose:** An individual berth — the physical slot where a vessel ties up.

| Field | Type | Notes |
|---|---|---|
| `BerthId` | Guid | |
| `TerminalId` | Guid | FK → TerminalHolon |
| `BerthNumber` | string | |
| `MaxDraftM` | double | Maximum vessel draft in metres |
| `MaxLoaM` | double | Maximum length overall |
| `Status` | Enum | `free` \| `occupied` \| `reserved` \| `maintenance` |

**Dashboard tile:** `berth` KPI — occupied/free ratio, berth productivity.

---

### YardBlockHolon
**Purpose:** A named block or row in the terminal container yard.

| Field | Type | Notes |
|---|---|---|
| `BlockId` | Guid | |
| `TerminalId` | Guid | FK → TerminalHolon |
| `BlockName` | string | e.g. `A1`, `RF-03` (reefer block) |
| `Zone` | Enum | `import` \| `export` \| `reefer` \| `hazmat` \| `empty` \| `transship` |
| `Capacity` | int | Max TEU slots |
| `CurrentFill` | int | TEUs currently stored |
| `FillPct` | double | Computed: `CurrentFill / Capacity` |

**Dashboard tile:** `yard` KPI — fill percentage per zone.

---

## Zome 2 — VesselZome

Maritime arrivals. Feeds the berth and PCS dashboard tiles.

### VesselHolon
**Purpose:** Master record for a ship, identified by IMO number.

| Field | Type | Notes |
|---|---|---|
| `VesselId` | Guid | |
| `ImoNumber` | string | Globally unique ship identifier |
| `VesselName` | string | |
| `Flag` | string | ISO country of registry |
| `Operator` | string | Shipping line |
| `VesselType` | Enum | `container` \| `bulk` \| `tanker` \| `ro-ro` \| `general` |
| `GrossTonnage` | double | |
| `Teu` | int | TEU capacity (container vessels) |

---

### VesselCallHolon
**Purpose:** A vessel's specific visit — from ETA to ATD. The core PCS entity.

| Field | Type | Notes |
|---|---|---|
| `CallId` | Guid | |
| `VesselId` | Guid | FK → VesselHolon |
| `BerthId` | Guid | FK → BerthHolon (assigned on berthing) |
| `PortId` | Guid | FK → PortHolon |
| `VoyageNumber` | string | |
| `Eta` / `Etd` | DateTime | Estimated times |
| `Ata` / `Atd` | DateTime | Actual times (null until event occurs) |
| `Status` | Enum | `scheduled` \| `at-anchor` \| `berthed` \| `operations` \| `departed` \| `cancelled` |
| `ManifestReadinessPct` | double | % of manifest submitted before arrival |
| `ImportTeu` / `ExportTeu` | int | Planned load/discharge volumes |

**Dashboard tile:** `berth` timeline; PCS vessel schedule.

---

## Zome 3 — ShipmentZome

The cargo identity chain. Every physical movement references a holon from this zome.

### ShipmentHolon
**Purpose:** A shipment identified by its Bill of Lading. Top-level cargo identity.

| Field | Type | Notes |
|---|---|---|
| `ShipmentId` | Guid | |
| `BillOfLading` | string | Unique BL number |
| `CallId` | Guid | FK → VesselCallHolon |
| `ShipperName` / `ConsigneeName` | string | |
| `OriginCountry` | string | ISO country |
| `DestinationPort` | string | UN/LOCODE |
| `Incoterms` | string | e.g. `FOB`, `CIF`, `DAP` |
| `Status` | Enum | `pre-arrival` → `in-customs` → `released` → `delivered` |

---

### ConsignmentHolon
**Purpose:** A group of containers under one BL — carries commodity and compliance metadata.

| Field | Type | Notes |
|---|---|---|
| `ConsignmentId` | Guid | |
| `ShipmentId` | Guid | FK → ShipmentHolon |
| `HsCode` | string | Harmonised System commodity code |
| `GrossWeightKg` | double | |
| `TemperatureControlled` | bool | Triggers reefer yard placement |
| `SetTempC` | double | Required reefer temperature |
| `EudrRequired` | bool | Triggers EUDR compliance checks |
| `HazmatClass` | string | IMDG class if hazardous |

---

### ContainerHolon
**Purpose:** A single ISO container — the **atomic unit** of port logistics. Every gate, yard, and customs holon references a ContainerHolon.

| Field | Type | Notes |
|---|---|---|
| `ContainerId` | Guid | |
| `IsoNumber` | string | e.g. `MSCU1234567` |
| `ConsignmentId` | Guid | FK → ConsignmentHolon |
| `ContainerType` | Enum | `20GP` \| `40GP` \| `40HQ` \| `40RF` \| `20RF` \| `45HQ` \| `20OT` |
| `SealNumber` | string | Verified at gate |
| `CurrentTempC` | double | Reefer temp reading |
| `GrossWeightKg` | double | |
| `Status` | Enum | `inland` → `pre-gate` → `gate-in` → `yard` → `loading` → `on-vessel` → `gate-out` → `delivered` |
| `IsEmpty` | bool | Empty repositioning flag |

**Status progression** mirrors the entire container lifecycle — query `Status` to know exactly where any box is at any moment.

---

## Zome 4 — MovementZome

Every physical movement event. This is the highest-volume write path in the system.

### InlandLegHolon
**Purpose:** A transport leg from origin/warehouse to the port gate. Source of hinterland ETAs.

| Field | Type | Notes |
|---|---|---|
| `LegId` | Guid | |
| `ContainerId` | Guid | FK → ContainerHolon |
| `CarrierId` | Guid | FK → CarrierHolon |
| `WarehouseId` | Guid | FK → WarehouseHolon (origin) |
| `ModeOfTransport` | Enum | `truck` \| `rail` \| `barge` \| `multimodal` |
| `EtaDeparture` / `EtaGate` | DateTime | Planned times |
| `ActualDeparture` / `ActualArrivalZone` | DateTime | Actual times |
| `DelayRiskScore` | double | 0–1 AI-computed risk |
| `Status` | Enum | `planned` → `in-transit` → `arrived-zone` → `gate-in` |

---

### VehicleHolon
**Purpose:** A truck, train wagon, or barge used for inland transport.

| Field | Type | Notes |
|---|---|---|
| `VehicleId` | Guid | |
| `CarrierId` | Guid | FK → CarrierHolon |
| `PlateNumber` | string | OCR-matched at gate |
| `DriverName` / `DriverId` | string | |
| `VehicleType` | Enum | `trailer` \| `rigid` \| `rail-wagon` \| `barge` |
| `CurrentLegId` | Guid | FK → InlandLegHolon (active leg) |

---

### SlotBookingHolon
**Purpose:** A pre-booked gate appointment — the pre-gate domain's core record.

| Field | Type | Notes |
|---|---|---|
| `SlotId` | Guid | |
| `VehicleId` / `ContainerId` | Guid | FKs |
| `SlotWindowStart` / `SlotWindowEnd` | DateTime | Booked arrival window |
| `Status` | Enum | `booked` \| `confirmed` \| `arrived` \| `no-show` \| `cancelled` |
| `DoConfirmed` | bool | Delivery order confirmed by customs/agent |

**Dashboard tile:** `pre_gate` — slot fill rate, no-show %, DO confirmation rate.

---

### GateEventHolon
**Purpose:** A truck arrival or departure gate transaction from the OCR system (LFG System 01).

| Field | Type | Notes |
|---|---|---|
| `EventId` | Guid | |
| `VehicleId` / `ContainerId` / `SlotId` | Guid | FKs |
| `Direction` | Enum | `in` \| `out` |
| `OcrReadPlate` | string | OCR-detected plate |
| `PlateMatchResult` | Enum | `match` \| `mismatch` \| `manual` |
| `SealCheck` | Enum | `pass` \| `fail` \| `manual` \| `not-checked` |
| `Timestamp` | DateTime | Gate transaction time |
| `ProcessingTimeSeconds` | int | Gate throughput metric |
| `ExceptionFlag` | bool | Triggers an AlertHolon if `true` |

**Dashboard tile:** `gate` — avg processing time, mismatch rate, throughput/hr.

---

### PortVisitHolon
**Purpose:** A container's complete visit — gate-in to gate-out. Carries the dwell-time SLA.

| Field | Type | Notes |
|---|---|---|
| `VisitId` | Guid | |
| `ContainerId` | Guid | FK → ContainerHolon |
| `CallId` | Guid | FK → VesselCallHolon |
| `GateInTime` → `GateOutTime` | DateTime | Full dwell window |
| `DwellHours` | double | Computed metric |
| `Status` | Enum | `in-gate` → `in-yard` → `loaded` → `discharged` → `gate-out` |

---

### YardPositionHolon
**Purpose:** Current or historical position of a container in the yard — from TOS (LFG System 03).

| Field | Type | Notes |
|---|---|---|
| `PositionId` | Guid | |
| `ContainerId` | Guid | FK → ContainerHolon |
| `BlockId` | Guid | FK → YardBlockHolon |
| `Row` / `Bay` / `Tier` | string / int | Physical slot address |
| `AssignedAt` / `MovedAt` | DateTime | Time tracking |
| `Status` | Enum | `active` \| `relocated` \| `loaded` \| `gate-out` |

---

## Zome 5 — ComplianceZome

Customs triage, trade documents, and ESG/EUDR certificates.

### CustomsTriageHolon
**Purpose:** An AI customs triage decision — GREEN / YELLOW / RED (LFG System 04).

| Field | Type | Notes |
|---|---|---|
| `TriageId` | Guid | |
| `ContainerId` / `ConsignmentId` | Guid | FKs |
| `RiskLevel` | Enum | `GREEN` \| `YELLOW` \| `RED` |
| `ClearanceStatus` | Enum | `pending` → `cleared` \| `hold` \| `inspection-required` \| `seized` |
| `QueuePosition` | int | Position in customs queue |
| `AvgClearanceMinutes` | double | SLA metric |
| `InspectionType` | Enum | `none` \| `document` \| `scanner` \| `physical` |

**Dashboard tile:** `customs` — RED/YELLOW/GREEN distribution, avg clearance time, hold count.

---

### TradeDocumentHolon
**Purpose:** A trade or compliance document — BL, phytosanitary, permit, manifest.

| Field | Type | Notes |
|---|---|---|
| `DocId` | Guid | |
| `ShipmentId` / `ContainerId` | Guid | FKs |
| `DocType` | Enum | BL \| phytosanitary \| EUDR \| permit \| manifest \| commercial-invoice \| packing-list \| certificate-of-origin |
| `Status` | Enum | `pending` → `submitted` → `approved` \| `rejected` \| `expired` |
| `IpfsCid` | string | IPFS content hash for immutable storage |
| `FileHash` | string | SHA-256 file hash for integrity |

---

### ESGCertHolon
**Purpose:** An ESG or EUDR sustainability certificate — hash stored on-chain.

| Field | Type | Notes |
|---|---|---|
| `CertId` | Guid | |
| `ShipmentId` | Guid | FK → ShipmentHolon |
| `CertType` | Enum | `EUDR` \| `organic` \| `fair-trade` \| `carbon-neutral` \| `rainforest-alliance` |
| `CoveredPct` | double | % of shipment value covered |
| `Status` | Enum | `valid` \| `expired` \| `revoked` \| `pending` |
| `OnChainTxHash` | string | Blockchain proof |

**Dashboard tile:** `esg` — EUDR coverage %, certified vs non-certified volume.

---

## Zome 6 — HinterlandZome

Pre-arrival supply chain intelligence — the upstream signal feed.

### HinterlandETAHolon
**Purpose:** Predicted arrival at the port gate for a leg — the pre-arrival horizon signal.

| Field | Type | Notes |
|---|---|---|
| `EtaId` | Guid | |
| `LegId` | Guid | FK → InlandLegHolon |
| `EstimatedArrivalAt` | DateTime | Best current ETA |
| `ConfidenceScore` | double | 0–1 model confidence |
| `DelayMinutes` | int | Positive = late, negative = early |
| `DelayReason` | Enum | `traffic` \| `breakdown` \| `border` \| `weather` \| `admin` \| `unknown` |
| `Source` | Enum | `tms` \| `carrier-api` \| `barge-system` \| `rail-system` \| `manual` |

**Dashboard tile:** `hinterland` — ETA distribution, on-time %, delay reasons.

---

### WarehouseHolon
**Purpose:** An inland depot in the supply chain — origin for InlandLegHolons.

| Field | Type | Notes |
|---|---|---|
| `WarehouseId` | Guid | |
| `Name` / `Address` | string | |
| `Latitude` / `Longitude` | double | Geo anchor |
| `ContainerCapacity` / `CurrentOccupancy` | int | Fill metrics |

---

### CarrierHolon
**Purpose:** A logistics carrier — road, rail, or barge.

| Field | Type | Notes |
|---|---|---|
| `CarrierId` | Guid | |
| `Name` | string | |
| `Mode` | Enum | `road` \| `rail` \| `sea` \| `multimodal` |
| `FleetSize` | int | |
| `KpiOnTimeRatePct` | double | Historical on-time delivery rate |

---

### InlandExceptionHolon
**Purpose:** An anomaly detected before gate arrival — triggers a pre-gate alert.

| Field | Type | Notes |
|---|---|---|
| `ExceptionId` | Guid | |
| `LegId` / `ContainerId` | Guid | FKs |
| `ExceptionType` | Enum | `missed-slot` \| `temp-break` \| `seal-mismatch` \| `doc-missing` \| `delay` \| `vehicle-breakdown` \| `border-hold` |
| `Severity` | Enum | `low` \| `medium` \| `high` \| `critical` |
| `IsResolved` | bool | |

**Dashboard tile:** `inland_exceptions` — unresolved by severity, type breakdown.

---

## Zome 7 — OperationsZome

Operational state, sweep snapshots, alerts, revenue, and handoff SLAs.

### ShiftHolon
**Purpose:** A time-bounded operational shift — aggregates throughput metrics.

| Field | Type | Notes |
|---|---|---|
| `ShiftId` | Guid | |
| `TerminalId` | Guid | FK |
| `StartTime` / `EndTime` | DateTime | |
| `TrucksThroughGate` | int | |
| `CraneProductivityMph` | double | Moves per hour |
| `AvgGateTimeSeconds` | double | |
| `IncidentCount` | int | |

---

### SweepSnapshotHolon
**Purpose:** One complete dashboard sweep — OASIS becomes the system-of-record for Port OS state.

| Field | Type | Notes |
|---|---|---|
| `SweepId` | Guid | |
| `PortId` | Guid | FK |
| `SweepStartedAt` | DateTime | |
| `SweepCompletedMs` | int | Performance metric |
| `SourceHealthJson` | string | JSON health status per upstream system |
| `CanonicalPayloadHash` | string | SHA-256 of the full sweep payload |
| `AlertCount` | int | Alerts generated in this sweep |
| `DeltaVsPrevious` | string | JSON diff since last sweep |

---

### AlertHolon
**Purpose:** A high-severity Port OS alert — ROUTINE / ADVISORY / FLASH.

| Field | Type | Notes |
|---|---|---|
| `AlertId` | Guid | |
| `Domain` | Enum | `gate` \| `berth` \| `yard` \| `customs` \| `hinterland` \| `pre_gate` \| `revenue` \| `security` \| `intel` |
| `Severity` | Enum | `ROUTINE` \| `ADVISORY` \| `FLASH` |
| `Title` / `Body` | string | |
| `TriggeredAt` | DateTime | |
| `AcknowledgedAt` / `AcknowledgedBy` | DateTime / string | |
| `IsResolved` | bool | |

**Dashboard tile:** `intel` — unacknowledged FLASH alerts, alert history.

---

### RevenueItemHolon
**Purpose:** A port charge or fee — collected vs outstanding.

| Field | Type | Notes |
|---|---|---|
| `ItemId` | Guid | |
| `ChargeType` | Enum | `port-dues` \| `storage` \| `reefer-plugin` \| `gate-fee` \| `customs-fee` \| `scanning-fee` \| `overtime` |
| `Amount` / `Currency` | double / string | |
| `Status` | Enum | `outstanding` \| `collected` \| `disputed` \| `waived` |
| `DueAt` / `PaidAt` | DateTime | |

**Dashboard tile:** `revenue` — collected vs outstanding, overdue items.

---

### HandoffSLAHolon
**Purpose:** Time from inland arrival zone to gate-processed — the supply chain SLA metric.

| Field | Type | Notes |
|---|---|---|
| `SlaId` | Guid | |
| `ContainerId` | Guid | FK |
| `InlandArrivalZoneAt` | DateTime | When truck enters the port zone |
| `GateProcessedAt` | DateTime | When truck clears the gate |
| `SlaTargetMinutes` | int | Agreed SLA window |
| `ActualMinutes` | int | Computed |
| `SlaBreached` | bool | `true` if ActualMinutes > SlaTargetMinutes |
| `BreachReasonCode` | string | Classification for analysis |

---

## Flow Example: Container from Inland to Vessel

```
1. InlandLegHolon created  (carrier books a leg)
2. HinterlandETAHolon created  (TMS pushes ETA)
3. SlotBookingHolon created  (agent books gate slot)
4. GateEventHolon: direction=in  (truck arrives, OCR reads plate)
   → ContainerHolon.Status = gate-in
   → HandoffSLAHolon created (clock starts)
5. YardPositionHolon created  (TOS assigns yard slot)
   → ContainerHolon.Status = yard
6. CustomsTriageHolon: GREEN  (AI clears container)
   → TradeDocumentHolon: approved
7. YardPositionHolon.Status = loaded  (crane loads to vessel)
   → ContainerHolon.Status = on-vessel
8. VesselCallHolon.Atd set  (vessel departs)
9. SweepSnapshotHolon written  (OASIS records full sweep)
10. RevenueItemHolon: status = collected  (port dues paid)
```

---

## Dashboard Domain → Holon Mapping

| Dashboard tile | Primary holons |
|---|---|
| `berth` | VesselCallHolon, BerthHolon |
| `gate` | GateEventHolon, SlotBookingHolon, VehicleHolon |
| `yard` | YardBlockHolon, YardPositionHolon |
| `customs` | CustomsTriageHolon, TradeDocumentHolon |
| `hinterland` | HinterlandETAHolon, InlandLegHolon |
| `pre_gate` | SlotBookingHolon, HinterlandETAHolon |
| `esg` | ESGCertHolon, TradeDocumentHolon (EUDR) |
| `revenue` | RevenueItemHolon |
| `intel` / `security` | AlertHolon, SweepSnapshotHolon |
