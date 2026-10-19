# EVM record history: current gap and reuse map

## Checked-write preparation boundary — implementation started

2026-10-19: Web3CoreOASIS now has internal UpsertPreparedAsync for avatar/detail/holon. It reuses the existing native read, freezes external identity, computes native presence/hash BEFORE invoking preparation, passes preparation a detached copy of that SAME record, then submits one existing checked write. Existing ordinary upserts delegate to the same owner and freeze proposed payload bytes before awaiting the read. No extra read, retry, alternate mutation path or automatic contract deployment. A callback's edits cannot change the already captured expected hash. Returned preparation bytes are copied before submission.

This is a reusable history prerequisite, NOT staged versions/encryption/archive integration or public historical reads. Native tests extend existing three-kind checked-upsert verification with successful detached preparation and an interleaving native update during preparation: stale prepared write must fail and preserve the actual newer bytes. The native entry point calls VerifyCheckedUpsertsAsync; these are executed assertions, not unused test code. Earlier audit statement about no history changes below applies to the original inventory, not this subsequent prerequisite.

Final scoped verification: producer0errors/4warnings; controlled115/115pass713ms (SHA256 `14F5D5C4770CAA181189A448AABC061880C16E82AA54687B13AEE5803C75DF99`), Ethereum native192/192pass54s (`C28FA3398EC2C0752A1084EC4C65CD98AEF0A2FEFBBD7BCA292CB58746419DCC`), independent page59checks pass, Web3Core native4/4pass84s (`9E7BB36D86E11A5C1430EC0BA2BB78000C93E5EFC3BF34A422FC86529F936185`), zero skips and runner terminal0. Same retained local chain31337/runtime/cache/lease, no downloads/install/new environment or public gas. Owned18545/18546listeners absent afterward. Free bytes82,433,343,488before/82,431,438,848after; no unrelated cleanup/reclaim attribution. Manifest/gitlink/whitespace checks pass; fetched Development0/0 before scoped integration. Full history lifecycle/read/config/fee-growth/hosted/V2 gates remain open; separate Solana/Firestore changes excluded.

2026-10-19 source audit. Not implemented/completed by this report. Native status/CRUD regression passes do not prove record version history.

## Actual active implementations

| Provider | Current owner | History gap |
| --- | --- | --- |
| Ethereum | EthereumOASIS, NextGenSoftwareOASIS contract protocol3 | Positive record/collection/provider-key versions explicitly unsupported; existing CreateAvatar/CreateAvatarDetail/CreateHolon upserts do not require an observed ciphertext hash. |
| Arbitrum V2 | EvmProviderFactory selects ArbitrumOASIS_Web3Core | Shared Web3Core history gap; do not implement this in the legacy-only ArbitrumOASIS class. |
| Polygon | Web3CoreOASISBaseProvider | Same shared history gap. |
| Web3Core | Web3CoreOASISBaseProvider/Web3CoreOASIS | IsVersionControlEnabled exists, but positive collection reads still reject history. Wallet transaction history is a different feature. |

Authority: `OASIS Architecture/NextGenSoftware.OASIS.OASISBootLoader/EvmProviderFactory.cs`; provider declarations; Ethereum history rejections in AvatarLoadExport/IdentityQueries/LoadHolonSave/SaveAvatarLoad; Web3Core CollectionReads/CredentialSelectors. Legacy HyperDrive and legacy-only Arbitrum gaps remain documentation-only under the task's scope policy.

## Existing reusable components

- EmbeddedRecordHistory already owns detached version staging, complete archive validation and exact version selection. Reuse it rather than another EVM version algorithm.
- Web3Core's contract already exposes upsertAvatarChecked/upsertAvatarDetailChecked/upsertHolonChecked. Web3CoreOASIS.UpsertObservedAsync reads original native EntityOASIS, derives presence and Keccak ciphertext hash, and sends the checked upsert. Use this **same observed record** for history staging and condition binding; a second independent provider pre-read would permit archive loss across concurrent updates.
- Existing private encryption, persisted keys, signed preparation/outbox, nonce gate, receipt/reconciliation and graph publication remain the owners of their responsibilities. Do not build a second submit/finality/retry path.
- Ethereum protocol3's ordinary create/update ABI lacks this checked-write input. Its own contract/client binding requires reviewed changes before claiming equivalent conditional history. A process-local semaphore does not provide native cross-process concurrency safety.

## Implementation/verification sequence

1. Add a same-snapshot preparation hook/result at the existing checked Web3Core writer boundary. Detached decrypted canonical prior feeds shared history; staged encrypted replacement uses that exact native prior ciphertext condition. Validate identity/kind/archive, freeze option before awaits, retain archive even when new-history creation is disabled. Publish caller version/linkage only after existing verified receipt.
2. Integrate ordinary Avatar/Detail/Holon saves, graph nodes and atomic lifecycle stages. Preserve provider-owned children independently from selected root version; credentials/authentication always latest. Do not open positive public reads until native writer/lifecycle acceptance passes.
3. Bind existing DNA/factory version option explicitly and test true/false. Current property presence is not evidence the feature works. Apply canonical read selection consistently to ID/provider key/collections/metadata/search/export; no latest-as-history fallback.
4. Prove real checked contract conflicts, original-response loss, archive retention, exact history, current-child selection, encrypted bytes, soft/hard lifecycle and failure/post-ACK publication using retained local chain and compact pending journals. Cover all active entry points. Separately upgrade Ethereum native ABI/protocol capability with truthful activation compatibility checks; no automatic remote deployment or public gas spending.

Outstanding design/acceptance constraints include on-chain payload/gas growth and bounded archive policy, full canonical/private metadata query behavior, contract capacity, retention/security, L2 execution versus settlement, hosted API/DNA configuration and actual V2 routing. Do not enable silently, trim history, retry uncertain writes, or certify every interface from existing local CRUD/status tests. Source changes for this history feature have not yet been made; no native mutation/download/new environment was needed for this audit.
