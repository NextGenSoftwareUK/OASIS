# Timo × OASIS Layered Roadmap (Separation of Concerns)

## Purpose

- Clarify technical and IP boundaries between the Timo App Layer and the OASIS Core.
- Enable LFG (and other vendors) to deliver quickly without IP ambiguity.
- Lock in continuity for Timo while preserving OASIS’s infrastructure ownership.

## Layer Definitions

### OASIS Core (owned by OASIS)
- Dispatch/booking engine domain model (STAR holons for Driver, Vehicle, RideRequest, Trip, Association).
- Identity and trust: Avatar, Karma eventing/aggregation, role/permission model.
- Offline/resilience: HoloNET sync, Hyperdrive failover, provider abstraction.
- Pricing/ETA: fare estimation rules, surge/zone pricing, ETA computation interfaces.
- Wallet/payments abstraction: WalletManager and provider bridges (mobile money, USDC, card).
- Core APIs/SDKs: OpenAPI contracts; TS/Kotlin/Swift SDKs; event hooks/webhooks.
- Observability and governance hooks: audit events, compliance logging, admin policy interfaces.

### Timo App Layer (owned by Timo)
- Rider app, Driver app, and Admin Portal UX/flows (web/mobile).
- Marketplace presentation and local business logic (city rules, tiers, promos).
- Integration adapters: payments selection, map/telemetry providers, messaging (SMS/WhatsApp/email).
- Operations tooling: KYC workflows, support dashboards, fleet/association portals.
- Marketing/CRM: referrals, loyalty/benefits UI, region/campaign configuration.
- Data views & analytics specific to Timo’s ops (dashboards, exports).

## Ownership and Licensing

- OASIS owns OASIS Core; Timo receives an exclusive, perpetual, irrevocable license for Africa, sublicensable to delivery vendors for Timo’s purposes.
- Timo owns Timo App Layer; OASIS retains a compatibility license to ensure Core interoperability.
- Contributions to OASIS Core are assigned to OASIS with a royalty‑free license back to Timo (Africa). Contributions to Timo App Layer are assigned to Timo.

## Repository and Package Boundaries

- `oasis-core` (OASIS private): Core services, APIs, SDK source; versioned packages published to registry.
- `timo-app` (Timo private): Rider/Driver/Admin frontends and server components that consume Core SDKs.
- `timo-integrations` (Timo private): adapters for payments, maps, telco, notifications; thin and swappable.

Consumption pattern:
- Timo repos depend on `@oasis/core-sdk` (TS) and mobile SDKs via semantic versioning.
- No direct source vendoring of OASIS Core into Timo repos; use registries only.

## API Contracts (Core)

- Identity
  - POST /auth/register, POST /auth/login (Avatar-backed)
  - GET/PUT /avatars/{id} (profile, roles, verification fields)
- Marketplace
  - GET /drivers/search?filters=... (availability, tier, amenities)
  - POST /ride-requests, GET/ride-requests/{id}, PATCH status
  - POST /trips/{id}/events (accept, start, complete, cancel)
- Pricing/ETA
  - POST /pricing/estimate
  - POST /eta/compute
- Wallet/Payments
  - POST /wallets, GET /wallets/{id}
  - POST /charges, POST /payouts
- Telemetry
  - POST /telemetry/locations
  - POST /telemetry/trip-events
- Webhooks/Events
  - POST to Timo endpoints: ride.updated, payment.succeeded, avatar.verified

SDKs:
- Typescript (web/Admin), Kotlin (Android), Swift (iOS); typed models for STAR holons and DTOs.

## Data Model Boundary

- System of record for Drivers, Vehicles, RideRequests, Trips, Wallets, and Trust is OASIS Core (STAR/Avatar/Karma).
- Timo App Layer maintains view models, caches, and ops-specific aggregates derived from Core data.
- Data migration scripts seed Core from existing Mongo collections; thereafter Core remains authoritative.

## Delivery Responsibilities

- OASIS
  - Own Core APIs/SDKs v1.0; publish OpenAPI and client SDKs; maintain SLA and versioning.
  - Operate Core environments or provide deployable artifacts and IaC.
  - Provide integration test suite and mock server for LFG development.
- LFG (for Timo)
  - Build/operate Timo App Layer and Integration adapters against Core SDKs/mocks, then prod endpoints.
  - Implement marketplace UX, KYC/admin flows, payments adapters, telemetry wiring.
  - Contribute Core PRs via CLA only when Core changes are required (reviewed by OASIS).

## Versioning, Release, and SLAs

- Core follows semver: MAJOR for breaking, MINOR for additive, PATCH for fixes.
- Deprecations announced with migration notes and feature flags where feasible.
- SLA targets (to be finalized): 99.5% Core API uptime; P0 < 1h response; P1 < 8h; P2 < 2bd.

## Security and Compliance

- Secrets reside in Timo App Layer for Timo-owned integrations; Core holds provider secrets for Core-managed bridges.
- PII boundary: Avatar minimal profile in Core; enriched KYC documents can remain in Timo storage with reference hashes in Core.
- Audit trails in Core for bookings, payments, and trust events; export feeds for Timo compliance.

## Clean‑Room and IP Safety

- Where prior contributions are contested, Timo/LFG re‑spec and re‑implement from requirements and API contracts without exposure to disputed code.
- All contributors to Core sign CLA; license scanners and provenance checks run in CI.

## 90‑Day Plan (Layered)

- Week 0–1
  - Sign TLA/JDA/CLA; finalize OpenAPI; publish SDKs v0.9; provide mock server and acceptance tests.
  - Stand up Core sandbox; map Mongo → STAR holons; define payment providers (mobile money/USDC).
- Week 2–4
  - LFG builds Rider/Driver/Admin on mocks; implement payments, telemetry, KYC flows in App Layer.
  - OASIS stabilizes Core services and event/webhook contracts; pricing/ETA modules.
- Week 5–8
  - Switch to Core staging; run end‑to‑end tests; data migration rehearsal; pilot city launch plan.
- Week 9–12
  - Production cutover; resilience drills (offline/failover); analytics dashboards; iterate on ops tooling.

## Acceptance and Testing

- Contract tests: App Layer consumer tests run against Core mock and staging.
- E2E: booking lifecycle, payment charge/payout, trust score update, offline sync.
- Performance: search latency p95, booking throughput, webhook delivery reliability.

## References

- Current MVP repos: `ride-scheduler-be`, `ride-scheduler-fe`, `Timo-Android-App`
- Related docs: Timo_MVP_Roadmap.md, PathPulse_OASIS_Integration_Guide.md


