# Timo MVP: Core Priorities

The following points are what we’ve identified as priorities, and will be matching them to technical solutions.

### 1. **Premium Ride Experience**

- **Problem:** Durban lacks Uber Black or premium ride options.
- **MVP Goal:** Create a **high-quality, premium ride service** that guarantees the type of car and service the user expects.
- **UX Feature:** Riders **choose their own driver and vehicle** (not matched algorithmically).

> “You request a car and then the car can pull up and then you can be very surprised what you pull up... So we’re moving away from random assignment.”

---

### 2. **Marketplace-Like Selection**

- **Feature:** **Users browse drivers like e-commerce listings**, including reviews, vehicle stats, language, and comfort features.
- **Goal:** Build **trust and transparency**, particularly critical for safety (esp. for women riders).
- **Differentiator:** Choice-first ride matching model, not proximity-based.

> “It’s like a mobility marketplace… you can choose your own driver, see reviews, features, and so on.”

---

### 3. **Lower Operating Costs**

- **Problem:** Current payment gateways (like Flutterwave) are too expensive.
- **MVP Goal:** Integrate **mobile money or crypto rails** to significantly reduce transaction/merchant fees.
- **Implication:** Payment flexibility is a **must-have**, not a future feature.

> “The variable costs that stem from merchant fees… can be substituted with mobile money — that would be a game changer.”

---

### 4. **Offline Functionality**

- **Problem:** Connectivity issues and high mobile data costs in South Africa.
- **MVP Goal:** Enable **offline-first operations** — storing data on-device and syncing later.
- **Strategic Benefit:** Differentiates Timo from Uber/Bolt, whose apps break when offline.

> “Going offline would work magic. Africa and the issue of mobile data and stuff...”

---

### 5. **Driver & Rider Trust System**

- **Feature:** Include a **visible trust score** or reputation layer (e.g. via Karma API).
- **Use case:** Riders feel safer, and drivers get rewarded for good behavior.
- **Future Opportunity:** Could be used to **gate access to the platform** or unlock perks.

> “We give our clients the ability to choose their driver… that plays a role in safety and trust between rider and driver.”

---

### 6. **Lean, Flexible Architecture**

- **Tinyiko explicitly stated** they want to **avoid complex microservices**, and emphasized the need for:
  - Modular pricing logic
  - Localized deployment
  - Ability to **scale without bloated architecture**

> “We’ve had tough experiences with microservices… we're looking for a simplified, flat framework that really delivers.”

---

### 7. **Driver Acquisition + Local Ecosystem Inclusion**

- **Challenge:** Competing for drivers already on Uber/Bolt.
- **Opportunity:** Make drivers feel **seen and valued** (e.g. by enabling profile customization, ownership).
- **Strategic ask:** Possibly create a **parallel platform for taxi associations** to avoid territorial conflict.

> “Taxi associations are quite strong… maybe we can build a platform for them so they feel part of it.”

---

## Summary Table: Timo MVP Priorities

| Priority | Description |
| --- | --- |
| Premium Rides | Rider chooses exact vehicle/driver for trust and experience |
| Marketplace UX | E-commerce-style browsing of drivers and cars |
| Mobile Money Integration | Replace Flutterwave; reduce fees |
| Offline Capability | Store trip data locally, sync later |
| Trust System (Karma) | Build safety through transparent rider/driver behavior |
| Simple, Lean Tech Stack | Avoid microservices; deploy modular, scalable backend |
| Local Driver + Taxi Integration | Engage traditional transport networks to reduce friction |

## 🧩 MVP Feature → 🔗 OASIS Module Mapping

