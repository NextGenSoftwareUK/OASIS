# Timo MVP × OASIS Integration Roadmap

## Snapshot

- **Vision:** Deliver a premium, choice-first ride marketplace with offline resilience, low-fee payments, and transparent trust scoring.
- **Current Stack:** Node/Express + MongoDB backend (`ride-scheduler-be`) and Angular 15 + NgRx frontend (`ride-scheduler-fe`) with Flutterwave payments and JWT auth.
- **OASIS Advantage:** Unified identity (Avatar + Karma), STAR holon data model, Wallet abstraction, and Hyperdrive/HoloNET resilience to power Timo’s differentiated experience.

## Capability Comparison

| Timo MVP Priority | Current Build Status | OASIS Capability | Gap To Bridge |
| --- | --- | --- | --- |
| Premium ride selection & marketplace UX | Driver/car data in Mongo; Angular marketplace UI | STAR holons for drivers, vehicles, rides | Migrate entities to STAR; expose search/filter APIs via OASIS |
| Trust & safety layer | Ratings stored per booking; no shared trust score | Karma + Avatar metadata | Map ratings to Karma events; surface trust score in UI |
| Low-fee payments | Flutterwave integration only | WalletManager + provider bridge | Plug mobile money/USDC rails via Wallet API |
| Offline-first operations | No offline caching/sync | HoloNET sync, Hyperdrive failover | Add client-side caching; configure OASIS failover |
| Lean, modular backend | Express app without OASIS managers | ProviderManager, DNA-driven config, REST/Native endpoint | Integrate or replace with OASIS-driven services |
| Taxi ecosystem inclusion | Manual admin flows | Multi-avatar support, holon roles | Model fleets/associations; build tailored portals |

## Roadmap

### Phase 0 – Alignment & Sandbox (Week 0-1)

- Standing up OASIS Native or REST endpoint locally with required DNA configuration.
- Audit Mongo models vs required STAR holons (Driver, Vehicle, RideRequest, Association).
- Decide integration approach: wrap current Express API or migrate to OASIS endpoints.

### Phase 1 – Identity & Trust Foundation (Weeks 1-3)

- Integrate Timo auth flow with `AvatarManager` for registration/login and JWT issuance.
- Sync driver/rider profiles from Mongo into Avatar holons with extended metadata (language, amenities, associations).
- Start recording ride ratings and admin actions as Karma events; expose aggregated trust score in frontend components.
- Provide lightweight admin onboarding tooling for driver verification leveraging Avatar attributes.

### Phase 2 – Marketplace Data & Booking Engine (Weeks 3-6)

- Model drivers, vehicles, and ride requests as STAR holons; implement OASIS search/filter APIs for the Angular marketplace experience.
- Rework booking flow so selection, acceptance, and status changes persist through STAR rather than Mongo-only collections.
- Route notifications (email/SMS) through OASIS event hooks to keep providers swappable.

### Phase 3 – Payments & Wallet Integration (Weeks 6-9)

- Connect WalletManager to target rails (mobile money, USDC, card fallback) and replace Flutterwave-only flows.
- Implement dual-sided wallet balances, payouts, and automated receipts based on OASIS wallet transactions.
- Configure STAR-based pricing cards to support premium tiers and regional adjustments.

### Phase 4 – Offline & Resilience Layer (Weeks 9-12)

- Introduce local storage queues in Angular for ride drafts and driver browsing; sync via HoloNET when connection resumes.
- Configure Hyperdrive/ProviderManager failover policies so Avatar/STAR operations remain available during outages.
- Pilot SMS/USSD fallback confirmations for areas with limited data connectivity.

### Phase 5 – Ecosystem & Expansion (Weeks 12+)

- Model taxi associations and fleet operators as holons with delegated permissions and role-specific dashboards.
- Expand analytics: zone-based pricing, ride volume, and trust metrics surfaced through OASIS data.
- Layer loyalty and rewards (Karma-based incentives) and cross-vertical identity sharing with partner services (e.g., Zulzi).

## Immediate Next Steps

- Confirm hosting strategy for OASIS services (self-hosted vs managed) and align on infrastructure budget.
- Plan data migration scripts to seed STAR holons from existing Mongo collections.
- Prototype Avatar-authenticated login to validate compatibility with current session/JWT flows.
- Select priority payment providers for Wallet integration and gather required compliance details.

