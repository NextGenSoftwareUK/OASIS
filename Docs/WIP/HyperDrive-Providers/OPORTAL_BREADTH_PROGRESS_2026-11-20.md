# OPORTAL breadth-first completion pass — 20 November 2026

Status: active, incomplete. Whole-site main-flow acceptance takes priority over additional hardening and optimization. This is a navigation/main-action checklist, not a claim that an opening function proves backend functionality. Retain previously accepted scoped evidence in OPORTAL_FULL_STATUS_2026-11-18.md; do not rerun unchanged accepted cohorts.

## Current source batch

Portal branch `work/oportal-subscription-performance`, latest source commit `4ffb630317e2565dfb03760e624fb326bb71b030` (retains prior tab/navigation batches):

- Settings destinations use existing subscription and HyperDrive surfaces; actual subscription summary replaces fake Free Tier. Three security placeholders remain open.
- Router covers41 feature keys, with48 hosting paths. Early clicks now wait for the actual mounted fragment; selected target, rather than any visible popup, determines history. Latest queued click wins; navigation, close and logout cancel pending opens. Failed fragment completion reports failure, without a retry loop or substituted content.
- Classic menu OAPPs, Quests, Missions, Eggs and Game now open their existing screens instead of informational placeholders.
- Router syntax passed existing Node stdin check. Current remote router, portal cache version and classic menu match the exact intended bytes. No browser/backend acceptance claimed for this new source batch.

- Seven new-menu tab destinations fixed: All/Search OAPPs, All/Search Missions, All Eggs/Egg Quests/Egg Leaderboard. Three openers accept actual tab keys; no substitute records or new backend paths. OAPP session-readiness errors now display in the list. Existing Node stdin syntax passed; all six changed remote source/cache files match intended bytes. Actual main-flow acceptance pending.

- Notification inbox now uses real initialized SDK reads and acknowledged JSON-array updates; failed/anonymous reads no longer show demo records. Explicit errors, pagination/refresh, late-fragment event delegation and no background polling. Four remote source/cache files match; Node stdin syntax passed. Actual browser/native acceptance remains pending; see OPORTAL_NOTIFICATION_PANEL_EVIDENCE_2026-11-20.json. Usage notification settings are separate and retain prior evidence.

## Main-flow checklist

Inventory:44 actual popup templates (43 declared in portal-components.js plus the lazy Developer Portal). The notifications panel and dashboard are additional surfaces, also in scope. Existing narrower acceptance remains valid; every row still requires a complete main-flow decision for the final whole-site claim.