| 🧩 **Timo MVP Priority** | 🔗 **OASIS Module / Capability** |
| --- | --- |
| **Premium Ride Selection** | `Avatar API`: Each driver and rider has a customizable profile, incl. metadata + visual UX |
|  | `STAR`: Dynamic data storage of vehicle details, tags (e.g. luggage space, child seat, etc.) |
|  | `OASIS Frontend Support`: Optional UX/UI assistance to build marketplace-style interface |
|  |  |
| **Mobility Marketplace UX** | `Holons in STAR`: Each driver/vehicle is a holon; searchable, filterable, attachable to rides |
|  | `Avatar + Karma`: Trust scores and metadata shown alongside each profile |
|  |  |
| **Mobile Money Integration** | `Wallet API`: Connects fiat, mobile money (e.g. M-Pesa), USDC, and stablecoins |
|  | `OASIS Payments Bridge`: Abstracted payments module (can integrate Flutterwave, MTN, etc.) |
|  |  |
| **Offline Functionality** | `HoloNET + Holochain`: Store ride requests, GPS, and payment data locally until reconnected |
|  | `OASIS Hyperdrive`: Auto-sync and fallback to best available node (Web2, Web3, or local cache) |
|  |  |
| **Trust Layer / Safety** | `Karma API`: Driver and rider actions logged; trust score calculated from reviews and behavior |
|  | `Avatar API`: Scores and flags tied to a persistent identity across all interactions |
|  |  |
| **Lean, Scalable Infrastructure** | `.NET + Modular REST API`: Easy deployment via cloud or local instances |
|  | `Providers API`: Switch between MongoDB, SQLLite, Neo4j, etc. based on environment |
|  | `Hyperdrive`: Load-balancing and auto-failover ensures uptime across local + distributed stack |
|  |  |
| **Taxi Association Inclusion** | `Multi-Avatar Support`: Separate portals for different user roles (drivers, taxis, fleet mgrs) |
|  | `White-labeling`: Easily spin up “Timo Local” versions for co-branded regional taxi partners |

---

## 🧠 MVP Architecture Summary Diagram (Textual)

```
pgsql
CopyEdit
                           +---------------------+
                           |   Timo Web/Mobile   |
                           |     Interface       |
                           +---------------------+
                                     |
                                     v
                      +-----------------------------+
                      |   OASIS Identity (Avatar)    |
                      |  + Trust Layer (Karma)       |
                      +-----------------------------+
                                     |
              +----------+----------+-------------+-----------------+
              |          |                        |                 |
              v          v                        v                 v
      [Wallet API]  [STAR (Holons)]         [HoloNET Sync]     [Map Provider Abstraction]
        Mobile $       Vehicles,                Offline              Google, Mapbox, OSM
      / USDC / M-Pesa   Drivers, Metadata        Storage
                        Ratings, Filters

                                     |
                            +----------------+
                            |     Hyperdrive |
                            |  (Auto Failover |
                            |   + Load Balance)|
                            +----------------+

```

---

```
                 Timo x OASIS Integration Roadmap

                         ┌────────────────────────┐
                         │   Phase 1: Foundation  │
                         │     Weeks 1–4          │
                         └────────────────────────┘
                                   │
   ┌────────────────────────────────────────────────────────────┐
   │  • Avatar: Rider/Driver accounts, KYC                      │
   │  • Karma: Trust scores, behavior logging                   │
   │  • Admin Portal (light): Driver onboarding, user mgmt     │
   └────────────────────────────────────────────────────────────┘
                                   │
                                   ▼
                         ┌────────────────────────┐
                         │   Phase 2: Core MVP     │
                         │     Weeks 5–10          │
                         └────────────────────────┘
                                   │
   ┌────────────────────────────────────────────────────────────┐
   │  • Ride Booking Engine (select driver, track ride)         │
   │  • STAR Holons: RideRequest, DriverProfile, VehicleData    │
   │  • Wallet API: Mobile money, USDC, crypto support          │
   │  • Map Layer: Live tracking, route ETAs                    │
   │  • Offline Mode: HoloNET sync, SQLite fallback             │
   │  • Trust UX: Karma scores in UI, ride ratings              │
   └────────────────────────────────────────────────────────────┘
                                   │
                                   ▼
                         ┌────────────────────────┐
                         │ Phase 3: Expansion      │
                         │   Weeks 11–16+          │
                         └────────────────────────┘
                                   │
   ┌────────────────────────────────────────────────────────────┐
   │  • Dynamic pricing by zone (STAR config)                   │
   │  • Admin dashboard: metrics, ride volume, regions          │
   │  • Taxi portal: special interface for association drivers  │
   │  • Enhanced driver UX: preferences, filters, alerts        │
   │  • Role-based portals: e.g. fleet managers                 │
   └────────────────────────────────────────────────────────────┘
                                   │
                                   ▼
                         ┌────────────────────────┐
                         │ Phase 4: Future Add-ons │
                         │      Post-MVP           │
                         └────────────────────────┘
                                   │
   ┌────────────────────────────────────────────────────────────┐
   │  • Loyalty Layer: Karma-based rewards                      │
   │  • Zulzi Integration: Shared identity + trust + wallet     │
   │  • Unified Agent Roles: Ride and Delivery agents           │
   │  • Our World AR Features: Geo-quests, navigation           │
   └────────────────────────────────────────────────────────────┘

```

