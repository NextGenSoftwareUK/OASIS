# NEAR profile identity lookup verification

## Export identity version separation — 23 October 2026

Email/username exports now resolve current avatar identity independently of the requested holon Version. Their shared selector owner rejects negative Version before any identity dispatch and preserves the full existing identity-error envelope. GUID owner exports reject empty ID/negative Version before SDK activation and set IsLoaded/LoadedCount only after complete owner filtering. Explicit profile-history lookups are unchanged; no SDK/storage/encryption/ownership-rule rewrite or new retry.

New20 controlled cases:12 selector baseline8fail/4pass; final full240 unit cases PASS0skip13s. Four actual owner-filter cases reuse the existing Node IPC fixture and encrypted C# entries reader, count two matching roots versus zero, and exclude a foreign owner. They are NOT near-api-js/native-chain proof. The other16 cover sync/async email/username routing, error envelope and preactivation admission. Prepared native assertions in the existing version fixture compile (avatar2/current identity exporting root1 and retired-alias rejection), but have NOT run. Exact source/copy/PDB and report evidence in ENVIRONMENT_INVENTORY.md.

Latest retained native report is the interrupted21October cohort:19total/15pass/4fail, SHA AA17152C617B8C49D9C52E2EE7D87FE11EF43C868E6A1C52A0463955D4690EF3, not the older19/19 success below. Fresh read-only recovery inventory matches prior1156rows/hash05707c0e2a930f9c3f98e0f08dbb34512f3c0d495634077daa49c41ab09eeaa2; original report/log/chain/code/key retained, node stopped, config retention5 unchanged. This proves unchanged readable encrypted records, NOT original ordinary-write closure. Full native rerun remains gated by that original recovery review. No fresh native mutation, ledger reset/replay, public funds or deployment. All relevant interfaces/fees/full V2 remain OPEN.

## Current implementation and limits

The reusable `StoredRecordIdentityLookup` selects a unique username/email match from the provider-owned versioned collection. NEAR's four avatar/avatar-detail selectors and SOLID's private profile selector use this shared implementation. Duplicate identities fail with `AmbiguousIdentity` and no selected payload; missing identities return `RecordNotFound`. Matching trims whitespace and uses ordinal case-insensitive comparison. Collection failures retain their original cause/code; malformed collections fail rather than silently selecting a record.

This is profile lookup, not credential authentication. Positive profile versions are forwarded unchanged; the separate latest-only credential policy remains unchanged. Deleted-record visibility remains the collection owner's responsibility. Collection scans do not establish transactional uniqueness or scalable indexed lookup.

## Controlled evidence

Completed suites, zero skips:

| Suite | Passed | Current TRX SHA-256 |
| --- | ---: | --- |
| NEAR | 160 | `494181F7F5EC7EFB6E4B72233CF401F6D9CCC86EF7A1D64CCF5A414920B6954F` |
| Shared | 732 | `C7B1240392192238050241B4E1BA662BE7CAB31FF0B3FD8CEA0D447ED07BDDCB` |
| SOLID | 234 | `69AE08C75F117266162C10D4246F2E22446CE71FB012605D7A4E4F202D5801A5` |

Reports: `TestResults/NearRuntime/near-identity-current.trx`, `TestResults/NearRuntime/shared-identity-current.trx`, `TestResults/SolidCurrent/solid-identity-current.trx`. These are controlled checks, not proof of current native encrypted duplicate-identity behavior. Retained NEAR native19 and SOLID private DPoP native acceptance must be refreshed before release. Shared suites include concurrent unpublished COSMIC work and are not an immutable release certificate.

## Retained encryption-key safety

`Scripts/TestHosts/near-local/run-dotnet-evidence.ps1` now requires the original DPAPI-protected key at `%LOCALAPPDATA%/OASIS/provider-evidence/near/storage-key.dpapi`. It no longer creates a replacement when the key is absent: the retained ledger may contain records encrypted with that original key. Recovery, not replacement, is required. No key material was printed or changed.

PowerShell AST parsing passed after the change, and a read-only existence check confirmed the retained original key. `Scripts/TestHosts/Test-NearRetainedKeyGuard.ps1` then executed the actual guard AST with existing and guaranteed-absent paths: both checks passed, without key access, file creation or node startup. Available C: space was 72,627,912,704 bytes; the requested 123GB target is not met.

The native19 fixture now adds an acknowledged distinct avatar with the same username/email, requires both selectors to fail with `AmbiguousIdentity`/no payload/not loaded, deletes only that duplicate by GUID, verifies absence, and requires unique lookup to recover. Consumer compilation passed with zero errors/two warnings. The retained node was confirmed stopped with no original pending files before temporarily changing retention5 to50. The existing reusable runner has resumed it on loopback for native19; results are pending. Restore retention5 after verified terminal shutdown. No download, new ledger, signer or key was created.

