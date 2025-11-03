# The ERC-S Standard (v0.1)

*to visit our website, go to [**street.app**](http://street.app)*

---

## Table of Contents

1. [Abstract - what ERC-S is / isn't](#1-abstract---what-erc-s-is--isnt)
2. [Motivation - why wrappers fail; equity-anchored approach](#2-motivation---why-wrappers-fail-equity-anchored-approach)
3. [Spec overview - SPV/Foundation; discretionary DAO payouts; non-security posture](#3-spec-overview---spvfoundation-discretionary-dao-payouts-non-security-posture)
4. [Document map - Charter, SHA, Bylaws, TA/Custody, SPA riders, Disclosures](#4-document-map---charter-sha-bylaws-tacustody-spa-riders-disclosures)
5. [Operations - auto-pause triggers; status-page SOP; evidence binder; T-minus](#5-operations---auto-pause-triggers-status-page-sop-evidence-binder-t-minus)
6. [Regulatory posture - Lenses A/B/C; residual-risk table](#6-regulatory-posture---lenses-abc-residual-risk-table)
7. [Precedents and comparison - how ERC-S differs](#7-precedents-and-comparison---how-erc-s-differs)
8. [Kled case study - tokenization as a growth engine](#8-kled-case-study---tokenization-as-a-growth-engine)
9. [Adoption path - audits, partners, intake, investor dashboard](#9-adoption-path---audits-partners-intake-investor-dashboard)
10. [Appendix](#appendix)

---

# 1) Abstract - what ERC-S is / isn't

### **What ERC-S is**

ERC-S is an on-chain and legal standard for routing discretionary economic flows from a shareholder-of-record vehicle (SPV or Foundation) to tokenholders under DAO governance, with transparent operations and guardrails. The real equity sits with the SPV/Foundation as a normal shareholder; tokenholders never receive equity rights, and distributions are discretionary only, governed by clear pause and disclosure rules. ERC-S also includes a 20-module scenario library (S#01–S#20) that standardizes how the pattern is applied across launch, operations, and exit.

ERC-S allows for potential exposure to the economic upside of startups, without it becoming a security, and without startups losing control rights over their company.

### Scenario library

| Document | Examined Potential Problem | Document Link | One-Pager summary Link |
| --- | --- | --- | --- |
| S#01 | Founder tries to withdraw or rescind SPV-held shares, breaking the equity link to tokens. | [S#01 - Founder Equity Withdrawal](https://www.notion.so/S-01-Founder-Equity-Withdrawal-266463bea9d480bb905cea2b7d4f8300?pvs=21) | [S#01 One Pager](https://www.notion.so/S-01-One-Pager-267463bea9d480e39030e5985b330360?pvs=21) |
| S#02 | Founder's personal bankruptcy lets creditors or a trustee attempt to seize or unwind equity transferred to the SPV. | [S#02 - Bankrupt Founder](https://www.notion.so/S-02-Bankrupt-Founder-267463bea9d480558b67df17e46720ae?pvs=21) | [S#02 Onepager](https://www.notion.so/S-02-Onepager-267463bea9d480be9722c8cdd19b22eb?pvs=21) |
| S#03 | Company issues new securities or expands the option pool, diluting the SPV's stake and priority. | [S#03 - Company Issues More Equity](https://www.notion.so/S-03-Company-Issues-More-Equity-267463bea9d480eb8599c0ae5d750b9d?pvs=21) | [S#03 Onepager](https://www.notion.so/S-03-Onepager-267463bea9d480db9508de63069df400?pvs=21) |
| S#04 | Buyer demands a clean cap table and pressures the founder to unwind or bypass the SPV/DAO. | [S#04 - Buyer wants clean cap table](https://www.notion.so/S-04-Buyer-wants-clean-cap-table-267463bea9d4803e8fe5c36cf8c7cd1a?pvs=21) | [S#04 Onepager](https://www.notion.so/S-04-Onepager-267463bea9d480248214cc807d5a41bd?pvs=21) |
| S#05 | Exit value is hidden or shifted via side payments, earn-outs, stock-for-stock terms, or IP splits that shortchange the SPV. | [S#05 - Exit Valuation Manipulation](https://www.notion.so/S-05-Exit-Valuation-Manipulation-267463bea9d48005a899d1853307c11f?pvs=21) | [S#05 Onepager](https://www.notion.so/S-05-Onepager-267463bea9d480c99596eda88db4f7c3?pvs=21) |
| S#06 | Regulator signals the token is a security, triggering venue halts and forcing a compliant response. | [S#06 - Regulatory Reclassification](https://www.notion.so/S-06-Regulatory-Reclassification-267463bea9d4808f945fd6130b5a03b3?pvs=21) | [S#06 Onepager](https://www.notion.so/S-06-Onepager-267463bea9d480af9f89cf6dba9d6612?pvs=21) |
| S#07 | Exit economics are disputed due to escrows, working-capital math, earn-out KPIs, or consideration mix, delaying SPV payout. | [S#07 - Exit Event Dispute](https://www.notion.so/S-07-Exit-Event-Dispute-26d463bea9d480df95a0ee0d0ff250fc?pvs=21) | [S#07 Onepager](https://www.notion.so/S-07-Onepager-26d463bea9d480378b37e2dac549936d?pvs=21) |
| S#08 | Founder captures SPV governance to move shares, waive protections, or amend bylaws without an independent check. | [S#08 - SPV Governance Capture](https://www.notion.so/S-08-SPV-Governance-Capture-267463bea9d480428754eec471244e2b?pvs=21) | [S#08 Onepager](https://www.notion.so/S-08-Onepager-267463bea9d4803ebb58d349393a0606?pvs=21) |
| S#09 | Founder cuts private side-deals with the buyer that divert consideration outside the SPV waterfall. | [S#09 - Founder Side‑Deals with Buyer](https://www.notion.so/S-09-Founder-Side-Deals-with-Buyer-267463bea9d48058b45ceee771ec781b?pvs=21) | [S#09 Onepager](https://www.notion.so/S-09-Onepager-267463bea9d4805395e7cfef64e7403e?pvs=21) |
| S#10 | Buyer refuses to recognize the SPV/DAO as a normal shareholder and conditions the deal on excluding or unwinding it. | [S#10 - Buyer Refuses to Recognize SPV/DAO](https://www.notion.so/S-10-Buyer-Refuses-to-Recognize-SPV-DAO-267463bea9d4805e960fe18560b2b054?pvs=21) | [S#10 Onepager](https://www.notion.so/S-10-Onepager-267463bea9d480929dade8faa7774ede?pvs=21) |
| S#11 | Buyer demands 100 percent control at close and pressures a cheap SPV buy-out or waivers of protections. | [S#11 - Buyer Demands Full Control](https://www.notion.so/S-11-Buyer-Demands-Full-Control-267463bea9d480f2b5b5cfb713bf6c88?pvs=21) | [S#11 Onepager](https://www.notion.so/S-11-Onepager-267463bea9d48075a095e7b4aa90ac9a?pvs=21) |
| S#12 | Buyer withholds SPV proceeds or reroutes payment using inflated escrows, set-offs, or KYC pretexts. | [S#12 - Buyer Refuses SPV Distribution](https://www.notion.so/S-12-Buyer-Refuses-SPV-Distribution-267463bea9d4806baa0fcb4d0b212a69?pvs=21) | [S#12 Onepager](https://www.notion.so/S-12-Onepager-267463bea9d480d7a16ad123625a4d7e?pvs=21) |
| S#13 | A regulatory inquiry hits during exit, freezing venues and complicating buyer conditions, comms, and payout timing. | [S#13 - Regulatory Trigger During Exit](https://www.notion.so/S-13-Regulatory-Trigger-During-Exit-267463bea9d480edbb75f38545313304?pvs=21) | [S#13 Onepager](https://www.notion.so/S-13-Onepager-267463bea9d480c4b338fbf83bb721e7?pvs=21) |
| S#14 | Acquirer imposes founder lockups and tries to extend them to the SPV or DAO, trapping value or delaying distributions. | [S#14 - Acquirer Demands Founder Lockups](https://www.notion.so/S-14-Acquirer-Demands-Founder-Lockups-267463bea9d480a29d11fc835acd89e1?pvs=21) | [S#14 Onepager](https://www.notion.so/S-14-Onepager-267463bea9d480b5806cf6b6244d5f5f?pvs=21) |
| S#15 | Tokenholders or plaintiff firms allege equity-like promises or undisclosed terms at exit and pursue class actions. | [S#15 - Tokenholder Class Action Risk (exit)](https://www.notion.so/S-15-Tokenholder-Class-Action-Risk-exit-267463bea9d4807691bccd37b74a72fb?pvs=21) | [S#15 Onepager](https://www.notion.so/S-15-Onepager-267463bea9d480a5b885d7b20cc1807c?pvs=21) |
| S#16 | Founder starts a competing entity and shifts IP, people, customers, or contracts away from the OpCo where the SPV holds equity. | [S#16 - Founder Starts Competing Entity](https://www.notion.so/S-16-Founder-Starts-Competing-Entity-26d463bea9d4807fa484e871f47f57e3?pvs=21) | [S#16 Onepager](https://www.notion.so/S-16-Onepager-26d463bea9d480d49badde55de69eff4?pvs=21) |
| S#17 | Cross-border enforcement delays enable forum shopping that can stall share or cash movement. | [S#17 - Cross-border Enforcement [DEMO-VERSION]](https://www.notion.so/S-17-Cross-border-Enforcement-DEMO-VERSION-267463bea9d480dd8f6fe065c059f5cb?pvs=21) | [S#17 Onepager](https://www.notion.so/S-17-Onepager-267463bea9d48038b52fe3b26445b440?pvs=21) |
| S#18 | Tax authority recharacterizes SPV or DAO flows, triggering withholding and freezing distributions. | [S#18 - Tax Authority Intervention [DEMO-VERSION]](https://www.notion.so/S-18-Tax-Authority-Intervention-DEMO-VERSION-267463bea9d480fc9b7dda4907a6a827?pvs=21) | [S#18 Onepager](https://www.notion.so/S-18-Onepager-267463bea9d48014a2aeedaad79336f2?pvs=21) |
| S#19 | Conflicting court orders paralyze the transfer agent or custodian and freeze the value path. | [S#19 - Jurisdictional Conflict [DEMO-VERSION]](https://www.notion.so/S-19-Jurisdictional-Conflict-DEMO-VERSION-267463bea9d480e2a6bdd1fe34525dad?pvs=21) | [S#19 Onepager](https://www.notion.so/S-19-Onepager-267463bea9d480c49803f9d60c219e7e?pvs=21) |
| S#20 | Tokenholder litigation over equity-like promises or hidden value halts SPV or DAO distributions. | [S#20 - Investor Class Action / Holder Challenge (general)](https://www.notion.so/S-20-Investor-Class-Action-Holder-Challenge-general-26d463bea9d480309aa5ca80013984e0?pvs=21) | [S#20 Onepager](https://www.notion.so/S-20-Onepager-26d463bea9d480d6a7d2c272bab4eef1?pvs=21) |
| S#21 | Internal Mismanagement & Fiduciary Enforcement | Work in progress | |

### **What ERC-S isn't**

Not a share or security token. Not a wrapper or IOU with redemption rights. Not a stablecoin or custodial deposit. Not an exchange-traded product. No guaranteed redemptions or profit rights.

→ Not equity, not a dividend claim, no redemption

→ Not a wrapper that promises 1:1 backing or conversion

→ Not an exchange-traded security or deposit product

→ Not a profit-rights instrument

### **Who it's for**

Venture-scale startups first, then mid-cap and large-cap issuers.

### **How it works in one sentence**

OpCo → contractual flows → SPV/Foundation → DAO vote → on-chain distributor → tokenholder claims.

### **Operational pattern and controls**

ERC-S makes one enforceable pattern the same for everyone: OpCo → SPV → DAO, centered on a New York seat, with pre-made mirror-order paths so relief can move quickly between Delaware, Cayman, and buyer venues. It locks in choke points with TA and custodian dual-key controls and a "final or mirror order = sufficient" instruction, and it enables escrow releases on New York awards so counterparties cannot move value or delay closings.

### **Non-security behavior and disclosures**

For investors and users, ERC-S lays out rules for non-security behavior and disciplined operations: "tokens ≠ equity," discretionary distributions, automatic pause triggers during disputes or inquiries, and a public status page backed by an evidence binder to manage class-action and regulatory optics. The SPV is one of the buyer's shareholders, and acknowledgments keep tokenholders outside the transaction perimeter, lowering execution risk at exit.

### **Module scope**

Modules span basic rails (arbitration and terms, solvency and true sale, cap-snapshot and no-issuance, buyer acknowledgments, no-leakage and expert determination, comms and regulatory posture, escrow and closing discipline) and late-stage stress tests (founder conflict, cross-border enforcement, tax-authority intervention, jurisdictional conflict, investor class action), each with phase maps and practical fixes.

### **Net**

A single standard that makes deals easier to close, lowers litigation and regulatory risk, and gives counterparties and holders a process they can trust.

---

# 2) Motivation - why wrappers fail; equity-anchored approach

### **Wrapper pain points**

- Redemption and run dynamics create destabilizing bank-run behavior
- Governance capture or counterparty failure can strand assets or slow-pay holders
- Price or oracle drift versus underlying value invites disputes at exit
- Enforceability of claims is unclear across venues and intermediaries
- Cross-border venue conflicts and inconsistent orders can freeze value
- Incident response is ad hoc; disclosures are inconsistent and late

### Equity-anchored alternative (ERC-S)

- Contractual flow lives at the company layer: OpCo pays the SPV/Foundation, which is the single record shareholder the buyer recognizes
- DAO sets a discretionary payout policy with buffers and clear auto-pause triggers for disputes, inquiries, or anomalies
- Open accounting, evidence binders, expert determination and NY arbitration enable fast, consistent resolution
- Transfer agent and custodian are pre-instructed to act only on dual signatures and final or recognized orders, with failover providers and alert webhooks

### **Why this matters for startups**

- Cleaner story for counsel and buyers: SPV is one normal shareholder; tokenholders are outside the buyer's perimeter
- Operational clarity at exit: cap-snapshot, no-issuance, escrow discipline, and expert paths reduce friction
- Governance and disclosure discipline contain regulatory optics and class-action risk via pause-first behavior and status pages

### **Mini-table: Wrapper vs Equity-anchored**

| Dimension | Wrapper | ERC-S equity-anchored |
| --- | --- | --- |
| Claim | Often implied redemption | No redemption, discretionary distributions |
| Buyer interface | Ambiguous holder perimeter | SPV is normal shareholder, buyer-facing only |
| Enforcement | Varies across venues | NY arbitration, expert determination, mirror-order playbook |
| Custody | Single chokepoint risk | TA/custodian dual-key, final-order filter, failover |
| Disclosures | Ad hoc | Status-page SOP, evidence binder, auto-pause rules |

---

# 3) Spec overview - SPV/Foundation; discretionary DAO payouts; non-security posture

### **Components**

- **Operating Company (OpCo)** - operating business
- **SPV/Foundation** - sole record shareholder that receives proceeds and interfaces with buyers
- **DAO** - sets policy, votes on discretionary distributions, operates pause rules
- **Transfer Agent and Custodian** - registrar and document control with dual-key, final-order filter, failover providers and alerts
- **Token contract** - governance-only token, no equity or redemption rights
- **Distributor** - Merkle-based payout module for claims after DAO approval
- **Auditors and status page** - regular proofs, logs, and incident communications

### **Flow**

Cash or consideration hits the SPV/Foundation. After buffer checks and a DAO vote, the distributor publishes a Merkle root for claims. Status page and evidence binder show what changed, when, and why. Auto-pause stops distributions if a dispute, inquiry, or anomaly is detected.

### **Design choices for a non-security posture (intent, not a legal conclusion)**

- Distributions are discretionary only, not mandatory
- No equity, dividend, or redemption rights to tokenholders
- Clear "tokens are not equity" messaging and pause-first behavior during uncertainty
- Buyers never interact with tokenholders; the SPV is the only shareholder of record recognized in the deal perimeter

### **Interfaces (summary)**

- Policy proposal, vote, buffer thresholds, pause and resume controls
- Distributor publishes commitments, claim windows, and post-event reports
- Evidence binder retains approvals, TA confirmations, escrow releases, expert decisions, and arbitration outcomes

### **Diagrams**

*[Figure 3: ERC-S Architecture - Image placeholder]*

*[Figure 3: Sequence Diagram - Image placeholder]*

---

# 4) Document map - Charter, SHA, Bylaws, TA/Custody, SPA riders, Disclosures

### **Document table**

| Document | Purpose | Owner | Status | Link | Last updated |
| --- | --- | --- | --- | --- | --- |
| SPV Charter | Independence, reserved matters, dual-key, purpose-lock | External counsel | TBD | TBD | TBD |
| Shareholders Agreement (SHA) | Rights: no-leakage, broad sale, MFN, fairness or expert path | External counsel | TBD | TBD | TBD |
| Bylaws / DAO Policy | Discretionary distributions, auto-pause, comms SOP | Street legal + ops | TBD | TBD | TBD |
| TA/Custody Letters | Dual-key, final-order filter, failover, alerts | Ops + counsel | TBD | TBD | TBD |
| SPA Riders (buyer-facing) | SPV acknowledgment, cap-snapshot, no-issuance, payee clarity | Deal counsel | TBD | TBD | TBD |
| Disclosure Pack | Non-equity messaging, status page, risk factors | Street comms + counsel | TBD | TBD | TBD |

### **Doc notes**

Each document is versioned, signed, and stored with exhibits and process-agent details. TA/custody letters include dual-key requirements and a "final or recognized order" standard plus failover. SPA riders add SPV payee-of-record and no alternate routing. DAO policy encodes pause triggers and status-page expectations.

---

# 5) Operations - auto-pause triggers; status-page SOP; evidence binder; T-minus

### **Auto-pause triggers and owners**

- **Smart-contract anomaly or oracle error** - DevOps triggers pause, leads incident response
- **Adverse legal notice or buyer dispute** - Legal triggers pause, owns updates and counsel interface
- **KYC or sanctions hit** - Compliance triggers pause, coordinates vendor re-checks
- **Governance or key compromise** - Board or independent supervisor triggers pause, rotates keys
- **Accounting mismatch or escrow dispute** - Finance or Legal triggers pause, moves to expert path

Resume requires independent supervisor + counsel sign-off and an update on the status page.

### **Status-page SOP**

- Severity tiers, first update within hours, daily cadence until Green
- Message templates for Inquiry, Dispute, Paused, and Cleared states
- RACI matrix for who writes, who approves, and who signs the update
- Post-mortem window and evidence pointers to audits, votes, and releases

### **Evidence binder**

- **01-governance** - proposals, votes, quorum logs
- **02-title-and-custody** - TA confirmations, custody receipts, stock powers
- **03-flows-and-escrows** - wires, escrow agreements, release certificates
- **04-expert-and-arb** - notices, decisions, awards
- **05-disclosure** - status-page updates, counsel approvals
- **06-audits** - code and financial attestations

### **T-minus checklist (T-28 to T+14)**

- **T-28** - finalize docs and riders, TA failover, incident dry-run
- **T-21** - publish Disclosure Pack, confirm pause webhooks, back up signers
- **T-14** - deploy contracts, freeze message bank, open status page
- **T-7** - publish launch notice, stage buffers and escrow rails
- **T-0** - launch, activate monitoring, publish "What ERC-S is not"
- **T+7** - publish first KPI and audit badge
- **T+14** - run post-launch review

---

# 6) Regulatory posture - Lenses A/B/C; residual-risk table

### **Lens A - Securities and issuer-level**

- **Intent**: avoid direct equity claims or redemption obligations; distributions remain discretionary only
- **Disclosure approach**: consistent non-equity messaging and pause-first behavior on uncertainty
- **Counsel touchpoints**: pre-launch doc and UI review; exit riders; pause and off-ramp templates as needed

### **Lens B - Market structure and custody**

- Exchange/ATS considerations handled by keeping token utility-only and buyer interface limited to the SPV
- Transfer restrictions and registrar discipline live at TA and custody with dual-key and final-order filter
- Listing or secondary statements are conservative; if uncertainty arises, pause and disclose

### **Lens C - AML, payments, consumer**

- KYC or sanctions handled through providers with documented SLAs and sunset conditions for buyer diligence
- Consumer comms avoid dividends or percentage claims; refund policies are clarified in the Disclosure Pack
- Tax statements are high-level; proceeds are net-of-tax with holdbacks if authorities intervene

### **Residual-risk table (subset)**

| Risk | Lens | Likelihood | Impact | Mitigation | Owner | Status |
| --- | --- | --- | --- | --- | --- | --- |
| Buyer withholds SPV payment | Market structure | Medium | High | Payee clarity, no-set-off, escrow caps, NY emergency specific performance | Legal Ops | In docs |
| Side-deal value leakage | Contract/Governance | Medium | High | No-leakage + aggregation, MFN, disclosure duty | Legal | In docs |
| Exit dispute on WC or earn-out | Contract/Process | Medium | Medium-High | Fixed definitions, expert determination, waterfall schedule | Legal Ops | In docs |
| Reclassification optics during inquiry | Securities/Comms | Medium | High | Non-equity design, auto-pause, status page, optional swap | Comms/Legal | Playbook ready |
| TA or custodian rogue instruction | Custody/Control | Low-Medium | High | Dual-key, final-order filter, failover provider | Ops | SLA signed |
| Governance capture at SPV | Governance | Low-Medium | High | Independent supervisor, reserved matters, founders off signer list | Board | In charter |
| Founder competing entity siphons IP | IP/People | Low-Medium | High | PIIA present assignment, non-solicit, access controls, emergency arb | HR/Legal | Rollout |
| Tax authority intervention | Tax/Payments | Low-Medium | Medium-High | Net-of-tax waterfall, withholding SOP, escrow for tax deltas | Finance | Planned |
| Tokenholder class action after exit | Litigation/Comms | Medium | Medium-High | Pause on dispute, evidence binder, disclosure pack | Comms/Legal | Playbook ready |
| Oracle or anomaly wrong payout | Ops/Tech | Low | Medium | Pause trigger on anomaly, multi-sig review, audit logs | DevOps | Monitoring |

---

# 7) Precedents and comparison - how ERC-S differs

### **Nearby patterns**

- **ERC-20 with payout modules** - flexible but often lacks buyer-facing rails, TA discipline, and consistent pause behavior
- **Security token frameworks (e.g., share-like tokens)** - aim for regulated claims; ERC-S keeps tokens non-equity with the SPV as the single legal shareholder
- **Bond-like claim schemes** - explicit payment promises; ERC-S is discretionary only
- **Wrapper models** - frequently rely on redemption and oracles; ERC-S anchors at the company and registrar, with expert or arbitration rails and a status-page SOP

### **What matters for founders and counsel**

ERC-S makes buyers see one normal shareholder while giving holders consistent, transparent operations. It adds registrar locks, expert and NY arbitration paths, and pause-first communications so disputes and inquiries do not become runs or lawsuits.

### **Side-by-side table (summary)**

| Criterion | ERC-20 + payouts | Security-token frameworks | Wrappers | ERC-S |
| --- | --- | --- | --- | --- |
| Equity claim | None | Yes | Often implied | None |
| Redemption | Optional | Often | Often | None |
| Buyer interface | N/A | Tokenholder registry | Ambiguous | SPV only |
| Enforcement | Varies | Regime-driven | Varies | Expert + NY arbitration |
| Custody rails | Varies | TA-like | Varies | TA dual-key, final-order filter |
| Incident tooling | Varies | Varies | Varies | Pause, SOP, evidence binder |

---

# 8) Kled case study - tokenization as a growth engine

### Company overview

Kled is a data marketplace where people and rights holders sell content to AI developers. The platform focuses on ethically sourced, licensed datasets across video, music, text and more, and positions the SPV side of licensing with AI labs and enterprises. Users can upload raw data and get paid when that data is licensed. ([kled.ai](https://www.kled.ai/?utm_source=chatgpt.com), [app.kled.ai](https://app.kled.ai/?utm_source=chatgpt.com), [RootData](https://www.rootdata.com/Projects/detail/Kled%20AI?k=MTc4MTc%3D&utm_source=chatgpt.com), [Messari](https://messari.io/project/kled-ai/profile?utm_source=chatgpt.com))

**Tokenization timing**

Kled launched its token in May 2025. The results below cover the period after that launch.

### Headline results after token launch

- Approaching 10,000 users, with the user base nearly doubling month over month since May 2025.
- 1.2 million+ website visits and 24 million+ impressions post‑launch, vs less than 15k impressions before.
- Social audience acceleration: Kled account near 7,000 followers, founder Avi 10,000+ followers.
- Deep engagement: 89% of users have active wallets connected, and 94% of beta‑access users are uploading and getting paid.
- $2 million+ raised in platform fees since token launch.
- Organic acquisition now outpacing paid channels.

### What changed because of tokenization

**1) User growth and community velocity**

- Since May 2025, Kled's user base has been nearly doubling month over month and is now approaching 10,000 users.
- The growth slope is steeper than the pre‑launch period.

**Why tokenization matters**: the token gave the community a reason to join early, invite friends, and stick around. The shared upside and mission made joining and referring feel like contributing, not just signing up.

*[Kled User Index Chart - Image placeholder]*

**2) Website engagement and reach**

- Post‑launch, the site crossed 1.2 million visits and 24 million impressions, up from less than 15k impressions before launch.

**Why tokenization matters**: the launch created a focal event, a story to share, and frequent updates to amplify. Community posts and partner callouts compounded impressions into traffic and then into signups.

*[Kled Website Impressions Chart - Image placeholder]*

**3) Social growth and earned media**

- Kled's account is near 7,000 followers; Avi's account is at 10,000+. Growth accelerated after tokenization.

**Why tokenization matters**: token launches are moments that convert curiosity into ongoing follow. Community members began posting proof‑of‑payouts and product demos, generating durable earned reach.

**4) Engagement depth and activation**

- 89% of current users have active wallets connected. This dipped from 97% after a UX change that moved wallet connect to after beta access to reduce signup friction.
- 94% of users who got beta access are actively uploading and getting paid.

**Why tokenization matters**: wallets and payouts make the product loop tangible. Users who connect a wallet and receive their first payment are far more likely to become repeat contributors.

*[Kled Engagement Rates Chart - Image placeholder]*

**5) Platform economics and sustainability**

- Over $2 million in fees raised from usage since launch.

**Why tokenization matters**: community participation translated into real platform activity and fee generation, which funds operations without relying only on traditional fundraising.

**6) Acquisition efficiency**

- Organic growth is significantly outpacing prior paid acquisition.

**Why tokenization matters**: incentives plus community status turned users into distributors. Word of mouth and social proof replaced a chunk of paid spend, improving unit economics.

### Founder perspective

> "Going from 0 to 1 for very obscure ideas that are hard to get the masses on board isn't easy, and crypto can speed up this process by 100x due to the community aspect."
> 
> — Avi Patel, founder of Kled

**Why this resonates here**: Kled needed both supply and demand. Tokenization aligned both sides quickly. Creators upload because they can get paid. AI buyers come because there is fresh, licensed data. The token tied both loops together with a visible momentum story.

### Mechanisms that drove the uplift

1. **Ownership feeling and status** - users are not just visitors; they feel part of the network. That converts to more sharing and higher return rates.
2. **Fast reward loop** - connect wallet, upload, get paid. Short feedback cycles fuel habit formation.
3. **Public milestones** - token events, listings, and community announcements create recurring news peaks that pull in new cohorts.
4. **Proof posts** - user screenshots and payouts circulating on social show the product working, which reduces skepticism and increases conversions.
5. **Aligned storytelling** - the token gave a simple narrative: help build the first licensed AI data marketplace and share in the ecosystem's growth as an early participant.

### What another project can copy

- **Design an activation moment** - use token launch as a clear invite to join, contribute, and share.
- **Shorten the reward loop** - push users from signup to first earning event quickly.
- **Instrument the funnel** - track wallet connect, first payout, and first referral as goal events.
- **Give the community a script** - publish easy share prompts and visuals people can post after their first payout.
- **Keep operations honest** - publish regular status updates so growth stays credible as numbers scale.

### Sources

- Kled official site and app copy describing the data marketplace and licensed content focus. ([kled.ai](https://www.kled.ai/?utm_source=chatgpt.com), [app.kled.ai](https://app.kled.ai/?utm_source=chatgpt.com))
- Public project profiles summarizing Kled's marketplace model and rightsholder focus. ([RootData](https://www.rootdata.com/Projects/detail/Kled%20AI?k=MTc4MTc%3D&utm_source=chatgpt.com), [Messari](https://messari.io/project/kled-ai/profile?utm_source=chatgpt.com))
- Public social posts providing context on community activity and launch momentum. ([X](https://x.com/usekled?utm_source=chatgpt.com), [X replies](https://twitter.com/useKled/with_replies?utm_source=chatgpt.com))

---

# 9) Adoption path - audits, partners, intake, investor dashboard

### **Audits**

- **Security audit** - at least 2 independent reviews before mainnet
- **Financial attestation** - verification of flows and buffers
- **Policy review** - operations and pause SOP reviewed with counsel

### **Partners**

- **Legal** - by jurisdiction for Charter, SHA, riders
- **KYC and AML** - provider SLAs and sunset timelines
- **Custody and TA** - dual-key, final-order filtering, failover and alerts
- **Analytics and Oracle** - metrics, anomaly detection, and pause hooks
- **Incident tooling** - status page, log capture, and evidence store

### **Issuer intake**

- **Qualification rubric** - stage, traction, governance readiness
- **Data room list** - cap table, board approvals, TA letters, custody receipts, solvency and true-sale file
- **Timeline** - doc review, dry runs, T-minus checklist
- **Post-launch obligations** - transparency cadence, status-page updates, audit badges

### **Investor dashboard (fields visible)**

- Supply, vesting, and holder distribution
- Payout history and buffers
- Incident log and status state
- Upcoming votes and quorum proof
- Auditor badges and latest attestations

### **Diagrams**

*[Figure 9: Partner Stack - Image placeholder]*

---

## Appendix

### **Glossary**

- **OpCo** - the operating company
- **SPV or Foundation** - the shareholder-of-record that receives proceeds and interfaces with buyers
- **DAO** - governance body setting discretionary policy and pause rules
- **Distributor** - on-chain module that publishes Merkle roots and claim windows
- **Buffer** - amount held to absorb timing or disputes before distributions
- **Auto-pause** - rule that stops distributions during anomalies, disputes, or inquiries
- **Status page** - public page with simple states and timestamps for incidents
- **No-leakage** - covenant that sweeps side payments or related-party benefits back into price
- **Cap-snapshot and no-issuance** - freeze cap table math between signing and closing
- **Expert determination** - fast, binding mechanism for KPI or accounting disputes
- **NY arbitration** - New York seated arbitration with emergency specific performance
- **Final-order filter** - TA instruction to act only on final or recognized orders, with failover protocol

### **Why NY venue and arbitration**

A single, fast forum reduces cross-border friction, enables emergency specific performance, and gives TA/custodians a clear instruction standard. StreetLegal maps title, governance, and buyer-seat issues to a one-map venue model and filters registrar actions to final or recognized orders, with mirror-order and failover protocols to move relief between seats.

### **Class-action waiver note**

Where enforceable, class-action waivers and arbitration help contain litigation risk tied to percentage talk or payout optics. The real mitigant remains behavior: pause on disputes, publish clear status updates, and keep tokens non-equity and distributions discretionary. See S#15 narratives and counter-mitigations.

---

**Document Version:** v0.1  
**Last Updated:** October 15, 2025  
**Contact:** [street.app](http://street.app)



