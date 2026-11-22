# Native LINQ grouped queries

Use a registered COSMIC IQueryable backed by a shipping MongoDBOASIS,
SQLLiteDBOASIS or Neo4jOASIS provider. The existing manager, caller read scope,
capability admission and HyperDrive V2 query-attempt owner remain in force.
This surface translates grouping to the provider's existing native grouped
contract; it does not compile application selectors or group an unrestricted
client-side entity scan.

## Grouped summaries

    var summaries = source
        .Where(holon => holon.IsActive)
        .GroupBy(holon => holon.Description)
        .Select(group => new
        {
            Category = group.Key,
            Records = group.Count(),
            TotalRecords = group.LongCount(),
            LowestVersion = group.Min(holon => holon.Version),
            HighestVersion = group.Max(holon => holon.Version),
            VersionTotal = group.Sum(holon => holon.Version)
        })
        .Where(row => row.Records > 1)
        .OrderByDescending(row => row.Records)
        .ThenBy(row => row.Category)
        .Skip(0)
        .Take(10);

    var result = await summaries.ToCosmicListAsync(cancellationToken);
    // Handle result.IsError / result.ErrorCode before using result.Result.

Source filters and source windows run before grouping. Selected Where runs as
native grouped-result filtering (HAVING); subsequent ordering and pages apply
to the reduced groups. Complete bounded group proofs remain validated even
for groups outside the result page. A result limit never silently truncates
aggregate inputs.

Count, LongCount and Any on a selected grouped query count matching groups,
rather than source entities. Anonymous reselection is supported before HAVING,
ordering or paging. Scalar key domains, aggregate domains and resource limits
remain subject to the actual provider's admission contract.

## Element and result-selector overloads

Standard key-plus-element, key-plus-result and key-plus-element-plus-result
overloads use the same native grouped planner. An element may be an admitted
scalar path, the source entity identity, or a projection whose scalar fields
can be bound without invoking DTO getters. Aggregate selectors over transparent
anonymous elements are substituted back to their original source columns.

    var summary = source.GroupBy(
        entity => entity.Description,
        entity => entity.Version,
        (category, versions) => new
        {
            Category = category,
            Records = versions.LongCount(),
            VersionTotal = versions.Sum()
        })
        .Where(row => row.Records > 1)
        .OrderBy(row => row.Category)
        .Take(10);

    var result = await summary.ToCosmicListAsync(cancellationToken);

C# grouping comprehensions use this same path:

    var summary =
        from entity in source
        group entity.Version by entity.Description into versions
        select new { Category = versions.Key, Total = versions.Sum() };

The source window remains before grouping; HAVING, ordering and paging remain
after reduction. Element functions without native grouping admission and custom
comparers return explicit errors. Raw IGrouping member materialization is a
separate unfinished feature.

## Parent records and matching collections

    var parentsWithMatches = parents.GroupJoin(
        children,
        parent => parent.Description,
        child => child.Description,
        (parent, matches) => new
        {
            parent.Id,
            parent.Name,
            Matches = matches,
            Count = matches.Count(),
            Total = matches.LongCount(),
            HasMatches = matches.Any()
        });

    var result = await parentsWithMatches
        .Skip(0).Take(10).ToCosmicListAsync(cancellationToken);

Both inputs must be registered on the same native provider and manager.
Independent parents with identical keys remain separate. Empty matching
collections produce zero counts and false existence flags. Input ordering is
preserved within each complete matching collection. GroupJoin may select a
summary without publishing the matching collection; selection from an
anonymous carrier may also retain parent fields and matching summaries.
No predicate delegate is executed by Count, LongCount or Any.

## Typed results and direct selections

The same validated native pair stream can construct a public DTO or record,
apply public member initializers, or return a parent, matching collection or
scalar summary directly. Constructors and setters run only after the native
pair result has passed validation.

    public sealed record ParentSummary(Guid Id, string Name, int Matches);

    var typed = parents.GroupJoin(
        children,
        parent => parent.Id,
        child => child.ParentHolonId,
        (parent, matches) => new ParentSummary(
            parent.Id, parent.Name, matches.Count()));

    var counts = parents.GroupJoin(
        children,
        parent => parent.Id,
        child => child.ParentHolonId,
        (parent, matches) => matches.Count());

    var result = await typed.ToCosmicListAsync(cancellationToken);

Public member-initializer assignments may bind the same admitted parent fields,
matching collections and summaries. Further reselection or flattening through an
opaque DTO getter is refused: a constructor or setter does not prove that the
getter returns its original input. Use a transparent anonymous carrier when
composing another selection. Indexed or arbitrary executable selectors are not
invoked during query translation.

## Explicit remaining limits

This is scoped grouped-query support, not complete EF/NHibernate parity.

- Materializing raw entity IGrouping collections is not implemented.
- Custom grouping comparers and computed element functions need independent native admission.
- GroupJoin binds at most 32 admitted parent fields, collections or summaries. Nested executable expressions require further admission.
- Predicate aggregates and arbitrary computed aggregate expressions need native admission.
- LINQ integer Average returns double; the existing grouped integer Average contract
  returns decimal. The bridge refuses this mismatch. Already matching floating
  or decimal Average domains use their existing native admission.
- Reselection after HAVING is refused because dropping or reinterpreting its
  predicate would change results. Ordering/filtering after a grouped page and
  from-end grouped terminals require further native composition support.
- Opaque DTO getters and arbitrary application functions are not invoked to
  translate a query. Use explicit supported columns.

Representative native evidence and deployment status are recorded separately:
[GroupJoin acceptance](../WIP/COSMIC%20ORM/GROUPED_SUMMARY_NATIVE_ACCEPTANCE_2026-11-22.json),
[LINQ grouping acceptance](../WIP/COSMIC%20ORM/LINQ_GROUPBY_NATIVE_ACCEPTANCE_2026-11-22.json),
[typed GroupJoin acceptance](../WIP/COSMIC%20ORM/TYPED_GROUPJOIN_NATIVE_ACCEPTANCE_2026-11-22.json),
[grouping overload acceptance](../WIP/COSMIC%20ORM/GROUPBY_OVERLOADS_NATIVE_ACCEPTANCE_2026-11-22.json),
and [current delivery status](../WIP/COSMIC%20ORM/CURRENT_STATUS.md).
