# Universal Asset Token (UAT) Builder

## Executive Summary
The UAT Builder is the authoring environment for creating Universal Asset Tokens inside the OASIS ecosystem. It allows compliance, legal, and product teams to compose a token's operational and regulatory blueprint by assembling pre-defined modules. Each module represents a legally enforceable obligation or operational capability. By designing tokens through the builder, stakeholders guarantee that every issuance follows the same rigor, generates an audit-ready payload, and integrates with existing trust, tokenization, and distribution infrastructure.

## Key Concepts
- **Modules:** Reusable building blocks that encapsulate required clauses, data, and workflow hooks. Core modules cover asset identity, jurisdiction, offering status, and issuer credentials; advanced modules extend revenue automation, governance, liquidity controls, and investor protections.
- **Drag-and-drop Canvas:** Visual workspace where modules are dropped, sequenced, and configured. The canvas provides real-time status, compliance alerts, and duplicate checks to prevent misconfiguration.
- **Module Inspector:** Contextual form that surfaces only the fields relevant to the selected module. Each field includes guidance, validation rules, and sample values to streamline data entry.
- **Compliance Progression:** Module badges track Draft → Needs Review → Ready states. Required modules must reach Ready before the workspace enables mint payload generation.
- **Mint Payload:** Deterministic JSON specification containing all module data, status, and policy references. The payload feeds downstream systems (trust creation, token contracts, revenue distribution, and x402 compliance services).

## How the Builder Works
1. **Select Environment:** Choose between Local, Devnet, or Testnet to align with current deployment stage.
2. **Authenticate:** Establish JWT session for secure access to contract generation and distribution endpoints.
3. **Drag Required Modules:** Place compliance anchors (identity, jurisdiction, distribution gating, KYC/KYB) onto the canvas. Duplicate protection ensures non-repeatable modules only appear once.
4. **Configure Fields:** Use the inspector to populate each module with regulator-facing data (e.g., asset description, trust trustees, valuation, investor thresholds).
5. **Add Advanced Modules:** Layer optional capabilities (revenue waterfalls, governance voting, risk controls, cash management) to match the deal structure.
6. **Validate:** Monitor status badges and system warnings. Required modules must reach Ready; optional modules can ship in Draft or Ready depending on the issuance plan.
7. **Generate Payload:** Once compliance requirements are satisfied, use Preview Payload for read-only review and Mint Ready to initiate the downstream trust and tokenization workflows.

## Why It Matters
- **Compliance-by-Design:** Encodes regulatory policy into modular building blocks, reducing the risk of ad-hoc or inconsistent implementations across deals.
- **Operational Efficiency:** Condenses what used to be multi-team document exchanges into a single interface with pre-populated samples, validation, and status tracking.
- **Auditability:** Every payload includes explicit module lineage, versioning, and completion status, enabling rapid audits and downstream traceability.
- **Platform Consistency:** Ensures that assets minted via the OASIS stack always adhere to the same interface and data contracts, simplifying integrations with the trust, token bridge, and risk engines.
- **Collaboration:** Enables legal, compliance, product, and engineering teams to co-author and review the token blueprint without writing code.

## Representative Use Cases
| Use Case | Modules Involved | Outcome |
| --- | --- | --- |
| **Institutional RWA Launch** | Core metadata, asset diligence, investor accreditation, trust structure, revenue waterfall | Produces a compliant real-world asset token with automated distribution and audit trail. |
| **Secondary Market Liquidity** | Compliance gating, distribution controls, liquidity lockups | Configures tokens for controlled secondary trading while respecting regulatory limits. |
| **Revenue Sharing Programs** | Revenue automation, cash management, reporting | Establishes automated calculation and payout cycles for tokenized cash flows. |
| **Governance-enabled Assets** | Governance oversight, voting hooks, risk monitoring | Adds governance functions for stakeholders, including voting rights and alerts. |
| **Cross-border Distribution** | Jurisdictional compliance, multi-region disclosures | Creates region-specific modules that satisfy local regulatory disclosures and limits. |

## Roles Supported
- **Legal & Compliance:** Validate that required modules are present, configure mandatory fields, and monitor readiness.
- **Product & Structuring:** Add optional modules that match deal strategy (e.g., revenue mechanics or governance).
- **Engineering & Operations:** Consume the payload to trigger downstream automations, monitor status, and maintain revisions.
- **Auditors & Trustees:** Review module stacks, statuses, and payload data for verification prior to token issuance.

## Extension Points
- **Module Library Expansion:** New modules can be authored to add jurisdictions, instrument types, or operational policies.
- **Policy Engine Integration:** Module statuses can connect to automated checks or external regulatory APIs.
- **Analytics & Reporting:** Payloads can be piped into analytics dashboards for tracking asset performance and compliance health.
- **Template Presets:** Common deal archetypes can be saved as presets, allowing rapid cloning of complex module stacks.

## Disclaimers
- The UAT Builder is an internal tool that assists with compliance orchestration. It does not replace legal consultation or regulatory filings.
- Payloads generated by the UAT Builder require validation and sign-off from licensed professionals before being used in live markets.
- Jurisdictional requirements may change. Keep module libraries and policy references up to date with current regulations.

---
*Last updated: 2025-11-09*

