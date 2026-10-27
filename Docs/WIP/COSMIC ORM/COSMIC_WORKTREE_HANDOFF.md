# COSMIC workspace handoff

Work/build/test root: **C:/Source/OASIS-cosmic**. Always pass this explicit working directory to command tools. The desktop app cannot attach this manually created Git worktree as a managed worktree; that does not change the required command directory. Do not edit or build in C:/Source/OASIS.

Parent and Core branches: `work/cosmic-cql`, pushed with upstream tracking. Other submodules remain at pinned commits and must receive a task branch before editing. All eight submodules initialized recursively using existing local object caches, with original GitHub remotes retained. Do not repeat setup or installations.

The user approved one long-lived worktree per concurrent agent. No routine messages about builds, leases or ownership to the Providers chat. Contact it only for an overlapping Core/Shared public API change. Fetch/merge Development daily and batch verified Development integration once or twice daily. Use `/m:2 /nr:false /p:UseSharedCompilation=false`.

## Preserved native state

`LocalState/COSMIC/` is ignored by Git and **must survive cleanup**. It contains the cold Mongo data snapshot, CQL SQLite file, journals and migration SHA256 manifest. It is not disposable test output and may contain synthetic fixture credentials; never commit its contents. Temporary migration staging was moved here, hash-verified and removed after becoming empty. Original files were preserved without deletion.

Reviewed acceptance modes now explicitly use `LocalState/COSMIC/mongo-data` and `LocalState/COSMIC/Neo4jCurrent` for persistent data/journals. Logs/TRX/build outputs remain in this worktree's disposable `TestResults`/bin/obj. Neo4j still uses the retained pinned WSL container; no second container/runtime has been installed. Service port conflicts must fail safely; do not restart another owner's service or send routine coordination messages.

## Current CQL work

Q23e and Q49a remain red. Core source capture/admission passes 3,851 tests and both targets. Native projected All exists on all three providers; focused gates passed SQLite47+9 compiler, Mongo49 and Neo18. The combined 168-check gate is not complete.

The reviewed gate verified six native fixture IDs per store and matching actors/versions without replaying creates. It then exposed `MongoCqlTranslator.Translate` not supporting a nested source relationship below Boolean logic. Unverified SourceRelationshipComposition feature enrollment was withdrawn from all three native profiles. Fix the actual source translation/protected-error owner before reenrollment; no client predicate fallback or green-feature hardening.

Pinned initial pending journal SHA256: `FE53AE55DF84AC35E5A7D0A6787CDC51BB7AC4B5CDFC4207164D38C4B300EA1F`. Original WEB4 journal SHA256: `892A8AF780ED445887ECBD6C191A7577B053872A13E5CF35FE592A56899894C5`. No cycle mutation, create replay, reset or fixture cleanup occurred. Continue through the reviewed mode only; it pins the initial state and skips creates. Failed TRX evidence is committed in Verification.

New checkout build assets have not been restored/built yet. Bootstrap only required projects from the existing global NuGet cache, record restores/outputs in the shared task ledger, and keep all generated outputs here. No old-checkout output copies or new runtime installs.
