# Metadata encryption context

20 October 2026. Save-publication ownership is implemented locally; durable key context remains open.

Preparation encrypts metadata using the effective per-holon override or runtime configuration. It now snapshots those settings onto the detached save value. Publication carries that snapshot into the committed value before decryption, so a caller changing global or override settings while I/O is pending cannot change the decryption context of that acknowledgement. The caller's configuration object is not replaced by publication.

The settings snapshot uses an explicit field/list copy, not JSON serialization of key material. This avoids passing keys through configurable serializer hooks. Both Core targets build and full Core1,132/1,132 passes after this change (`encryption-snapshot-core-current.trx`). The earlier native711 checkpoint predates this producer refinement.

Core1,128/1,128 passes (`TestResults/CosmicMongoCurrent/encryption-context-expanded-current.trx`), including sync/async global and per-holon override mutation cases. The initial override cases reproduced the old global-only decrypt defect (`encryption-context-before.trx`). Both Core targets build.

SQLite native verification passes711/711, zero skipped (`TestResults/CosmicMongoCurrent/encryption-native-current.trx`). Two new sync/async V2 cases save a globally encrypted root with an independently encrypted child override, verify plaintext publication and clean tracking, and independently read ciphertext-at-rest from the provider. Pre-start producer/consumer hashes match: Core `C69047DD0B2937DF881CC67237A0EDC21210818A719160CE3FFBCAA3B0E26911`, Shared `B8FBFFB6E9587E7695DBA6BF642EAD0FCFAB043D25CB9071DA811B0F22363B90`. The consumer retains these copied assemblies independently of later producer rebuilds. Owned disposable database is absent after terminal completion. This certifies these save-publication routes, not generic encrypted reloads.

## Remaining durable contract

Failure-state verification: Core1,132/1,132 passes (`TestResults/CosmicMongoCurrent/encryption-failure-core-current.trx`). Four added cases verify wrong-key, malformed-ciphertext, invalid-decrypted-JSON and null-payload errors preserve the original ciphertext and existing metadata without publishing decrypted fields. This required no further producer change and does not establish ciphertext authenticity or durable key-resolution support.

The process-local settings snapshot is not stored key lineage. Generic future loads currently depend on runtime settings or an explicitly supplied override; the stored ciphertext does not identify the per-record effective configuration. Do not treat successful acknowledged-save decryption as proof of independent generic-load support after key changes.

Implementation must introduce a versioned, data-only encryption context containing a stable key reference and algorithm/format identifiers, with authorized key resolution. Key material must not be stored with records or emitted in error/evidence logs. Context must survive provider DTO mapping, native commits, history, receipt replay, replication and migrations. Authentication must bind the relevant record/tenant context rather than trust arbitrary stored key references. Unsupported or unauthorized contexts must return explicit errors before caller publication.

Acceptance remains open for independent process reloads, rotated/retired/unavailable keys, mixed child contexts, history, replication, migrations, tampered contexts/ciphertexts and authorization failures on all certified providers. Key-reference admission and callback/service ownership need implementation before claiming full encryption-context support.
