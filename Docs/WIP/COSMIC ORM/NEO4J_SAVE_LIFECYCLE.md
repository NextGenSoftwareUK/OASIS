# Neo4j save lifecycle audit

20 October 2026. Source audit identifies that conditional primary saves passed expected-revision checks and immediately stamped history/revisions without validating whether a stored tombstone was cleared or replaced. Ordinary holon snapshots likewise lacked this invariant regardless of history policy. This is separate from the completed selected committed-delta adoption.

The intended Save invariant is that a committed tombstone's deletion timestamp/actor and deleted/inactive state remain intact. A normal Save is not an implicit restore. Compatible tombstone content edits and live-record edits remain valid. Explicit authorized restore remains a separate roadmap requirement.

Implementation uses one provider lifecycle validator at locked authoritative observations: all selected conditional rows are checked before any history stamp; ordinary graph rows use the existing membership lock observations before history/native writes. Lifecycle failures return `OASIS_LIFECYCLE_CONFLICT`. Replicated committed lineage remains under its own protocol.

Sixteen controlled cases cover history on/off and active resurrection, clearing deletion while inactive, changing the original timestamp or deletion actor, compatible tombstone edits and ordinary live edits, plus sync/async ordinary save rejection at its locked observation. Against the hash-bound original Neo producer `B3B8239D89B5EDFB6FF1A3BB989D561E157232C6B7EB06871F3FEF4267AC2F3E`, the original16-case gate has12 failures and4 controls (`TestResults/Neo4jCurrent/neo-save-lifecycle-red.trx`). After the fix, full Neo controlled190/190 passes, zero skips (`neo-save-lifecycle-controls-full.trx`). Selected native acceptance passes below; wider avatar/detail/provider coverage remains open.

Native conditional acceptance also checks direct conditional and ordinary resurrection attempts against the committed tombstone, unchanged stored bytes and absence of a failed operation receipt/delta before proceeding to hard deletion. The strengthened native conditional13/13 passes, zero skips. Native ordinary outcome16/16 and paired Shared917/917 also pass, zero skips.

## Acknowledgement ownership

Two original receipt-cleanup cases also fail because Neo4j returned successful original receipt acknowledgements with `HasAnyHolonsChanged=false` (`neo-ack-result-red.trx`). Neo4j now delegates graph/count/new-state/changed-state assembly to Shared `ConditionalHolonAcknowledgement.Create`, retaining its own commit/receipt proof, operation identity and delta metadata. No duplicate graph/count algorithm is retained. Both cases pass in the full190 controlled suite; source outbox and replica protocols remain unchanged.

This increment covers holon saves, not avatar/detail saves or an explicit restore API. Wider provider lifecycle parity, authorization and all remaining roadmap requirements remain open.

## Final verification boundary

The owning batch terminates0 at parent `717a14d85`, with unchanged Core `aa209f94418d5f11280345ceebd411c912717858`. `TestResults/Neo4jCurrent/neo-save-lifecycle-final-artifacts.json` verifies identical Core/Shared across Neo controlled/native and Shared consumers, matching Neo controlled/native producer, stopped retained runtime and free disk65.5GB. Core SHA256 `7B8680F86B797D84CAE0120D167F0BFA36E72B2BC4B7E9EFB22A379DC17620CC`; Shared `3971413F773A95498DCA80296CFCAB5010EC67C2D54C04BC32CAF910DE1D6A99`; Neo `A069BEE4BAF13A7A0ECEA98372EA3BACF2D7672EF01CB0E94F3EC99278991D31`.

Reports: `neo-save-lifecycle-controls-full.trx`, `neo4j-cosmic-conditional-current.trx`, `neo4j-cosmic-ordinary-outcomes-current.trx` under `TestResults/Neo4jCurrent`, plus `TestResults/SharedCurrent/shared-neo-lifecycle-paired.trx`. No failed native fixture required cleanup. Existing environments are reused; no Core source/pin change, dependency download or Development/master deployment is included.