| Popup | Main flow to finish/check | Whole-flow status |
| --- | --- | --- |
| login | Beam in and return to the requested screen | Open — scoped evidence retained, full flow not yet accepted |
| signup | Submit registration and show the actual result | Open — scoped evidence retained, full flow not yet accepted |
| forgot | Request the reset email and show the actual result | Open — scoped evidence retained, full flow not yet accepted |
| reset | Submit token and matching passwords | Open — scoped evidence retained, full flow not yet accepted |
| avatar | Load/edit profile and portrait | Open — scoped evidence retained, full flow not yet accepted |
| search-avatars | Search directory and view selected avatar | Open — scoped evidence retained, full flow not yet accepted |
| wallet | Load accounts and complete supported wallet actions | Open — scoped evidence retained, full flow not yet accepted |
| oapps | Browse OAPP lists and details | Open — scoped evidence retained, full flow not yet accepted |
| quests | Browse quest tabs and refresh | Open — scoped evidence retained, full flow not yet accepted |
| missions | Browse mission tabs and refresh | Open — scoped evidence retained, full flow not yet accepted |
| settings | Account, security, navigation, routing and billing destinations | Open — scoped evidence retained, full flow not yet accepted |
| karma | Load karma and complete supported karma actions | Open — scoped evidence retained, full flow not yet accepted |
| map | Load map and supported geo data/actions | Open — scoped evidence retained, full flow not yet accepted |
| data | Browse/load/save supported data | Open — scoped evidence retained, full flow not yet accepted |
| subscription | Plans, checkout, current subscription, usage, orders and MCP | Open — scoped evidence retained, full flow not yet accepted |
| subscription-admin | Authorized catalogue, accounts, app ownership, usage and billing controls | Open — scoped evidence retained, full flow not yet accepted |
| hyperdrive | Status/history, saved routing preferences and storage policy | Open — scoped evidence retained, full flow not yet accepted |
| provider-migration | Load providers and execute supported migration flow | Open — scoped evidence retained, full flow not yet accepted |
| nft | Load NFT lists and complete supported NFT actions | Open — scoped evidence retained, full flow not yet accepted |
| messaging | Load conversations, send messages and view responses | Open — scoped evidence retained, full flow not yet accepted |
| onet | Load network information and supported network actions | Open — scoped evidence retained, full flow not yet accepted |
| onode | Load node information and supported node actions | Open — scoped evidence retained, full flow not yet accepted |
| eggs | Load egg tabs and supported hatch action | Open — scoped evidence retained, full flow not yet accepted |
| ai | Load models/conversations and complete supported AI request | Open — scoped evidence retained, full flow not yet accepted |
| files | List and complete supported file actions | Open — scoped evidence retained, full flow not yet accepted |
| competition | Load competition lists and supported actions | Open — scoped evidence retained, full flow not yet accepted |
| clan | Load clan tabs and supported membership actions | Open — scoped evidence retained, full flow not yet accepted |
| gifts | Load gifts and supported send/claim actions | Open — scoped evidence retained, full flow not yet accepted |
| games | Load all/library/cross-game quest tabs and refresh | Open — scoped evidence retained, full flow not yet accepted |
| inventory | Load inventory and supported item actions | Open — scoped evidence retained, full flow not yet accepted |
| celestial-map | Load and select actual celestial records | Open — scoped evidence retained, full flow not yet accepted |
| social-graph | Load actual avatar relationships | Open — scoped evidence retained, full flow not yet accepted |
| oapp-map | Load actual OAPP relationships | Open — scoped evidence retained, full flow not yet accepted |
| karma-ledger | Load actual ledger and filters | Open — scoped evidence retained, full flow not yet accepted |
| chain-explorer | Load provider activity and filters | Open — scoped evidence retained, full flow not yet accepted |
| holon-designer | Add fields, preview schema and export | Open — scoped evidence retained, full flow not yet accepted |
| holon-diff | Load and compare selected revisions | Open — scoped evidence retained, full flow not yet accepted |
| provider-benchmark | Run explicit API response measurements | Open — scoped evidence retained, full flow not yet accepted |
| oapp-launcher | Find and view the selected real OAPP | Open — scoped evidence retained, full flow not yet accepted |
| activity-feed | Load activity and explicit live mode | Open — scoped evidence retained, full flow not yet accepted |
| holon-browser | Browse/filter and view actual holons | Open — scoped evidence retained, full flow not yet accepted |
| global-search | Search supported sources and follow actual results | Open — scoped evidence retained, full flow not yet accepted |
| provider-health | Run explicit health check and optional polling | Open — scoped evidence retained, full flow not yet accepted |
| dev-portal | Load developer resources and follow supported links | Open — scoped evidence retained, full flow not yet accepted |

## Additional surfaces and known gaps

- Dashboard and notifications panel: complete their main navigation/read/action pass; retain existing bearer/usage evidence.
- Settings security: Enable2FA, SignOutAllSessions and DeleteAccount remain unfinished. Current-token revoke is not all-session revocation. Avatar DELETE is not proof of permanent deletion of all associated data. No replacement claims or unsafe backend actions made.
- New-menu game entries ODOOM, OQUAKE, Our World and StarCraft2 still show Coming Soon. Connect them only to an accurate supported destination; do not invent game IDs or claim a native launch from a catalogue link.
- The dormant generic side-nav.html still contains placeholder entries; classic/new menu loading is owned by portal-components.js. Track reachability separately; do not count a dormant template cleanup as runtime completion.
- Browser opening/loading/main-action checks for the changed source remain pending. Keep shared Atlas/PIPER held; use existing isolated real-backend test resources when runtime checks become necessary.

## Coordination and cleanup

Read the current main-checkout COSMIC status before another provider: Int32 mixed HTTP accepted, replacement image/handoff pending; no peer import or rerun in this batch. No DNA, worker, load-balancing toggle, deployment or shared Atlas/PIPER action. No installs/downloads/local build outputs/temp result files; syntax read stdin. Existing dirty portal checkout preserved; exact files published through remote branch only. No full OPORTAL, provider-programme or HyperDrive reliability completion claim.

