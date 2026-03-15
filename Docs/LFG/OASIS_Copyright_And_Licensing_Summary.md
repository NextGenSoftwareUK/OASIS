# OASIS Codebase — Copyright & Licensing Summary

**Purpose:** Summarise the current copyright and licensing structure in the OASIS / NextGen Software repo so you can answer LFG (and other partners) on licence types, copyleft risk, and what the structure allows.  
**Scope:** OASIS_CLEAN repo (NextGen Software / OASIS codebase).  
**Last checked:** Feb 2025 (file contents as of this date).

---

## 1. Bottom line (what it allows)

- **Core OASIS code (NextGen Software packages)** is **not copyleft**. Package metadata declares **MIT**. Integrating with OASIS (e.g. Pose, B2B/B2G products) does **not** legally force the partner’s proprietary code to be open-sourced.
- **Repo-level:** The file linked from the main README as “LICENSE” is **CC0 1.0**, not MIT — so there is a **documentation mismatch** (see below).
- **Dependencies:** Some sub-repos or archived components use **GPL v3** (e.g. holochain-client-csharp, archived Holochain hApp). They need to be identified in an SBOM and their impact on any distributed “product” assessed (e.g. dynamic vs static linking, distribution scope).
- **CLA / contributor IP:** No formal CLA or CONTRIBUTING file was found in the repo. Contributor IP and any “work for hire” or assignment should be confirmed in writing for LFG/legal (as raised on the LFG call).

---

## 2. What licenses exist and where

### 2.1 Repo-level / docs

| Location | License | Copyright / notes |
|----------|---------|-------------------|
| **README.md** | States **“MIT License”** and points to `./Docs/LICENSE` | — |
| **Docs/LICENSE** | **CC0 1.0 Universal** (Creative Commons public domain waiver) | No copyright retained; waiver of copyright and related rights; “use for any purpose including commercial.” |
| **NextGenSoftware-Libraries/NextGenSoftware Libraries/LICENSE** | **CC0 1.0 Universal** | Same as Docs/LICENSE. |

**Mismatch:** README says MIT but the linked `Docs/LICENSE` is CC0. For external/legal clarity, either (a) change README to say CC0 and point to Docs/LICENSE, or (b) add a separate MIT LICENSE file and point README to that. Right now the “canonical” repo licence file is CC0.

### 2.2 Core OASIS packages (C# / .NET)

All of the following declare **MIT** in the `.csproj` and **Copyright © NextGen Software Ltd** (years vary). **GPL v3 is commented out** in the same projects — so the *distributed* licence is MIT.

| Project / area | Package licence (declared) | Copyright (in .csproj) |
|----------------|----------------------------|-------------------------|
| NextGenSoftware.OASIS.API.Core | MIT | © NextGen Software Ltd 2023 |
| NextGenSoftware.OASIS.API.ONODE.Core | MIT | © NextGen Software Ltd 2023 |
| NextGenSoftware.OASIS.API.DNA | MIT | © NextGen Software Ltd 2023 |
| NextGenSoftware.Logging | MIT | © NextGen Software Ltd 2022–2044 |
| NextGenSoftware.OASIS.API.Providers.* (Sui, Hashgraph, Cardano, BaseOASIS, AlephiumOASIS, etc.) | MIT | © NextGen Software Ltd 2022 / 2025 |

So: **core OASIS code is MIT, not GPL.** No copyleft on the main API, ONODE, providers, or logging.

### 2.3 Other in-repo products / folders

