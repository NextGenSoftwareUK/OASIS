#nullable enable
using System;
using System.Text.Json.Serialization;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data;

/// <summary>Ordered native result stages after one protected, bounded projection source.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class CqlCompositionV1Request
{
    public string? Provider { get; set; }
    public CqlProjectionV1Request Source { get; set; } = new();
    public CqlCompositionV1Stage[] Stages { get; set; } = Array.Empty<CqlCompositionV1Stage>();
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class CqlCompositionV1Stage
{
    /// <summary>Exactly Where, OrderBy, Skip, Take, Select or Distinct. Array order is execution order.</summary>
    public string? Operation { get; set; }
    /// <summary>AND comparisons over exact selected aliases. Used only by Where.</summary>
    public CqlGroupedV1Filter[]? Filters { get; set; }
    /// <summary>Optional OR group, ANDed with Filters. Used only by Where.</summary>
    public CqlGroupedV1Filter[]? AnyOf { get; set; }
    /// <summary>First term starts OrderBy; remaining terms are ThenBy.</summary>
    public CqlGroupedV1Ordering[]? Order { get; set; }
    /// <summary>Used only by Skip or Take. Zero is meaningful.</summary>
    public int? Count { get; set; }
    /// <summary>Current source alias to final alias bindings. Used only by Select.</summary>
    public CqlCompositionV1Column[]? Columns { get; set; }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class CqlCompositionV1Column
{
    public string? Alias { get; set; }
    public string? SourceAlias { get; set; }
}
