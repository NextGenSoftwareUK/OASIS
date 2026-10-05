# Changelog

## Unreleased

- Added complete durable Our World inventory grant and update payloads, retaining value, thumbnail URL and custom
  properties across the Edge journal, optimistic projection, hosted commit and authoritative synchronization.
- Added offline inventory-stat aggregation from the authoritative avatar-detail projection so Edge-enabled clients
  do not require a REST request to render totals, values, weights, types or rarities.
- Added a portable inventory-item type wire contract for Unity/Edge while retaining the established full-Core enum
  ABI, with an automated name/value parity test preventing contract drift.

All notable changes to OGEngineClient for Unity are documented here.

## 1.0.0 - 2026-09-24

- Added the integrated OASIS Edge Runtime and portable Native Integrated Endpoint.
- Added durable SQLite operation journal, projections, checkpoints, receipts and crash recovery.
- Added HyperDrive online/offline state transitions and automatic resynchronization.
- Added ONET Edge runtime support.
- Added Android Keystore, iOS Keychain and Windows Credential Manager offline-session storage.
- Made iOS/tvOS Keychain grant replacement atomic and preserved native load error status across the Unity ABI.
- Added Android ARM64 IL2CPP and Unity 2022.3 validation.
- Added shared OGEngineClient authentication and lifecycle APIs used by Our World.
- Added first-class generic NFT, NFT-collection and GeoHotSpot synchronized entity types plus offline Karma history.
- Routed the canonical OGEngineClient GeoHotSpot read through durable Edge state when Edge mode is enabled.
- Added durable GeoHotSpot trigger evidence commands; protected quest, inventory, GeoNFT, Karma and XP effects remain hosted-authoritative.
- Added reusable durable OGEngineClient command routes for Our World quest progress, GeoNFT collection and active quest/objective selection.
- Expanded the private avatar-detail inventory projection with item type names, stackability, timestamps and image URLs needed by offline game inventory screens.
- Routed Our World inventory use/removal through the durable avatar-gameplay journal and made cross-avatar transfers atomic and idempotent in the hosted Mongo transaction store.
- Added durable avatar preferences commands/projections and offline Our World settings routing.
- Added private, server-authoritative GeoNFT collection availability projections so Edge-enabled clients can show
  exact quota, ownership and cooldown eligibility offline without permitting client-side rule overrides.
