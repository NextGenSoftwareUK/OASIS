# STAR API: remote vs native transport — architecture notes

**Purpose:** Capture the decision space for **in-process OASIS** (`star_transport: native`) versus the shipped **HTTP client** (`remote`), so future work does not re-derive size, AOT, and split-build trade-offs from chat history.

**Status (repo):**

- **Remote:** Default. `OASIS Omniverse/STARAPIClient` (NativeAOT `star_api`) talks to WEB5/WEB4 over HTTP.
- **Native flag:** Games and `star_api_config_t` expose `transport` / `oasis_dna_path`; the **default** `star_api` build returns **`InitFailed`** with an explicit message for `native` (no silent fallback) — see policy in `Docs/Devs/AGENT_Root_Cause_No_Fallbacks.md`.
- **DNA / HyperDrive plumbing:** `AutoFailOverLocalProviders` + `AutoFailOverLocalProvidersEnabled` on `StorageProviderSettings`; `ProviderManager` local list + `ActivateNextLocalAutoFailOverStorageProvider()`; `OASISBootLoader` loads from DNA. Native host code should call the activation helper when offline/local failover is required; **automatic offline detection + background resync** is **not** wired in the slim client.

---

## What the “current” client is

| Aspect | Shipped `STARAPIClient` (`star_api`) |
|--------|--------------------------------------|
| TFM | `net9.0` |
| Publish | **NativeAOT** (`PublishAot`), `IlcTrimMode` **copy** (stability over minimal size) |
| References | **OASIS Common** + prebuilt **API.Contracts** DLL (AOT-friendly reference pattern) |
| Does **not** reference | **API.Core**, **OASISBootLoader**, provider projects |

It is intentionally a **thin HTTP + JSON** layer suitable for **OQuake / ODOOM** and small native interop surface.

---

## What “full native” implies

**OASISBootLoader** (`NextGenSoftware.OASIS.OASISBootLoader`) targets **net8.0** and **project-references** **API.Core**, **DNA**, and **many provider assemblies** (storage, network, blockchain, cloud, etc.). That is a **large transitive dependency graph** (third-party SDKs, DB drivers, optional chains, logging).

Bringing that **into the same NativeAOT binary** as today’s `star_api` means:

1. **Binary size:** Expect **large growth** (often **tens of MB** toward **100+ MB** depending on RID and how much ILC can trim) versus the current slim DLL — not a “small bump” if the **full** BootLoader graph is linked.
2. **NativeAOT / trimming:** Many providers and dependencies assume **reflection**, **dynamic code**, or patterns **not** guaranteed AOT-safe. A monolithic “BootLoader inside `star_api`” build typically needs **dedicated ILC spikes**, **descriptor roots**, and possibly **excluding** unsafe providers from the native build.
3. **Runtime cost:** For **remote-only** users, cost stays low **only if** native stacks are **not** booted until `transport == native`. Once native runs, cost is comparable to a **small integrated OASIS endpoint** (DNA load, provider registration/activation, managers, optional replication).

---

## Overhead summary

| Concern | Remote-only (default) | Native path |
|--------|------------------------|-------------|
| **CPU / RAM at idle** | Minimal (HTTP client) | Full OASIS boot + active provider(s) |
| **Disk (approx.)** | Current `star_api` native output | **Much larger** if full BootLoader + providers are compiled in |
| **Build / maintainability** | Established AOT pipeline | New matrix: AOT vs JIT, provider subsets, TFM alignment (net8 vs net9) |

---

## Design options (avoid duplicating *behavior*)

1. **Single fat `star_api`**  
   One NativeAOT binary with Core + BootLoader (+ providers). **Simplest to ship one file**; **hardest** for AOT size and compatibility unless the provider set is **radically narrowed**.

2. **Base + optional native artifact (recommended direction)**  
   - Keep **current `star_api`** as the **default / slim** line for games.  
   - Add **`star_api_native`** (second native library) **or** a small **host process** that exposes the **same C ABI** (`star_api_init`, etc.) but links **OASIS runtime** (possibly **subset** of providers aligned with `AutoFailOverLocalProviders`).  
   - Games choose DLL at deploy time or via launcher, **or** `dlopen` the native build when `star_transport` is native.

3. **Shared library (no duplicated quest/inventory logic)**  
   Extract **shared C#** (DTO mapping, validation, error codes, optional shared HTTP client helpers) into a **library** referenced by both **slim AOT** and **native host**. Only **composition roots** differ (HTTP vs BootLoader), not necessarily all business rules.

**Duplication is optional:** prefer **one shared “STAR client core”** assembly + two hosts, rather than forking two full copies of `StarApiClient.cs`.

---

## Recommendation (for when implementation resumes)

- **Treat shipped NativeAOT `star_api` as the baseline** for most users and CI/game pipelines.  
- **Implement native** as a **separate build product** (or host) unless a **scoped spike** proves that a **minimal** provider-only graph meets size and AOT constraints in **one** binary.  
- Align the **native** dependency set with **local/offline** DNA (`AutoFailOverLocalProviders`) rather than pulling the **entire** BootLoader `.csproj` as-is into AOT without analysis.  
- Wire **offline / online** transitions in the **native host**: call `ProviderManager.ActivateNextLocalAutoFailOverStorageProvider()` when policy says “local only”; use existing **HyperDrive / replication** APIs for **resync** when connectivity returns — **explicit**, not hidden fallbacks in the HTTP client.

---

## Related code and docs

| Area | Location |
|------|-----------|
| Slim client | `OASIS Omniverse/STARAPIClient/` |
| C ABI / config | `star_api.h`, `star_api_config_t.transport`, `oasis_dna_path` |
| Games | `oquake_star_integration.c`, `uzdoom_star_integration.cpp`, `oasisstar.json` (`star_transport`, `oasis_dna_path`) |
| DNA | `NextGenSoftware.OASIS.API.DNA` — `AutoFailOverLocalProviders*`, default `OASIS_DNA.json` samples |
| Provider lists + failover | `ProviderManager` — `GetProviderAutoFailOverLocalList`, `ActivateNextLocalAutoFailOverStorageProvider` |
| Boot load | `OASISBootLoader.LoadProviderLists` |
| Integrated native STAR (historical / server-style) | `Native EndPoint/.../STARAPI.cs` and related — compare when designing the native host |

---

## Changelog

| Date | Note |
|------|------|
| 2026-03-27 | Initial doc: overhead, split-build recommendation, pointers to implemented DNA/ProviderManager and explicit native `InitFailed` in default `star_api`. |
