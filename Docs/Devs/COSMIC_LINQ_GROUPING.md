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
preserved within each complete matching collection. GroupJoin may select an
anonymous summary without publishing the matching collection; selection from
an anonymous carrier may also retain parent fields and matching summaries.
No predicate delegate is executed by Count, LongCount or Any.

## Explicit remaining limits

This is scoped grouped-query support, not complete EF/NHibernate parity.

- Materializing raw entity IGrouping collections is not implemented.
- GroupBy element/result-selector and custom-comparer overloads are not admitted.
- GroupJoin result shapes remain bounded anonymous carriers.
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
and [current delivery status](../WIP/COSMIC%20ORM/CURRENT_STATUS.md).
