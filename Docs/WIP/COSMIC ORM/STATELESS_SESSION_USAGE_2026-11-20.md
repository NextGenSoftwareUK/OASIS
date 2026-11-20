# Stateless bulk sessions — functional breadth

Source: Core `a187a221505fe3c717dc1c415bb883db6ebffe93`, `Queries/CosmicStatelessSession.cs`. Native qualification passed on all three real stores in run44743889763 (exact source7cf97c6cce6d; artifact14631299627, SHA2564571aa4fed4fdf1ca13a54c094112613d0c3980cee16e477cf5070d7cf7ff1b0). This API is excluded from the current session/date release image, which pins accepted Core `7b04e2dc`.

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

No full EF/NHibernate parity or hosted acceptance is claimed here. Graph attachment/fix-up, mixed save/delete transactions, generated-key stateless inserts, query-based set update/delete and cross-call transactions remain separate open work.
