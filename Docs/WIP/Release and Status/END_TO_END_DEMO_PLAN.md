# End-to-end OASIS demo plan

The goal David set on 2026-11-26: one full end-to-end demo of everything OASIS has been building for 15 years, centred on OGEngine and a quest line that spans several games.

## The demo story

1. **STARNET:** create quests, a cross-game quest line, GeoNFTs and Smart Bricks.
2. **ODOOM, OQUAKE and Our World:** see and play those quests and GeoNFTs in each game, through OGEngine, with progress carrying across games.
3. **OPORTAL:** see the avatar's progress, karma, inventory and NFTs live, on desktop and mobile.
4. **OASIS Omniverse Hub/HUD:** move between the games and see shared state.
5. **WEB4 sandbox:** play with HyperDrive (take a provider offline and watch fail-over) and CQL (JSON, SQL-like and plain-English queries across providers).
6. **WEB6 sandbox:** the AI layer (agents, holonic memory, free-text queries).
7. **HoloNET sandbox:** Holochain through HoloNET.

## Verification checkpoint (28 Nov 2026)

HyperDrive matrix:121/163 storage-role rows have recorded26sync+26async native baselines;238tracked provider projects include75non-storage and legacy variants. This is not all-provider hosted/current-graph certification. Global v2 gates remain open. See `../HyperDrive-Providers/CURRENT_STATUS.md`.

OPORTAL: existing dev/checkout browser runs passed203/203 executed checks per target;80signed-in checks were skipped without CI account credentials. Representative isolated real-backend popup journeys have separate evidence; Social Graph/Activity Feed continuation is in progress. Whole-portal and paid subscription acceptance remain open.

## Historical workstream checkpoint (26 Nov 2026)

| Workstream | State | Lead |
|---|---|---|
| OPORTAL | polished and working on desktop and mobile; PWA and store builds done (signing needs David's keys) | done; keep fixing what the demo finds |
| STARNET (store, quests, GeoNFTs, Smart Bricks) | store live and polished; OPORTAL-standard pass under way | STARNET session |
| COSMIC ORM / CQL | parity 130/164; SQL-like and free-text API endpoints in progress (phase 2a) | COSMIC + CQL sessions |
| HyperDrive and providers | about 115/234 providers pass native storage; v2 release gates next | HyperDrive sessions |
| WEB4 sandbox | 107 sources live on dev | CQL sandbox session |
| WEB6 sandbox | to be made fully working end to end | to start |
| HoloNET sandbox | to be made fully working end to end | to start |
| Our World (Unity) | working, with demo videos already made (per David) | keep working through the demo script |
| OASIS Omniverse Hub/HUD | working, with demo videos already made (per David) | keep working through the demo script |
| ODOOM / OQUAKE via OGEngine | working, quests and GeoNFTs in-game, with demo videos already made (per David) | the cross-game quest line for the demo |

## Order of work

1. Finish what's close: COSMIC 164/164 and the SQL-like/free-text endpoints; the STARNET store pass; the HyperDrive release gates.
2. The quest pipeline already works end to end (STARNET → OGEngine → ODOOM, OQUAKE and Our World → OPORTAL), with videos. Re-check it against the latest Development after the COSMIC, HyperDrive and OPORTAL changes, then write the cross-game quest line for the demo.
3. Re-check Our World and the Omniverse Hub/HUD against the latest Development.
4. The WEB6 and HoloNET sandboxes working end to end, alongside the WEB4 sandbox.
5. A scripted demo run-through, rehearsed on dev, then the cross-game quest line written for it.

## How the work is run

Claude (Tech Lead plus Sonnet dev sessions) and GPT agents share this plan. Claim items on the `claims` branch, push to Development in 30-minute batches, take the recommended option on decisions, report progress as done/total, and log only secrets, accounts or payments in ACTIONS-FOR-DAVID.md.
