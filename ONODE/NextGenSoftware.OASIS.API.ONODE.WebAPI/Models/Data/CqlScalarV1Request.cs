#nullable enable
using System;
using System.Text.Json.Serialization;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data;

/// <summary>Bounded scalar intent. Omitted Take counts the complete eligible source, not the row route's default page.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class CqlScalarV1Request
{
    public string? Provider { get; set; }
    public string Operation { get; set; } = "LongCount";
    public CqlScalarV1Selection? Selection { get; set; }
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


/// <summary>One bounded canonical field or typed metadata path for Min/Max.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class CqlScalarV1Selection
{
    public string? Field { get; set; }
    public string[]? MetadataPath { get; set; }
    public string? ValueType { get; set; }
    public bool? Nullable { get; set; }
}