### ✅ 1. **User & Driver Accounts**

| Feature | Roadmap Location |
| --- | --- |
| Registration & Login (email, phone, wallet) | Phase 1 – Avatar API |
| Profile Management (name, photo, vehicle) | Phase 1 – Avatar Holons |
| Verification (KYC, documents) | Phase 1 – Admin Portal (manual onboarding) |
| Ratings & Reviews | Phase 2 – Karma API + Trust UX |

KYC/manual verification assumed in Phase 1 admin tooling.

---

### ✅ 2. **Booking & Dispatch System**

| Feature | Roadmap Location |
| --- | --- |
| Ride Request Flow | Phase 2 – Booking Engine (choose driver) |
| Matching Engine | Phase 2 – Custom (manual select vs auto-assign) |
| Ride Status Tracking | Phase 2 – STAR + Hyperdrive |
| Notifications (SMS/push) | Phase 2 – STAR triggers + external integration (not core OASIS) |

✔️ Fully covered — matching is user-driven in Timo’s model.

---

### ✅ 3. **Map & Routing Services**

| Feature | Roadmap Location |
| --- | --- |
| Live Driver/Rider Location | Phase 2 – Map Layer |
| ETAs & Route Calculation | Phase 2 – Map Provider Abstraction |
| Traffic Awareness | Phase 3 – Optional, dependent on provider |
| Map Integration (Google, OSM, Mapbox) | Phase 2 – OASIS Map Manager |

✔️ Covered at MVP with extensibility in Phase 3.

---

### ✅ 4. **Payments & Pricing**

| Feature | Roadmap Location |
| --- | --- |
| Fare Estimation | Phase 2 – STAR pricing logic |
| Payments (card, mobile money, USDC, fiat) | Phase 2 – Wallet API |
| Driver Earnings | Phase 2 – Wallet + Admin dashboard |
| Receipts & Invoicing | Phase 2/3 – STAR-generated + external option |

✔️ Fully covered. Receipts not deeply specified, but assumed via STAR or simple add-on.

---

### ✅ 5. **Trust & Safety**

| Feature | Roadmap Location |
| --- | --- |
| Trust Score | Phase 1/2 – Karma API |
| SOS / Emergency Button | Phase 2 – Marked as optional |
| Driver Preferences (e.g. Women-only rides) | Phase 3 – Avatar filters + UI metadata |
| Blacklist / Flagging | Phase 2 – Karma + STAR logging |

✔️ Covered. Emergency button acknowledged as optional for MVP.

---

### ✅ 6. **Admin Tools**

| Feature | Roadmap Location |
| --- | --- |
| Dashboard (bookings, earnings, activity) | Phase 3 – Admin Dashboard |
| Driver Onboarding | Phase 1 – Manual via Admin Portal |
| Region/Zone Control | Phase 3 – Multi-region pricing config |
| Analytics & Reporting | Phase 3 – STAR + dashboard |

✔️ Fully covered across all phases.

---

### ✅ 7. **Offline & Resilience Layer**

| Feature | Roadmap Location |
| --- | --- |
| Local Data Cache | Phase 2 – HoloNET + SQLite fallback |
| Sync When Online | Phase 2 – HoloNET auto-sync |
| Fallback Channels (SMS, mesh) | Phase 3 – Optional under HoloNET |


