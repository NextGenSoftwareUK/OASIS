#nullable enable
using System;
using System.Text.Json.Serialization;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data;

/// <summary>Bounded grouped intent. Source paging precedes reduction; group budgets never truncate inputs.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class CqlGroupedV1Request
{
    public string? Provider { get; set; }
    public CqlProjectionV1Column[] Keys { get; set; } = Array.Empty<CqlProjectionV1Column>();
    public CqlGroupedV1Column[] Columns { get; set; } = Array.Empty<CqlGroupedV1Column>();
    /// <summary>Selected output aliases, applied after grouping. Source OrderBy remains independent.</summary>
    public CqlGroupedV1Ordering[] ResultOrder { get; set; } = Array.Empty<CqlGroupedV1Ordering>();
    public int MaximumGroups { get; set; } = 25;
    public CqlV1Filter[] Filters { get; set; } = Array.Empty<CqlV1Filter>();
    public CqlV1Filter[] AnyOf { get; set; } = Array.Empty<CqlV1Filter>();
    public CqlV1ChildExists[] AnyChildren { get; set; } = Array.Empty<CqlV1ChildExists>();
    public CqlV1DescendantExists[] AnyDescendants { get; set; } = Array.Empty<CqlV1DescendantExists>();
    public string OrderBy { get; set; } = "Name";
    public bool Descending { get; set; }
    public int Skip { get; set; }
    public int? Take { get; set; }
    public CqlV1Graph? Graph { get; set; }
}


[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class CqlGroupedV1Column
{
    public string? Alias { get; set; }
    public string? KeyAlias { get; set; }
    public string? Operation { get; set; }
    public CqlScalarV1Selection? Selection { get; set; }
}


[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class CqlGroupedV1Ordering
{
    public string? Alias { get; set; }
    public bool Descending { get; set; }
}