| Location | License | Copyright / notes |
|----------|---------|-------------------|
| **SERV/** | MIT (own LICENSE file) | © 2026 NextGen Software UK |
| **BillionsHealed/** | MIT (own LICENSE file) | © 2025 OASIS Platform |
| **holochain-client-csharp/** | **GPL v3** | FSF standard GPL; **copyleft** — derivatives / linking may need care. |
| **holochain-client-csharp.backup/** | GPL v3 | Same. |
| **Archived/NextGenSoftware.Holochain.hApp.OurWorld/** | **GPL v3** | Archived; still in repo. |
| **Archived/NextGenSoftware.Holochain.hApp.OurWorld.External.Partial/** | (check) | — |
| **worldgen-oasis-demo/WorldGen/** | Apache License 2.0 | Copyright 2025 Ziyang Xie |
| **worldgen-oasis-demo/WorldGen/submodules/UniK3D/** | CC BY-NC 4.0 | Non-commercial only. |
| **IsoCity/** | MIT | © 2025 amilich |
| **External Libs / external-libs** (e.g. net-ipfs-http-client) | Each dependency has its own licence | Must be listed in SBOM. |
| **Providers/Blockchain/.../EOSIOOASIS/SmartContracts/** | Has own LICENSE | (Check if used in distribution.) |

### 2.4 Source file headers

Spot-check of core C# under `OASIS Architecture` did **not** show consistent per-file copyright or SPDX headers. Package-level licence is set in `.csproj` only. For a full SBOM and legal pack, consider adding a short licence/copyright notice to key files or at least documenting that the project is licensed under MIT (or CC0, if you align repo to that).

---

## 3. What this allows you to do (practical)

- **Use OASIS in proprietary products:** Yes. MIT (and CC0) allow use, modification, distribution, and commercial use without forcing the rest of the product to be open-sourced. So Pose (or any B2B/B2G product) can integrate OASIS and keep their own code proprietary, **provided** they comply with MIT (and any other licences of code they actually use).
- **Sublicense / commercial licensing:** The entity that holds the IP (e.g. Next Gen World Limited) can license the same code under other terms (e.g. commercial licence) in parallel; MIT/CC0 do not prevent that.
- **SBOM / “no copyleft” for core:** For core OASIS APIs and providers declared as MIT, you can state in an SBOM that **those components** are not copyleft and do not force B2B/B2G code to be open-sourced. You must still list any GPL (or other copyleft) **dependencies** and clarify how they are used (e.g. Holochain client, archived hApp) so partners can assess risk.
- **Patents:** CC0 and MIT do not grant patent licences explicitly; the repo does not state a patent grant. “Open permission” / patents (as mentioned on the LFG call) would need to be clarified separately (e.g. patent policy or separate licence).

---

## 4. Gaps and recommendations (for LFG / legal)

1. **Align README and LICENSE:** Either declare the repo as CC0 (and fix README) or add a root MIT LICENSE and point README to it. Avoid saying “MIT” while linking to a CC0 file.
2. **CLA / contributor IP:** Document whether contributors have signed a CLA or assigned IP (e.g. by contract). If not, get legal advice and, if needed, introduce a CLA or confirm existing assignments for past contributors.
3. **SBOM:** Produce an SBOM for “core OASIS / Holonic Braid” (and any deliverable you give to Pose or LFG) that lists:
   - All components and their licences (MIT, CC0, GPL, Apache, CC BY-NC, etc.),
   - Which are copyleft,
   - How they are used (e.g. linked, static, optional, archived).
4. **GPL components:** Clearly call out holochain-client-csharp and any other GPL usage: whether they are in the distribution path for B2B/B2G deliverables and what the implications are (e.g. if only used in optional/archived features, state that).
5. **Copyright holder:** Core packages say “NextGen Software Ltd”. Confirm that this matches the entity that holds the IP (e.g. Next Gen World Limited) and that branding/entity names are consistent for contracts.

---

## 5. Short answers for partners (e.g. LFG)

- **Is OASIS core GPL or copyleft?** No. Core OASIS packages are declared **MIT**. The repo-level file in Docs is **CC0**. There is no GPL on the main API/ONODE/provider code.
- **If we integrate OASIS (e.g. Pose), will our code have to be open-sourced?** No, for the MIT/CC0 parts. You must comply with MIT (and any other licences of code you actually use) and watch for GPL in dependencies if you distribute a product that includes them.
- **Who owns the copyright?** Declared in packages as **NextGen Software Ltd**; IP is described elsewhere as residing in **Next Gen World Limited** (UK). Confirm entity alignment with the team.
- **Is there a CLA?** Not found in the repo; contributor IP should be confirmed (e.g. CLA or assignment) for full legal clarity.

---

## 6. Reference: licence locations in repo

- `Docs/LICENSE` — CC0 1.0  
- `NextGenSoftware-Libraries/NextGenSoftware Libraries/LICENSE` — CC0 1.0  
- `SERV/LICENSE` — MIT  
- `BillionsHealed/LICENSE` — MIT  
- `holochain-client-csharp/LICENSE` — GPL v3  
- `holochain-client-csharp/NextGenSoftware.Holochain.HoloNET.Client/LICENSE` — GPL v3  
- `Archived/NextGenSoftware.Holochain.hApp.OurWorld/LICENSE` — GPL v3  
- `worldgen-oasis-demo/WorldGen/LICENSE` — Apache 2.0  
- `worldgen-oasis-demo/WorldGen/submodules/UniK3D/LICENSE` — CC BY-NC 4.0  
- Core OASIS `.csproj` files — `PackageLicenseExpression`: MIT; `Copyright`: © NextGen Software Ltd
