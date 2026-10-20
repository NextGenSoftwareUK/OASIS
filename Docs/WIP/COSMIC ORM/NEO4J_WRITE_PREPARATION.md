# Neo4j durable preparation checkpoint

20 October 2026. This scoped increment passes **174/174 controlled tests and 9/9 native tests**, zero skips. Eight missing-capability cases failed before implementation. Core, Shared and Neo4j assembly hashes match between the final unit and native consumers. Broader roadmap acceptance remains open.

## Ownership

Core continues to own envelope authentication, exact logical comparison, graph/type binding, encryption and caller-state publication. Neo4j implements the existing `IHolonWritePreparationStorageProvider` interface and stores only opaque envelopes using the shared `ProtectedPreparationJson` codec.

The existing uniquely constrained `CosmicWriteReceipt` node owns both preparation and acknowledgement. An explicit transaction acquires that node's write lock before inspecting either state. First preparation claims atomically persist `PreparationActorId`, `PreparationFormat`, `PreparationJson` and `PreparationCreatedAtUtc`; they do not create holons or a successful write receipt. No additional fence label or uniqueness contract is introduced.

Conditional writes recognize the valid preparation-only state under that same lock and promote it to their existing transaction-local receipt claim. The acknowledgement preserves original preparation fields. A failed conditional transaction rolls back its promotion and entity mutations while retaining the previously committed preparation. Existing receipt replay keeps its original acknowledgement validation and rolls back its temporary lock writes.

An existing acknowledgement without its original preparation rejects with `COSMIC_PREPARATION_RECONCILIATION_REQUIRED`. The store does not fabricate a historical preparation from new randomized ciphertext. A different actor receives no envelope contents. Format and timestamp checks precede decoding. The shared codec retains ordered record identity, envelope and count/size validation.

No automatic transaction callback retry is used. Commit acknowledgement or rollback uncertainty produces a reconciliation marker with the logical operation ID; a native transaction ID is not invented. A known original winner survives later read-rollback or disposal failure as a warning. Cancellation is checked before admission, transaction work and commit dispatch.

## Evidence

- `TestResults/CosmicMongoCurrent/neo-preparation-admission-red.trx`: eight failures on the absent capability before implementation.
- `TestResults/CosmicMongoCurrent/neo-preparation-first-unit-full.trx`: 144/144 controlled cases after capability and transaction implementation.
- `TestResults/CosmicMongoCurrent/neo-preparation-transaction-unit-full.trx`: 160/160 controlled cases including sync/async first claim, immutable original winner, actor isolation, malformed format, receipt cutover, uncertain commit and known-winner cleanup failures.
- `TestResults/CosmicMongoCurrent/neo-preparation-paired-unit-full.trx`: final 174/174, including malformed JSON, rollback uncertainty, combined commit/disposal failure, known committed preparation cleanup and rejection of invalid preparation promotion before entity mutation.
- `TestResults/Neo4jCurrent/neo4j-cosmic-conditional-current.trx`: final 9/9. Three synchronized cases hold both actual SDK transactions before receipt-lock dispatch, then exercise competing preparation candidates and both preparation-versus-conditional-write commit orders. Native first-winner claims converge on one original envelope; a committed pre-journal receipt cannot acquire a later preparation. Earlier four- and six-case reports were hashed and retained in place.
- `TestResults/CosmicMongoCurrent/neo-preparation-paired-artifacts.json`: exact matching consumer hashes. Core `D4909562A46E3A44A1E9C88C6E09DC79D1235CFD7E36561F2503A70DCEE49C83`, Shared `DD9CA43FF5B659576BDC3892552ABE3927EF8D5260B4125D0FC57880CF88DC55`, Neo4j `12FE0D9C2FDD52C545E63B30F1743986CA372D068FC842D61FF0A50D9FF51152`.

Native tests extend the reusable conditional suite with actual encrypted graph saves through HolonManager and Hyperdrive V2, fresh manager/provider replay, write-key rotation, separate child context, exact decimal metadata, clean tracking and receipt cutover. Failed fixtures retain an explicit `CosmicPreparationFixture` marker and operation identity for independent reconciliation; they are not silently deleted to permit reruns.

## Remaining acceptance

The retained node stopped after verification; successful tests removed only their owned fixture records. Changed private intent, another actor and denied historical preparation keys reject without changing native snapshots or receipt/preparation properties. The earlier six-case native run used Shared `579822ED8178549F2FB98B048923BC601529D6768EB96ECF79C16DF1AF58FAFA`; the final paired cohort above supersedes it.

Actual process/network interruption, broader contention/rollback fault schedules, historical key/tenant admission, journal retention and operator reconciliation remain open. This increment does not implement CQL adapters, committed save deltas or the remaining migration/placement roadmap. Legacy Hyperdrive routing remains document-only. Mixed deployments must understand preparation-only operation nodes; older binaries may reject them rather than continue the operation.
