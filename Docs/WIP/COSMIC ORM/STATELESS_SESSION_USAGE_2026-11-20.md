20 November — GENERATED/ASSIGNED CREATE-ONLY STATELESS INSERT NATIVE ACCEPTED.44756910311/job136358931472 SUCCESS on exactd25053a7a23ff4a00a3f763df0ef52cb351d0283/Core d44f2bfe56c6c7da215857b90c92a680c61daede. Strict RAM-only3110byte artifact14640810588 SHA256f6b402222c0ad62a2da81f7ff74069e21e697178e37ec973fb830804ddaa1cc8 verifies EXACT1TRX+3JSON,3passed/0failed/0skipped,8timings/3successful holon writes/3owned cleanup IDs/8required flags per real MongoDB/SQLite/Neo4j store. Two empty-ID roots acquire distinct acknowledged IDs/revisions; denied attachment to a real permissioned foreign parent refuses BOTH roots, publishes no IDs and writes no matching records; existing assigned-ID insertion conflicts without upsert/data mutation. Exact cleanup passed. Previous managed4/stateless3 accepted cases NOT rerun.

Stateless functional breadth now includes assigned or generated create-only bulk inserts, explicit-revision update/save/delete batches, fresh native reads without context identity reuse, stale/foreign whole-batch refusal and no implicit flush. Full EF/NHibernate row certification remains separate from these scoped capabilities; no graph/mixed-transaction/full programme claim. Existing S19 explanation updated to actual native evidence rather than saying no API exists; cells/counts remain unchanged pending complete row semantics. Tiny CI samples, not production benchmarks: generated bulk Mongo275/SQLite344/Neo708ms.

Next scoped image is being prepared from current Development6498c9fb: ONLY Core d44 + paired manifest, one new Core source file relative to deployed7b04. Actual Development session/date source6498 remains successful/metadata-ready; its1108byte handoff journal is removed and readiness documented0ecab437. Task branch5d0daf18 integrated that Development ancestry without runtime changes, preserving both progress histories and current owner actions. Latest peer14:59: Portal/STAR Holon browser/diff actual WEB5 build44755467900 passed; no session API overlap or unverified peer import.

CLEANUP: native ZIP/TRX/JSON inspected in RAM,0local artifact files; source-only temporary index removed in finally. No local build/runtime/env/database/image/archive, shared Atlas/PIPER data writes, DNA/configuration changes or staging/production action. Compact source proofs/usage guide retained deliberately. Vercel team and permanent Free inference owner actions remain pending without another sign-in request. Continue owned-graph/relationship family using actual native graph owners, not a flattened substitute.

20 November — CREATE-ONLY STATELESS INSERT SOURCE Cored44f2bfe56c6c7da215857b90c92a680c61daede published; ONLY3new native journeys prepared (acceptance pending). InsertAsync accepts new roots with generated/assigned IDs, binds assigned roots to explicit null create-only conditions, and delegates allocation/acknowledged publication to the existing owned write owner. It never upgrades an insert to update and adds no tracking/cache/retry. New journeys:2empty-ID roots committed/read; foreign parent has actual explicit empty permissions; whole mixed attachment batch refused with COSMIC_ADD_CHILDREN_FORBIDDEN/IDs remain empty/no rows written; assigned existing-ID insert conflicts and stored values survive.8timings/3successful writes/3cleanup IDs per store. Prior managed4/stateless3 accepted cohorts NOT selected again. One reused isolated CI fixture; no laptop build/runtime/database/results/archive or shared Atlas/PIPER writes. Native/full row/live acceptance remain pending; source not swept into deployed Core7b04.

Development ancestry5d0daf18 resolved and published with runtime unchanged; allunique progress history and current owner actions preserved. Actual Development6498c9fb/Core7b04 ready, recorded0ecab437, owned1108byte handoff journal removed/absence verified; no duplicate rollout or DNA/configuration change. Owned graph/relationship family remains next; full roadmap intact.

# Stateless bulk sessions — functional breadth

Source: Core `d44f2bfe56c6c7da215857b90c92a680c61daede`, `Queries/CosmicStatelessSession.cs`. Native qualification passed on all three real stores in run44743889763 (exact source7cf97c6cce6d; artifact14631299627, SHA2564571aa4fed4fdf1ca13a54c094112613d0c3980cee16e477cf5070d7cf7ff1b0). This API is excluded from the current session/date release image, which pins accepted Core `7b04e2dc`.

