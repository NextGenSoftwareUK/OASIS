# zkPass × OASIS Avatar System — Integration & Benefits

Based on the [OASIS DeepWiki](https://deepwiki.com/NextGenSoftwareUK/OASIS) and the current avatar implementation in this repo.

---

## OASIS Avatar System (Recap)

From the docs and code:

- **Identity & auth**: Registration (username, email, password, `AvatarType`), email verification, JWT + refresh tokens, login by username / email / public key.
- **Profile**: `IAvatar` (core) and `IAvatarDetail` (extended) — name, email, wallets per provider, portrait, address fields, etc.
- **Karma**: `KarmaManager` / `AvatarManager-Karma` — add/remove karma, `KarmaAkashicRecord`, karma types (positive/negative), source types (App, dApp, etc.).
- **Roles**: Wizard/Admin for authorization.
- **Provider-agnostic**: Avatars and karma flow through the provider layer (Mongo, Neo4j, blockchains, etc.).

---

## How zkPass Could Integrate

zkPass turns **Web2 private data** (KYC, financials, credentials, social/learning accounts) into **verifiable zero-knowledge proofs** without OASIS ever seeing raw PII. Integration can be additive and optional.

### 1. **Proof-based identity / KYC (avatar registration & verification)**

- **Flow**: During or after registration, user completes a zkPass verification (e.g. ZKKYC or “proof of legal identity”) via TransGate; the dApp/ONODE receives only a **proof + attestation**, not name/ID document.
- **OASIS side**: 
  - New optional field(s) on the avatar (e.g. `VerificationProofs` or a small “attestations” holon) storing **proof identifiers / attestation metadata** (and optionally a reference to an on-chain zkSBT if they mint one).
  - No storage of raw KYC data; only “avatar has passed zkPass schema X at time T” (and similar).
- **Benefit**: Stronger identity assurance for high-trust flows (e.g. Saints, gated features, compliance) while staying **privacy-first** and avoiding OASIS holding PII.

### 2. **Verifiable reputation / karma input (VRS-style)**

- **zkPass VRS** ([docs](https://docs.zkpass.org/vrs)) scores users from verified sources (e.g. Binance, LinkedIn, GitHub, Duolingo) via ZK proofs; [portal.zkpass.org](https://portal.zkpass.org) computes a 0–100 score.
- **Integration**: 
  - Allow an avatar to **link a VRS (or equivalent proof)** to their account — e.g. “proof that this wallet/avatar has VRS ≥ X” or “proof of verified LinkedIn/GitHub” without exposing the underlying data.
  - Use that as **input to karma or access**: e.g. grant karma for “verified professional identity” or “verified learning streak,” or gate features by “has verified credential set Y.”
- **OASIS side**: 
  - Store only **proof references / scores** (e.g. “VRS attestation id”, “schema id”, “score band”) and timestamp; optionally feed into `KarmaAkashicRecord` as a new `KarmaSourceType` (e.g. “zkPass” or “VerifiableCredential”).
- **Benefit**: **Richer, fraud-resistant karma**: karma can reflect real-world credentials and behavior without OASIS handling raw LinkedIn/Binance/etc. data.

### 3. **Wallet / account linkage (prove ownership without exposing keys)**

- User proves **ownership of a wallet or Web2 account** via zkPass (e.g. “this Ethereum address is controlled by the same person who holds Binance KYC”) and associates that proof with their avatar.
- **OASIS side**: When linking a `ProviderWallet` (or a new “verified external account” structure), optionally require or attach a **zkPass proof id** so that linkage is verifiable.
- **Benefit**: Fewer fake or stolen wallets linked to avatars; better Sybil resistance without asking users to hand over keys or dox themselves.

### 4. **Compliance and regulated use cases**

- For features that need **KYC/AML or jurisdictional rules**, zkPass can provide “proof of KYC” or “proof of jurisdiction” that ONODE (or a partner) verifies; the avatar only stores **“compliant for schema Z”** and expiry.
- **Benefit**: Enables **compliant DeFi, gated NFT drops, or regional access** while keeping OASIS non-custodial over personal data.

---

## Benefits Summary

| Area | Benefit |
|------|--------|
| **Privacy** | OASIS never stores raw KYC, financial, or social data; only proof metadata and attestation references. |
| **Trust** | Avatars can carry verifiable, tamper-evident credentials (identity, reputation, wallet ownership). |
| **Karma** | Karma can be driven by **verified** external data (VRS, credentials) instead of only on-chain/OASIS-native actions. |
| **Compliance** | Enables KYC/AML-friendly flows and regional gating without PII in your DB. |
| **Sybil resistance** | Proof-of-identity and proof-of-wallet ownership make it harder to farm avatars. |
| **UX** | Users prove once with TransGate (browser/App Clip/Android); many OASIS apps can reuse the same proofs. |

---

## Implementation Directions (High Level)

1. **API**: New ONODE endpoints, e.g. `POST /api/avatar/verify-zkpass` (body: proof/attestation payload from TransGate SDK) and optionally `GET /api/avatar/{id}/verifications` (metadata only).
2. **Data model**: Extend avatar (or a linked holon) with a small “verification” or “attestation” structure (schema id, proof id, timestamp, optional score/claims).
3. **Karma**: New karma source type(s) for zkPass-derived actions (e.g. “VerifiedIdentity”, “VerifiableReputation”) and optional rules that grant karma when a user links a VRS or credential proof.
4. **Client**: Integrate [zkPass TransGate SDK](https://docs.zkpass.org/developer-guides/js-sdk) in STAR/OAPP or any dApp that needs verification; redirect or embed TransGate flow, then send the returned proof to ONODE.

---

## References

- [zkPass](https://zkpass.org/) — Private Data Protocol (3P-TLS, MPC, ZKP).
- [zkPass User Guidelines](https://docs.zkpass.org/user-guidelines) — TransGate setup (Desktop/iOS/Android).
- [zkPass VRS](https://docs.zkpass.org/vrs) — Verifiable Reputation Score (0–100, portal.zkpass.org).
- [zkPass JS-SDK](https://docs.zkpass.org/developer-guides/js-sdk) — DApp integration.
- [OASIS DeepWiki — NextGenSoftwareUK/OASIS](https://deepwiki.com/NextGenSoftwareUK/OASIS) — System architecture, avatar system, provider layer, karma.
