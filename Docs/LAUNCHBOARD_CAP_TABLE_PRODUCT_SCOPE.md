# Launchboard Cap Table Product Scope

## Goal

Build a real-time, legally-grounded tokenised cap table where every ownership entry is created from a signed contract document and cryptographically verifiable.

For onboarding, the key is that the system does not ask users to re-enter cap table data—we derive the ownership state from existing legal documents.

---

## Core Profiles

1. **Company has no cap table / using spreadsheets**
2. **Company using Carta / Mantle / Pulley etc**
3. **Law Firm / Accelerator / VC led onboarding**

---

## A. Company with no cap table / spreadsheets

- **Founder** creates workspace/onboarding flow.
- **Uploads** all executed equity agreements (PDF form best?) **OR** uploads existing spreadsheet (can we add this?).
- **System:**
  - Hashes and stores documents in data room
  - Identifies and extracts metadata
  - Builds event ledger
  - Compares extracted ownership vs spreadsheet
  - System generates cap table and flags any mismatches
- **Founder / lawyer** review and confirm.
- **System** tokenises ownership as per verified documents/agreements.
- **Live cap table.**

---

## B. Company using Carta / Mantle / Pulley etc

- **Founder** creates workspace/onboarding flow.
- **Connects** existing cap table export:
  - Open Cap Table JSON import, or
  - CSV export
- **Uploads** executed equity agreements archive.
- **System:**
  - Imports current cap table state
  - Builds ledger from documents
  - Cross-checks imported state vs derived state
  - System highlights any potential discrepancies
- **Founder/lawyer** resolves and attests clean state.
- **System** tokenises for live cap table.

---

## C. Law Firm / Accelerator / VC led onboarding

- **Law firm/VC/accelerator** creates firm workspace.
- Creates **client/portfolio company** workspace.
- **Uploads** all executed agreements.
- **System** derives cap table.
- **Law firm** (or company/fund lawyer) attests baseline state → tokenised.
- **Founder** (and lawyer) invited to dashboard.

### E.g. Onboarding during raise

- Workspace creation.
- Uploads existing agreements.
- System builds verified baseline cap table.
- New financing documents uploaded (SAFEs or priced).
- Signatures collected in-platform.
- Automatic issuance + tokenisation.
- Investor receives verification bundle.

---

## New SAFE Issuance (Native Workflow)

1. **Founder** initiates SAFE.
2. **Selects** SAFE template (e.g., YC Post-Money).
3. **Enters** investor + terms.
4. **System** generates SAFE document—or founder/lawyer uploads externally prepared SAFE.
5. **Investor and founder** sign via link.
6. **System** seals document: stores immutable copy & generates document hash.
7. **Optional** lawyer attestation/signature.
8. **System** tokenises SAFE referencing.
9. SAFE appears in **investor dashboard**.
10. **Outstanding SAFEs** table.
11. **Included** in diluted ownership modeling.

---

## SAFEs At Priced Round

1. **Founder** initiates financing round which defines new share class + price.
2. **System** identifies all outstanding SAFEs.
3. **System** calculates conversion (applies caps / discounts), produces conversion schedule.
4. **Lawyer** reviews conversion math.
5. **System** generates conversion documents:
   - Board consent
   - Conversion notices (if required)
6. **Signatures** collected (if required).
7. **System** tokenisation events:
   - Burns SAFE tokens
   - Mints preferred share tokens
   - Links new tokens to original SAFE contract hash and conversion document hash
8. **Cap table** updates live.

---

## Core Trust Established

For a real-time system:

**DOCUMENT → SIGN → ATTEST → TOKENISE → CAP TABLE**

Every ownership token references:

- Executed agreement hash
- Lawyer attestation on baseline
- Spreadsheet / legacy system is no longer authoritative after charter update
- We create stickiness via document signing → auto onboarding shareholders
- Cap table is reproducible and verifiable

---

## Next Steps

- **Governance module** to add board members and auto distribute docs for signing to their dashboard.
- **Scenario modelling** and valuation testing to simulate outcomes.

---

## Capital Market Integration

Sequence for a **secondary sale**:

1. Board approves the existence of a liquidity program
2. Company sets rules (who can sell, how much, timing, buyer eligibility)
3. Price discovery happens (either fixed price or market-based)
4. Transactions are executed
5. Company updates cap table

### A. Board Approval & Program Creation

- **Founder** initiates “New Liquidity Program”.
- **System** generates:
  - Board consent document
  - Liquidity program rules
- **Governance module** triggers board members’ signatures.
- **Lawyer** can attest program validity.
- **System** seals program document and hashes it.
- Liquidity program becomes **“Active”**.

### B. Seller/Shareholder Participation

- Eligible shareholders are **notified**.
- **Seller** selects:
  - Quantity to sell
  - Minimum price (if applicable)
- **System** locks seller’s tokens into escrow contract.
- Escrow smart contract recorded in ledger.
- Tokens now marked **“available for liquidity”**.

### C. Buyer Participation + Pricing

**Fixed Price Program**

- Issuer-approved price loaded.
- Buyers subscribe to posted price.
- No discovery needed.

**Market Price Discovery**

- Company or partner broker solicits buyers.
- Buyers submit bids within window.
- Matching engine clears price.

### D. Trade Execution

- **Matching** occurs.
- **Buyer** funds payment (via onchain payment rail / escrow).
- **System** executes transaction:
  - Transfers tokens
  - Links transaction to:
    - Original issuance contract hash
    - Liquidity program approval hash
    - Trade confirmation document hash
- **System** issues:
  - Trade confirmation documents
  - Updated cap table
- **Remits** funds to sellers accordingly.