Use `CosmicStatelessSession.Create(manager, nativeProvider, readAccess)` with the application's existing HolonManager, real native provider and admitted actor policy. Check its OASISResult before using Result. The session borrows those services; it does not open or dispose their connections.

| Behaviour | Stateful CosmicSession | CosmicStatelessSession |
|---|---|---|
| Root identity map | Retains tracked root instances | None; reads return fresh instances |
| Pending changes | Add/Attach/Remove and SaveChanges | None; SaveAsync/DeleteAsync write only submitted roots |
| Revision ownership | Explicit conditions or accepted per-entity baselines | Explicit condition for every submitted root is required |
| Native batch scope | Pending homogeneous save or delete batch | Explicit homogeneous save or delete batch |
| Mixed create/update | Supported in a save batch | Supported in a save batch |
| Mixed save/delete | Not implemented | Not implemented |
| Transaction across calls | Not implemented | Not implemented |

## Explicit write conditions

Every selected root must have a nonempty assigned ID. Supply exactly one dictionary entry per root. A null revision means create-only; an existing provider revision means update/delete only if that exact revision is still current. Guid.Empty is a legitimate legacy revision, not a missing revision.

```csharp
public static Task<OASISResult<IEnumerable<IHolon>>> WriteOneAsync(
    CosmicStatelessSession session, IHolon holon,
    Guid operationId, Guid? expectedProviderRevision)
{
    var options = new CosmicWriteOptions(operationId,
        new Dictionary<Guid, Guid?> {
            [holon.Id] = expectedProviderRevision
        });
    return session.SaveAsync(new[] { holon }, options);
}
```

The example uses the Core Queries/Interfaces and Common namespaces plus System collections/tasks. For a new root, set the normal owner/active/new-holon fields and pass null as its create-only condition. For an existing root, retain the provider revision returned by its authorized native read (`CosmicConcurrency.GetProviderRevisionId(root, session.Authority)`). Stop if the required existing revision is missing; never refresh it silently to make an old write succeed.

For multiple roots, submit one explicit selection and a matching condition dictionary. The existing owned native batch enforces permissions, revisions, receipts, replication enrollment and acknowledgements. A stale or unauthorized root refuses the whole batch. A normal unchanged save may be a reported no-op; inspect IsError, IsSaved and the returned outcome.

Call DeleteAsync with explicit existing revisions and an explicit softDelete choice. Children are not implicitly selected. Save and delete remain separate operation types; this is not a cross-call transaction.

## Fresh reads and operation ownership

Build an actor-bound query with Query<T>(), check its OASISResult and compose supported CQL expressions. Pass the admitted query to ReadAsync. maximumResults is bounded query execution, not permission to return a partial collection as complete.

Reads do not register entities or flush local edits. Changing an earlier result has no effect unless that root is explicitly submitted to a later write. Repeating a read does not reuse an identity-map entry. A query from another actor or authority is refused.

One owned operation runs at a time per stateless session. Cancellation before write dispatch refuses the operation. After dispatch, the API awaits the definite owned outcome; it does not invent a retry after an uncertain commit. Retain the logical operation identity and use the existing explicit recovery contract when necessary.

## Verification and remaining scope

Three new real MongoDB/SQLite/Neo4j journeys passed: assigned-key bulk create, fresh identity and unsubmitted local edits, explicit revision requirement, mixed create/update, stale whole-batch refusal, actor isolation, whole-batch deletion authorization, hard deletion/native absence and exact cleanup. They do not rerun the accepted four stateful session cases.

No full EF/NHibernate parity or hosted acceptance is claimed here. Graph attachment/fix-up, mixed save/delete transactions, query-based set update/delete and cross-call transactions remain separate open work.

## Create-only insertion with generated identities (native verified)

Core d44f2bfe56c6c7da215857b90c92a680c61daede adds InsertAsync(roots, options). Pass an explicit stable operation ID. New empty IDs are allocated privately by the existing native plan and published only on a valid commit acknowledgement; assigned IDs receive create-only null conditions. Existing revisions/non-new roots cannot be inserted, and an assigned-ID collision cannot become an update. Parent attachment permissions still apply to the whole batch. No automatic retry or premature client ID assignment.

The existing assigned-key Save/Delete/Read cohort remains verified on Corea187; the insertion entry point passed its separate three-store qualification44756910311, artifact14640810588/SHA256f6b402222c0ad62a2da81f7ff74069e21e697178e37ec973fb830804ddaa1cc8. This verified SDK source is still excluded from the currently deployed Core7b04 and awaits its scoped image/package gates.
