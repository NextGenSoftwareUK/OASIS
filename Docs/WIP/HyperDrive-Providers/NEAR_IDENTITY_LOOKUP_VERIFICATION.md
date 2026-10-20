# NEAR profile identity lookup verification

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

Native19 has now completed successfully:19 passed, zero failed/skipped,4m20s; existing runner exited0. Current report `TestResults/NearRuntime/near-provider-current.trx` SHA-256 `2D2F8DB793A3C900653360BFE742A43352872E04462FABFE1D2A33AF2B1FA9E3`. This includes actual encrypted duplicate-avatar username/email rejection and GUID-scoped cleanup, followed by restored unique lookup. Historical profile-version cases also pass. After exit, no near-sandbox process or root pending files were present and the checked SDK pending file was absent. Retention was restored50→5 only after those checks. Final free C:72,475,512,832bytes. Duplicate avatar-detail native coverage remains separate/unproven; controlled selector tests cover it. This is not hosted consensus, full-interface or V2 multi-provider acceptance.

Inspect write journaling and record cleanup before adding duplicate avatar/detail fixtures. Reuse the pinned NEAR2.6.2 node and existing ledger, contract, signer and encryption key. Preserve historical profile-version cases. Temporary retention50 may be used only with the stopped node and must return to5 after clean shutdown. Do not rerun while original pending mutations remain. Provider-wide interfaces, hosted acceptance and cross-provider V2 certification remain open.
