# Algorand durable storage implementation scope

The shipping Algorand provider reads account and transaction data through Algod and Indexer and submits signed transactions. Its native payment and public read evidence is recorded in the [provider matrix](PROVIDER_MATRIX.md). Durable OASIS records need an application with their own ledger state and a native acceptance fixture.

## Ledger contract

- Bind the store to the Algod genesis hash, application ID, verified approval program and expected writer address. Confirm the same identity before loading or publishing any record.
- Keep one manifest box per opaque record key. Derive the key from the store secret, record kind and OASIS ID. Encrypt payloads and any private metadata with the existing `EncryptedRecordCodec`, using the store and record identity as authenticated context.
- Stage larger encrypted payloads in bounded revision boxes. The owning application publishes a manifest only after verifying the authorized writer, the expected predecessor and all required chunks. Readers resolve the manifest and verify its payload digest and authenticated envelope. Unpublished chunks remain eligible for explicit cleanup by revision.
- Check the current manifest inside the application before creating, replacing or tombstoning it. Preserve published predecessor references for versioned reads. Treat an acknowledged hard delete as removal of the current and historical payloads in the application state, subject to the chain's public transaction history. Design the encrypted application-call arguments with that permanent history in mind.
- Report an unsupported record size or insufficient application funding before dispatch where the limit is knowable. Use the application account's native minimum-balance requirement and the transaction's current box reference and argument budgets.

The Algod box endpoint supplies the exact native key, value and confirmed round for independent readback. Application boxes are readable off chain. Every application transaction must keep private fields inside an authenticated envelope. [Algorand box storage](https://dev.algorand.co/concepts/smart-contracts/storage/box/) · [Algod box lookup](https://dev.algorand.co/reference/rest-api/algod/operations/getapplicationboxbyname/)

## Acceptance order

1. Use the existing authenticated LocalNet fixture to deploy and fund the exact contract. Verify program identity, authorized writes and a wrong-writer rejection.
2. Publish and read an encrypted holon through the shipping provider, independently verify the native box, and verify a conflicting revision fails without changing the head. Exercise chunk admission and a fresh client after process restart.
3. Qualify lookup, collection enumeration, version history and deletion through the shared storage contract before enrolling the provider in storage routing. Exercise avatar and avatar-detail capabilities with the same private-store semantics.
4. Preserve actual transaction receipts and native box digests as compact evidence. Release the fixture application and its boxes on the owned LocalNet. The funded public TestNet action already lives in [Actions for David](../Release%20and%20Status/ACTIONS-FOR-DAVID.md).

The first approval program at 97165221c603 implements creator-authorized, create-only chunk staging and publication of a digest-checked head with an exact predecessor. It admits up to seven chunks of 896 bytes per publication. The [LocalNet test](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/43738712271) passed native compilation, deployment, rejected writer and stale predecessor, missing or mismatched chunks, exact readback, and preservation of previous chunk data. The runner verified teardown. Shipping provider binding, encrypted serialization, durable historical manifests, deletion and routing verification remain open. Existing payment and public-chain evidence carries forward.

Revision c8bded270ce0 stores an immutable manifest per revision and replaces the current head in the same application call. Each manifest encodes the revision ID, predecessor revision ID, predecessor digest, and ordered chunk digests. The head and history consume two box references, leaving six chunk references per publish transaction. The [updated native test](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/43739225328) passed preservation of the previous manifest and rejection of a previous revision presented with the current head's digest. The runner confirmed teardown.

Provider revision 9513612cd4df adds explicit record application enrollment and `LoadStoredHolonAsync`. It verifies the ledger and application identity at activation, validates chunks and authenticated payloads, and follows confirmed predecessor manifests for historical reads. A [provider LocalNet test](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/43739866052) provisions two encrypted revisions and exercises the shipping provider. Generic write, deletion, discovery and routing work remains open.
