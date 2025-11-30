# Making the OASIS Codebase Easier to Navigate

These are structural and tooling ideas to simplify maintenance without changing functionality.

## 1. Rationalize Repository Layout
- **Canonical Source Tree:** Consolidate duplicated directories. Retain one authoritative `NextGenSoftware.OASIS.*` tree and archive legacy copies in `/archive` with a README explaining their status.
- **High-Level Grouping:** Organize top-level folders by purpose (e.g., `/core-sdk`, `/services`, `/apps`, `/docs`, `/experiments`) so developers immediately see where to focus.
- **Project Boundary Clarity:** Break monolithic projects (like `NextGenSoftware.OASIS.API.Core`) into smaller assemblies or directories (`*.Core.Holons`, `*.Core.Managers`, etc.) to highlight responsibility boundaries and reduce build scope.

## 2. Improve Entry Points
- **Directory README Files:** Add short overviews to each major folder describing its role, key subprojects, and how to run relevant tests.
- **Targeted Solutions/Workspaces:** Offer slimmer solution files (e.g., `OASIS.Core.sln`, `Bridge.sln`, `SmartContractGenerator.sln`) or solution filters, letting engineers load only what they need.
- **Boot/Provider Documentation:** Document the OASIS DNA boot process and provider registration in a concise guide so new contributors understand how storage/failover config maps to ProviderManager.

## 3. Enhance Discoverability & Ownership
- **CODEOWNERS:** Assign maintainers per directory to route questions and reviews to the right people.
- **Consistent Naming:** Normalize namespace and folder names across subprojects (e.g., `UniversalAssetBridge` vs. `NextGenSoftware.*`) or add README links when divergent naming is intentional.
- **Tools Index:** Create a `tools/README.md` cataloging scripts like `deploy_devnet_oasis.sh`, `test-multichain-nft.sh`, etc., with one-line descriptions.

## 4. Modernize Tooling
- **EditorConfig & Analyzer Baselines:** Provide a root `.editorconfig` and standardized analyzer settings so formatting and linting are consistent across IDEs.
- **Dev Containers or Docker:** Supply a `devcontainer.json`/docker-compose that spins up MongoDB, Arbitrum test nodes, etc., to minimize environment drift.
- **Shared MSBuild Configuration:** Use `Directory.Build.props/targets` for shared settings and solution filters to keep light IDE configurations.

## 5. Manage Legacy Content
- **Archive Inactive Projects:** Move unused apps (e.g., `BillionsHealed`, `ReformUK_Policy`, `temp-oasis-repo`) under `/examples` or `/archive` with status notes to reduce clutter.
- **Clean Generated/Binary Files:** Relocate large PDFs, ZIP archives, and build outputs into releases or artefact storage; consider Git LFS for assets that must remain.

## 6. Tie Documentation to Code
- **Source Links in Docs:** For each GitBook page, include a pointer to the source `.md` file so developers can jump into the repo quickly.
- **Architecture Doc Change Log:** Maintain a simple changelog noting updates to documents like `OASIS_INTEROPERABILITY_ARCHITECTURE.md` to help readers track evolution.

Implementing these ideas in stages will make the repository less intimidating for newcomers while preserving functionality for existing contributors.