## Next native gate

**Soft-delete/history native gate PASS:** same retained native19 completed19/19,zero failures/skips,4m45s,runner exit0. Current TRX SHA256 `6CF7C7CA0997858B58EDD07756BB9920BDBF1301EC0F20CED9ECBBFB9EE03C2E`. All newly added lifecycle assertions passed, including explicit historical version1 before hard deletion and its absence afterward. Existing profile/detail ambiguity and checked-write conflict cases remain passing; completed journal verified-fixtures-absent/2detail keys/10receipts, pending files absent and node stopped. Retention restored50→5 after shutdown. C:free72,386,093,056bytes; no download/install/reset. This tests avatar tombstone/history visibility, not automatic cascade deletion of independently stored avatar-detail records, every interface or hosted/V2 behavior.

Soft-delete/history follow-up: source inspection found both point reads and collection reads already suppress selected tombstones. Existing native lifecycle now additionally enables history, verifies current point/collection/username/email exclusion after soft deletion, allows explicit version1 historical username lookup, then requires hard deletion to remove version1. Consumer builds0errors2warnings; same retained native19 runner now executing. Previous detail proof moved in-place to `TestResults/NearRuntime/near-detail-identity-B183851E53D5.trx` after exact hash validation. No production behavior was changed for this test addition. Temporary retention50 must be restored5 after terminal shutdown. Do not count this pending run as passing.

**Avatar-detail native gate PASS:** extended native19 completed19/19, zero failures/skips,4m32s, runner exit0. Current TRX SHA256 `B183851E53D5896A169D96FE8237B998E67EBBD86FF3528FFDE2EB715C30CB69`. Completed checked-write journal is `verified-fixtures-absent`, includes both detail keys and10native receipts across existing holon conflict tests and added detail create/cleanup. Node is stopped, no pending journals remain; retention restored50→5 after checks. Consumer provider DLL SHA256 `28046DF1779FAD1079AA62946DDC3C4D0CB415861B756B6A1691DF37BBD782C2`, Shared `2ED11E8C86D8251761C5DBD580D9A65AC4C12363DFDB96D5CF51B0A3B3F6C52B`, Core `089E456DF569F02A450986852195FC14EA1AAA6B13C514A7F047057264F894E6`. Remaining full-interface/hosted/V2 acceptance is not inferred from this result. Final C:free72,380,719,104bytes.

Avatar-detail extension is now compiled (zero errors/two warnings) and running on the same retained native19 runner. The existing checked SDK fixture journals two exact `avatar-detail:<GUID>` keys before writes, verifies encrypted ciphertext/plaintext readback and native receipts, requires both public detail selectors to reject duplicates, then removes each exact owned key and verifies absence plus restored unique lookup. New journals retain `identityKeys`; prior completed journals that lack this field are the old fixture schema, and are still checked against their original holon/archive keys. All old checked-write/archive conflict tests remain. On failure the pending journal remains for same-node recovery rather than unconditional cleanup. This is native selector coverage through real checked encrypted SDK writes, not independent certification of every public detail-save option.

Previous avatar-profile report was moved in-place (not copied) to `TestResults/NearRuntime/near-profile-identity-2D2F8DB793A3.trx` after exact hash validation so the new fixed current report cannot overwrite that evidence. Node379614 resumed; temporary retention50 must return5 after terminal verified shutdown. No installation/download or ledger reset.

Native19 has now completed successfully:19 passed, zero failed/skipped,4m20s; existing runner exited0. Current report `TestResults/NearRuntime/near-provider-current.trx` SHA-256 `2D2F8DB793A3C900653360BFE742A43352872E04462FABFE1D2A33AF2B1FA9E3`. This includes actual encrypted duplicate-avatar username/email rejection and GUID-scoped cleanup, followed by restored unique lookup. Historical profile-version cases also pass. After exit, no near-sandbox process or root pending files were present and the checked SDK pending file was absent. Retention was restored50→5 only after those checks. Final free C:72,475,512,832bytes. Duplicate avatar-detail native coverage remains separate/unproven; controlled selector tests cover it. This is not hosted consensus, full-interface or V2 multi-provider acceptance.

Inspect write journaling and record cleanup before adding duplicate avatar/detail fixtures. Reuse the pinned NEAR2.6.2 node and existing ledger, contract, signer and encryption key. Preserve historical profile-version cases. Temporary retention50 may be used only with the stopped node and must return to5 after clean shutdown. Do not rerun while original pending mutations remain. Provider-wide interfaces, hosted acceptance and cross-provider V2 certification remain open.
